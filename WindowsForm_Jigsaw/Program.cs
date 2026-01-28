using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using ImageProcessor;

namespace WindowsForm_Jigsaw
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
            //Bitmap image = new Bitmap("snakeglasses2.jpg");
            // I don't think anything here after Application.Run();
            // ever gets run
            
            
            // snakeglasses2.jpg is 500 wide/550 high
        }
    }
}
