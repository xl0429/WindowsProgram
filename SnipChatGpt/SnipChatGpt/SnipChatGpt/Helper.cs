using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tesseract;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace SnipChatGpt
{
    public class Helper
    {
         public static Pix BitmapToPix(Bitmap bmp)
        {
            using (var stream = new MemoryStream())
            {
                // Clone as 24bppRgb which Tesseract expects
                using (var clone = bmp.Clone(new Rectangle(0, 0, bmp.Width, bmp.Height), PixelFormat.Format24bppRgb))
                {
                    clone.Save(stream, ImageFormat.Bmp);
                    stream.Position = 0;
                    return Pix.LoadFromMemory(stream.ToArray());
                }
            }
        }

    }
}
