using System;
using System.Collections.Generic;
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
using System.Diagnostics;
using TreeAlgorithms;
using DatabaseHandler;

namespace a_level_project
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DatabaseHandler.DatabaseModules.InitialiseDatabase();
            MenuPage menuPage = new MenuPage();
            menuPage.GoingToWorkspace += new EventHandler(MenuPage_OnGoingToWorkspace);
            PageFrame.Content = menuPage;
        }

        private void MenuPage_OnGoingToWorkspace(object sender, EventArgs e)
        {
            MenuListElement menuListElement = (MenuListElement)sender;

            WorkspacePage workspacePage = new WorkspacePage(menuListElement);
            workspacePage.GoingToMenu += new EventHandler(WorkspacePage_OnGoingToMenu);
            PageFrame.Content = workspacePage;
        }

        private void WorkspacePage_OnGoingToMenu(object sender, EventArgs e)
        {
            MenuPage menuPage = new MenuPage();
            menuPage.GoingToWorkspace += new EventHandler(MenuPage_OnGoingToWorkspace);
            PageFrame.Content = menuPage;
        }
    }
}
