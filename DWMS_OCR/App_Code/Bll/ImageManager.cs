using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using DWMS_OCR.App_Code.Helper;

namespace DWMS_OCR.App_Code.Bll
{
    class ImageManager
    {
        const double THUMB_WIDTH = 160;
        const double THUMB_HEIGHT = 160;
        const double A4_WIDTH = 3508;
        const double A4_HEIGHT = 3508;

        public static string Resize(string filePath)
        {
            ImageInfo imageInfo = new ImageInfo();
            imageInfo.Load(filePath);
            int imageWidth = imageInfo.Width;
            int imageHeight = imageInfo.Height;

            double ratio;

            if (imageWidth > imageHeight)
            {
                ratio = imageWidth < THUMB_WIDTH ? 1 : THUMB_WIDTH / (double)imageWidth;
            }
            else
            {
                ratio = imageHeight < THUMB_HEIGHT ? 1 : THUMB_HEIGHT / (double)imageHeight;
            }

            int thumbnailWidth = (int)((double)imageWidth * ratio);
            int thumbnailHeight = (int)((double)imageHeight * ratio);

            string thumbnailFilePath = string.Empty;

            try
            {
                using (System.Drawing.Image fullSizeImg = System.Drawing.Image.FromFile(filePath))
                {
                    using (Bitmap bitmap = new Bitmap(fullSizeImg, thumbnailWidth, thumbnailHeight))
                    {
                        ImageCodecInfo[] Info = ImageCodecInfo.GetImageEncoders();

                        using (EncoderParameters Params = new EncoderParameters(1))
                        {
                            Params.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 60L);

                            using (MemoryStream stream = new MemoryStream())
                            {
                                bitmap.Save(stream, Info[1], Params);
                                thumbnailFilePath = filePath + "_th.jpg";
                                bitmap.Save(thumbnailFilePath, Info[1], Params);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                imageInfo.Dispose();
            }

            return thumbnailFilePath;
        }

        public static string ReduceSize(string filePath)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            ImageInfo imageInfo = new ImageInfo();
            imageInfo.Load(filePath);
            int imageWidth = imageInfo.Width;
            int imageHeight = imageInfo.Height;

            double ratio;

            if (imageWidth > imageHeight)
            {
                ratio = imageWidth < A4_WIDTH ? 1 : A4_WIDTH / (double)imageWidth;
            }
            else
            {
                ratio = imageHeight < A4_HEIGHT ? 1 : A4_HEIGHT / (double)imageHeight;
            }

            int A4Width = (int)((double)imageWidth * ratio);
            int A4Height = (int)((double)imageHeight * ratio);

            string A4FilePath = string.Empty;

            try
            {
                using (System.Drawing.Image fullSizeImg = System.Drawing.Image.FromFile(filePath))
                {
                    using (Bitmap bitmap = new Bitmap(fullSizeImg, A4Width, A4Height))
                    {
                        if (detailLogging) Util.DWMSLog("DWMS_OCR_Service.ReduceSize", "Reduce image size: " + filePath, EventLogEntryType.Information);
                        using (Bitmap bitmapGray = MakeGrayscale(bitmap))
                        {
                            ImageCodecInfo CodecInfo = ImageCodecInfo.GetImageEncoders().Where(codec => codec.FormatID.Equals(ImageFormat.Jpeg.Guid)).FirstOrDefault();

                            using (EncoderParameters Params = new EncoderParameters(1))
                            {
                                //Params.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Compression, (long)EncoderValue.CompressionLZW);
                                //Params.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 16L);

                                //Jpeg
                                Params.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 70L);
                                //Params.Param[1] = new EncoderParameter(System.Drawing.Imaging.Encoder.ColorDepth, 8L); no use

                                using (MemoryStream stream = new MemoryStream())
                                {
                                    A4FilePath = filePath + "_re.jpg";
                                    bitmapGray.SetResolution(300f, 300f);
                                    //bitmap.Save(stream, CodecInfo, Params);
                                    //bitmap.Save(A4FilePath, CodecInfo, Params);
                                    if (detailLogging) Util.DWMSLog("DWMS_OCR_Service.ReduceSize", "Reduce image size saving : " + A4FilePath, EventLogEntryType.Warning);
                                    bitmapGray.Save(stream, ImageFormat.Jpeg);
                                    bitmapGray.Save(A4FilePath, CodecInfo, Params);

                                    //A4FilePath = filePath + "_re.Png";
                                    //CodecInfo = ImageCodecInfo.GetImageEncoders().Where(codec => codec.FormatID.Equals(ImageFormat.Png.Guid)).FirstOrDefault();
                                    //bitmapGray.Save(stream, ImageFormat.Png);
                                    //bitmapGray.Save(A4FilePath, CodecInfo, Params);

                                    //A4FilePath = filePath + "_re.Gif";
                                    //CodecInfo = ImageCodecInfo.GetImageEncoders().Where(codec => codec.FormatID.Equals(ImageFormat.Gif.Guid)).FirstOrDefault();
                                    //bitmapGray.Save(stream, ImageFormat.Gif);
                                    //bitmapGray.Save(A4FilePath, CodecInfo, Params);

                                    //A4FilePath = filePath + "_re.Tiff";
                                    //CodecInfo = ImageCodecInfo.GetImageEncoders().Where(codec => codec.FormatID.Equals(ImageFormat.Tiff.Guid)).FirstOrDefault();
                                    //bitmapGray.Save(stream, ImageFormat.Tiff);
                                    //bitmapGray.Save(A4FilePath, CodecInfo, Params);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log in the windows service log
                string errorSummary = string.Format("Error (DWMS_OCR_Service.ReduceSize): File={0}, Message={1}, StackTrace={2}",
                        A4FilePath, ex.Message, ex.StackTrace);
                Util.DWMSLog("DWMS_OCR_Service.PrepareMultiPageTiffForProcessing", errorSummary, EventLogEntryType.Error);
            }
            finally
            {
                imageInfo.Dispose();
            }

            return A4FilePath;
        }

        public static Bitmap MakeGrayscale(Bitmap original)
        {
            //create a blank bitmap the same size as original
            Bitmap newBitmap = new Bitmap(original.Width, original.Height);

            //get a graphics object from the new image
            Graphics g = Graphics.FromImage(newBitmap);

            //create the grayscale ColorMatrix
            ColorMatrix colorMatrix = new ColorMatrix(
               new float[][] 
              {
                 new float[] {.3f, .3f, .3f, 0, 0},
                 new float[] {.59f, .59f, .59f, 0, 0},
                 new float[] {.11f, .11f, .11f, 0, 0},
                 new float[] {0, 0, 0, 1, 0},
                 new float[] {0, 0, 0, 0, 1}
              });

            //create some image attributes
            ImageAttributes attributes = new ImageAttributes();

            //set the color matrix attribute
            attributes.SetColorMatrix(colorMatrix);

            //draw the original image on the new image
            //using the grayscale color matrix
            g.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
               0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);

            //dispose the Graphics object
            g.Dispose();
            return newBitmap;
        }

        public static void Resize(string filePath, int width, int height)
        {
            ThumbnailManager thumbnailManager = new ThumbnailManager();
            System.Drawing.Bitmap bm = thumbnailManager.GetThumbnail(filePath, 113, 160);
            bm.Save(filePath + "_th.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
        }
    }
}
