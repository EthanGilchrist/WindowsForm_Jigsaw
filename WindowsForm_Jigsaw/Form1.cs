using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
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
        // how many pixels away from your target can you be, and still
        // have the pieces snap to each other?
        // this should be dynamic later; more leniant with larger pieces
        const int tolerance = 25;
        const string puzzleFile = "images/snakeglasses4.png";
        const string backgroundFile = "images/blank_600_1200.png";
        const int borderThickness = 2;
        // these two won't always be const, but for now they are
        const int x = 4;
        const int y = 5;

        Bitmap background;
        Bitmap canvas;
        byte[] photoBytes;
        Puzzle puzzle;
        Random rand;
        // todo: implement this
        Dictionary<int, int> prioritizer;
        bool selectedPiece;
        Piece cow;
        Point ranch;
        Point mouseOffset;
        int cowXOffset;
        int cowYOffset;

        public Form1()
        {
            InitializeComponent();
            puzzle = new Puzzle(puzzleFile, x, y, puzzleBox);
            background = new Bitmap(backgroundFile);
            rand = new Random();
            selectedPiece = false;
            prioritizer = new Dictionary<int, int>();
            InitializeDictionary();
            InitializeCanvas();
            puzzleBox.Image = background;
            puzzleBox.BackColor = Color.Transparent;
            //puzzleBox.Controls.Add(cowBox); this adds transparency, but like... um... it's not worth it.

            MovePieces();
            //TestSetup();
            RenderPuzzle();
        }

        private void MovePieces()
        {
            int xMax = puzzleBox.Width - puzzle.GetImage().Width / x;
            int yMax = puzzleBox.Height - puzzle.GetImage().Height / y;
            for (int i = 0; i < puzzle.GetPieces().Count(); i++)
            {
                puzzle.GetPieces()[i].SetPos(rand.Next(xMax), rand.Next(yMax));
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

        private void puzzleBox_Click(object sender, EventArgs e)
        {
            // the internet says Click events aren't sent until
            // after MouseDown/MouseUp, so this event handler is only here
            // in case I decide that drag-and-drop was a mistake
        }

        private void puzzleBox_MouseUp(object sender, MouseEventArgs e)
        {
            // put down the piece we were holding, if any
            if (!selectedPiece)
                // This is normal and will happen every time the background is clicked
                return;
            selectedPiece = false;

            // did we just move a group? This will take care of that.
            int dx = cow.GetPos().X - ranch.X;
            int dy = cow.GetPos().Y - ranch.Y;
            puzzle.Tempt(cow, dx, dy);

            Snap(cow); // so much cleaner in here!

            if (cow.GetGroupID() != -1)
            {
                int piecesSize = puzzle.GetConglomorates()[cow.GetGroupID()].Count();
                Piece[] pieces = new Piece[piecesSize];
                puzzle.GetConglomorates()[cow.GetGroupID()].CopyTo(pieces);
                foreach (Piece piece in pieces)
                {
                    if (piece != cow)
                        Snap(piece); // it can't be that easy
                }
            } // it's that easy

            RenderPuzzle();
            cowBox.Visible = false;
            cowBox.SendToBack();
            cowBorder.SendToBack();
            statusMessage.Text = cow.GetPos().ToString();
        }

        private void puzzleBox_MouseDown(object sender, MouseEventArgs e)
        {
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

            if (selectedPiece)
            {
                RenderPuzzle(cow);
                cowBox.Visible = true;
                if (!PrepareCanvas())
                {   // PrepareCanvas() does this automatically, if it returns true
                    cowBox.Image = cow.GetImage();     
                    cowBox.Location = new Point(  
                        cow.GetPos().X + puzzleBox.Location.X,
                        cow.GetPos().Y + puzzleBox.Location.Y);
                    cowBox.Size = cow.GetImage().Size;
                }
                cowBorder.BringToFront(); // bring border to front FIRST!
                cowBox.BringToFront();
                // activate the cow border
                cowBorder.Visible = true;
                cowBorder.Location = new Point(
                    cowBox.Location.X - borderThickness,
                    cowBox.Location.Y - borderThickness);
                cowBorder.Size = new Size(
                    cowBox.Width  + borderThickness * 2, 
                    cowBox.Height + borderThickness * 2);
            }
        }

        private void puzzleBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!selectedPiece)
                // this line of code will probably run more than any other
                return;
            // move selected piece to position of mouse
            cow.SetPos(e.Location.X - mouseOffset.X, e.Location.Y - mouseOffset.Y);
            // e.Location is probably relative to the window. It BETTER not
            // be relative to the monitor.
            // nope! It's relative to the element! Let's goooo!
            //cow.pos.X -= cow.image.Width / 2;
            //cow.pos.Y -= cow.image.Height / 2;
            cowBox.Location = new Point(
                cow.GetPos().X + puzzleBox.Location.X - cowXOffset, 
                cow.GetPos().Y + puzzleBox.Location.Y - cowYOffset);
            cowBorder.Location = new Point(
                cowBox.Location.X - borderThickness,
                cowBox.Location.Y - borderThickness);
            //cowBox.Location.Offset(puzzleBox.Location);
            //RenderPuzzle();

            // Ok. So. As expected, this was as slow as beans.
            // but I have an idea. What if, I have a dedicated picture box
            // just for the piece I'm currently moving, that turns invisible
            // when it's not being moved?
            // I would have to do a special render without the piece on MouseDown
            // and then shrimply not render for any mousemove events
            // and then render normally on mouseup
            // the only hard part in terms of RenderPuzzle();
            // is not rendering the abducted cow

            // done lol

            // cursed test:
            //PictureBox cursedBox = new PictureBox();
            //cursedBox.Size = cow.GetImage().Size;
            //cursedBox.Location = new Point(rand.Next(100), rand.Next(100));
            //cursedBox.Image = cow.GetImage();
            //cursedBox.Visible = true;
            //cursedBox.BringToFront();
            //puzzleBox.Controls.Add(cursedBox); // oh no. it worked.
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
                target = new Point(buddyPos.X, buddyPos.Y + puzzle.GetPieces()[buddyIndex].GetImage().Height);
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
                target = new Point(buddyPos.X - puzzle.GetPieces()[buddyIndex].GetImage().Width, buddyPos.Y);
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
                target = new Point(buddyPos.X, buddyPos.Y - puzzle.GetPieces()[buddyIndex].GetImage().Height);
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
                target = new Point(buddyPos.X + puzzle.GetPieces()[buddyIndex].GetImage().Width, buddyPos.Y);
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

        private void InitializeCanvas()
        {
            photoBytes = File.ReadAllBytes(puzzleFile);
            ISupportedImageFormat format = new PngFormat(); // note! this is a png now!

            using (MemoryStream inStream = new MemoryStream(photoBytes))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (ImageFactory factory = new ImageFactory())
                    {
                        factory.Load(inStream);
                        factory.Alpha(0);
                        factory.Format(format).Save(outStream);
                    }
                    canvas = new Bitmap(outStream);
                }
            }
        }

        public bool PrepareCanvas()
        {
            // IIRC this code is to create a larger cow
            // it's a bad sign that a week is long enough for me to forget this much

            // I'm going to make an executive decision to not worry about off-by-1 errors
            // until the core functionality is in place

            // alternate strategy: once the bounds of the cow are determined,
            // crop that from the original image, then block out
            // *missing* pieces with white squares
            // that probably won't work without square-shaped pieces though

            if (cow.GetGroupID() == -1)
            {
                cowXOffset = 0;
                cowYOffset = 0;
                return false; // return false if the cow is not part of a conglomorate
            }

            int minX = x;
            int maxX = 0;
            int minY = y;
            int maxY = 0;

            // the list of pieces that will be part of the mega-cow
            List<Piece> pieces = puzzle.GetConglomorates()[cow.GetGroupID()];

            // determine the bounds of the mega-cow
            for (int i = 0; i < puzzle.GetConglomorates()[cow.GetGroupID()].Count(); i++)
            {
                if (pieces[i].GetX() > maxX)
                    maxX = pieces[i].GetX();
                if (pieces[i].GetX() < minX)
                    minX = pieces[i].GetX();
                if (pieces[i].GetY() > maxY)
                    maxY = pieces[i].GetY();
                if (pieces[i].GetY() < minY)
                    minY = pieces[i].GetY();
            }

            photoBytes = File.ReadAllBytes(puzzleFile);
            ImageLayer layer = new ImageLayer();
            layer.Opacity = 100;
            layer.Position = new Point(0, 0);
            ISupportedImageFormat format = new PngFormat(); // note! this is a png now!

            // I stole this from MouseDown(), it might come in handy here
            //   cowBox.Location = new Point(
            //cow.pos.X + puzzleBox.Location.X,
            //cow.pos.Y + puzzleBox.Location.Y);

            using (MemoryStream inStream = new MemoryStream(photoBytes))
            {
                using (MemoryStream outStream = new MemoryStream())
                {
                    using (ImageFactory factory = new ImageFactory())
                    {
                        factory.Load(inStream);
                        factory.Alpha(0);
                        factory.Crop(new Rectangle(new Point(0, 0), puzzle.CowSize(cow)));

                        //List<Piece> pieces = puzzle.conglomorates[cow.groupID];
                        foreach (Piece piece in pieces)
                        {
                            layer.Image = piece.GetImage();
                            layer.Position = new Point( // there's nothing in the rule book that says I can't look at parantheses the same way I currently look at curly brackets
                                (piece.GetX() - minX) * puzzle.GetWidth() / x, 
                                (piece.GetY() - minY) * puzzle.GetHeight() / y
                                );
                            factory.Overlay(layer);
                        }

                        factory.Format(format).Save(outStream);
                    }
                    canvas = new Bitmap(outStream);
                }
            }

            // enlarge the cow to contain the entire conglomorate
            Size cowSize = puzzle.CowSize(cow);
            cowBox.Size = cowSize; // I forgot I wrote a method for this...
            cowBox.Image = canvas;

            // Rewrite this to avoid off-by-one errors later. Use two sequential
            // for loops if you have to (that's all I can think of anyway).
            cowXOffset = (cow.GetX() - minX) * cowSize.Width / (maxX - minX + 1); // that + 1 had to come from the Spirit, noticing that that error was occuring without spending 20 minutes wondering what it could be was too perfect to be anything else
            cowYOffset = (cow.GetY() - minY) * cowSize.Height / (maxY - minY + 1);
            cowBox.Location = new Point(
                cow.GetPos().X + puzzleBox.Location.X - cowXOffset,
                cow.GetPos().Y + puzzleBox.Location.Y - cowYOffset);
            return true;
        }

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

        private void cowBox_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            // I might need to write code in here to make the cow transparent?
            // this might be the wrong place to right it?
            // maybe anything here will execute WITH the default code
            // rather than REPLACING it?
        }
    }
}
