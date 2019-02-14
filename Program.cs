using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GMM
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
           // InitializeData2(20);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static List<MyPoint> InitializeData2(int datSize)
        {
            //Bitmap bmpOrig = new Bitmap( System.Reflection.Assembly.GetEntryAssembly().GetManifestResourceStream("GMM.Resources.tennis1.jpg"));
            Bitmap bmpOrig = global::GMM.Properties.Resources.tennis1;
            List<MyPoint> PList = new List<MyPoint>();
            PList.Clear();
            int Width = bmpOrig.Width;
            int Height = bmpOrig.Height;
            for (int x = 0; x < Width; x++)
            {
                {
                    for (int y = 0; y < Height; y++)
                    {
                        MyPoint pt = new MyPoint();
                        Color CurrentPixel;
                        CurrentPixel = bmpOrig.GetPixel(x, y);
                        int red = CurrentPixel.R;
                        //System.Diagnostics.Debug.WriteLine(red);
                        int green = CurrentPixel.G;
                        int blue = CurrentPixel.B;
                        pt.red = red;
                        pt.blue = blue;
                        pt.green = green;
                        pt.X = x;
                        pt.Y = y;
                        PList.Add(pt);
                    }
                }
            }
            return PList;
        }
    }
}
