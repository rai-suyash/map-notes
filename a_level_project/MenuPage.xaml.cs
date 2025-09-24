using DatabaseHandler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

namespace a_level_project
{
    public partial class MenuPage : Page
    {
        public event EventHandler GoingToWorkspace;
        List<MenuListElement> menuList = new List<MenuListElement>();

        // Subroutines for initialising menu page
        public MenuPage()
        {
            InitializeComponent();
            LoadMenuList();
        }

        private void LoadMenuList()
        {
            // Get information of each mind map in the database
            List<DatabaseHandler.MenuListElementInfo> menuListElementInfoList = DatabaseHandler.DatabaseModules.LoadAllMenuListElementInfo();

            if (menuListElementInfoList.Count == 0 ) { return; }

            foreach(DatabaseHandler.MenuListElementInfo menuListElementInfo in menuListElementInfoList)
            {
                // Create a representative UI widget for each mind map in the database
                MenuListElement menuListElement = new MenuListElement(MenuScrollViewerStackPanel, menuListElementInfo.Id, menuListElementInfo.Name, menuListElementInfo.DateCreated, menuListElementInfo.DateModified);
                menuListElement.MenuListElementDelete += new EventHandler(OnMenuListElementDeleted);
                menuListElement.MenuListElementSelected += new EventHandler(MenuListElement_MenuListElementSelected);
                menuList.Add(menuListElement);
            }
        }

        private void MenuListElement_MenuListElementSelected(object? sender, EventArgs e)
        {
            MenuListElement menuListElement = (MenuListElement)sender;

            if (menuListElement == null) { return; }

            GoingToWorkspace.Invoke(menuListElement, EventArgs.Empty);
        }

        // UI event handlers
        private void CreateNewMindMapButton_OnClick(object sender, RoutedEventArgs e)
        {
            GoingToWorkspace.Invoke(null, EventArgs.Empty);
        }

        private void MenuSearchBar_OnKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            if (textBox == null) { return; }

            string textBoxText = textBox.Text;

            foreach (MenuListElement menuListElement in menuList)
            {
                string menuListElementName = menuListElement.GetName();

                if ((textBoxText.Length == 0) || (menuListElementName.Contains(textBoxText)))
                {
                    // If they are not searching for anything or the project name contains what they are searching for, show mind map project
                    menuListElement.Show();
                }
                else
                {
                    // Else, hide unwanted mind map projects
                    menuListElement.Hide();
                }
            }
        }

        // Menu list element UI events
        private void OnMenuListElementDeleted(object sender, EventArgs e)
        {
            // Pop up message box asking user to confirm if they want to delete the mind map project
            string messageBoxText = "Do you want to delete this mind map?";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;
            MessageBoxResult result = MessageBox.Show(messageBoxText, "", button, icon, MessageBoxResult.No);

            if (result == MessageBoxResult.No) { return; }

            MenuListElement menuListElement = (MenuListElement)sender;

            if (menuListElement == null ) { return; }

            DatabaseHandler.DatabaseModules.DeleteMindMap(menuListElement.GetMindMapId());
            menuList.Remove(menuListElement);
            MenuScrollViewerStackPanel.Children.Remove(menuListElement.GetMenuListElementGrid());
        }

        private void OnMenuListElementSelected(object sender, EventArgs e)
        {
            MenuListElement menuListElement = (MenuListElement)sender;

            if (menuListElement == null ) { return; }

            GoingToWorkspace.Invoke(menuListElement, EventArgs.Empty);
        }
    }

    public class MenuListElement
    {
        private long MindMapId;
        private string Name;
        private string DateCreated;
        private string DateModified;

        public event EventHandler MenuListElementDelete;
        public event EventHandler MenuListElementSelected;

        private StackPanel StackPanelToBeDisplayedOn;

        private Grid elementGrid;
        private Button elementGridSelectButton;
        private Button elementGridDeleteButton;
        private TextBlock elementGridNameTextBlock;
        private TextBlock elementGridDateCreatedTextBlock;
        private TextBlock elementGridDateModifiedTextBlock;
        
        public MenuListElement(StackPanel stackPanelToBeDisplayedOn, long mindMapId, string name, string dateCreated, string dateModified)
        {
            // Set attributes
            MindMapId = mindMapId;
            Name = name;
            DateCreated = dateCreated;
            DateModified = dateModified;

            StackPanelToBeDisplayedOn = stackPanelToBeDisplayedOn;
            
            // Make the element UI
            elementGrid = new Grid { 
                Height = 40,
                Margin = new Thickness(5, 5, 5, 5)
            };
            stackPanelToBeDisplayedOn.Children.Add(elementGrid);

            ColumnDefinition col1 = new ColumnDefinition { Width = new GridLength(400) };
            ColumnDefinition col2 = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };
            ColumnDefinition col3 = new ColumnDefinition { Width = new GridLength(150) };
            ColumnDefinition col4 = new ColumnDefinition { Width = new GridLength(40) };

            elementGrid.ColumnDefinitions.Add(col1);
            elementGrid.ColumnDefinitions.Add(col2);
            elementGrid.ColumnDefinitions.Add(col3);
            elementGrid.ColumnDefinitions.Add(col4);

            RowDefinition row1 = new RowDefinition();
            RowDefinition row2 = new RowDefinition();

            elementGrid.RowDefinitions.Add(row1);
            elementGrid.RowDefinitions.Add(row2);

            elementGridSelectButton = new Button { 
                Background = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 0)
            };
            Grid.SetColumnSpan(elementGridSelectButton, 4);
            Grid.SetRowSpan(elementGridSelectButton, 2);
            Panel.SetZIndex(elementGridSelectButton, 1);
            elementGrid.Children.Add(elementGridSelectButton);

            elementGridDeleteButton = new Button
            {
                Background = Brushes.Red,
                Foreground = Brushes.White,
                FontSize = 9,
                Margin = new Thickness(5, 5, 5, 5)
            };
            Grid.SetColumn(elementGridDeleteButton, 3);
            Grid.SetRowSpan(elementGridDeleteButton, 2);
            Panel.SetZIndex(elementGridDeleteButton, 2);
            elementGrid.Children.Add(elementGridDeleteButton);

            TextBlock elementGridDeleteButtonTextBlock = new TextBlock { TextWrapping = TextWrapping.Wrap };
            elementGridDeleteButtonTextBlock.Text = "Delete";
            elementGridDeleteButton.Content = elementGridDeleteButtonTextBlock;

            elementGridNameTextBlock = new TextBlock
            {
                FontSize = 20,
                Margin = new Thickness(5, 5, 5, 5)
            };
            Grid.SetRowSpan(elementGridNameTextBlock, 2);
            Panel.SetZIndex(elementGridNameTextBlock, 3);
            elementGridNameTextBlock.Text = name;
            elementGrid.Children.Add(elementGridNameTextBlock);

            elementGridDateCreatedTextBlock = new TextBlock();
            Grid.SetColumn(elementGridDateCreatedTextBlock, 2);
            Panel.SetZIndex(elementGridDateCreatedTextBlock, 3);
            elementGridDateCreatedTextBlock.Text = "Date created: " + dateCreated;
            elementGrid.Children.Add(elementGridDateCreatedTextBlock);

            elementGridDateModifiedTextBlock = new TextBlock();
            Grid.SetColumn(elementGridDateModifiedTextBlock, 2);
            Grid.SetRow(elementGridDateModifiedTextBlock, 1);
            Panel.SetZIndex(elementGridDateModifiedTextBlock, 3);
            elementGridDateModifiedTextBlock.Text = "Date modified: " + dateModified;
            elementGrid.Children.Add(elementGridDateModifiedTextBlock);

            // Connect event handlers
            elementGridDeleteButton.Click += elementGridDeleteButton_OnClick;
            elementGridSelectButton.Click += elementGridSelectButton_OnClick;
        }

        public long GetMindMapId()
        {
            return MindMapId;
        }

        public Grid GetMenuListElementGrid()
        {
            return elementGrid;
        }

        public void Show()
        {
            elementGrid.Visibility = Visibility.Visible;
        }

        public void Hide()
        {
            elementGrid.Visibility = Visibility.Collapsed;
        }

        public string GetName()
        {
            return elementGridNameTextBlock.Text;
        }

        // Event handlers
        private void elementGridDeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            MenuListElementDelete?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void elementGridSelectButton_OnClick(object sender, RoutedEventArgs e)
        {
            MenuListElementSelected?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
