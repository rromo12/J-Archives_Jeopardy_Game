using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jeopardy
{
    public struct Clue
    {
        public Clue(string Text, string Answer, int Value, bool DD)
        {
            Clue_Text = Text;
            Clue_Answer = Answer;
            Clue_Value = Value;
            daily_double = DD;

        }

        public string Clue_Text;
        public string Clue_Answer;
        public int Clue_Value;
        public bool daily_double;

    }
    public struct Category
    {
        public string Category_Text;
        public Clue[] Clues;

        public Category(string text, Clue[] clues)
        {
            Category_Text = text;
            Clues = clues;
        }
    }
    public struct Board
    {
        public int round;
        public Category[] categories;

        public Board(int rnd, Category[] category_array)
        {
            round = rnd;
            categories = category_array;

        }

    }
    public struct Game
    {
        public string AirDate;
        public Board round1;
        public Board round2;
        public Clue final_jeopardy;
        public String FJ_category;
        public Game(Board Rnd1, Board Rnd2, Clue FJ, string FJ_cat, string AD)
        {
            AirDate = AD;
            round1 = Rnd1;
            round2 = Rnd2;
            final_jeopardy = FJ;
            FJ_category = FJ_cat;

        }
    }
}
