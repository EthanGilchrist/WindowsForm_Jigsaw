using ImageProcessor;
using ImageProcessor.Imaging.Formats;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsForm_Jigsaw
{
    class Puzzle
    {
        // a puzzle needs to know how many pieces it has, as well as the dimensions
        // of the image
        int x; // number of pieces wide
        int y; // number of pieces tall
        int width;   // width of the solution in pixels
        int height; // height of the solution in pixels
        int scaleFactor; // divide the dimensions of large images into something smaller
        string imagePath;
        Piece[] pieces;
        Bitmap image;
        List<List<Piece>> conglomorates;

        public Puzzle(string imagePath, int x, int y, Control puzzleBox)
        {
            this.pieces = new Piece[x * y];
            this.imagePath = imagePath;
            image = new Bitmap(imagePath);
            int judgeWidth = image.Width;
            int judgeHeight = image.Height;
            scaleFactor = 1;
            while (judgeWidth > 600 || judgeHeight > 600)
            {
                scaleFactor++;
                judgeWidth = image.Width / scaleFactor;
                judgeHeight = image.Height / scaleFactor;
            }

            if (scaleFactor != 1) // don't bother if not scaled
                image = new Bitmap(image, image.Width / scaleFactor, image.Height / scaleFactor);

            this.height = image.Height;
            this.width = image.Width;
            this.x = x;
            this.y = y;
            Shatter(imagePath, puzzleBox);
            // bro I spent so long trying to understand what I needed to initialize
            // to make the null reference exceptions go away, and it was that the
            // entire meta-list was unitialized this whole time?
            conglomorates = new List<List<Piece>>();
        }

        #region getters and setters
        public int GetPiecesCount()
        {
            return x * y;
        }

        public int GetWidth()
        { 
            return width;
        }
        public int GetHeight()
        {
            return height;
        }

        public int GetIndex(Piece piece)
        {
            return piece.GetIndex(x);
        }

        public Piece[] GetPieces() // I'm *really* not sure that this one is worth the trouble...
        {
            return pieces;
        }

        public void SetPieces(Piece[] pieces)
        {
            this.pieces = pieces;
        }

        public Bitmap GetImage()
        {
            return image;
        }

        public void SetImage(Bitmap newImage)
        {
            image = newImage;
        }

        public List<List<Piece>> GetConglomorates()
        {
            return conglomorates;
        }

        public void SetConglomorates(List<List<Piece>> conglomorates)
        {
            this.conglomorates = conglomorates;
        }
        #endregion

        public void Bind(Piece a, Piece b, int dx, int dy)
        {
            // assume for now that this method will only
            // be called if it has already been determined
            // that a and b are indeed supposed to connect.

            // the end result is that a and b need to be in the same conglomorate.

            // consider the following cases:
            // 1. this is literally the first connection of the puzzle, and
            //    listlist boy is empty
            // 2. this is the *second* connection of the puzzle, and listlistboy[1] is empty
            // 3. this is the *second* connection of the puzzle, it needs to go in listlistboy[0]
            // 4. two conglomoroonies are joining together
            // 5. the edge case I can't think of
            // I thought of it! make sure to prohibit duplicate bindings.
            // that might have been scary if that hadn't occured to me.
            // 6. the other edge case I can't think of that won't appear for days
            // yep, this one wasn't on track to exist when past me wrote the line above, but
            // when combining two groups, how do I update the north/east/south/westbound bools?

            // the answer to that last one turned out to be... just don't bother?
            // just abort the method early if they're already in the same group
            // and nothing will even notice that anything even happened

            bool accountedFor = false;
            int andex = 0;
            bool bccountedFor = false;
            int bndex = 0;
            for (int i = 0; i < conglomorates.Count(); i++)
            {
                if (conglomorates[i].Contains(a))
                { 
                    accountedFor = true;
                    andex = i;
                }
                    
                if (conglomorates[i].Contains(b))
                {
                    bccountedFor = true;
                    bndex = i;
                }
            }

            if (andex == bndex && accountedFor && bccountedFor)
                return; // this should probably fix itself?
                        //throw new Exception("trying to bind a group to itself!");

            // combining two existing clumps of pieces
            if (accountedFor && bccountedFor)
            {
                for (int i = 0; i < conglomorates[andex].Count(); i++) // snap
                {
                    conglomorates[andex][i].SetPos(
                        conglomorates[andex][i].GetPos().X + dx,
                        conglomorates[andex][i].GetPos().Y + dy);
                }

                //conglomorates[andex] = (List<Piece>)conglomorates[andex].Concat(conglomorates[bndex]);
                // that didn't work...
                for (int i = 0; i < conglomorates[bndex].Count(); i++)
                {
                    conglomorates[bndex][i].AssignToGroup(andex);
                    conglomorates[andex].Add(conglomorates[bndex][i]);
                }

                // if changing this line causes bugs, I'll have to validate
                // the groupID of every piece in the puzzle
                conglomorates[bndex].Clear(); //conglomorates.RemoveAt(bndex);
            }
            // connecting two free-floating pieces
            else if (!accountedFor && !bccountedFor)
            {
                conglomorates.Add(new List<Piece> { a, b });
                a.SetPos(
                    a.GetPos().X + dx, 
                    a.GetPos().Y + dy);
                a.AssignToGroup(conglomorates.Count() - 1);
                b.AssignToGroup(conglomorates.Count() - 1);
            }
            // the next two both connect a free-floater to a group
            else if (accountedFor)
            {
                conglomorates[andex].Add(b);
                b.AssignToGroup(andex);
                b.SetPos(
                    b.GetPos().X - dx, 
                    b.GetPos().Y - dy);
            }
            else 
            { 
                conglomorates[bndex].Add(a);
                a.AssignToGroup(bndex);
                a.SetPos(
                    a.GetPos().X + dx,
                    a.GetPos().Y + dy);
            }

        }

        // now that they've been bound by the ring, the ring can tempt them
        public void Tempt(Piece cow, int dx, int dy)
        {
            bool accountedFor = cow.GetGroupID() != -1;
            int index = cow.GetGroupID();
            if (!accountedFor)
                return;

            for (int i = 0; i < conglomorates[index].Count(); ++i)
            {
                if (cow != conglomorates[index][i])
                {
                    conglomorates[index][i].SetPos(
                        conglomorates[index][i].GetPos().X + dx,
                        conglomorates[index][i].GetPos().Y + dy);
                }
            }
            // ok, so now I need to calculate an offset. How.
            // especially when piece lengths will vary by +- 1 pixel.
            // mmmmmmm
        }

        private void Shatter(string imagePath, Control parent)
        {
            // break an image into individual pieces

            byte[] photoBytes = File.ReadAllBytes(imagePath);
            ISupportedImageFormat format = new JpegFormat { Quality = 90 };
            int temperWidth = image.Width / x;
            int temperHeight = image.Height / y;
            int tempWidth = temperWidth;
            int tempHeight = temperHeight;
            for (int i = 0; i < x * y; i++)
            {
                // what are x and y on each call?
                // x starts at 0 and wraps around when y increments,
                // which is every 1 in this.y calls
                
                // this saves two division operations per loop.
                // is that substantial? no. But it's optimal.
                // born to code on 1975 hardware. Forced to code on 2025 hardware.
                tempWidth = temperWidth;
                tempHeight = temperHeight;
                if ((i % x) * width % x < width % x)
                    tempWidth++;
                if ((i / x) * height % y < height % y)
                    tempHeight++;

                // I hope making all these streams is ok for performance...
                // can I reuse the input stream??? I'm reusing the byte array at least,
                // so everything should be in RAM the whole time.
                // I wish I knew how to find answers about these things.
                using (MemoryStream inStream = new MemoryStream(photoBytes))
                {
                    using (MemoryStream outStream = new MemoryStream())
                    {
                        using (ImageFactory factory = new ImageFactory())
                        {
                            // these calculations will probably break when
                            // the image isn't cleanly divisible by the dimensions
                            // ...
                            // just checked by hand, and good news! there will be gaps,
                            // not crashes. This gets less trivial at lower resolutions
                            // and higher piece counts
                            factory.Load(inStream)
                                   .Resize(new Size(image.Width, image.Height))
                                   .Crop(new Rectangle(
                                       width * (i % x) / x, // distance from left
                                       height * (i / x) / y, // distance from top
                                       tempWidth, // width of piece
                                       tempHeight)) // height of piece
                                   .Format(format)
                                   .Save(outStream);
                        }
                        pieces[i] = new Piece(new Bitmap(outStream), i % x, i / x);
                        pieces[i].MouseDown += new MouseEventHandler(pieces[i].Piece_MouseDown);
                        pieces[i].MouseUp += new MouseEventHandler(pieces[i].Piece_MouseUp);
                        pieces[i].MouseMove += new MouseEventHandler(pieces[i].Piece_MouseMove);
                        parent.Controls.Add(pieces[i]);
                    }
                }
            }
        }

        public Size CowSize(Piece cow)
        {
            if (cow.GetGroupID() == -1)
                return cow.GetImage().Size;

            int minX = x;
            int maxX = 0;
            int minY = y;
            int maxY = 0;
            List<Piece> pieces = conglomorates[cow.GetGroupID()];
            for (int i = 0; i < conglomorates[cow.GetGroupID()].Count(); i++)
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

            // this may or may not be off by up to 2 pixels,
            // but in this case the problem is negligible.
            int cowWidth = width * (1 + maxX - minX) / x; 
            int cowHeight = height * (1 + maxY - minY) / y;

            return new Size(cowWidth, cowHeight);
        }

        public Point GetImageCoordinates(Piece piece)
        {
            int xCoordinate = width  * piece.GetX() / x;
            int yCoordinate = height * piece.GetY() / y;
            return new Point(xCoordinate, yCoordinate);
        }

        public void Kill(Control puzzleBox)
        {
            foreach (Piece piece in pieces)
            {
                puzzleBox.Controls.Remove(piece);
                piece.Dispose();
            }
            foreach (List<Piece> conglomorate in conglomorates)
            {
                conglomorate.Clear();
            }
            conglomorates.Clear();
            image.Dispose();
        }
    }
}
