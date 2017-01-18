using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
namespace jeopardy
{


    public class JCluesFunctions
    {
        SqlConnectionStringBuilder connectionString =  new SqlConnectionStringBuilder("Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\JClues.mdf;Integrated Security=True");
        SqlConnection connection;
        SqlCommand command;


        //CreateDatabase TO BE IMPLEMENTED
        public void CreateSqlDatabase()
        {
            string path = System.AppDomain.CurrentDomain.BaseDirectory;
            string jclue = path + "JClues.mdf";
            string jclue_log = path + "JClues_log.ldf";


            using (var connection = new System.Data.SqlClient.SqlConnection(
                "Data Source=(LocalDB)\\MSSQLLocalDB;Integrated Security=True;"))
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    string str = String.Format("CREATE DATABASE JClues ON PRIMARY " +
                 "(NAME = JClues, " +
                    "FILENAME = '{0}', " +
                    "SIZE = 2MB, MAXSIZE = UNLIMITED, FILEGROWTH = 10%) " +
                    "LOG ON (NAME = JClues_Log, " +
                    "FILENAME = '{1}', " +
                    "SIZE = 1MB, " +
                    "MAXSIZE =  2048MB, " +
                    "FILEGROWTH = 10%)",jclue,jclue_log);
                    command.CommandText = str;
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch(SqlException e)
                    {
                        command.CommandText = "Drop Database JClues";
                        command.ExecuteNonQuery();
                        command.CommandText = str;
                        command.ExecuteNonQuery();
                    }
                    command.CommandText = "USE JClues";
                    
                    command.ExecuteNonQuery();
                    str = String.Format("CREATE TABLE [dbo].[Clues] (" +
                    "[Id]          INT            IDENTITY (1, 1) NOT NULL," +
                    "[Text]        NVARCHAR (MAX) NOT NULL," +
                    "[Answer]      NVARCHAR (MAX) NOT NULL," +
                    "[Value]       INT            NOT NULL," +
                    "[Category]    NVARCHAR (MAX) NOT NULL," +
                    "[DailyDouble] BIT            NOT NULL," +
                    "[Round]       INT            NOT NULL," +
                    "[Airdate]     SMALLDATETIME  NOT NULL);");
                    command.CommandText = str;
                    command.ExecuteNonQuery();

                }
            }
        }



        //Insert into Database
        private void InsertClue(Clue c,string category,int rnd, string airdate)
        {
            if(c.Clue_Text=="" && c.Clue_Answer == "")
            {
                return;
            }
            string query = "INSERT INTO dbo.Clues (Text,Answer,Value,Category,DailyDouble,Round,Airdate) VALUES (@Text, @Answer,@Value,@Category,@DailyDouble,@Round,@Airdate)";

            using (connection = new SqlConnection(connectionString.ConnectionString))
            using (command = new SqlCommand(query,connection))
            {
                command.Connection.Open();
                command.Parameters.Add("@Text", SqlDbType.NVarChar).Value = c.Clue_Text;
                command.Parameters.Add("@Answer", SqlDbType.NVarChar).Value = c.Clue_Answer;
                command.Parameters.Add("@Value", SqlDbType.Int).Value = c.Clue_Value;
                command.Parameters.Add("@Category", SqlDbType.NVarChar). Value = category;
                command.Parameters.Add("@DailyDouble", SqlDbType.Bit).Value = c.daily_double;
                command.Parameters.Add("@Round", SqlDbType.Int).Value = rnd;
                command.Parameters.Add("@Airdate", SqlDbType.SmallDateTime).Value = airdate;
                command.ExecuteNonQuery();


            }


        }
        private void InsertCategory(Category cat, int rnd, string airdate)
        {
            foreach (Clue c in cat.Clues){
                InsertClue(c, cat.Category_Text, rnd, airdate);
            }
        }
        private void InsertRound(Board b, int rnd, string airdate)
        {
            foreach (Category cat in b.categories)
            {
                InsertCategory(cat, b.round, airdate);
            }
        }
        public void InsertGame(Game g)
        {
            if(g.round1.categories == null && g.round2.categories == null && g.AirDate==null && g.FJ_category == null)
            {
                return;
            }
            InsertRound(g.round1, 1, g.AirDate);
            InsertRound(g.round2, 2, g.AirDate);
            //Final Clue Category(if any)
            if (g.final_jeopardy.Clue_Text==null  && g.FJ_category == "")
            {
                return;
            }
            InsertClue(g.final_jeopardy, g.FJ_category, 3, g.AirDate);
        }

        public DataTable LoadAirdates()
        {
            string query = "SELECT DISTINCT Airdate FROM Clues";
            using (connection = new SqlConnection(connectionString.ConnectionString))
            using (command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable airdates = new DataTable();
                adapter.Fill(airdates);
                airdates.DefaultView.Sort = "Airdate ASC";
                return airdates;
            }
            
        }



        //Load Specific Game From Database
        public Game LoadGame(DateTime airdate)
        {
            Board rnd1, rnd2;
            Clue FJ_Clue;
            String FJ_Cat;
            string query = "SELECT * FROM Clues WHERE Airdate = @Airdate";
            using (connection = new SqlConnection(connectionString.ConnectionString))
            using (command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                //Category[] categories = new Category[6];

                command.Parameters.Add("@Airdate", SqlDbType.SmallDateTime).Value = airdate.ToLongDateString();
                DataTable roundClues = new DataTable();
                adapter.Fill(roundClues);

                //Split Results By Round
                List<DataTable> Rounds = roundClues.AsEnumerable()
                .GroupBy(row => row.Field<int>("Round"))
                .Select(g => g.CopyToDataTable())
                .ToList();

                //Parse Round 1 and 2 into Board
                if (Rounds.Count < 2)
                {
                    //IF EMPTY HTML PAGE (e.g. http://www.j-archive.com/showgame.php?game_id=320)
                    return new Game();
                }
                rnd1 = LoadRound(Rounds[0]);
                rnd2 = LoadRound(Rounds[1]);
                //Parse Rnd3 into a Clue
                if(Rounds.Count == 3)
                {
                    FJ_Clue = rowToClue(Rounds[2].Rows[0]);
                    FJ_Cat = Rounds[2].Rows[0]["Category"].ToString();
                }
                else
                {//no FINAL
                    FJ_Clue = new Clue();
                    FJ_Cat = "NO FINAL JEOPARDY";
                }

            }
            return new Game(rnd1,rnd2, FJ_Clue,FJ_Cat,airdate.ToLongDateString());



        }
        public Clue rowToClue(DataRow row)
        {
            return new jeopardy.Clue(row["Text"].ToString(), row["Answer"].ToString(), Convert.ToInt32(row["Value"].ToString()), Convert.ToBoolean(row["DailyDouble"].ToString()));

        }
        public Board LoadRound(DataTable roundClues)
        {
            List<DataTable> Categories = roundClues.AsEnumerable()
           .GroupBy(row => row.Field<string>("Category"))
           .Select(g => g.CopyToDataTable())
           .ToList();
            int rnd;
            Category[] RndCategories = new Category[Categories.Count];
            for(int i=0; i < Categories.Count;i++ )
            {
                DataTable cat = Categories[i];
                string catName;
                Clue[] catClues = new Clue[5];

                for(int j =0; j<cat.Rows.Count;j++)
                {
                    catClues[j] = rowToClue(cat.Rows[j]);
                }
                catName = Categories[i].Rows[0]["Category"].ToString();
                RndCategories[i] = new Category(catName, catClues);
            }
            rnd = Convert.ToInt32(Categories[0].Rows[0]["Round"].ToString());

            return new jeopardy.Board(rnd, RndCategories);
            /*
            string query = "select* from Clues where Airdate = @Airdate";
            using (connection = new SqlConnection(connectionString.ConnectionString))
            using (command = new SqlCommand(query, connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                Category[] categories = new Category[6];
                command.Parameters.Add("@Round", SqlDbType.Int).Value = rnd;
                command.Parameters.Add("@Airdate", SqlDbType.SmallDateTime).Value = airdate.ToShortDateString();
                DataTable roundClues = new DataTable();
                adapter.Fill(roundClues);

                //Split Results By Category
                List<DataTable> result = roundClues.AsEnumerable()
                .GroupBy(row => row.Field<int>("Category"))
                .Select(g => g.CopyToDataTable())
                .ToList();
                for (int i = 0; i<roundClues.Rows.Count; i++)
                {
                    string text = roundClues.Rows[i]["Text"].ToString();
                    string ans = roundClues.Rows[i]["Answer"].ToString();
                    string cat = roundClues.Rows[i]["Category"].ToString();
                    int val = Convert.ToInt32(roundClues.Rows[i]["Value"].ToString());
                    bool dd = Convert.ToBoolean(Convert.ToInt32(roundClues.Rows[i]["DailyDouble"].ToString()));
                    //need to split into categories
                    
                    new Clue(text, ans, val, dd);
                }

            }

            */
            return new Board();
        }


    }
   
}
