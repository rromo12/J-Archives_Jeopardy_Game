using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using System.Web;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Net;
using HtmlAgilityPack;

namespace jeopardy
{
    /// <summary>
    /// Interaction logic for Scraper.xaml
    /// </summary>
    public partial class Scraper : Window 
    {
        
        private const int NumberOfRetries = 3;
        private const int DelayOnRetry = 1000;
        JCluesFunctions DB = new JCluesFunctions();

        public Scraper()
        {
            InitializeComponent();
            if (!System.IO.File.Exists("./JClues.mdf"))
            {
                DB.CreateSqlDatabase();
            }
        }

        private async void Download_Click(object sender, RoutedEventArgs e)
        {
            Progress.IsIndeterminate = true;
            //Start Scraping
            int page = 1;
            int num_threads = 20;
            int request_delay = 1000; //in ms
            if (!Directory.Exists("downloaded_games")) {
                Directory.CreateDirectory("downloaded_games");
            }

            while (true) {
                Task<bool>[] taskArray = new Task<bool>[num_threads];
                bool[] results = new bool[taskArray.Length];

                //Start threads/downloads
                for (int i = 0; i<taskArray.Length; i++)
                {
                    taskArray[i] = Task<bool>.Factory.StartNew(() => Download_Page(page++));
                }
                
                //Collect results
                for(int i = 0; i<taskArray.Length; i++)
                {
                    results[i] = taskArray[i].Result;
                }

                //if any false results we have reached the end of the scrape
                if (results.Contains(false))
                {
                    break;
                }
                else
                {
                    
                    Task.WaitAll(taskArray);
                    Label.Content = "Downloaded " + page + "Games";
                    await Task.Delay(request_delay);
                }

            }
            Progress.IsIndeterminate = false;
            return;
        }

        private bool Download_Page(int gameid)
        { //Download page i from j-archives
          
            
            string error = "ERROR: No game";
            string url = "http://www.j-archive.com/showgame.php?game_id=" + gameid;
            string path = "downloaded_games//" + gameid + ".html";
            string html = "";
            using (WebClient client = new WebClient()) {
                for (int i = 0; i < NumberOfRetries; i++)
                {
                    try
                    {
                        html = client.DownloadString(url);
                        break;
                    }
                    catch (WebException e)
                    {
                        // You may check error code to filter some exceptions, not every error
                        // can be recovered.
                        if (i == NumberOfRetries) // Last one, (re)throw exception and exit
                            throw;

                        Thread.Sleep(DelayOnRetry);

                    }
                }
                if (html.Contains(error))
                {
                   //tried to access nonexistent game
                    return false ;
                }


                else
                {
                    for (int i = 1; i <= NumberOfRetries; ++i)
                    {
                        try
                        {
                            // Do stuff with file
                            File.WriteAllText(path, html);
                            break; // When done we can break loop
                        }
                        catch (IOException e)
                        {
                            // You may check error code to filter some exceptions, not every error
                            // can be recovered.
                            if (i == NumberOfRetries) // Last one, (re)throw exception and exit
                                return true;

                            Thread.Sleep(DelayOnRetry);
                        }
                    }
                    return true;
                }
                
            }
        }

        private Board Parse_Round(HtmlNode round_html,int rnd)
        {
            //board contains 5 rows and 6 columns of clues
            Clue[,] board_clues = new Clue[5, 6];
            Category[] board_categories = new Category[6];
            string Clue_Answer;
            string Clue_Text;
            int[] round_values;
            if (rnd == 1)
            {
                 round_values = new int[] { 200, 400, 600, 800, 1000 };
            }
            else
            {
                round_values = new int[] { 400, 800, 1200, 1600, 2000 };
            }
            //Categories
            HtmlNodeCollection round_categories = round_html.SelectNodes("./table[@class='round']/tr/td[@class='category']");
            //Clues
            HtmlNodeCollection round_clues = round_html.SelectNodes("./table[@class='round']/tr/td[@class='clue']"); ;

            int col = 0, row = 0;
            int row_value;
            foreach (HtmlNode clue in round_clues)
            {
                row_value = round_values[row];
                //process clues
                int clues_processed = 0;
                //Implement missing clues behavior; 
                if(clue.InnerHtml != "\n    ")
                {

                    //Clue Text
                    Clue_Text = clue.SelectSingleNode("./table/tr/td[@class='clue_text']").InnerText;

                    //Parse answer
                    string answer_html = clue.SelectSingleNode("./table/tr/td/div").OuterHtml;
                    Clue_Answer = Parse_Answer_Html(answer_html);

                }
                else
                {
                    Clue_Text = "";
                    Clue_Answer = "";

                }
                //Maybe just don't parse clue values and have defaults
                Clue newClue = new Clue(Clue_Text, Clue_Answer, row_value, false);
                //Store into ClueArray at row,col
                board_clues[row, col] = newClue;

                //int Clue_Value = Int32.Parse(clue.SelectSingleNode("./tr/td/div/table/tr/td[@class='clue_value']").InnerText.Trim().TrimStart('$'));

                col++;
                if (col % 6 == 0)
                {
                    col = 0;
                    row++;
                }

                clues_processed++;
            }

            //At this point we have categories  and clues we need to create board
            col = 0;
            row =0;
            //Create Category
            
            foreach (HtmlNode category in round_categories)
            {
                string category_title = category.InnerText.Trim();
                Clue[] category_clues = new Clue[5];
                for(int i = 0; i < 5; i++)
                {
                    category_clues[i] = board_clues[i, col];
                }
                Category cat = new Category(category_title, category_clues);
                col++;
                if (col % 6 == 0)
                {
                    col = 0;
                    row++;
                }
                board_categories[col] = cat;
            }

            return new Board(rnd,board_categories);
        }

        public Game Parse_Page(int gameid,bool online = true)
        {
            Game parsed_game;
            Board rnd1, rnd2;
            Clue final_round_clue;
            string airdate;
            HtmlDocument game_html;
            string path = "downloaded_games//" + gameid + ".html";
            //Parse HTML and return a Game Object
            if (!System.IO.File.Exists(path))
            {
                string url = "http://j-archive.com/showgame.php?game_id=" + gameid;
                HtmlWeb htmlWeb = new HtmlWeb();
                game_html = htmlWeb.Load(url);                
            }
            else
            {
                game_html = new HtmlDocument();
                game_html.Load(path);
            }
            
            string date_pattern = "[0-9]{4}-[0-9]{2}-[0-9]{2}";
            Regex datere = new Regex(date_pattern);
            string date = game_html.DocumentNode.SelectSingleNode("/html/head/title").InnerText;
            airdate = datere.Match(date).Groups[0].Value;
            
            //Get First Round Board
            HtmlNode first_round = game_html.DocumentNode.SelectSingleNode("/html/body/div[@id='content']/div[@id='jeopardy_round']");
            if (first_round != null) {
                 rnd1 = Parse_Round(first_round, 1);
            }
            else
            {
                return new Game();
                //throw new System.NullReferenceException();
            };
            //Second Round Board
            HtmlNode second_round = game_html.GetElementbyId("double_jeopardy_round");
            if (second_round != null)
            {
                rnd2 = Parse_Round(second_round, 2);
            }
            else
            {
                return new Game();
                //throw new System.NullReferenceException();
            };
            
            //Final Jeopardy Clue
            HtmlNode final_round = game_html.DocumentNode.SelectSingleNode("/html/body/div[@id='content']/div[@id='final_jeopardy_round']");
            //Final Jeopardy Category
            if (final_round == null)
            {
                return new Game(rnd1, rnd2, new Clue(), "",airdate);


            }
            string FJ_Clue_Category = final_round.SelectSingleNode("./table/*").InnerText.Trim();
            HtmlNodeCollection FJ_Clue = final_round.SelectNodes("//*[@id='clue_FJ']");
            //Final Jeopardy  Clue Text
            string FJ_Text = final_round.SelectSingleNode("//*[@id='clue_FJ']").InnerText;
            //Final Jeopardy  Parse answer
            HtmlNode answer_node = final_round.SelectSingleNode("./table/tr/td/div");
            string answer_html = WebUtility.HtmlDecode(answer_node.Attributes["onmouseover"].Value);
            string FJ_Answer = Parse_Answer_Html(answer_html);


            //Final Jeopardy Clue Object
           final_round_clue = new Clue(FJ_Text, FJ_Answer, -1, true);

            parsed_game = new Game(rnd1,rnd2,final_round_clue,FJ_Clue_Category,airdate);
            return parsed_game;
        }

        void Commit_Pages(object sender, DoWorkEventArgs e)
        {
            string[] paths = System.IO.Directory.GetFiles("downloaded_games/");
            
            for (int i = 1; i < paths.Length; i++)
            {

                DB.InsertGame(Parse_Page(i));
                (sender as BackgroundWorker).ReportProgress(i);
                //Thread.Sleep(50);
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string[] paths = System.IO.Directory.GetFiles("downloaded_games/");
            Progress.Value = e.ProgressPercentage;
            Label.Content = "Parsing Page: " + (e.ProgressPercentage + 1) + " of " + paths.Length ;

        }

        private string Parse_Answer_Html(string answer_html)
        {
            //Use Regex to parse answer html for answer

            string answer_text = WebUtility.HtmlDecode(answer_html);
            string answer_pattern = "<em.*?>(.*)<\\/em>";
            Regex re = new Regex(answer_pattern);
            string clue_answer;

            if (re.IsMatch(answer_text))
            {
                clue_answer = re.Match(answer_text).Groups[1].Value;

            }
            else
            {
                clue_answer = "Unknown Answer";
            }
            return clue_answer;
        }

        private void Commit_Pages()
        {
            string[] paths = System.IO.Directory.GetFiles("downloaded_games/");
            JCluesFunctions DB = new JCluesFunctions();
            Progress.Maximum = paths.Length;
            for (int i = 1; i < paths.Length; i++) {
               //DB.InsertGame(Parse_Page(i));
                Progress.Value++;
            }
            

        }

        private void Scrape_Click(object sender, RoutedEventArgs e)
        {
            //Progress.BeginInit();
            string[] paths = System.IO.Directory.GetFiles("downloaded_games/");
            Progress.Maximum = paths.Length;

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += Commit_Pages;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerAsync();
            //Commit_Pages();
     
            return;
        }
    }
}
