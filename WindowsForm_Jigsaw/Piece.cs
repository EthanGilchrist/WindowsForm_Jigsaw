using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using ImageProcessor;
using System.Windows.Forms;

namespace WindowsForm_Jigsaw
{
    class Piece : PictureBox
    {
        int x;
        int y;
        Bitmap imageMap;
        public bool northbound;
        public bool southbound;
        public bool eastbound;
        public bool westbound;
        int groupID; // this is for convienience, and slight performance gains
        // does a piece need to know if it is an edge?


        // eventually I'm going to want to suggest dimensions to avoid
        // overly elongated pieces.
        public Piece(Bitmap image, int x, int y, int posX = 0, int posY = 0)
        {
            this.Image = image; // not confusing at all
            imageMap = image;
            this.x = x;
            this.y = y;
            Location = new Point(posX, posY);
            northbound = false;
            southbound = false;
            eastbound = false;
            westbound = false;
            groupID = -1;
            //this.parent = parent; // is this allowed??? It's just a pointer, right?
            // IT'S JUST A POINTER, RIGHT?

            // how was the size being defined before??? I think because I was
            // overlaying images, and detecting clicks with coordinate math
            // based on those same images, the pieces themselves never technically
            // had a size? That is so weird that I never noticed.
            Size = image.Size;

            // and what's weirder, I think I can keep using the existing click
            // detection, and just ditch the RenderPuzzle code in exchange
            // for moving the pieces directly.
        }

        public void DrawTest(int buffer)
        {
            Graphics g = Graphics.FromImage(Image);
            // are you serious
            Rectangle bigRect = new Rectangle(Location, Size);
            Rectangle smallRect = new Rectangle(Location.X + buffer, Location.Y + buffer,
                Size.Width - buffer * 2, Size.Height - buffer * 2);
            Region negative = new Region(bigRect);
            negative.Exclude(smallRect);
            TextureBrush brush = GetTextureBrush(this);
            // cover my bases...
            Brush clear = Brushes.Transparent;
            Brush white = Brushes.White;
            Brush black = Brushes.Black;
            Brush red = Brushes.Red;

            g.FillRegion(red, new Region(bigRect));
            g.FillRegion(brush, negative);
            g.FillRectangle(white, smallRect);
            // this is compiling, but nothing is happening.
            // wait a second.
            g.FillRectangle(black, 2, 3, 4, 5);
            // THAT'S WHY!
            // this.Location gives the position within the board, but it needs to be 0,0 instead.
            // fortunately, that's really easy to change, but I'm going to commit the code
            // in this state for historical purposes.
        }

        static TextureBrush GetTextureBrush(Piece piece)
        {
            // why did I isolate this method? Because it's the bad part, so I need to
            // be able to focus in as hard as I can.
            Bitmap bitmap = new Bitmap(piece.GetImage());
            return new TextureBrush(bitmap);

            // I split:
            // return new TextureBrush(piece.GetImage());
            //
            // into:
            // Bitmap bitmap = new Bitmap(piece.GetImage());
            // return new TextureBrush(bitmap);
            //
            // and it runs now. I'm livid.
        }

        #region ain't broke
        public void Piece_MouseDown(object sender, MouseEventArgs e)
        {
            // ok it's actually working. I can make every piece have a click event, but...
            // would it be easier to bind it to the form and not be declaring 1000 of these
            // every time?
            // I think I will, because I want Puzzle.cs to know what's going on.

            // so. Um. Bad news. Form1_mouseDown does NOT, in fact, capture clicks of
            // its constituent elements. How am I supposed to coordinate with other
            // pieces in a conglomorate?
            // How do I tell the parent class what's going on?
            // is a piece allowed to know about it's parents?
            Point fixedLocation = new Point(    // please work
                e.Location.X + this.Location.X, 
                e.Location.Y + this.Location.Y);
            MouseEventArgs fixedArgs = new MouseEventArgs(
                e.Button, e.Clicks, fixedLocation.X, fixedLocation.Y, e.Delta);
            Program.form.puzzleBox_MouseDown(sender, fixedArgs); // this might be even worse
            // ohhh this doesn't feel right.


            // major flippin' bug discovered!
            // e in this context contains the location of the cursor...
            // RELATIVE TO THE PIECE THAT GOT CLICKED!
            
        }

        public void Piece_MouseUp(object sender, MouseEventArgs e)
        {
            Program.form.puzzleBox_MouseUp(sender, ArgFixer(e));
        }

        public void Piece_MouseMove(object sender, MouseEventArgs e)
        {
            Program.form.puzzleBox_MouseMove(sender, ArgFixer(e));
        }

        public MouseEventArgs ArgFixer(MouseEventArgs e)
        {
            Point fixedLocation = new Point(
                e.Location.X + this.Location.X,
                e.Location.Y + this.Location.Y);
            MouseEventArgs fixedArgs = new MouseEventArgs(
                e.Button, e.Clicks, fixedLocation.X, fixedLocation.Y, e.Delta);
            return fixedArgs;
        }

        public int GetIndex(int puzzleWidth)
        {
            return x + y * puzzleWidth; 
        }

        public int GetX()
        {
            return x;
        }

        public int GetY()
        {
            return y;
        }

        public Point GetPos()
        {
            return Location; // is this even enforcable anymore?
        }

        public void SetPos(int xPos, int yPos)
        {
            Location = new Point(xPos, yPos);
        }

        public void SetPos(Point newPos)
        {
            Location = newPos;
        }

        public void SetXPos(int xPos)
        {
            x = xPos;
        }

        public void SetYPos(int yPos)
        { 
            y = yPos; 
        }

        public Bitmap GetImage()
        {
            // is this stupid?
            return imageMap;
        }

        public int GetGroupID()
        {
            return groupID;
        }

        public void AssignToGroup(int newGroupID)
        {
            groupID = newGroupID;
        }
        #endregion
    }

}
