using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using TreeClasses;
using DatabaseHandler;

namespace a_level_project
{
    enum ColourEditTypes
    {
        None,
        Fill,
        Outline,
        Text,
        Highlight
    }

    public partial class WorkspacePage : Page
    {
        public long mindMapId = -1;

        public event EventHandler GoingToMenu;

        double[] CENTRE = new double[] { 500, 300 };
        List<TreeClasses.Node> selectedNodes = new List<TreeClasses.Node>();
        TreeClasses.Tree tree;
        ColourEditTypes ColourEditType = ColourEditTypes.None;

        bool isPanning = false;
        Point lastViewboxPosition;
        Point lastMousePosition;

        public WorkspacePage(MenuListElement? menuListElement)
        {
            InitializeComponent();

            tree = new TreeClasses.Tree(TreeViewboxCanvas, CENTRE, 100, 70);
            TreeClasses.Node root = tree.GetRootNode();
            root.NodeClicked += new EventHandler(OnNodeClicked);

            GenerateMindMapFromMenuListElement(menuListElement);
        }

        // Workspace UI event handlers
        private void WorkspaceCanvas_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            WorkspaceCanvas_OnClicked();
        }

        private void WorkspaceCanvas_OnTouchDown(object sender, TouchEventArgs e)
        {
            WorkspaceCanvas_OnClicked();
        }

        private void WorkspaceCanvas_OnClicked()
        {
            HideToggledWidgets();
            DeselectAllNodes();
            ClearFocus();
        }

        private void WorkspaceCanvas_OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ModifyTreeViewboxZoom(e.Delta, 20);
        }

        private void WorkspaceCanvas_OnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePosition = e.GetPosition(WorkspaceCanvas);
            lastViewboxPosition = TreeViewbox.TransformToAncestor(WorkspaceCanvas).Transform(new Point(0, 0));
            isPanning = true;
        }

        private void WorkspaceCanvas_OnMouseMove(object sender, MouseEventArgs e)
        {
            if (isPanning == false) { return; }

            if (e.RightButton != MouseButtonState.Pressed) { return; }

            Point currentMousePosition = e.GetPosition(WorkspaceCanvas);
            Double mousePositionDifferenceX = currentMousePosition.X - lastMousePosition.X;
            Double mousePositionDifferenceY = currentMousePosition.Y - lastMousePosition.Y;

            // Move mind map in direction of mouse drag
            Canvas.SetLeft(TreeViewbox, lastViewboxPosition.X + mousePositionDifferenceX);
            Canvas.SetTop(TreeViewbox, lastViewboxPosition.Y + mousePositionDifferenceY);
        }

        private void WorkspaceCanvas_OnMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            isPanning = false;
        }

        // Save button UI event handlers
        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Save mind map
            SaveMindMap(tree, mindMapId);

            // Pop up box telling user that the mind map has been saved
            string messageBoxText = "Mind map saved.";
            string caption = "";
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBoxImage icon = MessageBoxImage.None;
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
        }

        // Back to menu button UI event handlers
        private void BackToMenuButton_OnClick(Object sender, RoutedEventArgs e)
        {
            string messageBoxText = "Do you want to save your mind map before leaving the workspace?";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Question;
            MessageBoxResult result = MessageBox.Show(messageBoxText, "", button, icon, MessageBoxResult.Cancel);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveMindMap(tree, mindMapId);
                    break;
                case MessageBoxResult.Cancel:
                    return;
                default:
                    break;
            }

            GoingToMenu.Invoke(null, EventArgs.Empty);
        }

        // Zoom button UI event handlers
        private void ZoomInButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModifyTreeViewboxZoom(1, 60);
        }

        private void ZoomOutButton_OnClick(object sender, RoutedEventArgs e)
        {
            ModifyTreeViewboxZoom(-1, 60);
        }

        // ToolBox UI event handlers
        private void AddNodeButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideToggledWidgets();

            if (selectedNodes.Count == 0) { return; }

            foreach (TreeClasses.Node node in selectedNodes)
            {
                TreeClasses.Node addedNode = tree.AddNode(node); // Get new node from return value of method
                addedNode.NodeClicked += new EventHandler(OnNodeClicked);
            }
        }

        private void DeleteNodeButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideToggledWidgets();

            if (selectedNodes.Count == 0) { return; }

            // Create a copy of the list for nodes to delete to avoid modifying a list while its being looped through.
            List<TreeClasses.Node> nodesToDelete = new List<TreeClasses.Node>(selectedNodes);

            foreach (TreeClasses.Node node in nodesToDelete)
            {
                if (node.IsRoot() == true) { continue; }

                selectedNodes.Remove(node); // Remove references to deleted nodes in selectedNodes
                tree.DeleteNode(node);
            }

            nodesToDelete.Clear(); // Completely remove references to deleted nodes
        }

        private void EmboldenButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideToggledWidgets();

            if (selectedNodes.Count == 0) { return; }
            
            foreach (TreeClasses.Node node in selectedNodes)
            {
                if (node.IsBold() == true)
                {
                    node.Unembolden();
                }
                else
                {
                    node.Embolden();
                }
            }
        }

        private void ItaliciseButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideToggledWidgets();

            if (selectedNodes.Count == 0) { return; }

            foreach (TreeClasses.Node node in selectedNodes)
            {
                if (node.IsItalicised() == true)
                {
                    node.Deitalicise();
                }
                else
                {
                    node.Italicise();
                }
            }
        }

        private void UnderlineButton_OnClick(object sender, RoutedEventArgs e)
        {
            HideToggledWidgets();

            if (selectedNodes.Count == 0) { return; }

            foreach (TreeClasses.Node node in selectedNodes)
            {
                if (node.IsUnderlined() == true)
                {
                    node.Deunderline();
                }
                else
                {
                    node.Underline();
                }
            }
        }

        private void NodeFillColourButton_OnClick(object sender, RoutedEventArgs e)
        {   
            ColourButton_OnClick(sender, ColourEditTypes.Fill);
        }

        private void NodeOutlineColourButton_OnClick(object sender, RoutedEventArgs e)
        {
            ColourButton_OnClick(sender, ColourEditTypes.Outline);
        }

        private void NodeTextColourButton_OnClick(object sender, RoutedEventArgs e)
        {
            ColourButton_OnClick(sender, ColourEditTypes.Text);
        }

        private void HighlightButton_OnClick(object sender, RoutedEventArgs e)
        {
            ColourButton_OnClick(sender, ColourEditTypes.Highlight);
        }

        private void ColourButton_OnClick(object sender, ColourEditTypes TargetColourEditType)
        {
            Button targetButton = (Button)sender;

            if (targetButton != null)
            {
                if (ColourEditType == TargetColourEditType)
                {
                    // If the colour palette is already open above the button clicked, close it
                    ColourPalette.Visibility = Visibility.Hidden;
                    ColourEditType = ColourEditTypes.None;
                }
                else
                {
                    // Otherwise, put the colour palette above the button clicked
                    Point relativePoint = targetButton.TransformToAncestor(WorkspaceCanvas).Transform(new Point(0, 0));

                    HideToggledWidgets();
                    ColourEditType = TargetColourEditType;
                    ColourPalette.Visibility = Visibility.Visible;

                    // Set position of colour paletter above button clicked
                    Canvas.SetLeft(ColourPalette, relativePoint.X);
                    Canvas.SetTop(ColourPalette, relativePoint.Y - ColourPalette.Height);
                }
            }
        }

        private void NodeFontFamilyButton_OnClick(Object sender, RoutedEventArgs e) 
        { 
            if (FontFamilyScrollViewer.Visibility == Visibility.Visible)
            {
                FontFamilyScrollViewer.Visibility = Visibility.Hidden;
            }
            else
            {
                HideToggledWidgets();
                FontFamilyScrollViewer.Visibility = Visibility.Visible;
            }
        }
        private void NodeEdgeWeightButton_Click(object sender, RoutedEventArgs e)
        {
            if (EdgeWeightStackPanel.Visibility == Visibility.Visible)
            {
                EdgeWeightStackPanel.Visibility = Visibility.Hidden;
            }
            else
            {
                HideToggledWidgets();
                EdgeWeightStackPanel.Visibility = Visibility.Visible;
            }
        }

        private void NodeFontSizeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (FontSizeScrollViewer.Visibility == Visibility.Visible)
            {
                FontSizeScrollViewer.Visibility = Visibility.Hidden;
            }
            else
            {
                HideToggledWidgets();
                FontSizeScrollViewer.Visibility = Visibility.Visible;
            }
        }

        private void NodeShapeButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ShapeStackPanel.Visibility == Visibility.Visible)
            {
                ShapeStackPanel.Visibility = Visibility.Hidden;
            }
            else
            {
                HideToggledWidgets();
                ShapeStackPanel.Visibility = Visibility.Visible;
            }
        }

        // Colour palette UI event handlers
        private void ColourPaletteRectangle_OnMouseEnter(object sender, MouseEventArgs e)
        {
            Rectangle targetRectangle = (Rectangle)sender;

            if (targetRectangle != null)
            {
                // Make the border around the colour thick and blue
                targetRectangle.Stroke = Brushes.Cyan;
                targetRectangle.StrokeThickness = 2;
            }
        }

        private void ColourPaletteRectangle_OnMouseLeave(object sender, MouseEventArgs e)
        {
            Rectangle targetRectangle = (Rectangle)sender;

            if (targetRectangle != null)
            {
                // Return the border around the colour to the default thin and black
                targetRectangle.Stroke = Brushes.Black;
                targetRectangle.StrokeThickness = 0.5;
            }
        }

        private void ColourPaletteRectangle_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ColourPaletteRectangle_OnClick(sender, e);
        }

        private void ColourPaletteRectangle_OnTouchDown(object sender, TouchEventArgs e)
        {
            ColourPaletteRectangle_OnClick(sender, e);
        }

        private void ColourPaletteRectangle_OnClick(object sender, EventArgs e)
        {
            Rectangle targetRectangle = (Rectangle)sender;

            if (targetRectangle != null)
            {
                ApplyChosenColour(targetRectangle);
            }

            // Hide colour palette after colour is chosen
            ColourEditType = ColourEditTypes.None;
            ColourPalette.Visibility = Visibility.Hidden;
        }

        private void ApplyChosenColour(Rectangle rectangle)
        {
            if (selectedNodes.Count == 0) { return; }

            Brush brushToApply;

            if ((Grid.GetColumn(rectangle) == 6) && (Grid.GetRow(rectangle) == 6))
            {
                brushToApply = Brushes.Transparent;
            }
            else
            {
                brushToApply = rectangle.Fill;
            }

            foreach(TreeClasses.Node node in selectedNodes) {
                switch (ColourEditType)
                {
                    case ColourEditTypes.Fill:
                        node.SetNodeFillColour(brushToApply);
                        break;
                    case ColourEditTypes.Outline:
                        node.SetNodeOutlineColour(brushToApply);
                        break;
                    case ColourEditTypes.Text:
                        node.SetNodeTextColour(brushToApply);
                        break;
                    case ColourEditTypes.Highlight:
                        node.SetHighlight(brushToApply);
                        break;
                    default:
                        break;
                }
            }
        }

        // Font family UI event handlers
        private void FontFamilyScrollViewerStackPanelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button targetButton = (Button)sender;

            if ((targetButton != null) && (selectedNodes.Count > 0))
            {
                FontFamily fontFamilyToApply = targetButton.FontFamily;

                foreach(TreeClasses.Node node in selectedNodes)
                {
                    node.SetFontFamily(fontFamilyToApply);
                }

                FontFamilyScrollViewer.Visibility = Visibility.Hidden;
            }
        }

        // Edge weight UI event handlers
        private void EdgeWeightStackPanelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button targetButton = (Button)sender;

            if ((targetButton != null) && (selectedNodes.Count > 0))
            {
                double edgeWeightToApply = Convert.ToDouble(targetButton.Content.ToString());

                foreach (TreeClasses.Node node in selectedNodes)
                {
                    node.SetEdgeWeight(edgeWeightToApply);
                }

                EdgeWeightStackPanel.Visibility = Visibility.Hidden;
            }
        }

        // Font size UI event handlers
        private void FontSizeScrollViewerStackPanelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button targetButton = (Button)sender;

            if ((targetButton != null) && (selectedNodes.Count > 0))
            {
                double fontSizeToApply = Convert.ToDouble(targetButton.Content.ToString());

                foreach (TreeClasses.Node node in selectedNodes)
                {
                    node.SetFontSize(fontSizeToApply);
                }

                FontSizeScrollViewer.Visibility = Visibility.Hidden;
            }
        }

        // Shape UI event handlers
        private void ShapeStackPanelButton_OnClick(object sender, RoutedEventArgs e)
        {
            Button targetButton = (Button)sender;

            if ((targetButton != null) && (selectedNodes.Count > 0))
            {
                string shapetoApply = targetButton.Name;

                foreach (TreeClasses.Node node in selectedNodes)
                {
                    node.SetNodeShape(shapetoApply);
                }

                ShapeStackPanel.Visibility = Visibility.Hidden;
            }
        }

        // Node radius size event handlers
        private void NodeRadiusSizeTextBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) { return; }

            try
            {
                double newRootNodeRadius = Convert.ToDouble(NodeRadiusSizeTextBox.Text);

                if (newRootNodeRadius > 0)
                {
                    tree.RootNodeRadius = newRootNodeRadius;
                }
                else
                {
                    NodeRadiusSizeTextBox.Text = tree.RootNodeRadius.ToString();
                }
            }
            catch (Exception ex)
            {
                NodeRadiusSizeTextBox.Text = tree.RootNodeRadius.ToString();
            }

            ClearFocus();
        }

        // Node size event handlers
        private void NodeSizeTextBox_OnKeyDown(Object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return) { return; }

            try
            {
                double newNodeSize = Convert.ToDouble(NodeSizeTextBox.Text);

                if (newNodeSize > 0)
                {
                    tree.NodeSize = newNodeSize;
                }
                else
                {
                    NodeSizeTextBox.Text = tree.NodeSize.ToString();
                }
            }
            catch (Exception ex)
            {
                NodeSizeTextBox.Text = tree.NodeSize.ToString();
            }

            ClearFocus();
        }

        // Node event handlers
        private void OnNodeClicked(object sender, EventArgs e)
        {
            HideToggledWidgets();

            TreeClasses.Node clickedNode = (TreeClasses.Node)sender;

            if (clickedNode == null) { return; }

            bool isNodeSelected = selectedNodes.Contains(clickedNode);

            if (isNodeSelected == true)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) == false)
                {
                    DeselectAllNodes();
                }
                else
                {
                    DeselectNode(clickedNode);
                }
            }
            else
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) == false)
                {
                    DeselectAllNodes();
                }

                SelectNode(clickedNode);
            }
        }

        // Node related subroutines
        private void DeselectNode(TreeClasses.Node nodeToDeselect)
        {
            nodeToDeselect.HideNodeSelectionBorder();
            selectedNodes.Remove(nodeToDeselect);
        }

        private void DeselectAllNodes()
        {
            if (selectedNodes.Count == 0) { return; }

            // Loop through all selected nodes and hide their selection border
            foreach(TreeClasses.Node node in selectedNodes)
            {
                node.HideNodeSelectionBorder();
            }

            // Remove all selected nodes from selectedNodes, deselecting them all
            selectedNodes.Clear();
        }

        private void SelectNode(TreeClasses.Node nodeToSelect)
        {
            selectedNodes.Add(nodeToSelect);
            nodeToSelect.ShowNodeSelectionBorder();
        }

        // Misc subroutines
        private void SaveMindMap(TreeClasses.Tree mindMapToSave, long mindMapToSaveId)
        {
            // Set mind map name to what the user has put in the name text box
            tree.Name = ProjectNameTextBox.Text;

            // Check if the mind map in the workspace already exists in the database
            // -1 means that the mind map does not have an idea because it is not in the database
            if (mindMapToSaveId > -1)
            {
                DatabaseHandler.DatabaseModules.UpdateMindMap(mindMapToSave, mindMapToSaveId);
            }
            else
            {
                // Update the id of the mind map in the workspace as the mind map now exists in the database
                mindMapId = DatabaseHandler.DatabaseModules.SaveNewMindMap(mindMapToSave);
            }
        }

        private void ClearFocus()
        {
            Keyboard.ClearFocus();
        }

        private void HideToggledWidgets()
        {
            ShapeStackPanel.Visibility = Visibility.Hidden;
            FontSizeScrollViewer.Visibility = Visibility.Hidden;
            EdgeWeightStackPanel.Visibility = Visibility.Hidden;
            FontFamilyScrollViewer.Visibility = Visibility.Hidden;
            ColourPalette.Visibility = Visibility.Hidden;
            ColourEditType = ColourEditTypes.None;
        }

        private void ModifyTreeViewboxZoom(double delta, double magnitude)
        {
            if (delta < 0)
            {
                // If zooming outwards, reverse the direction of scaling the mind map (decreasing instead of increasing)
                magnitude = -magnitude;
            }
            
            double newWidth = TreeViewbox.Width + magnitude;
            double newHeight = TreeViewbox.Height + (magnitude * 0.6); // 0.6 required to scale proportionally

            if ((newWidth <= 0) || (newHeight <= 0)) { return; }
            
            TreeViewbox.Width = newWidth;
            TreeViewbox.Height = newHeight;
        }

        private void GenerateMindMapFromMenuListElement(MenuListElement? menuListElement)
        {
            if (menuListElement == null) { return; }

            mindMapId = menuListElement.GetMindMapId();

            // Get info of mind map from database and apply it
            DatabaseHandler.MindMapInfo mindMapInfo = DatabaseHandler.DatabaseModules.GetMindMapInfoFromMindMapId(mindMapId);
            tree.RootNodeRadius = mindMapInfo.RootNodeRadius;
            tree.NodeSize = mindMapInfo.NodeSize;
            ProjectNameTextBox.Text = mindMapInfo.Name;
            NodeRadiusSizeTextBox.Text = Convert.ToString(mindMapInfo.RootNodeRadius);
            NodeSizeTextBox.Text = Convert.ToString(mindMapInfo.NodeSize);

            // Get node info of root node and 'generate' it.
            DatabaseHandler.NodeInfo rootNodeInfo = DatabaseHandler.DatabaseModules.GetRootNodeInfoFromMindMapId(menuListElement.GetMindMapId());
            GenerateNodeFromNodeInfo(tree.GetRootNode(), rootNodeInfo);
        }

        private void GenerateNodeFromNodeInfo(TreeClasses.Node node, DatabaseHandler.NodeInfo nodeInfo)
        {
            node.SetText(nodeInfo.Text);
            
            if (nodeInfo.Shape == "Rectangle")
            {
                node.SetNodeShape("RectangleButton");
            }

            node.SetNodeFillColour(nodeInfo.FillColour);
            node.SetNodeOutlineColour(nodeInfo.OutlineColour);
            node.SetNodeTextColour(nodeInfo.FontColour);
            node.SetHighlight(nodeInfo.Highlight);
            node.SetFontSize(nodeInfo.FontSize);
            node.SetEdgeWeight(nodeInfo.EdgeWeight);
            node.SetFontFamily(nodeInfo.FontFamily);
            
            if (nodeInfo.Emboldened == true)
            {
                node.Embolden();
            }

            if (nodeInfo.Underlined == true)
            {
                node.Underline();
            }

            if (nodeInfo.Italicised == true)
            {
                node.Italicise();
            }

            List<DatabaseHandler.NodeInfo> nodeChildrenInfoList = DatabaseHandler.DatabaseModules.GetNodeChildrenInfoListFromParentNodeId(nodeInfo.Id);

            if (nodeChildrenInfoList.Count == 0) { return; }

            // Preorder traversal build-up of the tree
            foreach(DatabaseHandler.NodeInfo childNodeInfo in nodeChildrenInfoList)
            {
                TreeClasses.Node childNode = tree.AddNode(node);
                childNode.NodeClicked += new EventHandler(OnNodeClicked);
                GenerateNodeFromNodeInfo(childNode, childNodeInfo);
            }
        }
    }
}