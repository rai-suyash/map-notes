using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using Microsoft.Win32;
using TreeClasses;
using System.Windows.Media.Animation;
using System.IO;
using System.Diagnostics;
using a_level_project;
using System.Windows.Media;
using System.Windows;
using System.Data.SqlClient;
using System.Data;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;

namespace DatabaseHandler
{
    public class MenuListElementInfo
    {
        public long Id;
        public string Name;
        public string DateCreated;
        public string DateModified;

    }

    public class MindMapInfo
    {
        public long Id;
        public string Name;
        public string DateCreated;
        public string DateModified;
        public double RootNodeRadius;
        public double NodeSize;
    }

    public class NodeInfo
    {
        public long Id;
        public bool IsRoot;
        public string Text;
        public string Shape;
        public Brush FillColour;
        public Brush OutlineColour;
        public Brush FontColour;
        public Brush Highlight;
        public int FontSize;
        public int EdgeWeight;
        public FontFamily FontFamily;
        public bool Emboldened;
        public bool Underlined;
        public bool Italicised;
        public long? ParentNodeId;
        public long MindMapId;
    }

    class DatabaseModules
    {
        // Path to database
        public static string connectionString = @"Data Source=..\..\..\Databases\ProjectDatabase.db;Version=3;";

        public static void InitialiseDatabase()
        {
            if (File.Exists(@"..\..\..\Databases\ProjectDatabase.db")) { return; }

            // Make new database if none exists
            SQLiteConnection.CreateFile(@"..\..\..\Databases\ProjectDatabase.db");

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL command

                // Create mind_map table
                string createMindMapTableQuery = @"
                    CREATE TABLE IF NOT EXISTS mind_map (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        name TEXT NOT NULL,
                        root_node_radius REAL NOT NULL,
                        node_size REAL NOT NULL,
                        date_created TEXT NOT NULL,
                        date_modified TEXT NOT NULL
                );";

                // Create node table
                string createNodeTableQuery = @"
                    CREATE TABLE IF NOT EXISTS node (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        root INT NOT NULL,
                        text TEXT NOT NULL,
                        shape TEXT NOT NULL,
                        fill_colour TEXT NOT NULL,
                        font_colour TEXT NOT NULL,
                        outline_colour TEXT NOT NULL,
                        highlight TEXT NOT NULL,
                        font_size INTEGER NOT NULL,
                        edge_weight INTEGER NOT NULL,
                        font_family TEXT NOT NULL,
                        emboldened INT NOT NULL,
                        underlined INT NOT NULL,
                        italicised INT NOT NULL,
                        parent_node_id INT,
                        mind_map_id INT
                );";

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Execute SQL commands
                    command.CommandText = createMindMapTableQuery;
                    command.ExecuteNonQuery();

                    command.CommandText = createNodeTableQuery;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void UpdateMindMap(TreeClasses.Tree tree, long mindMapId)
        {
            // Get mind map attributes
            string name = tree.Name;
            double rootNodeRadius = tree.RootNodeRadius;
            double nodeSize = tree.NodeSize;

            // Get current date in British format
            string dateModified = DateTime.UtcNow.ToString("dd/MM/yyyy");

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL commands

                // Update related mind map record with the given values
                string updateMindMapQuery = @" 
                    UPDATE mind_map
                    SET name=@name, root_node_radius=@root_node_radius, node_size=@node_size, date_modified=@date_modified
                    WHERE id=" + Convert.ToString(mindMapId);

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Replace placeholder values with respective mind map attributes
                    command.CommandText = updateMindMapQuery;
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@root_node_radius", rootNodeRadius);
                    command.Parameters.AddWithValue("@node_size", nodeSize);
                    command.Parameters.AddWithValue("@date_modified", dateModified);

                    // Execute SQL command
                    command.ExecuteNonQuery();
                }
            }

            // Delete old nodes
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL commands

                // Delete the all node records related to the mind map to update
                string deleteMindMapNodesQuery = @"DELETE FROM node WHERE mind_map_id=" + Convert.ToString(mindMapId);

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Execute SQL command
                    command.CommandText = deleteMindMapNodesQuery;
                    command.ExecuteNonQuery();
                }
            }

            // Save nodes in the mind map
            SaveNode(tree.GetRootNode(), mindMapId, null);
        }

        public static long SaveNewMindMap(TreeClasses.Tree tree)
        {
            // Get mind map attributes/characteristics
            string name = tree.Name;
            double rootNodeRadius = tree.RootNodeRadius;
            double nodeSize = tree.NodeSize;

            // Get current date in the British date format (dd/MM/yyyy)
            string currentDate = DateTime.UtcNow.ToString("dd/MM/yyyy");

            long mindMapId;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL commands

                // Create a new mind_map record with the given values
                string insertMindMapQuery = @"
                    INSERT INTO mind_map (name, root_node_radius, node_size, date_created, date_modified) 
                    VALUES (@name, @root_node_radius, @node_size, @date_created, @date_modified)
                ;";

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Replace placeholder values with respective mind map attributes
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@root_node_radius", rootNodeRadius);
                    command.Parameters.AddWithValue("@node_size", nodeSize);
                    command.Parameters.AddWithValue("@date_created", currentDate);
                    command.Parameters.AddWithValue("@date_modified", currentDate);

                    // Execute SQL command
                    command.CommandText = insertMindMapQuery;
                    command.ExecuteNonQuery();
                }

                // Get id of the mind map's record
                mindMapId = connection.LastInsertRowId;
            }

            // Save nodes in the mind map
            SaveNode(tree.GetRootNode(), mindMapId, null);

            return mindMapId;
        }

        private static void SaveNode(TreeClasses.Node node, long mindMapId, long? parentNodeId)
        {
            // Get attributes/characteristics of the node to save
            int root = Convert.ToInt32(node.IsRoot());
            string text = node.GetText();
            int fontSize = Convert.ToInt32(node.GetFontSize());
            int edgeWeight = Convert.ToInt32(node.GetEdgeWeight());
            string shape = node.GetShape();

            BrushConverter brushConvertor = new BrushConverter();
            string fillColour = brushConvertor.ConvertToString(node.GetFillColour());
            string outlineColour = brushConvertor.ConvertToString(node.GetOutlineColour());
            string fontColour = brushConvertor.ConvertToString(node.GetFontColour());
            string highlight = brushConvertor.ConvertToString(node.GetHighlight());

            FontFamilyConverter fontFamilyConverter = new FontFamilyConverter();
            string fontFamily = fontFamilyConverter.ConvertToString(node.GetFontFamily());

            int emboldened = Convert.ToInt32(node.IsBold());
            int underlined = Convert.ToInt32(node.IsUnderlined());
            int italicised = Convert.ToInt32(node.IsItalicised());

            // Create a new node record with the given values
            string insertNodeQuery = @"
                INSERT INTO node (root, text, shape, font_family, font_size, edge_weight, fill_colour, outline_colour, font_colour, highlight, emboldened, underlined, italicised, parent_node_id, mind_map_id)
                VALUES (@root, @text, @shape, @font_family, @font_size, @edge_weight, @fill_colour, @outline_colour, @font_colour, @highlight, @emboldened, @underlined, @italicised, @parent_node_id, @mind_map_id)
            ";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL commands

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Substitute placeholder values with respective node attributes
                    command.Parameters.AddWithValue("@root", root);
                    command.Parameters.AddWithValue("@text", text);
                    command.Parameters.AddWithValue("font_size", fontSize);
                    command.Parameters.AddWithValue("@shape", shape);
                    command.Parameters.AddWithValue("@font_family", fontFamily);
                    command.Parameters.AddWithValue("@edge_weight", edgeWeight);
                    command.Parameters.AddWithValue("@fill_colour", fillColour);
                    command.Parameters.AddWithValue("@outline_colour", outlineColour);
                    command.Parameters.AddWithValue("@font_colour", fontColour);
                    command.Parameters.AddWithValue("@highlight", highlight);
                    command.Parameters.AddWithValue("@emboldened", emboldened);
                    command.Parameters.AddWithValue("@underlined", underlined);
                    command.Parameters.AddWithValue("@italicised", italicised);
                    command.Parameters.AddWithValue("@parent_node_id", parentNodeId);
                    command.Parameters.AddWithValue("@mind_map_id", mindMapId);

                    // Execute SQL command
                    command.CommandText = insertNodeQuery;
                    command.ExecuteNonQuery();
                }

                // Get id of the node's record
                parentNodeId = connection.LastInsertRowId;

                List<TreeClasses.Node> children = node.GetChildren();

                if (children.Count == 0) { return; }

                // Save node's descendents to the database
                foreach (TreeClasses.Node child in children)
                {
                    SaveNode(child, mindMapId, parentNodeId);
                }
            }
        }

        public static List<MenuListElementInfo> LoadAllMenuListElementInfo()
        {
            List<MenuListElementInfo> menuListElementInfoList = new List<MenuListElementInfo>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL queries

                // Get all mind_map records in the database
                string loadAllMindMapIdsQuery = @"SELECT * FROM mind_map";

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = loadAllMindMapIdsQuery;
                    
                    // Execute SQL query
                    SQLiteDataReader reader = command.ExecuteReader();

                    // Iterate through each mind_map record
                    while (reader.Read())
                    {
                        // Store mind_map record values
                        MenuListElementInfo menuListElementInfo = new MenuListElementInfo();
                        menuListElementInfo.Id = reader.GetInt64(reader.GetOrdinal("id"));
                        menuListElementInfo.Name = reader.GetString(reader.GetOrdinal("name"));
                        menuListElementInfo.DateCreated = reader.GetString(reader.GetOrdinal("date_created"));
                        menuListElementInfo.DateModified = reader.GetString(reader.GetOrdinal("date_modified"));

                        // Put the record's information in a list
                        menuListElementInfoList.Add(menuListElementInfo);
                    }
                }
            }

            return menuListElementInfoList;
        }

        public static void DeleteMindMap(long mindMapId)
        {
            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL commands

                // Delete mind map with the given id
                string deleteMindMapNodesQuery = @"DELETE FROM node WHERE mind_map_id=" + Convert.ToString(mindMapId);

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Execute SQL command
                    command.CommandText = deleteMindMapNodesQuery;
                    command.ExecuteNonQuery();
                }
            }

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL commands

                // Delete all nodes related to the deleted mind map
                string deleteMindMapQuery = @"DELETE FROM mind_map WHERE id=" + Convert.ToString(mindMapId);

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    // Execute SQL command
                    command.CommandText = deleteMindMapQuery;
                    command.ExecuteNonQuery();
                }
            }
        }

        public static MindMapInfo GetMindMapInfoFromMindMapId(long mindMapId)
        {
            MindMapInfo mindMapInfo = new MindMapInfo();
            mindMapInfo.Id = mindMapId;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL queries

                // Get values of mind_map record with the given id
                string getMindMapInfoQuery = @"SELECT * FROM mind_map WHERE id=" + Convert.ToString(mindMapId);

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText= getMindMapInfoQuery;

                    // Execute SQL query
                    SQLiteDataReader reader = command.ExecuteReader();

                    // Iterate through query results
                    while (reader.Read())
                    {
                        // Store query results in appropriate attributes
                        mindMapInfo.Name = reader.GetString(reader.GetOrdinal("name"));
                        mindMapInfo.DateCreated = reader.GetString(reader.GetOrdinal("date_created"));
                        mindMapInfo.DateModified = reader.GetString(reader.GetOrdinal("date_modified"));
                        mindMapInfo.RootNodeRadius = Convert.ToDouble(reader.GetFloat(reader.GetOrdinal("root_node_radius")));
                        mindMapInfo.NodeSize = Convert.ToDouble(reader.GetFloat(reader.GetOrdinal("node_size")));
                    }
                }
            }

            return mindMapInfo;
        }

        public static NodeInfo GetRootNodeInfoFromMindMapId(long mindMapId)
        {
            NodeInfo rootNodeInfo = new NodeInfo();
            rootNodeInfo.IsRoot = true;
            rootNodeInfo.ParentNodeId = null;
            rootNodeInfo.MindMapId = mindMapId;

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL queries

                // Get values of the root node's record
                string getRootNodeInfoQuery = @"SELECT * FROM node WHERE (mind_map_id=" + Convert.ToString(mindMapId) + " AND root=1)";

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = getRootNodeInfoQuery;

                    BrushConverter brushConvertor = new BrushConverter();

                    // Execute SQL query
                    SQLiteDataReader reader = command.ExecuteReader();

                    // Iterate through query results
                    while (reader.Read())
                    {
                        // Store query results in appropriate attributes
                        rootNodeInfo.Id = reader.GetInt64(reader.GetOrdinal("id"));
                        rootNodeInfo.Text = reader.GetString(reader.GetOrdinal("text"));
                        rootNodeInfo.Shape = reader.GetString(reader.GetOrdinal("shape"));
                        rootNodeInfo.FillColour = (Brush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("fill_colour")));
                        rootNodeInfo.FontColour = (Brush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("font_colour")));
                        rootNodeInfo.OutlineColour = (Brush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("outline_colour")));
                        rootNodeInfo.Highlight = (Brush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("highlight")));
                        rootNodeInfo.EdgeWeight = reader.GetInt32(reader.GetOrdinal("edge_weight"));
                        rootNodeInfo.FontSize = reader.GetInt32(reader.GetOrdinal("font_size"));
                        rootNodeInfo.FontFamily = new FontFamily(reader.GetString(reader.GetOrdinal("font_family")));
                        rootNodeInfo.Emboldened = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("emboldened")));
                        rootNodeInfo.Underlined = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("underlined")));
                        rootNodeInfo.Italicised = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("italicised")));
                    }
                }
            }

            return rootNodeInfo;
        }

        public static List<NodeInfo> GetNodeChildrenInfoListFromParentNodeId(long parentNodeId)
        {
            List<NodeInfo> nodeChildrenInfoList = new List<NodeInfo>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open(); // Start listening for SQL queries

                // Get all values of all node records that are a child of the given node
                string getNodeInfoQuery = @"SELECT * FROM node WHERE parent_node_id=" + Convert.ToString(parentNodeId);

                using (SQLiteCommand command = new SQLiteCommand(connection))
                {
                    command.CommandText = getNodeInfoQuery;

                    BrushConverter brushConvertor = new BrushConverter();

                    // Execute SQL query
                    SQLiteDataReader reader = command.ExecuteReader();

                    // Iterate through each node record queried
                    while (reader.Read())
                    {
                        // Store values of each node record in the appropriate attributes
                        NodeInfo nodeInfo = new NodeInfo();
                        nodeInfo.Id = reader.GetInt64(reader.GetOrdinal("id"));
                        nodeInfo.Text = reader.GetString(reader.GetOrdinal("text"));
                        nodeInfo.Shape = reader.GetString(reader.GetOrdinal("shape"));
                        nodeInfo.FillColour = (SolidColorBrush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("fill_colour")));
                        nodeInfo.FontColour = (SolidColorBrush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("font_colour")));
                        nodeInfo.OutlineColour = (SolidColorBrush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("outline_colour")));
                        nodeInfo.Highlight = (SolidColorBrush)brushConvertor.ConvertFrom(reader.GetString(reader.GetOrdinal("highlight")));
                        nodeInfo.EdgeWeight = reader.GetInt32(reader.GetOrdinal("edge_weight"));
                        nodeInfo.FontSize = reader.GetInt32(reader.GetOrdinal("font_size"));
                        nodeInfo.FontFamily = new FontFamily(reader.GetString(reader.GetOrdinal("font_family")));
                        nodeInfo.Emboldened = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("emboldened")));
                        nodeInfo.Underlined = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("underlined")));
                        nodeInfo.Italicised = Convert.ToBoolean(reader.GetInt32(reader.GetOrdinal("italicised")));
                        nodeInfo.ParentNodeId = parentNodeId;
                        nodeInfo.MindMapId = reader.GetInt64(reader.GetOrdinal("mind_map_id"));

                        // Store the group of values into a list
                        nodeChildrenInfoList.Add(nodeInfo);
                    }
                }
            }

            return nodeChildrenInfoList;
        }
    }
}
