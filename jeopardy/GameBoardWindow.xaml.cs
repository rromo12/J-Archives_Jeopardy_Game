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
using System.Web;
using System.Net;
namespace jeopardy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class GameBoardWindow : Window
    {
        JCluesFunctions DB;
        Game CurrentGame;
        Board CurrentBoard;
        
        public GameBoardWindow()
        {
            //Make this a random game
            InitializeComponent();
            DB = new JCluesFunctions();
            CurrentGame = DB.LoadGame(Convert.ToDateTime("9/6/2004 12:00:00 AM"));
            
            PopulateBoard(1);
        }
        public GameBoardWindow(string airdate)
        {
            //Make this a random game
            InitializeComponent();
            DB = new JCluesFunctions();
            CurrentGame = DB.LoadGame(Convert.ToDateTime(airdate));
            PopulateBoard(1);
        }

        //Switch Rounds
        public void RoundOne(object sender, RoutedEventArgs e)
        {
            PopulateBoard(1);
        }
        public void RoundTwo(object sender, RoutedEventArgs e)
        {
            PopulateBoard(2);
        }
        public void RoundThree(object sender, RoutedEventArgs e)
        {
            PopulateBoard(3);
            return;
        }


        //Load
        public GameBoardWindow(int id)
        {
            InitializeComponent();
            DB = new JCluesFunctions();
            Scraper sc = new Scraper();
            CurrentGame = sc.Parse_Page(id);
            PopulateBoard(1);
        }
        private void PopulateBoard(int round)
        {//Populate ;
            if(round == 1)
            {
                Panel.SetZIndex(FinClue, -1);
                CurrentBoard = CurrentGame.round1;
                int row, column;
                string text;
                for (row =0; row < ClueBoard.RowDefinitions.Count;  row++)
                {
                    for (column=0; column < ClueBoard.ColumnDefinitions.Count; column++)
                    {
                        UIElement ele = ClueBoard.Children.Cast<UIElement>().First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == column);
                        if(row == 0)
                        {
                            text = CurrentBoard.categories[column].Category_Text;
                            if (text == null)
                            {
                                text = "";
                            }
                            Label catLabel = (Label)ele;
                            TextBlock button_text = (TextBlock)catLabel.Content;
                            button_text.Text = WebUtility.HtmlDecode(text.ToUpper());
                        }
                        else
                        {
                            text = CurrentBoard.categories[column].Clues[row - 1].Clue_Text;
                            if(text == null)
                            {
                                text = "";
                            }
                            Button clueBut = (Button) ele;
                            TextBlock button_text = (TextBlock)clueBut.Content;
                            button_text.Text = WebUtility.HtmlDecode(text.ToUpper());
                        }
                        
                    }
                }

            }
            else if(round == 2)
            {
                Panel.SetZIndex(FinClue, -1);
                CurrentBoard = CurrentGame.round2;
                int row, column;
                string text;
                for (row = 0; row < ClueBoard.RowDefinitions.Count; row++)
                {
                    for (column = 0; column < ClueBoard.ColumnDefinitions.Count; column++)
                    {
                        UIElement ele = ClueBoard.Children.Cast<UIElement>().First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == column);
                        if (row == 0)
                        {
                            text = CurrentBoard.categories[column].Category_Text;
                            if (text == null)
                            {
                                text = "";
                            }
                            Label catLabel = (Label)ele;
                            TextBlock button_text = (TextBlock)catLabel.Content;
                            button_text.Text = WebUtility.HtmlDecode(text.ToUpper());
                        }
                        else
                        {
                            text = CurrentBoard.categories[column].Clues[row - 1].Clue_Text;
                            if (text == null)
                            {
                                text = "";
                            }
                            Button clueBut = (Button)ele;
                            TextBlock button_text = (TextBlock)clueBut.Content;
                            button_text.Text = WebUtility.HtmlDecode(text.ToUpper());
                        }

                    }
                }

            }
            else
            {//Final Jeopardy
                Panel.SetZIndex(FinClue, 1);
                TextBlock button_text = (TextBlock)FinClue.Content;
                button_text.Text = WebUtility.HtmlDecode(CurrentGame.FJ_category);
            }
        }
        public void MenuItem_Click(object sender, RoutedEventArgs e)
        {

        }
        public void FJ_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            if(CurrentGame.FJ_category == "")
            {
                b.Content = "This Game Does Not Include a Final Jeopardy Clue!";
            }
            if((string)b.Content == CurrentGame.FJ_category.ToUpper())
            {
                b.Content = WebUtility.HtmlDecode(CurrentGame.final_jeopardy.Clue_Text.ToUpper());
            }
            else if((string)b.Content == CurrentGame.final_jeopardy.Clue_Text.ToUpper())
            {
                b.Content = WebUtility.HtmlDecode(CurrentGame.final_jeopardy.Clue_Answer.ToUpper());
            }
            else
            {
                b.Content = WebUtility.HtmlDecode(CurrentGame.FJ_category.ToUpper());
            }

        }
        public void genButtonClick(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            TextBlock t = (TextBlock)b.Content;
            int row = Grid.GetRow(b);
            int col = Grid.GetColumn(b);
            string clue_text = CurrentBoard.categories[col].Clues[row-1].Clue_Text;
            string clue_ans = CurrentBoard.categories[col].Clues[row-1].Clue_Answer;
            if(clue_ans == null)
            {
                clue_ans = "";
            }
            if(clue_text == null)
            {
                clue_text = "";
            }

            if ((string) t.Text == clue_text.ToUpper())
            {
                t.Text = WebUtility.HtmlDecode(clue_ans.ToUpper());
            }
                
            else
            {
                t.Text = WebUtility.HtmlDecode(clue_text.ToUpper());
            }

            return;
        }

        private void Random_Game(object sender, RoutedEventArgs e)
        {
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(".\\downloaded_games");
            int count = dir.GetFiles().Length;
            Random rand = new Random();
            Scraper sc = new Scraper();
            CurrentGame = sc.Parse_Page(rand.Next(1, count));
            //load first round
            PopulateBoard(1);

        }

        private void ID_Game(object sender, RoutedEventArgs e)
        {
            int id = GameIDSelection.ShowDialog("Please enter a game id from J-Archive", "");
            Scraper sc = new Scraper();
            CurrentGame = sc.Parse_Page(id);
            PopulateBoard(1);
        }

        private void Close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}