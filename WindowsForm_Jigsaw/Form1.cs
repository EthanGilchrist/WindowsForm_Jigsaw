using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImageProcessor;
using ImageProcessor.Imaging;
using ImageProcessor.Imaging.Formats;

namespace WindowsForm_Jigsaw
{
    public partial class Form1: Form
    {
        #region Fields & Initialization
        // how many pixels away from your target can you be, and still
        // have the pieces snap to each other?
        // this should be dynamic later; more leniant with larger pieces
        const int tolerance = 25;
        const int buffer = 30;
        const string puzzleFile = "images/3by5grid.png";
        const string backgroundFile = "images/blank_600_1200.png";
        const int borderThickness = 2;
        // these two won't always be const, but for now they are
        const int x = 5;
        const int y = 3;

        Bitmap background;
        byte[] photoBytes;
        Puzzle puzzle;
        Random rand;
        // todo: implement this
        Dictionary<int, int> prioritizer;
        bool selectedPiece;
        Piece cow;
        Point ranch;
        Point mouseOffset;

        public Form1()
        {
            InitializeComponent();
            puzzle = new Puzzle(puzzleFile, x, y, puzzleBox);
            background = new Bitmap(backgroundFile);
            rand = new Random();
            selectedPiece = false;
            prioritizer = new Dictionary<int, int>();
            InitializeDictionary();
            puzzleBox.Image = background;
            //puzzleBox.BackColor = Color.Transparent;
            //puzzleBox.Controls.Add(cowBox); this adds transparency, but like... um... it's not worth it.

            MovePieces();
        }
        #endregion

        #region Bread & Butter
        public void puzzleBox_MouseUp(object sender, MouseEventArgs e)
        {
            // put down the piece we were holding, if any
            if (!selectedPiece)
                // This is normal and will happen every time the background is clicked
                return;
            selectedPiece = false;
            //cow.DrawTest(buffer);
            //Refresh();
            // did we just move a group? This will take care of that.
            int dx = cow.GetPos().X - ranch.X;
            int dy = cow.GetPos().Y - ranch.Y;
            puzzle.Tempt(cow, dx, dy);

            Snap(cow); // so much cleaner in here!

            if (cow.GetGroupID() != -1)
            {
                // this for loop was originally written for MouseDown, but it also needs to run here. One or the other isn't enough, I checked.
                for (int j = 0; cow.GetGroupID() != -1 && j < puzzle.GetConglomorates()[cow.GetGroupID()].Count(); j++)
                {
                    BubbleSort(puzzle.GetIndex(puzzle.GetConglomorates()[cow.GetGroupID()][j]));
                    puzzle.GetConglomorates()[cow.GetGroupID()][j].BringToFront();
                }
                int piecesSize = puzzle.GetConglomorates()[cow.GetGroupID()].Count();
                Piece[] pieces = new Piece[piecesSize];
                puzzle.GetConglomorates()[cow.GetGroupID()].CopyTo(pieces);
                foreach (Piece piece in pieces)
                {
                    piece.BringToFront();
                    if (piece != cow)
                        Snap(piece); // it can't be that easy
                }
                if (puzzle.GetConglomorates()[cow.GetGroupID()].Count() == puzzle.GetPiecesCount())
                {
                    statusMessage.Visible = true;
                    statusMessage.Text = "You win!";
                    statusMessage.Width = statusMessage.Text.Length * 10;
                }
            } // it's that easy

            cow.Visible = true;
            cowBorder.SendToBack();
        }
        
        public void puzzleBox_MouseDown(object sender, MouseEventArgs e)
        {
            // This method might be really stupid now that Piece can send it.
            // Maybe I should unbind mousedown on puzzlebox and assume
            // that a piece called this method.

            if (selectedPiece)
            {
                throw new Exception("User clicked, but a piece was already selected!");
                // this has never happened B)
            }
            bool gottem = false;
            Point pos;
            Size size = new Size(puzzle.GetPieces()[0].GetImage().Width, puzzle.GetPieces()[0].GetImage().Height);
            // traverse backwards to collide with whichever piece was rendered LAST
            for (int i = puzzle.GetPiecesCount() - 1; i >= 0 && !gottem; i--)
            {
                pos = puzzle.GetPieces()[prioritizer[i]].GetPos();
                gottem = 
                    pos.X < e.X &&
                    pos.X + size.Width > e.X &&
                    pos.Y < e.Y &&
                    pos.Y + size.Height > e.Y;
                if (gottem)
                {
                    selectedPiece = true;
                    cow = puzzle.GetPieces()[prioritizer[i]];
                    cow.BringToFront();
                    if (cow.GetGroupID() != -1)
                    {
                        List<Piece> conglomorate = puzzle.GetConglomorates()[cow.GetGroupID()];
                        for (int pieceIndex = 0; pieceIndex < conglomorate.Count(); pieceIndex++)
                        {
                            conglomorate[pieceIndex].BringToFront();
                        }
                    }
                    // this better be pass by value
                    ranch = cow.GetPos(); // ok seriously, is it?
                    BubbleSort(prioritizer[i]); // this is redundant if the if statement runs
                    for (int j = 0; cow.GetGroupID() != -1 && j < puzzle.GetConglomorates()[cow.GetGroupID()].Count(); j++)
                    {
                        BubbleSort(puzzle.GetIndex(puzzle.GetConglomorates()[cow.GetGroupID()][j])); // this is surprisingly complicated
                    }
                    mouseOffset = new Point(e.X - pos.X, e.Y - pos.Y);
                }
            }
            if (selectedPiece) ranch = cow.Location;
        }

        public void puzzleBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!selectedPiece)
                // this line of code will probably run more than any other
                return;

            // adding System.Threading.Thread.Sleep(10) before OR after this code
            // makes the jitter way way worse. That's a clue though!
            // move selected piece to position of mouse
            cow.SetPos(e.Location.X - mouseOffset.X, e.Location.Y - mouseOffset.Y);
            int dx = cow.Location.X - ranch.X;
            int dy = cow.Location.Y - ranch.Y;
            ranch = cow.Location;
            puzzle.Tempt(cow, dx, dy);
            //System.Threading.Thread.Sleep(10);
            puzzleBox.Refresh(); // this fixed 90% of the problem even with .sleep enabled
        }

        private void Snap(Piece bull)
        {
            // pretend it's a cow
            int dx = bull.GetPos().X - ranch.X;
            int dy = bull.GetPos().Y - ranch.Y;
            Point bullPos = bull.GetPos();
            int bullIndex = puzzle.GetIndex(bull);
            int buddyIndex = -1;
            Point buddyPos;
            Point target;

            ranch = bull.GetPos(); // I was only doing this on mousedown, but we need to tempt twice in the event of snapping

            // check North. Pieces with index < puzzle width do not have North neighbors.
            if (bullIndex >= x && !bull.northbound)
            {
                buddyIndex = bullIndex - x;
                buddyPos = puzzle.GetPieces()[buddyIndex].GetPos();
                // define a point directly below the piece that will snap to the North edge.
                target = new Point(buddyPos.X, buddyPos.Y + puzzle.GetPieces()[buddyIndex].Height);
                if (Distance(bullPos, target) < tolerance)
                {
                    dx = target.X - ranch.X;
                    dy = target.Y - ranch.Y;
                    puzzle.Bind(bull, puzzle.GetPieces()[buddyIndex], dx, dy);
                    bull.northbound = true;
                    puzzle.GetPieces()[buddyIndex].southbound = true;
                }
            }

            ranch = bull.GetPos();

            // check East
            if (bullIndex % x != x - 1 && !bull.eastbound)
            {
                buddyIndex = bullIndex + 1;
                buddyPos = puzzle.GetPieces()[buddyIndex].GetPos();
                target = new Point(buddyPos.X - bull.Width, buddyPos.Y);
                if (Distance(bullPos, target) < tolerance)
                {
                    dx = target.X - ranch.X;
                    dy = target.Y - ranch.Y;
                    puzzle.Bind(bull, puzzle.GetPieces()[buddyIndex], dx, dy);
                    bull.eastbound = true;
                    puzzle.GetPieces()[buddyIndex].westbound = true;
                }
            }

            ranch = bull.GetPos();

            // check South
            if (bullIndex < puzzle.GetPiecesCount() - x && !bull.southbound)
            {
                buddyIndex = bullIndex + x;
                buddyPos = puzzle.GetPieces()[buddyIndex].GetPos();
                target = new Point(buddyPos.X, buddyPos.Y - bull.Height);
                if (Distance(bullPos, target) < tolerance)
                {
                    dx = target.X - ranch.X;
                    dy = target.Y - ranch.Y;
                    puzzle.Bind(bull, puzzle.GetPieces()[buddyIndex], dx, dy);
                    bull.southbound = true;
                    puzzle.GetPieces()[buddyIndex].northbound = true;
                }
            }

            ranch = bull.GetPos();

            // check West
            if (bullIndex % x != 0 && !bull.westbound)
            {
                buddyIndex = bullIndex - 1;
                buddyPos = puzzle.GetPieces()[buddyIndex].GetPos();
                target = new Point(buddyPos.X + puzzle.GetPieces()[buddyIndex].Width, buddyPos.Y);
                if (Distance(bullPos, target) < tolerance)
                {
                    dx = target.X - ranch.X;
                    dy = target.Y - ranch.Y;
                    puzzle.Bind(bull, puzzle.GetPieces()[buddyIndex], dx, dy);
                    bull.westbound = true;
                    puzzle.GetPieces()[buddyIndex].eastbound = true;
                }
            }
        }
        #endregion

        #region Reliable (subject to change)
        private void BubbleSort(int king)
        {
            // when the user clicks a piece, it needs to render last.
            // this method locates the "king" (freshly clicked piece)
            // and escorts it to the end of the prioritizer dictionary

            // if the user clicked the same piece twice, just go home already.
            if (prioritizer[prioritizer.Count() - 1] == king)
                return;
            bool foundKing = false;
            for (int i = 0; i < prioritizer.Count() - 1; i++)
            {
                if (prioritizer[i] == king)
                    foundKing = true;
                if (foundKing)
                    (prioritizer[i], prioritizer[i + 1]) = (prioritizer[i + 1], prioritizer[i]);
            }
            // priority should be key, piece number should be value?
            // example data
            // 0 0 render piece 0 first
            // 1 1 render piece 1 in the middle
            // 2 2 render piece 2 in the middle
            // 3 3 render piece 3, highest priority!
            // user clicks on piece 1
            // 0 0
            // 1 2
            // 2 3
            // 3 1

            // making prioritizer a dictionary was overkill, huh?
        }

        #endregion

        #region Reliable and also tiny
        private int Distance(Point a, Point b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            dx *= dx;
            dy *= dy;
            return (int)Math.Sqrt(dx + dy);
        }
        private void InitializeDictionary()
        {
            for (int i = 0; i < puzzle.GetPiecesCount(); i++)
            {
                prioritizer.Add(i, i); // (captain!)
            }
        }
        private void MovePieces()
        {
            int xMax = puzzleBox.Width - puzzle.GetImage().Width / x;
            int yMax = puzzleBox.Height - puzzle.GetImage().Height / y;
            for (int i = 0; i < puzzle.GetPieces().Count(); i++)
            {
                puzzle.GetPieces()[i].SetPos(rand.Next(xMax), rand.Next(yMax));
                puzzle.GetPieces()[i].BringToFront();
                BubbleSort(i);
            }
        }
        #endregion

        private void imageLoadButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Images (*.JPG,*.PNG)|*.BMP;*.JPG;*.PNG";
            dialog.ShowDialog();
            puzzle.Kill(this);
            puzzle = new Puzzle(dialog.FileName, x, y, puzzleBox);
            prioritizer = new Dictionary<int, int>();
            InitializeDictionary();
            MovePieces();
            statusMessage.Visible = false;
            //string test = dialog.FileName;
            //using (StreamWriter sr = new StreamWriter("..\\..\\autolog.txt", true))
            //{
            //    sr.WriteLine(test);
            //    sr.WriteLine("test");
            //}
        }
    }
}