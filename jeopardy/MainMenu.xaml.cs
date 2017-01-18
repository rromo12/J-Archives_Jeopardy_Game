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
using System.Windows.Shapes;

namespace jeopardy
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        public MainMenu()
        {
            InitializeComponent();
        }

        public void NewGame_Click(object sender, RoutedEventArgs e)
        {
            int count;
            //Loads from DB
            if (System.IO.Directory.Exists(".\\downloaded_games"))
            {
                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(".\\downloaded_games");
                count = dir.GetFiles().Length;
            }
            else
            {
                //Hardcode Highest value for use online without having downloaded games
                count = 5394;
            }
            Random rand = new Random();
            
            //GameBoardWindow window = new GameBoardWindow();
            GameBoardWindow window = new GameBoardWindow(rand.Next(1, count));
            window.Show();
            this.Close();
        }

        private void Scrape_Click(object sender, RoutedEventArgs e)
        {
            Scraper window = new Scraper();
            window.Show();
            this.Close();
        }

        private void NewGame_ID_Click(object sender, RoutedEventArgs e)
        {
            //Create Dialog Box Asking for ID (Loads from HTML Files)
            int id = GameIDSelection.ShowDialog("Please enter a game id from J-Archive", "");
            GameBoardWindow window = new GameBoardWindow(id);
            window.Show();
            this.Close();

        }

        private void LoadDBGame_Click(object sender, RoutedEventArgs e)
        {
            DBSelection window = new DBSelection();
            this.Close();
            
        }
    }
}
