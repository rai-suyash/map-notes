using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Serialization;
using TreeAlgorithms;
using System.Windows.Input;
using System.Globalization;
using System.Security.Permissions;

namespace TreeClasses
{
    public class Node
    {
        private Node? parent;
        public Node? Parent
        {
            get
            {
                return parent;
            }

            set
            {
                parent = value;
            }
        }
        private List<Node> Children;

        private Grid NodeGrid;
        private Shape NodeShape;
        private Border NodeSelectionBorder;
        private RichTextBox NodeTextBox;
        private FlowDocument NodeTextBoxFlowDocument;
        private Paragraph NodeTextBoxParagraph;
        private Line? NodeEdge;

        private double radius;
        public double Radius {
            get
            {
                return radius;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Radius cannot be less than or equal to 0.");
                }
                radius = value;
            } 
        }

        private double arcAngle;
        public double ArcAngle 
        {
            get
            {
                return arcAngle;
            }

            set
            {
                if ((value < 0) || (value > 180))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Arc angle must be between 0 and 180 inclusive.");
                }
                arcAngle = value;
            }
        }
        public double RelativeAngle { get; set; }

        private double angle;
        public double Angle
        {
            get
            {
                return angle;
            }

            set
            {
                if ((value < 0) || (value >= 360))
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Angle must normalised (be between 0 inclusive and 360 exclusive).");
                }
                angle = value;
            }
        }

        private double[] position;
        public double[] Position
        {
            get
            {
                return position;
            }

            set
            {
                position = value;
                Canvas.SetLeft(NodeGrid, position[0] - size/2);
                Canvas.SetTop(NodeGrid, position[1] - size/2);
                NodeEdge.X1 = position[0];
                NodeEdge.Y1 = position[1];

                // Reposition node edges of children, if there are any
                if (Children.Count == 0) { return; }

                foreach(Node child in Children)
                {
                    Line childEdge = child.GetNodeEdge();
                    childEdge.X2 = position[0];
                    childEdge.Y2 = position[1];
                }
            }
        }

        private double size;
        public double Size
        {
            get
            {
                return size;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Size must not be lower than or equal to 0.");
                }

                size = value;
                NodeGrid.Width = value;
                NodeGrid.Height = value;
                Canvas.SetLeft(NodeGrid, position[0] - size / 2);
                Canvas.SetTop(NodeGrid, position[1] - size / 2);
            }
        }

        public event EventHandler NodeClicked;

        public Node(Canvas canvasToBeDisplayedOn, Node? parent) 
        {
            // Form relationship of node
            Parent = parent;
            Children = new List<Node>();
            
            // Set up attributes required for tree algorithms
            Radius = 120;
            ArcAngle = 100;
            RelativeAngle = 0;
            Angle = 0;
            position = new double[] { 0, 0 }; // Use lowercase here to avoid triggering setter
            size = 70; // Use lowercase here to avoid triggering setter

            NodeGrid = new Grid
            {
                Width = size,
                Height = size
            };
            Panel.SetZIndex(NodeGrid, 5); // NodeGrid should appear behind the toolbox and other workspace button features
            canvasToBeDisplayedOn.Children.Add(NodeGrid);

            NodeShape = new Ellipse
            {
                Fill = Brushes.White,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            NodeGrid.Children.Add(NodeShape);
            Panel.SetZIndex(NodeShape, 3); // NodeShape should appear behind NodeSelectionBorder so that the selection border is not blocked by its stroke

            NodeSelectionBorder = new Border
            {
                BorderBrush = Brushes.Cyan,
                BorderThickness = new Thickness(2, 2, 2, 2),
                Visibility = Visibility.Hidden
            };
            NodeGrid.Children.Add(NodeSelectionBorder);
            Panel.SetZIndex(NodeSelectionBorder, 4); // NodeSelectionBorder should be in front of NodeShape

            // Form the text contents of the node
            NodeTextBox = new RichTextBox
            {
                Background = Brushes.Transparent,
                Foreground = Brushes.Black,
                BorderThickness = new Thickness(0, 0, 0, 0),
                FontFamily = new FontFamily("Calibri"),
                FontWeight = FontWeights.Normal,
                FontStyle = FontStyles.Normal,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            NodeGrid.Children.Add(NodeTextBox);
            Panel.SetZIndex(NodeTextBox, 5); // NodeTextBox should be in front of NodeSelectionBorder in order to be interactable

            NodeTextBoxFlowDocument = new FlowDocument();
            NodeTextBox.Document = NodeTextBoxFlowDocument;

            NodeTextBoxParagraph = new Paragraph
            {
                Background = Brushes.Transparent,
                TextDecorations = null,
                TextAlignment = TextAlignment.Center
            };
            NodeTextBoxFlowDocument.Blocks.Add(NodeTextBoxParagraph);

            Run PlaceholderRun = new Run("Type text in here...");
            NodeTextBoxParagraph.Inlines.Add(PlaceholderRun);

            // Form the visual relationship link of the node
            NodeEdge = new Line
            {
                StrokeThickness = 1,
                Stroke = Brushes.Black
            };
            Panel.SetZIndex(NodeEdge, 1); // NodeEdge should be the furthest back so that it does obscure the contents of the node
            canvasToBeDisplayedOn.Children.Add(NodeEdge);

            // A root node does not have a parent, so it should look like it has no edge
            if (Parent == null)
            {
                NodeEdge.StrokeThickness = 0;
            }
            else
            {
                NodeEdge.X1 = position[0];
                NodeEdge.Y1 = position[1];
                NodeEdge.X2 = Parent.Position[0];
                NodeEdge.Y2 = Parent.Position[1];
            }

            // Node visuals will not be positioned correctly at the end of construction.

            // Creating and connecting node events
            NodeGrid.MouseLeftButtonDown += new MouseButtonEventHandler(Node_OnMouseLeftButtonDown);
            NodeGrid.TouchDown += new EventHandler<TouchEventArgs>(Node_OnTouchDown);
        }

        // Methods
        public bool IsRoot()
        {
            return (Parent == null);
        }

        public Grid GetNodeGrid()
        {
            return NodeGrid;
        }
    
        public Line GetNodeEdge()
        {
            return NodeEdge;
        }

        public List<Node> GetChildren()
        {
            return Children;
        }

        public void ShowNodeSelectionBorder()
        {
            NodeSelectionBorder.Visibility = Visibility.Visible;
        }

        public void HideNodeSelectionBorder()
        {
            NodeSelectionBorder.Visibility = Visibility.Hidden;
        }

        public void AddChildNode(Node childNode)
        {
            Children.Add(childNode);
        }

        public void RemoveChildNode(Node childNode)
        {
            Children.Remove(childNode);
        }

        public bool IsBold()
        {
            return (NodeTextBox.FontWeight == FontWeights.Bold);
        }

        public void Embolden()
        {
            NodeTextBox.FontWeight = FontWeights.Bold;
        }

        public void Unembolden()
        {
            NodeTextBox.FontWeight = FontWeights.Normal;
        }

        public bool IsItalicised()
        {
            return (NodeTextBox.FontStyle == FontStyles.Italic);
        }

        public void Italicise()
        {
            NodeTextBox.FontStyle = FontStyles.Italic;
        }

        public void Deitalicise()
        {
            NodeTextBox.FontStyle = FontStyles.Normal;
        }

        public bool IsUnderlined()
        {
            return (NodeTextBoxParagraph.TextDecorations == TextDecorations.Underline);
        }

        public void Underline()
        {
            NodeTextBoxParagraph.TextDecorations = TextDecorations.Underline;
        }

        public void Deunderline()
        {
            NodeTextBoxParagraph.TextDecorations = null;
        }

        public void SetNodeFillColour(Brush colour)
        {
            NodeShape.Fill = colour;
        }

        public void SetNodeOutlineColour(Brush colour)
        {
            NodeShape.Stroke = colour;
            NodeEdge.Stroke = colour;
        }

        public void SetNodeTextColour(Brush colour)
        {
            NodeTextBox.Foreground = colour;
        }

        public void SetHighlight(Brush colour)
        {
            NodeTextBoxParagraph.Background = colour;
        }

        public void SetFontFamily(FontFamily fontFamily)
        {
            NodeTextBox.FontFamily = fontFamily;
        }

        public void SetEdgeWeight(double edgeWeight)
        {
            if (parent == null) { return; }

            NodeEdge.StrokeThickness = edgeWeight;
        }

        public void SetFontSize(double fontSize)
        {
            NodeTextBox.FontSize = fontSize;
        }

        public void SetNodeShape(string shape)
        {
            Shape newNodeShape;

            if (shape == "RectangleButton")
            {
                newNodeShape = new Rectangle();
            }
            else
            {
                newNodeShape = new Ellipse();
            }

            newNodeShape.Fill = NodeShape.Fill;
            newNodeShape.Stroke = NodeShape.Stroke;
            newNodeShape.StrokeThickness = NodeShape.StrokeThickness;
            Panel.SetZIndex(newNodeShape, 3);
            NodeGrid.Children.Remove(NodeShape);
            NodeShape = newNodeShape;
            NodeGrid.Children.Add(NodeShape);
        }

        public string GetText()
        {
            TextRange textRange = new TextRange(NodeTextBox.Document.ContentStart, NodeTextBox.Document.ContentEnd);
            return textRange.Text;
        }

        public double GetFontSize()
        {
            return NodeTextBox.FontSize;
        }

        public double GetEdgeWeight()
        {
            return NodeEdge.StrokeThickness;
        }

        public FontFamily GetFontFamily()
        {
            return NodeTextBox.FontFamily;
        }

        public Brush GetFillColour()
        {
            return NodeShape.Fill;
        }

        public Brush GetOutlineColour()
        {
            return NodeShape.Stroke;
        }

        public Brush GetFontColour()
        {
            return NodeTextBox.Foreground;
        }

        public Brush GetHighlight()
        {
            return NodeTextBoxParagraph.Background;
        }

        public string GetShape()
        {
            if (NodeShape is Rectangle)
            {
                return "Rectangle";
            }
            else
            {
                return "Circle";
            }
        }

        public void SetText(string text)
        {
            Run tempRun = new Run(text);
            NodeTextBoxParagraph.Inlines.Clear();
            NodeTextBoxParagraph.Inlines.Add(tempRun);
        }

        // Event Handlers
        private void Node_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            NodeClicked?.Invoke(this, EventArgs.Empty);
            e.Handled = true;

        }

        private void Node_OnTouchDown(object sender, TouchEventArgs e)
        {
            NodeClicked?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

    }

    public class Tree
    {
        public string Name;
        private Node RootNode;
        private Canvas CanvasToBeDisplayedOn;
        private double[] Origin;

        private double rootNodeRadius;
        public double RootNodeRadius
        {
            get 
            { 
                return rootNodeRadius; 
            } 
            
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Radius cannot be less than or equal to 0.");
                }

                rootNodeRadius = value;
                RootNode.Radius = value;
                TreeAlgorithms.TreeAlgorithmModules.PreorderTraversal(RootNode);
            }
        }

        private double nodeSize;
        public double NodeSize
        {
            get
            {
                return nodeSize;
            }

            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "Size cannot be less than or equal to 0.");
                }

                nodeSize = value;
                ChangeNodeSize(RootNode);
            }
        }


        public Tree(Canvas canvasToBeDisplayedOn, double[] origin, double _rootNodeRadius, double _nodeSize)
        {
            CanvasToBeDisplayedOn = canvasToBeDisplayedOn;
            Origin = origin;
            RootNode = new Node(CanvasToBeDisplayedOn, null);
            RootNode.Position = Origin;
            rootNodeRadius = _rootNodeRadius;
            RootNode.Radius = rootNodeRadius;
            nodeSize = _nodeSize;
            RootNode.Size = nodeSize;
        }

        public Node GetRootNode()
        {
            return RootNode;
        }

        public Node AddNode(Node parentNode)
        {
            Node childNode = new Node(CanvasToBeDisplayedOn, parentNode);
            parentNode.AddChildNode(childNode);
            childNode.Parent = parentNode;
            childNode.Size = nodeSize;
            TreeAlgorithms.TreeAlgorithmModules.PreorderTraversal(parentNode);

            return childNode;
        }

        public void DeleteNode(Node nodeToDelete)
        {
            if (nodeToDelete.IsRoot() == true) { return; } // Root node cannot be deleted by user

            List<Node> nodeToDeleteChildren = nodeToDelete.GetChildren();

            if (nodeToDeleteChildren.Count > 0)
            {
                foreach(Node child in nodeToDeleteChildren)
                {
                    // Disown child from parent to be deleted
                    child.Parent = nodeToDelete.Parent;

                    // Make the parent's parent adopt the child
                    nodeToDelete.Parent.AddChildNode(child);

                    // Attach child node edges to the parent's parent
                    Line childNodeEdge = child.GetNodeEdge();
                    childNodeEdge.X2 = nodeToDelete.Parent.Position[0];
                    childNodeEdge.Y2 = nodeToDelete.Parent.Position[1];
                }
            }

            nodeToDelete.Parent.RemoveChildNode(nodeToDelete);
            TreeAlgorithms.TreeAlgorithmModules.PreorderTraversal(nodeToDelete.Parent);

            nodeToDelete.Parent = null; // Remove reference to parent in the case the parent is deleted later
            CanvasToBeDisplayedOn.Children.Remove(nodeToDelete.GetNodeEdge());
            CanvasToBeDisplayedOn.Children.Remove(nodeToDelete.GetNodeGrid());
        }

        private void ChangeNodeSize(Node node)
        {
            node.Size = nodeSize;

            List<Node> children = node.GetChildren();

            if (children.Count == 0) { return; }

            // Preorder traversal of node's descendants
            // Modifies the size of all the node's descendents
            foreach(Node child in children)
            {
                ChangeNodeSize(child);
            }
        }
    }
}
