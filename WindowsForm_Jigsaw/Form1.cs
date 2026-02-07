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
            puzzle = new Puzzle(puzzleFile, x, y, puzzleBox, buffer);
            background = new Bitmap(backgroundFile);
            rand = new Random();
            selectedPiece = false;
            prioritizer = new Dictionary<int, int>();
            InitializeDictionary();
            puzzleBox.Image = background;
            //puzzleBox.BackColor = Color.Transparent;
            //puzzleBox.Controls.Add(cowBox); this adds transparency, but like... um... it's not worth it.

            MovePieces();
            //TestSetup();
            //RenderPuzzle();
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
            cow.DrawTest(buffer);
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
                }
            } // it's that easy

            cow.Visible = true;
            cowBorder.SendToBack();
            //statusMessage.Text = cow.GetPos().ToString();
        }
        
        public void puzzleBox_MouseDown(object sender, MouseEventArgs e)
        {
            // This method might be really stupid now that Piece can send it.
            // Maybe I should unbind mousedown on puzzlebox and assume
            // that a piece called this method.

            //FromGraphicsTest(puzzle.GetPieces()[7]);
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

        #region Experimental
        private void FromGraphicsTest(Piece piece)
        {
            piece.BringToFront();
            Graphics g = Graphics.FromImage(piece.GetImage());
            Brush brush = Brushes.Green;        // comment after testing
            g.FillRectangle(brush, 3, 4, 5, 6); // comment after testing

            Point bindCorner = puzzle.GetImageCoordinates(piece);
            Rectangle bind = new Rectangle(
                    bindCorner.X - buffer,
                    bindCorner.Y - buffer,
                    piece.GetImage().Width + buffer * 2,
                    piece.GetImage().Height + buffer * 2);
            Bitmap image = new Bitmap(puzzleFile);
            brush = new TextureBrush(image, bind);
            piece.Size = new Size(piece.GetImage().Width + buffer * 2, piece.GetImage().Height + buffer * 2);
            piece.Width = piece.Size.Width;
            piece.Height = piece.Size.Height; // this should do nothing... It did. I'm sad I was right.
            
            Rectangle rect = new Rectangle(0, 0 , piece.Size.Width + 5, piece.Size.Height + 5);
            g.FillRectangle(brush, rect);
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

        #region Unused code
        private void cowBox_Paint(object sender, PaintEventArgs e)
        {

        }
        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            string testMe = "hi again";
        }
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // I might need to write code in here to make the cow transparent?
            // this might be the wrong place to write it?
            // maybe anything here will execute WITH the default code
            // rather than REPLACING it?
            if (false)//!gTest) //!(puzzle is null) && !(puzzle.GetPieces() is null) && 
                // consider yourself cancelled.
            {
                //gTest = true;
                Graphics g = e.Graphics; // wat
                Piece guineaPig = puzzle.GetPieces()[4];
                Size oldSize = guineaPig.Size;
                Size newSize = new Size(oldSize.Width + 20, oldSize.Height + 20);
                Point smallCorner = guineaPig.Location;
                Rectangle smallRect = new Rectangle(smallCorner, guineaPig.Size);
                Point largeCorner = new Point(guineaPig.Location.X - 10, guineaPig.Location.Y - 10);
                Rectangle largeRect = new Rectangle(largeCorner, newSize);
                Region negative = new Region(largeRect);
                negative.Exclude(smallRect);
                Point leftKnobPoint = new Point(
                    guineaPig.Location.X - 10, 
                    guineaPig.Location.Y + 10);
                Rectangle leftKnob = new Rectangle(leftKnobPoint, new Size(20, 20));
                negative.Exclude(leftKnob);
                Region r = new Region(largeRect);
                r.Exclude(negative);
                // IT EXISTS
                Rectangle bind = new Rectangle(3, 3, 10, 10);
                System.Drawing.Brush brush;
                Bitmap image = new Bitmap(puzzleFile); //guineaPig.GetImage(); // it runs now. Dang it.
                MemoryStream stream = new MemoryStream();
                BitmapData bitmapData = new BitmapData();
                image.Save(stream, System.Drawing.Imaging.ImageFormat.Png); // a generic error? what?
                // I'm starting to think C# has limitations after all.
                brush = new TextureBrush(Image.FromStream(stream), bind);
                g.FillRegion(brush, r);
                stream.Close();
                puzzle.GetPieces()[4].SendToBack();
                System.Drawing.Brush highlighter = System.Drawing.Brushes.Yellow;
                g.FillRectangle(highlighter, 20, 30, 80, 90);
                Refresh();
            }
        }
        private void puzzleBox_Click(object sender, EventArgs e)
        {
            // the internet says Click events aren't sent until
            // after MouseDown/MouseUp, so this event handler is only here
            // in case I decide that drag-and-drop was a mistake
        }
        private void puzzleBox_Paint(object sender, PaintEventArgs e)
        { // what if I just paste all of that code over here?
            if (false)
            //if (!gTestb) //!(puzzle is null) && !(puzzle.GetPieces() is null) && 
            {   // test over
                //gTestb = true;
                Graphics g = e.Graphics; // wat
                Piece guineaPig = puzzle.GetPieces()[4];
                // well-behaved at 160/142, which equals newSize + buffer
                // well-behaved at any point such that x and y are buffer more than a multiple
                // of newSize
                guineaPig.Location = new Point(160, 264); // comment this after testing
                Size oldSize = new Size(guineaPig.Size.Width, guineaPig.Size.Height);
                Size newSize = new Size(oldSize.Width + buffer * 2, oldSize.Height + buffer * 2);
                Point smallCorner = guineaPig.Location;
                Rectangle smallRect = new Rectangle(smallCorner, oldSize);
                Point largeCorner = new Point(guineaPig.Location.X - buffer, guineaPig.Location.Y - buffer);
                Rectangle largeRect = new Rectangle(largeCorner, newSize);
                Region negative = new Region(largeRect);
                negative.Exclude(smallRect);
                Point leftKnobPoint = new Point(
                    guineaPig.Location.X - buffer,
                    guineaPig.Location.Y + 20);
                // eventually I want leftKnob to be a circle, hence making it too wide
                Rectangle leftKnob = new Rectangle(leftKnobPoint, new Size(buffer * 2, buffer * 2));
                negative.Exclude(leftKnob);
                Region r = new Region(largeRect);
                //r.Exclude(negative);
                Point bindCorner = puzzle.GetImageCoordinates(guineaPig);
                // bind should be the same every time
                Rectangle bind = new Rectangle(
                    bindCorner.X - buffer,
                    bindCorner.Y - buffer,
                    guineaPig.GetImage().Width  + buffer * 2,
                    guineaPig.GetImage().Height + buffer * 2);
                Brush brush;
                Bitmap image = new Bitmap(puzzleFile); //guineaPig.GetImage(); // it runs now. Dang it.
                // IT EXISTS
                brush = new TextureBrush(image, bind);
                g.FillRegion(brush, r); // it finally works! But I made such a mess in the process...

                Piece[] balrogs = puzzle.GetPieces();
                for (int i = 0; i < balrogs.Length; i++)
                    ;// balrogs[i].Visible = false;
                Refresh();
            }
        }
        private void RenderPuzzle(Piece skipMe = null)
        {
            photoBytes = File.ReadAllBytes(backgroundFile);
            ISupportedImageFormat format = new PngFormat();
            ImageLayer layer = new ImageLayer();
            layer.Opacity = 100;
            layer.Position = new Point(0, 0);
            using (MemoryStream inStream = new MemoryStream(photoBytes))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (ImageFactory factory = new ImageFactory())
                    {
                        factory.Load(inStream);
                        for (int i = 0; i < puzzle.GetPieces().Count(); i++)
                        {
                            layer.Image = puzzle.GetPieces()[prioritizer[i]].GetImage();
                            layer.Position = puzzle.GetPieces()[prioritizer[i]].GetPos();
                            // this if statement will probably margianally
                            // slow down this loop, but given that it now only needs
                            // to run on mousedown and mouseup, it really doesn't
                            // matter at all.
                            if (skipMe != puzzle.GetPieces()[prioritizer[i]])
                            //if (skipMe != puzzle.GetPieces()[prioritizer[i]] &&
                            //    skipMe.GetGroupID() != puzzle.GetPieces()[prioritizer[i]].GetGroupID())
                            {
                                if (skipMe == null || skipMe.GetGroupID() == -1)
                                    factory.Overlay(layer);
                                else if (skipMe.GetGroupID() != puzzle.GetPieces()[prioritizer[i]].GetGroupID())
                                    factory.Overlay(layer);
                            }
                        }
                        factory.Format(format).Save(outStream);
                    }
                    // do something
                    puzzleBox.Image = new Bitmap(outStream);
                }
            }
        }
        public void TestSetup()
        {
            statusMessage.Visible = true;
            for (int i = 0; i < puzzle.GetPiecesCount(); i++)
            {
                puzzle.GetPieces()[i].SetPos(
                    rand.Next(650) + 150, 
                    rand.Next(150) + 150);
            }
            // does calling SetPos work... *through* GetPieces()?
            // is this still the same under the hood as when it was
            // puzzle.pieces[0].pos = new Point(5, 5));?
            puzzle.GetPieces()[0].SetPos(5, 5);
            puzzle.GetPieces()[1].SetPos(85, 5);
            puzzle.GetPieces()[2].SetPos(165, 5);
            puzzle.GetPieces()[5].SetPos(85, 85);
        }
        #endregion
    }
}