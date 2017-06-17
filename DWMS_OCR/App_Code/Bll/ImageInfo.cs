using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;

namespace DWMS_OCR.App_Code.Bll
{
    class ImageInfo
    {
        Bitmap m_bmpRepresentation;

        public void Load(string strImageFile)
        {
            m_bmpRepresentation = new Bitmap(strImageFile, false);
        }

        public void Load(Stream stream)
        {
            m_bmpRepresentation = new Bitmap(stream, false);
        }

        public int Height
        {
            get { return m_bmpRepresentation.Height; }
        }

        public int Width
        {
            get { return m_bmpRepresentation.Width; }
        }

        public string Format
        {
            get
            {
                ImageFormat bmpFormat = m_bmpRepresentation.RawFormat;
                string strFormat = "unidentified format";

                if (bmpFormat.Equals(ImageFormat.Bmp)) strFormat = "BMP";
                else if (bmpFormat.Equals(ImageFormat.Emf)) strFormat = "EMF";
                else if (bmpFormat.Equals(ImageFormat.Exif)) strFormat = "EXIF";
                else if (bmpFormat.Equals(ImageFormat.Gif)) strFormat = "GIF";
                else if (bmpFormat.Equals(ImageFormat.Icon)) strFormat = "Icon";
                else if (bmpFormat.Equals(ImageFormat.Jpeg)) strFormat = "JPEG";
                else if (bmpFormat.Equals(ImageFormat.MemoryBmp)) strFormat = "MemoryBMP";
                else if (bmpFormat.Equals(ImageFormat.Png)) strFormat = "PNG";
                else if (bmpFormat.Equals(ImageFormat.Tiff)) strFormat = "TIFF";
                else if (bmpFormat.Equals(ImageFormat.Wmf)) strFormat = "WMF";

                return strFormat;
            }
        }

        public void Dispose()
        {
            if (m_bmpRepresentation != null)
            {
                m_bmpRepresentation.Dispose();
                m_bmpRepresentation = null;
            }
        }
    }
}
