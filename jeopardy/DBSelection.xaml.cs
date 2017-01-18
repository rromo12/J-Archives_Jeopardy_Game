using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
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
    /// Interaction logic for DBSelection.xaml
    /// </summary>
    public partial class DBSelection : Window
    {
        string date = "";
        public DBSelection()
        {
            InitializeComponent();
            if (System.IO.File.Exists("./Jclues.mdf"))
            {
                JCluesFunctions DB = new JCluesFunctions();
                DataTable airdates = DB.LoadAirdates(); ;
                dataGrid.DataContext = airdates.DefaultView;
                this.Show();
            }
            else
            {
                string messageBoxText = "Database not found please download and scrape from j-archives before using this option";
                string caption = "Database Not Found!";
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBox.Show(messageBoxText, caption, button, icon);
                MainMenu window = new MainMenu();
                this.Close();
                window.Show();

            }
            
            //dataGrid.SelectedItem = dataGrid.Items[0];
            


        }

        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataRowView row = (DataRowView)dataGrid.SelectedItem;
            date = row["Airdate"].ToString();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            GameBoardWindow window = new GameBoardWindow(date);
            window.Show();
            this.Close();
        }
    }
}
