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
        // every piece should know where it lives
        // I could also store the identities of its neighbors,
        // but I think this will be easier for iterating over the set
        // of all pieces during snap detection. Maybe.
        // Maybe I should do both?
        int x;
        int y;
        Bitmap imageMap;
        Point pos;
        public bool northbound;
        public bool southbound;
        public bool eastbound;
        public bool westbound;
        int groupID; // this is for convienience, and slight performance gains
        // does a piece need to know if it is an edge?


        // eventually I'm going to want to suggest dimensions to avoid
        // overly elongated pieces.
        // eventually don't bother with initializing with coords,
        // I'll just randomize their locations in a single method
        // when the time comes
        public Piece(Bitmap image, int x, int y, int posX = 0, int posY = 0)
        {
            this.Image = image; // not confusing at all
            imageMap = image;
            this.x = x;
            this.y = y;
            pos = new Point(posX, posY);
            northbound = false;
            southbound = false;
            eastbound = false;
            westbound = false;
            groupID = -1;
        }

        // I think I need to fake this???
        public void ClickEvent()
        {

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
            return pos;
        }

        public void SetPos(int xPos, int yPos)
        {
            pos.X = xPos;
            pos.Y = yPos;
        }

        public void SetPos(Point newPos)
        {
            pos = newPos;
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
    }

}
