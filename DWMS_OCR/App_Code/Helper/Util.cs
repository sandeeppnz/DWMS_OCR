using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Web;
using System.IO;
using WebSupergoo.ABCpdf9;
using System.Collections;
using iTextSharp.text.pdf;
using iTextSharp.text;
using DWMS_OCR.App_Code.Bll;
using System.Diagnostics;
using DWMS_OCR.App_Code.Dal;

using System.Drawing.Imaging;
using NHunspell;
using System.Net.Mail;
using org.apache.pdfbox.pdmodel;
using org.apache.pdfbox.util;
using Cyotek.GhostScript.PdfConversion;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Data;
using DWMS_OCR.CdbService;

namespace DWMS_OCR.App_Code.Helper
{
    class Util
    {
        /// <summary>
        /// Split the PDF into individual pages
        /// </summary>
        /// <param name="sourcePdf"></param>
        /// <param name="destFolder"></param>
        /// <param name="fileIndexStart"></param>
        /// <returns></returns>
        public static ArrayList PdfSplit(string sourcePdf, string destFolder, int fileIndexStart)
        {
            ArrayList aPdfFileList = new ArrayList();
            FileInfo file = new FileInfo(sourcePdf);
            string name = file.Name.Substring(0, file.Name.LastIndexOf("."));

            PdfReader reader1 = new PdfReader(sourcePdf);
            try
            {
                PdfDictionary root = reader1.Catalog;
                PdfDictionary documentnames = root.GetAsDict(PdfName.NAMES);
                PdfDictionary embeddedfiles = documentnames.GetAsDict(PdfName.EMBEDDEDFILES);
                PdfArray filespecs = embeddedfiles.GetAsArray(PdfName.NAMES);
                if (filespecs.Size > 0)
                {
                    for (int i = 0; i < filespecs.Size; )
                    {
                        filespecs.GetAsString(i++);
                        PdfDictionary filespec = filespecs.GetAsDict(i++);
                        PdfDictionary refs = filespec.GetAsDict(PdfName.EF);
                        foreach (PdfName key in refs.Keys)
                        {
                            PRStream stream = (PRStream)PdfReader.GetPdfObject(refs.GetAsIndirectObject(key));

                            string fileNameSplit = sourcePdf + filespec.GetAsString(key).ToString();
                            using (FileStream fs = new FileStream(fileNameSplit, FileMode.OpenOrCreate))
                            {
                                byte[] attachment = PdfReader.GetStreamBytes(stream);
                                fs.Write(attachment, 0, attachment.Length);
                            }

                            try
                            {
                                PdfReader reader2 = new PdfReader(fileNameSplit);
                                int pageCount = 0;
                                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

                                reader2.RemoveUnusedObjects();
                                pageCount = reader2.NumberOfPages;

                                for (int pageNo = 1; pageNo <= pageCount; pageNo++)
                                {
                                    string outfile = Path.Combine(fileNameSplit + i + "_" + pageNo.ToString() + ".pdf");

                                    if (!File.Exists(outfile))
                                    {
                                        Document doc = new Document(reader2.GetPageSizeWithRotation(pageNo));

                                        PdfCopy pdfCpy = new PdfCopy(doc, new System.IO.FileStream(outfile, System.IO.FileMode.Create));

                                        try
                                        {
                                            doc.Open();

                                            PdfImportedPage page = pdfCpy.GetImportedPage(reader2, pageNo);  //first page start from 1, NOT 0
                                            pdfCpy.AddPage(page);

                                            aPdfFileList.Add(outfile);
                                        }
                                        catch (Exception)
                                        {
                                        }
                                        finally
                                        {
                                            doc.Close();
                                            pdfCpy.CloseStream = true;
                                            pdfCpy.Close();
                                        }
                                    }
                                    else
                                    {
                                        aPdfFileList.Add(outfile);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                            //finally
                            //{
                            //    reader2.Close();
                            //}
                        }
                    }
                }
                return aPdfFileList;
            }
            catch (Exception)
            {
            }

            try
            {
                //reader1 = new PdfReader(sourcePdf);
                int pageCount = 0;
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();

                reader1.RemoveUnusedObjects();
                pageCount = reader1.NumberOfPages;

                for (int pageNo = 1; pageNo <= pageCount; pageNo++)
                {
                    string outfile = Path.Combine(destFolder, name + "_" + pageNo.ToString() + ".pdf");

                    if (!File.Exists(outfile))
                    {
                        Document doc = new Document(reader1.GetPageSizeWithRotation(pageNo));

                        PdfCopy pdfCpy = new PdfCopy(doc, new System.IO.FileStream(outfile, System.IO.FileMode.Create));

                        try
                        {
                            doc.Open();

                            PdfImportedPage page = pdfCpy.GetImportedPage(reader1, pageNo);  //first page start from 1, NOT 0
                            pdfCpy.AddPage(page);

                            aPdfFileList.Add(outfile);
                        }
                        catch (Exception)
                        {
                        }
                        finally
                        {
                            doc.Close();
                            pdfCpy.CloseStream = true;
                            pdfCpy.Close();
                        }                        
                    }
                    else
                    {
                        aPdfFileList.Add(outfile);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                reader1.Close();
            }
            
            return aPdfFileList;
        }

        /// <summary>
        /// Count the number of pages of the PDF file
        /// </summary>
        /// <param name="sourcePdf"></param>
        /// <returns></returns>
        public static int CountPdfPages(string sourcePdf)
        {
            PdfReader reader = new PdfReader(sourcePdf);

            return reader.NumberOfPages;
        }
        
        /// <summary>
        /// Merge the PDF documents
        /// </summary>
        /// <param name="inputPdfFiles"></param>
        /// <param name="destinationFile"></param>
        public static void MergePdfFiles(ArrayList inputPdfFiles, string destinationFile)
        {
            try
            {
                int f = 0;
                
                PdfReader reader = new PdfReader(inputPdfFiles[f].ToString());
                // we retrieve the total number of pages
                int n = reader.NumberOfPages;
                //Console.WriteLine("There are " + n + " pages in the original file.");

                // step 1: creation of a document-object
                Document document = new Document(reader.GetPageSizeWithRotation(1));

                // step 2: we create a writer that listens to the document
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(destinationFile, FileMode.Create));

                // step 3: we open the document
                document.Open();

                PdfContentByte cb = writer.DirectContent;
                PdfImportedPage page;
                int rotation;

                // step 4: we add content
                while (f < inputPdfFiles.Count)
                {
                    int i = 0;
                    while (i < n)
                    {
                        i++;
                        page = writer.GetImportedPage(reader, i);
                        rotation = reader.GetPageRotation(i);
                        document.SetPageSize(reader.GetPageSizeWithRotation(i));
                        document.NewPage();

                        if (rotation == 90 || rotation == 270)
                        {
                            if (rotation == 90)
                                cb.AddTemplate(page, 0, -1f, 1f, 0, 0, reader.GetPageSizeWithRotation(i).Height);
                            else
                                cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, reader.GetPageSizeWithRotation(i).Width, 0);

                        }
                        else
                        {
                            cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                        }

                    }
                    f++;
                    if (f < inputPdfFiles.Count)
                    {
                        reader = new PdfReader(inputPdfFiles[f].ToString());
                        // we retrieve the total number of pages
                        n = reader.NumberOfPages;
                        //Console.WriteLine("There are " + n + " pages in the original file.");
                    }
                }

                // step 5: we close the document
                writer.CloseStream = true;
                writer.Close();
                document.Close();
            }
            catch (Exception e)
            {
                string strOb = e.Message;
            }
        }

        /// <summary>
        /// Get OCR License
        /// </summary>
        /// <returns></returns>
        public static string GetOcrLicense()
        {
            return ConfigurationManager.AppSettings["OcrLicense"];
        }

        public static bool Logging()
        {
            return (ConfigurationManager.AppSettings["Logging"].Trim().ToUpper() == "TRUE");
        }

        public static bool DetailLogging()
        {
            return (ConfigurationManager.AppSettings["DetailLogging"].Trim().ToUpper() == "TRUE");
        }

        /// <summary>
        /// Returns the file as a byte array
        /// </summary>
        /// <param name="fullFilePath"></param>
        /// <returns></returns>
        public static byte[] FileToBytes(string fullFilePath)
        {
            try
            {
                FileInfo fi = new FileInfo(fullFilePath);

                using (FileStream fileStream = fi.Open(FileMode.Open, FileAccess.Read))
                {
                    byte[] fileBytes = new byte[0];
                    using (BinaryReader binaryReader = new BinaryReader(fileStream))
                    {
                        fileBytes = binaryReader.ReadBytes((Int32)fi.Length);
                    }

                    return fileBytes;
                }
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }

        /// <summary>
        /// Save the file as an image ##function not in use
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string SaveAsTiffImage(string filePath)
        {
            string imagePath = string.Empty;

            using (WebSupergoo.ABCpdf9.Doc doc = new WebSupergoo.ABCpdf9.Doc())
            {
                doc.Read(filePath);

                // set up the rendering parameters tiff file
                //doc.Rendering.ColorSpace = XRendering.ColorSpaceType.Gray;
                //doc.Rendering.SaveCompression = XRendering.Compression.LZW;
                //doc.Rendering.BitsPerChannel = 8;

                // set up the rendering parameters jpg file
                doc.Rendering.ColorSpace = XRendering.ColorSpaceType.Gray;
                doc.Rendering.SaveQuality = 70;
                doc.Rendering.BitsPerChannel = 8;

                long fileSize = 0;
                FileInfo fileInfo = new FileInfo(filePath);

                if (fileInfo.Exists)
                {
                    // Get the file size in KB
                    fileSize = fileInfo.Length / 1024;
                }

                if (fileInfo.Extension.ToUpper().Equals(".PDF"))
                {
                    doc.Rendering.DotsPerInch = 300;
                }
                else
                {
                    // Set the DPI based on the file size
                    if (fileSize <= 256)
                    {
                        doc.Rendering.DotsPerInch = 600;
                    }
                    else if (fileSize > 256 && fileSize <= 512)
                    {
                        doc.Rendering.DotsPerInch = 400;
                    }
                    else
                    {
                        doc.Rendering.DotsPerInch = 200;
                    }
                }

                doc.PageNumber = 1;
                doc.Rect.String = doc.CropBox.String;
                doc.Rendering.SaveAppend = false;
                //doc.SetInfo(0, "ImageCompression", "4");

                imagePath = filePath + "_.jpg";
                doc.Rendering.Save(imagePath);

                //// loop through the pages
                //int n = doc.PageCount;

                //for (int i = 1; i <= n; i++)
                //{
                //    doc.PageNumber = i;
                //    doc.Rect.String = doc.CropBox.String;
                //    doc.Rendering.SaveAppend = (i != 1);
                //    doc.SetInfo(0, "ImageCompression", "4");
                //    doc.Rendering.Save(Server.MapPath(Guid.NewGuid().ToString().ToUpper().Substring(0, 10) + ".png"));
                //}

                doc.Clear();
            }
            return imagePath;
        }

        public static string SaveAsTiffThumbnailImage(string filePath)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            string imagePath = filePath + "_tmp.jpg";

            try
            {
                if (logging) Util.DWMSLog("DWMS_OCR_Service.SaveThumbnail", "try create thumbnail ABCPDF9: " + imagePath, EventLogEntryType.Warning);
                using (WebSupergoo.ABCpdf9.Doc doc = new WebSupergoo.ABCpdf9.Doc())
                {
                    doc.Read(filePath);

                    // set up the rendering parameters
                    doc.PageNumber = 1;
                    //doc.Rendering.SaveCompression = XRendering.Compression.LZW;
                    doc.Rendering.ColorSpace = XRendering.ColorSpaceType.Gray;
                    doc.Rendering.BitsPerChannel = 8;
                    doc.Rendering.SaveQuality = 70;
                    doc.Rect.String = doc.CropBox.String;
                    doc.Rendering.SaveAppend = false;
                    doc.SetInfo(0, "ImageCompression", "4");

                    try
                    {
                        doc.Rendering.Save(imagePath);
                        doc.Clear();
                        doc.Dispose();
                    }
                    catch (Exception ex)
                    {
                        // Log in the windows service log
                        string errorSummary = string.Format("Error (DWMS_OCR_Service.SaveAsTiffThumbnailImage ABCPDF9): File={0}, Message={1}, StackTrace={2}",
                            imagePath, ex.Message, ex.StackTrace);
                        Util.DWMSLog("DWMS_OCR_Service.SaveAsTiffThumbnailImage", errorSummary, EventLogEntryType.Error);
                    }
                    if (logging) Util.DWMSLog("DWMS_OCR_Service.SaveThumbnail", "done create thumbnail ABCPDF9: " + imagePath, EventLogEntryType.Warning);
                }
            }
            catch (Exception exce)
            {
                // Log in the windows service log
                string errorSummaryABC = string.Format("Error (DWMS_OCR_Service.SaveAsTiffThumbnailImage ABCPFD9): File={0}, Message={1}, StackTrace={2}",
                    imagePath, exce.Message, exce.StackTrace);
                Util.DWMSLog("DWMS_OCR_Service.SaveAsTiffThumbnailImage", errorSummaryABC, EventLogEntryType.Error);

                Pdf2ImageSettings pdfSettings = new Pdf2ImageSettings();
                Pdf2Image pdf2Img = new Pdf2Image(filePath);

                pdfSettings.AntiAliasMode = Cyotek.GhostScript.AntiAliasMode.High;
                pdfSettings.GridFitMode = Cyotek.GhostScript.GridFitMode.SkipPatentedInstructions;
                pdfSettings.ImageFormat = Cyotek.GhostScript.ImageFormat.Jpeg;
                pdfSettings.Dpi = 300;

                pdf2Img.PdfFileName = filePath;
                pdf2Img.Settings = pdfSettings;
                Bitmap image;
                Bitmap bm;
                if (logging) Util.DWMSLog("DWMS_OCR_Service.SaveAsTiffThumbnailImage", "try create thumbnail", EventLogEntryType.Warning);
                //Hang for CIMC bank statement
                try
                {
                    pdf2Img.ConvertPdfPageToImage(imagePath, 1);
                    image = pdf2Img.GetImage(1);

                    // set the content type
                    //context.Response.ContentType = "image/png";

                    // save the image directly to the response stream
                    //BEGIN Edited by Edward 08.10.2013
                    //http://social.msdn.microsoft.com/Forums/vstudio/en-US/b15357f1-ad9d-4c80-9ec1-92c786cca4e6/bitmapsave-a-generic-error-occurred-in-gdi?forum=netfxbcl
                    bm = new Bitmap(image);
                    bm.Save(imagePath, ImageFormat.Jpeg);
                    bm.Dispose();
                    image.Dispose();
                    //image.Save(imagePath, ImageFormat.Jpeg);
                    //END
                }
                catch (Exception ex)
                {
                    // Log in the windows service log
                    string errorSummary = string.Format("Error (DWMS_OCR_Service.SaveAsTiffThumbnailImage): File={0}, Message={1}, StackTrace={2}",
                        imagePath, ex.Message, ex.StackTrace);
                    Util.DWMSLog("DWMS_OCR_Service.SaveAsTiffThumbnailImage", errorSummary, EventLogEntryType.Error);
                }
                bm = null;
                image = null;
                pdf2Img = null;
                if (logging) Util.DWMSLog("DWMS_OCR_Service.SaveAsTiffThumbnailImage", "done to create thumbnail", EventLogEntryType.Warning);
            }

            return imagePath;
        }

        //For Secured PDF processing
        public static ArrayList SavePdfToIndividualImage(string filePath)
        {
            ArrayList imagePath = new ArrayList();
            FileInfo file = new FileInfo(filePath);
            string name = file.Name.Substring(0, file.Name.LastIndexOf("."));
            string destFolder = file.Directory.FullName;

            #region Old Implementation using ABCPdf
            //using (WebSupergoo.ABCpdf9.Doc doc = new WebSupergoo.ABCpdf9.Doc())
            //{
            //    doc.Read(filePath);

            //    // set up the rendering parameters
            //    doc.Rendering.ColorSpace = XRendering.ColorSpaceType.Gray;
            //    doc.Rendering.SaveCompression = XRendering.Compression.LZW;
            //    doc.Rendering.BitsPerChannel = 8;

            //    long fileSize = 0;
            //    FileInfo fileInfo = new FileInfo(filePath);

            //    if (fileInfo.Exists)
            //    {
            //        // Get the file size in KB
            //        fileSize = fileInfo.Length / 1024;
            //    }

            //    if (fileInfo.Extension.ToUpper().Equals(".PDF"))
            //    {
            //        dpi = 300;
            //    }
            //    else
            //    {
            //        // Set the DPI based on the file size
            //        if (fileSize <= 256)
            //        {
            //            dpi = 600;
            //        }
            //        else if (fileSize > 256 && fileSize <= 512)
            //        {
            //            dpi = 400;
            //        }
            //        else
            //        {
            //            dpi = 200;
            //        }
            //    }

            //    //doc.PageNumber = 1;
            //    //doc.Rect.String = doc.CropBox.String;
            //    //doc.Rendering.SaveAppend = false;
            //    //doc.SetInfo(0, "ImageCompression", "4");

            //    //string imagePathTemp = filePath + "_.tiff";                
            //    //doc.Rendering.Save(imagePathTemp);
            //    //imagePath.Add(imagePathTemp);

            //    // loop through the pages
            //    int n = doc.PageCount;

            //    for (int pageNo = 1; pageNo <= n; pageNo++)
            //    {
            //        doc.PageNumber = pageNo;
            //        doc.Rect.String = doc.CropBox.String;
            //        doc.Rendering.SaveAppend = false;
            //        doc.SetInfo(0, "ImageCompression", "4");

            //        string outfile = Path.Combine(destFolder, name + "_" + pageNo.ToString() + ".tiff");
            //        //string imagePathTemp = filePath + "_" + i + ".tiff";
            //        imagePath.Add(outfile);

            //        doc.Rendering.Save(outfile);
            //    }

            //    doc.Clear();
            //}
            #endregion

            #region New Implementaion using Ghostscript
            Pdf2ImageSettings pdfSettings = new Pdf2ImageSettings();
            pdfSettings.AntiAliasMode = Cyotek.GhostScript.AntiAliasMode.High;
            pdfSettings.GridFitMode = Cyotek.GhostScript.GridFitMode.None;
            pdfSettings.ImageFormat = Cyotek.GhostScript.ImageFormat.Jpeg;
            pdfSettings.Dpi = 300;
            pdfSettings.DownScaleFactor = 2;

            Pdf2Image pdf2Img = new Pdf2Image(filePath);

            pdf2Img.PdfFileName = filePath;
            //pdf2Img.PdfPassword = pwd;
            pdf2Img.Settings = pdfSettings;

            //long fileSize = 0;
            FileInfo fileInfo = new FileInfo(filePath);
            //int dpi = 200;

            //if (fileInfo.Exists)
            //{
            //    // Get the file size in KB
            //    fileSize = fileInfo.Length / 1024;
            //}

            //// Set the DPI based on the file size
            //if (fileSize <= 256)
            //{
            //    dpi = 600;
            //}
            //else if (fileSize > 256 && fileSize <= 512)
            //{
            //    dpi = 400;
            //}
            //else
            //{
            //    dpi = 200;
            //}

            // if page count is zero then try with abc to create a .tiff file else use pdf2Image to create png file.
            if (pdf2Img.PageCount == 0)
            {
                using (WebSupergoo.ABCpdf9.Doc doc = new WebSupergoo.ABCpdf9.Doc())
                {
                    doc.Read(filePath);

                    // set up the rendering parameters
                    doc.Rendering.ColorSpace = XRendering.ColorSpaceType.Gray;
                    doc.Rendering.BitsPerChannel = 8;
                    doc.Rendering.SaveQuality = 70;

                    //doc.PageNumber = 1;
                    //doc.Rect.String = doc.CropBox.String;
                    //doc.Rendering.SaveAppend = false;
                    //doc.SetInfo(0, "ImageCompression", "4");

                    //string imagePathTemp = filePath + "_.tiff";                
                    //doc.Rendering.Save(imagePathTemp);
                    //imagePath.Add(imagePathTemp);

                    // loop through the pages
                    int n = doc.PageCount;

                    for (int pageNo = 1; pageNo <= n; pageNo++)
                    {
                        doc.PageNumber = pageNo;
                        doc.Rect.String = doc.CropBox.String;
                        doc.Rendering.SaveAppend = false;
                        doc.SetInfo(0, "ImageCompression", "4");

                        string outfile = Path.Combine(destFolder, name + "_" + pageNo.ToString() + ".jpg");
                        //string imagePathTemp = filePath + "_" + i + ".tiff";
                        imagePath.Add(outfile);

                        doc.Rendering.Save(outfile);
                    }
                    doc.Clear();
                }
            }
            else 
            {
                for (int pageNo = 1; pageNo <= pdf2Img.PageCount; pageNo++)
                {
                    // Save the image
                    string tempImagePath = Path.Combine(destFolder, name + "_" + pageNo.ToString() + ".jpg");

                    try
                    {
                        pdf2Img.ConvertPdfPageToImage(tempImagePath, pageNo);
                    }
                    catch (Exception ex)
                    {
                        // Log in the windows service log
                        string errorSummary = string.Format("Error (DWMS_OCR_Service.SaveAsTiffThumbnailImage): File={0}, Message={1}, StackTrace={2}",
                            imagePath, ex.Message, ex.StackTrace);
                        Util.DWMSLog("DWMS_OCR_Service.SaveAsTiffThumbnailImage", errorSummary, EventLogEntryType.Error);
                    }

                    // Add the page path to the list
                    imagePath.Add(tempImagePath);
                }
            }
            #endregion

            return imagePath;
        }

         //<summary>
         //Generate PDF Path By Set ID To Be Used for Email Attachments (Not Used)
         //</summary>
         //<param name="setId"></param>
         //<param name="errorMsg"></param>
         //<returns></returns>
        public static string GeneratePdfPathBySetId(int setId, out string errorMsg)
        {
            string saveDir = ConfigurationManager.AppSettings["TempFolder"].Trim();
            //HttpContext.Current.Server.MapPath("~/App_Data/Temp/");
            errorMsg = string.Empty;

            DocDb docDb = new DocDb();
            DocAppDb docAppDb = new DocAppDb();
            DocSetDb docSetDb = new DocSetDb();
            RawPageDb rawPageDb = new RawPageDb();
            DocTypeDb docTypeDb = new DocTypeDb();

            string rawPageDirPath = ConfigurationManager.AppSettings["RawPageOcrFolder"].Trim();
            //HttpContext.Current.Server.MapPath(Retrieve.GetRawPageOcrDirPath());
            DirectoryInfo rawPageDirInfo = new DirectoryInfo(rawPageDirPath);


            DWMS_OCR.App_Code.Dal.Doc.DocDataTable docTable = docDb.GetDocBySetId(setId);

            ArrayList docList = new ArrayList();
            if (docTable.Rows.Count > 0)
            {
                foreach (DWMS_OCR.App_Code.Dal.Doc.DocRow r in docTable.Rows)
                {
                    ArrayList pageList = new ArrayList();

                    RawPage.RawPageDataTable rawPages = rawPageDb.GetRawPageByDocId(r.Id);
                    for (int cnt = 0; cnt < rawPages.Count; cnt++)
                    {
                        RawPage.RawPageRow rawPage = rawPages[cnt];
                        DirectoryInfo[] rawPageDirs = rawPageDirInfo.GetDirectories(rawPage.Id.ToString());

                        if (rawPageDirs.Length > 0)
                        {
                            DirectoryInfo rawPageDir = rawPageDirs[0];


                            bool useRawPage = false;

                            FileInfo[] rawPagePdfFiles = rawPageDir.GetFiles("*_s.pdf");

                            if (rawPagePdfFiles.Length > 0)
                                pageList.Add(rawPagePdfFiles[0].FullName);
                            else
                                useRawPage = true;

                            if (useRawPage)
                            {
                                FileInfo[] rawPageFiles = rawPageDir.GetFiles();
                                foreach (FileInfo rawPageFile in rawPageFiles)
                                {
                                    if (!rawPageFile.Extension.ToUpper().Equals(".DB") &&
                                        !rawPageFile.Name.ToUpper().EndsWith("_S.PDF") &&
                                        !rawPageFile.Name.ToUpper().EndsWith("_TH.JPG"))
                                    {
                                        if (rawPageFile.Extension.ToUpper().Equals(".PDF"))
                                        {
                                            //path = Util.CreatePdfFileFromImage(path);
                                            pageList.Add(rawPageFile.FullName);
                                            //hasRawPage = true;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!Directory.Exists(saveDir))
                        Directory.CreateDirectory(saveDir);

                    if (pageList.Count > 0)
                    {
                        string docTypeDesc = r.DocTypeCode;

                        DocType.DocTypeDataTable docTypeTable = docTypeDb.GetDocType(r.DocTypeCode, "00");

                        if (docTypeTable.Rows.Count > 0)
                        {
                            DocType.DocTypeRow docType = docTypeTable[0];
                            docTypeDesc = docType.Description;
                        }

                        string mergedFileName = Path.Combine(saveDir, docTypeDesc.Replace("/", "_") + " - " + r.Id.ToString() + ".pdf");

                        try
                        {
                            if (File.Exists(mergedFileName))
                                File.Delete(mergedFileName);
                        }
                        catch (Exception)
                        {
                        }

                        Util.MergePdfFiles(pageList, mergedFileName);

                        docList.Add(mergedFileName);
                    }
                }
            }

            if (docList.Count > 0)
            {
                string setNo = docSetDb.GetSetNumber(setId);

                string mergedFileName = Path.Combine(saveDir, setNo + "_" + Format.FormatDateTime(DateTime.Now, DateTimeFormat.yyMMdd) + ".pdf");

                try { if (File.Exists(mergedFileName)) File.Delete(mergedFileName); }
                catch (Exception) { }

                string errorMessage = string.Empty;
                Util.MergePdfFiles(docList, mergedFileName, out errorMessage);

                if (String.IsNullOrEmpty(errorMessage))
                {
                    FileInfo mergedPdf = new FileInfo(mergedFileName);
                    if (mergedPdf.Exists)
                        return mergedPdf.FullName;
                    else
                    {
                        errorMsg = "Compiled PDF File Cannot Be Found.";
                        return null;
                    }
                }
                else
                {
                    errorMsg = errorMessage;
                    return null;
                }
            }
            else
            {
                errorMsg = "No documents were found.";
                return null;
            }
        }
 
        /// <summary>
        /// Get the full path of the raw page searchable PDF file
        /// </summary>
        /// <param name="rawPageId"></param>
        /// <returns></returns>
        public static string GetRawPageFilePath(int rawPageId)
        {
            string filePath = string.Empty;

            //string docMainDir = Retrieve.GetDocsForOcrDirPath();
            string rawPageMainDir = Retrieve.GetRawPageOcrDirPath();

            //DirectoryInfo mainDir = new DirectoryInfo(docMainDir);
            DirectoryInfo rawPageMainDirInfo = new DirectoryInfo(rawPageMainDir);

            //DirectoryInfo[] rawPageDirInfo = mainDir.GetDirectories(rawPageId.ToString(), SearchOption.AllDirectories);
            DirectoryInfo[] rawPageDirInfo = rawPageMainDirInfo.GetDirectories(rawPageId.ToString());

            if (rawPageDirInfo.Length > 0)
            {
                FileInfo[] rawPageFileInfo = rawPageDirInfo[0].GetFiles("*_s.pdf");

                if (rawPageFileInfo.Length > 0)
                    filePath = rawPageFileInfo[0].FullName;
            }

            return filePath;
        }

        /// <summary>
        /// Get the full path of the sample document directory
        /// </summary>
        /// <param name="sampleDocId"></param>
        /// <returns></returns>
        public static string GetSampleDocDirPath(int sampleDocId)
        {
            string dirPath = string.Empty;

            string docMainDir = Retrieve.GetSampleDocsForOcrDirPath();

            DirectoryInfo mainDir = new DirectoryInfo(docMainDir);

            DirectoryInfo[] sampleDocDirInfo = mainDir.GetDirectories(sampleDocId.ToString(), SearchOption.AllDirectories);

            if (sampleDocDirInfo.Length > 0)
            {
                dirPath = sampleDocDirInfo[0].FullName;
            }

            return dirPath;
        }

        /// <summary>
        /// Create a temporary folder
        /// </summary>
        /// <returns></returns>
        public static string CreateTempFolder()
        {
            string path = Path.Combine(Retrieve.GetTempDirPath(), Guid.NewGuid().ToString());

            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch(Exception)
            {
                path = string.Empty;
            }

            return path;
        }

        // Currently Used Method
        public static string[] SplitString(string text, bool removeNumbers, bool removeEmptyEntries)
        {
            string[] arr;

            #region Old Implementation - Special characters are hard coded
            //char[] delim = { ' ', '\n', '\t', '\r', ',', '.', ';', ':', 
            //'+', '=', '!', '$', '%', '&', '#', '@', '*', '(', ')', '[', ']', 
            //'{', '}', '-', '~','\'', '"', '<', '>', '/', '_', ' ' };

            //char[] delimWithNumbers = { ' ', '\n', '\t', '\r', ',', '.', ';', ':', 
            //'+', '=', '!', '$', '%', '&', '#', '@', '*', '(', ')', '[', ']', 
            //'{', '}', '-', '~','\'', '"', '<', '>', '/', '_', ' ',
            //'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'};

            //if (removeNumbers)
            //{
            //    if (removeEmptyEntries)
            //        arr = text.Split(delimWithNumbers, StringSplitOptions.RemoveEmptyEntries);
            //    else
            //        arr = text.Split(delimWithNumbers);
            //}
            //else
            //{
            //    if (removeEmptyEntries)
            //        arr = text.Split(delim, StringSplitOptions.RemoveEmptyEntries);
            //    else
            //        arr = text.Split(delim);
            //}
            #endregion

            #region New Implementation - Using regular expression to replace special characters
            string pattern = string.Empty;

            if (removeNumbers)
                pattern = "[^a-zA-Z]";
            else
                pattern = "[^a-zA-Z0-9]";

            Regex regex = new Regex(pattern);
            string modText = regex.Replace(text, " ");

            if (removeEmptyEntries)
                arr = modText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            else
                arr = modText.Split(new char[] { ' ' });
            #endregion

            return arr;
        }

        public static void CreateSearcheablePdfFile(string sourceFilePath)
        {
            //ParameterDb parameterDb = new ParameterDb();
            //bool logging = parameterDb.Logging();
            //bool detailLogging = parameterDb.DetailLogging();
            FileInfo file = new FileInfo(sourceFilePath);

            if (file.Exists)
            {
                //if (detailLogging) Util.DWMSLog("DWMS_OCR_Service.CreateSearcheablePdfFile", "OCR hang before copyto", EventLogEntryType.Warning);
                try
                {
                    if (file.Extension.ToLower().Equals(".pdf"))
                    {
                        // Copy the PDF file
                        string newRawPageTempPath = file.FullName + "_s.pdf";
                        file.CopyTo(newRawPageTempPath);
                    }
                    else
                    {
                        // Create a PDF file from the images
                        CreateSearcheablePdfFileFromImage(file.FullName);
                    }
                    //if (detailLogging) Util.DWMSLog("DWMS_OCR_Service.CreateSearcheablePdfFile", "OCR hang after copyto", EventLogEntryType.Warning);
                }
                catch (Exception)
                {
                }
            }
        }

        private static void CreateSearcheablePdfFileFromImage(string sourceFilePath)
        {
            Document doc = new Document();

            try
            {
                string pdfPath = sourceFilePath + "_s.pdf";

                //using (FileStream stream = new FileStream(pdfPath, FileMode.Create))
                //{                    
                    PdfWriter.GetInstance(doc, new FileStream(pdfPath, FileMode.Create));
                //    PdfWriter.GetInstance(doc, stream);
                    doc.Open();
                    iTextSharp.text.Image png = iTextSharp.text.Image.GetInstance(sourceFilePath);
                    png.ScaleToFit(PageSize.A4.Width, PageSize.A4.Height);
                    //png.Colorspace = 1;
                    //png.ScaleToFit(1125f, 1750f);
                    doc.Add(png);
                //}
            }
            catch (Exception)
            {
            }
            finally
            {
                if (doc != null)
                {
                    if (doc.IsOpen())
                        doc.Close();

                    doc = null;
                }
            }
        }

        public static string CreatePdfFileFromOriginalImage(string sourceFilePath)
        {
            string pdfPath = sourceFilePath + "_s.pdf";
            Document doc = new Document(PageSize.A4);
            try
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, new FileStream(pdfPath, FileMode.Create));
                doc.Open();
                iTextSharp.text.Image img = iTextSharp.text.Image.GetInstance(sourceFilePath);
                img.ScaleToFit(PageSize.A4.Width, PageSize.A4.Height);
                img.SetAbsolutePosition(0, PageSize.A4.Top - img.ScaledHeight);
                doc.Add(img);
            }
            catch { return "Error Creating PDF File"; }
            finally
            {
                if (doc != null)
                {
                    if (doc.IsOpen())
                        doc.Close();

                    doc = null;
                }
            }
            return pdfPath;
        }


        public static void DWMSLog(string functionName, string message, EventLogEntryType eventType)
        {
            Log(functionName, message, eventType, Constants.DWMSLogSource, Constants.DWMSLog);
        }

        public static void CDBLog(string functionName, string message, EventLogEntryType eventType)
        {
            Log(functionName, message, eventType, Constants.DWMSCDBLogSource, Constants.DWMSCDBLog);
        }

        public static void CDBDetailLog(string functionName, string message, EventLogEntryType eventType)
        {
            if (CDBVerifyUtil.isDetailLog().ToUpper().Trim() == "TRUE")
                CDBLog(functionName, message, eventType);
        }

        #region Added BY Edward or Leas Service
        public static void LEASLog(string functionName, string message, EventLogEntryType eventType)
        {
            Log(functionName, message, eventType, Constants.DWMSLEASLogSource, Constants.DWMSLEASLog);
        }

        public static void LEASDetailLog(string functionName, string message, EventLogEntryType eventType)
        {
            if (CDBVerifyUtil.isDetailLog().ToUpper().Trim() == "TRUE")
                LEASLog(functionName, message, eventType);
        }
        #endregion


        public static void SampleLog(string functionName, string message, EventLogEntryType eventType)
        {
            Log(functionName, message, eventType, Constants.DWMSSampleLogSource, Constants.DWMSSampleLog);
        }

        public static void MaintenanceLog(string functionName, string message, EventLogEntryType eventType)
        {
            Log(functionName, message, eventType, Constants.DWMSMaintenanceLogSource, Constants.DWMSMaintenanceLog);
        }

        public static void Log(string functionName, string message, EventLogEntryType eventType, string logSrc, string log)
        {
            EventLog eventLog = new EventLog();

            if (!System.Diagnostics.EventLog.SourceExists(logSrc))
            {
                System.Diagnostics.EventLog.CreateEventSource(logSrc, log);
            }

            eventLog.Source = logSrc;
            eventLog.Log = log;

            eventLog.WriteEntry(message, eventType);

            if (eventType == EventLogEntryType.Error)
            {
                ErrorLogDb errorLogDb = new ErrorLogDb();
                errorLogDb.Insert(functionName, message, DateTime.Now);
            }
        }

        public static string FormulateSetNumber(int id, string refNo, string refType, out int departmentId, out int sectionId)
        {
            departmentId = 1;
            sectionId = 1;
            string setNumber = string.Empty;

            DepartmentDb departmentDb = new DepartmentDb();
            SectionDb sectionDb = new SectionDb();

            if (String.IsNullOrEmpty(refNo) && String.IsNullOrEmpty(refType))
            {
                // Use the system account department and section if there is no reference number
                ProfileDb profileDb = new ProfileDb();
                profileDb.GetSystemAccountInfo(out sectionId, out departmentId);
            }
            else
            {
                // Use the reference number to determine the section and department
                // 1 - COS; 2 - SERS; 3 - RESALE; 4 - SALES
                if (refType.Equals(ReferenceTypeEnum.HLE.ToString()))
                {
                    departmentId = 1;
                    sectionId = 1;
                }
                else if (refType.Equals(ReferenceTypeEnum.RESALE.ToString()))
                {
                    departmentId = 3;
                    sectionId = 5;
                }
                else if (refType.Equals(ReferenceTypeEnum.SALES.ToString()))
                {
                    departmentId = 4;
                    sectionId = 6;
                }
                else if (refType.Equals(ReferenceTypeEnum.SERS.ToString()))
                {
                    departmentId = 2;
                    sectionId = 3;
                }
            }

            string departmentCode = string.Empty;
            string businessCode = string.Empty;

            // Get the departmentCode            
            using (Department.DepartmentDataTable deptDt = departmentDb.GetDepartmentById(departmentId))
            {
                if (deptDt.Rows.Count > 0)
                {
                    Department.DepartmentRow deptDr = deptDt[0];
                    departmentCode = deptDr.Code.Trim();
                }
            }

            // Get business code            
            using (DWMS_OCR.App_Code.Dal.Section.SectionDataTable secDt = sectionDb.GetSectionById(sectionId))
            {
                if (secDt.Rows.Count > 0)
                {
                    DWMS_OCR.App_Code.Dal.Section.SectionRow secDr = secDt[0];
                    businessCode = secDr.BusinessCode.Trim();
                }
            }

            setNumber = Format.FormatSetNumber(departmentCode, businessCode, DateTime.Now, id);

            return setNumber;
        }

        public static string GetReferenceType(string referenceNumber)
        {
            if (String.IsNullOrEmpty(referenceNumber))
                return "N.A.";

            string refType = ReferenceTypeEnum.Others.ToString();

            if (Validation.IsHLENumber(referenceNumber))
                refType = ReferenceTypeEnum.HLE.ToString();
            else if (Validation.IsCaseNumber(referenceNumber))
                refType = ReferenceTypeEnum.RESALE.ToString();
            else if (Validation.IsSalesNumber(referenceNumber))
                refType = ReferenceTypeEnum.SALES.ToString();
            else if (Validation.IsSersNumber(referenceNumber))
                refType = ReferenceTypeEnum.SERS.ToString();
            //else if (Validation.IsNric(referenceNumber))
            //    refType = ScanningReferenceTypeEnum.NRIC.ToString();
            else if (Validation.IsNricFormat(referenceNumber))
                refType = ReferenceTypeEnum.NRIC.ToString();

            return refType;
        }

        public static ArrayList TiffSplit(string sourceFile, string destinationFolder)
        {
            ArrayList tiffFileList = new ArrayList();
            FileInfo sourceFilePath = new FileInfo(sourceFile);

            // Get the frame dimension list from the image of the file and 
            using (System.Drawing.Image tiffImage = System.Drawing.Image.FromFile(sourceFile))
            {
                //get the globally unique identifier (GUID) 
                Guid objGuid = tiffImage.FrameDimensionsList[0];
                //create the frame dimension 
                FrameDimension dimension = new FrameDimension(objGuid);
                //Gets the total number of frames in the .tiff file 
                int noOfPages = tiffImage.GetFrameCount(dimension);

                ImageCodecInfo encodeInfo = null;
                ImageCodecInfo[] imageEncoders = ImageCodecInfo.GetImageEncoders();
                for (int j = 0; j < imageEncoders.Length; j++)
                {
                    if (imageEncoders[j].MimeType == "image/tiff")
                    {
                        encodeInfo = imageEncoders[j];
                        break;
                    }
                }

                // Save the tiff file in the output directory. 
                if (!Directory.Exists(destinationFolder))
                    Directory.CreateDirectory(destinationFolder);

                foreach (Guid guid in tiffImage.FrameDimensionsList)
                {
                    for (int index = 0; index < noOfPages; index++)
                    {
                        FrameDimension currentFrame = new FrameDimension(guid);
                        tiffImage.SelectActiveFrame(currentFrame, index);
                        string fileName = string.Concat(destinationFolder, @"\", sourceFilePath.Name, index, ".tiff");
                        string fileNameTemp;
                        tiffImage.Save(fileName, encodeInfo, null);
                        if (tiffImage.VerticalResolution > 300 || tiffImage.HorizontalResolution > 300 || tiffImage.Height > 3508 || tiffImage.Width > 3508 || System.Drawing.Bitmap.GetPixelFormatSize(tiffImage.PixelFormat) == 1)//tiff 1 bit depth cause inverted pdf 
                        {
                            fileNameTemp = fileName;
                            fileName = ImageManager.ReduceSize(fileName);
                            FileInfo fileNameTempPath = new FileInfo(fileNameTemp);
                            fileNameTempPath.Delete();
                        }
                        tiffFileList.Add(fileName);
                    }
                }
            }

            return tiffFileList;
        }

        public static int CountTiffPages(string sourceFile)
        {
            int pageCount = 0;

            //Get the frame dimension list from the image of the file and 
            System.Drawing.Image tiffImage = System.Drawing.Image.FromFile(sourceFile);
            //get the globally unique identifier (GUID) 
            Guid objGuid = tiffImage.FrameDimensionsList[0];
            //create the frame dimension 
            FrameDimension dimension = new FrameDimension(objGuid);
            //Gets the total number of frames in the .tiff file 
            pageCount = tiffImage.GetFrameCount(dimension);

            return pageCount;
        }

        public static bool SendMail(string senderName, string senderEmail, string recipientEmail, string ccEmail, string replyToEmail,
            string subject, string message)
        {
            ParameterDb parameterDb = new ParameterDb();

            char[] delimiterChars = { ';', ',', ':' };
            bool sent = false;
            string from = String.Format("{0} <{1}>", senderName, senderEmail);

            string cr = Environment.NewLine;

            message = message.Replace(cr, "<br />");
            message = message.Replace("\n\r", "<br />");
            message = message.Replace("\r\n", "<br />");
            message = message.Replace("\n", "<br />");
            message = message.Replace("\r", "<br />");

            if (parameterDb.GetParameterValue(ParameterNameEnum.RedirectAllEmailsToTestMailingList).Trim().ToUpper() == "YES")
            {
                subject = subject + " (UAT Email)";
                message += cr + cr + "<br /><br />-----<br /><br />This is a UAT email from DWMS. The original recipients are: "
                    + recipientEmail;

                if (!string.IsNullOrEmpty(ccEmail))
                {
                    message = message + ", CC: " + ccEmail;
                }

                recipientEmail = parameterDb.GetParameterValue(ParameterNameEnum.TestMailingList).Trim();
                ccEmail = string.Empty;
            }

            try
            {
                MailMessage m = new MailMessage();

                // From
                m.From = new MailAddress(from);

                // To
                string[] toEmails = recipientEmail.Split(delimiterChars);
                foreach (string s in toEmails)
                    if (s != null && s.Trim() != string.Empty && Validation.IsEmail(s.Trim()))
                        m.To.Add(new MailAddress(s.Trim()));

                // CC
                if (ccEmail != null && ccEmail.Trim() != string.Empty)
                {
                    string[] ccEmails = ccEmail.Split(delimiterChars);
                    foreach (string s in ccEmails)
                        if (s != null && s.Trim() != string.Empty && Validation.IsEmail(s.Trim()))
                            m.CC.Add(new MailAddress(s.Trim()));
                }

                // Reply
                if (!string.IsNullOrEmpty(replyToEmail))
                {
                    m.ReplyTo = new MailAddress(replyToEmail);
                }

                message = FormatBody(message);

                m.IsBodyHtml = true;
                m.Subject = subject;
                m.Body = message;
                SmtpClient client = new SmtpClient();

                //added by Sandeep 2012-07-20, can be removed once IIS can send the emails
                //client.DeliveryMethod = SmtpDeliveryMethod.Network;
                //

                client.Send(m);
                sent = true;
            }
            catch (Exception ex)
            {
                DWMSLog("SendEmail", ex.Message, EventLogEntryType.Warning);
                // Consider customizing the message for the EmailNotSentPanel in the ShowAds page.
                //HttpContext.Current.Response.Write(ex.Message);
                sent = false;
            }
            return sent;
        }

        public static bool SendMail(string senderName, string senderEmail,
            string recipientEmail, string ccEmail, string bCCEmail, string replyToEmail,
            string subject, string message, string attachments)
        {
            ParameterDb parameterDb = new ParameterDb();

            char[] delimiterChars = { ';', ',', ':' };
            bool sent = false;
            string from = String.Format("{0} <{1}>", senderName, senderEmail);

            string cr = Environment.NewLine;

            message = message.Replace(cr, "<br />");
            message = message.Replace("\n\r", "<br />");
            message = message.Replace("\r\n", "<br />");
            message = message.Replace("\n", "<br />");
            message = message.Replace("\r", "<br />");

            if (parameterDb.GetParameterValue(ParameterNameEnum.RedirectAllEmailsToTestMailingList).Trim().ToUpper() == "YES")
            {
                subject = subject + " (UAT Email)";
                message += cr + cr + "<br /><br />-----<br /><br />This is a UAT email from DWMS. The original recipients are: "
                    + recipientEmail;

                if (!string.IsNullOrEmpty(ccEmail))
                {
                    message = message + ", CC: " + ccEmail;
                }

                //recipientEmail = "lexin.pan@hiend.com;wintwah.toe@hiend.com;peter.zhou@hiend.com;matthew.narca@hiend.com;ns.subashini@hiend.com";
                recipientEmail = parameterDb.GetParameterValue(ParameterNameEnum.TestMailingList).Trim();
                ccEmail = string.Empty;
            }

            try
            {
                MailMessage m = new MailMessage();

                // From
                m.From = new MailAddress(from);

                // To
                string[] toEmails = recipientEmail.Split(delimiterChars);
                foreach (string s in toEmails)
                    if (s != null && s.Trim() != string.Empty && Validation.IsEmail(s.Trim()))
                        m.To.Add(new MailAddress(s.Trim()));

                // CC
                if (ccEmail != null && ccEmail.Trim() != string.Empty)
                {
                    string[] ccEmails = ccEmail.Split(delimiterChars);
                    foreach (string s in ccEmails)
                        if (s != null && s.Trim() != string.Empty && Validation.IsEmail(s.Trim()))
                            m.CC.Add(new MailAddress(s.Trim()));
                }

                // BCC

                if (bCCEmail != null && bCCEmail.Trim() != string.Empty)
                {
                    string[] bCCEmails = bCCEmail.Split(delimiterChars);
                    foreach (string s in bCCEmails)
                        if (s != null && s.Trim() != string.Empty && Validation.IsEmail(s.Trim()))
                            m.Bcc.Add(new MailAddress(s.Trim()));
                }

                // Reply
                if (!string.IsNullOrEmpty(replyToEmail))
                {
                    m.ReplyTo = new MailAddress(replyToEmail);
                }

                //Attachment

                m.Attachments.Clear();

                if (attachments != null && attachments.Trim() != string.Empty)
                {
                    string[] attachmentArray = attachments.Split(';');
                    foreach (string s in attachmentArray)
                        if (s != null && s.Trim() != string.Empty)
                        {
                            if (File.Exists(s))
                            {
                                Attachment attached = new Attachment(s);
                                m.Attachments.Add(attached);
                            }
                        }
                }

                message = FormatBody(message);
                //DWMSLog(string.Empty, String.Format("recipient={0}, from={1}, message={2}", recipientEmail,m.From.ToString(), message), EventLogEntryType.Warning);
                m.IsBodyHtml = true;
                m.Subject = subject;
                m.Body = message;
                SmtpClient client = new SmtpClient();
                client.Timeout = 360000;
                client.Send(m);
                sent = true;
                m.Dispose();
            }
            catch (Exception ex)
            {
                DWMSLog(string.Empty, String.Format("Sending email exception: Message={0}", ex.Message), EventLogEntryType.Warning);
                sent = false;
            }
            finally
            {
                if (attachments != null && attachments.Trim() != string.Empty)
                {
                    string[] attachmentArray = attachments.Split(';');
                    foreach (string s in attachmentArray)
                        if (s != null && s.Trim() != string.Empty)
                        {
                            try
                            {
                                File.Delete(s);
                            }
                            catch (Exception)
                            {
                            }
                        }
                }
            }

            return sent;
        }
        private static string FormatBody(string message)
        {
            //return "<p style=\"Arial, Helvetica, sans-serif; font-size: 12px;\">" + message + "</p>";
            return "<span style=\"font-family: arial,sans-serif; font-size: 10pt;\">" + message + "</span>";
        }

        public static int ExtractTextFromSearcheablePdf(string filePath, int? docSetId, bool isSampleDoc, out string result)
        {
            int errorCode = -1;
            result = string.Empty;

            if (File.Exists(filePath))
            {
                PDDocument doc = null;

                try
                {
                    doc = PDDocument.load(filePath);
                    PDFTextStripper stripper = new PDFTextStripper();

                    #region Added By Edwin
                    //Added on 27 Mar 2014
                    if (doc.isEncrypted())
                    {
                        DWMSLog(string.Empty, "Checking if the PDF is password-encrypted", EventLogEntryType.Information);
                        throw new Exception("This PDF document is password-encrypted");
                    }
                    #endregion

                    result = stripper.getText(doc).Trim();

                    errorCode = (!String.IsNullOrEmpty(result) && CategorizationHelpers.IsValidTextForRelevanceRanking(result)
                        ? 0 : -1);
                }
                catch (Exception ex)
                {
                    DWMSLog(string.Empty, String.Format("Util.ExtractTextFromSearcheablePdf (Warning): File={0}; Message={1}", new FileInfo(filePath).Name, ex.Message),
                        EventLogEntryType.Warning);
                }
                finally
                {
                    if (doc != null)
                        doc.close();
                }
            }

            return errorCode;            
        }

        public static bool IsSecuredPdf(string filePath)
        {
            PdfReader r = new PdfReader(filePath);
            return !r.IsOpenedWithFullPermissions;
           
        }


        public static void GetDownloadlinkAndFileSize(int docId, out string downloadLink, out long fileSize, out string fileName)
        {

            downloadLink = string.Empty;
            fileName = string.Empty;
            fileSize = 0;

            DocDb docDb = new DocDb();
            RawPageDb rawPageDb = new RawPageDb();
            DocTypeDb docTypeDb = new DocTypeDb();

            // RawPage Folder
            string rawPageDirPath = Retrieve.GetRawPageOcrDirPath();
            DirectoryInfo rawPageDirInfo = new DirectoryInfo(rawPageDirPath);

            string saveDir = Retrieve.GetTempDirPath();

          

            ArrayList docList = new ArrayList();

            //foreach (string idStr in ids)
            //{
            int id = docId;

            DWMS_OCR.App_Code.Dal.Doc.DocDataTable docTable = docDb.GetDocById(id);

            if (docTable.Rows.Count > 0)
            {
                DWMS_OCR.App_Code.Dal.Doc.DocRow doc = docTable[0];

                ArrayList pageList = new ArrayList();

                RawPage.RawPageDataTable rawPages = rawPageDb.GetRawPageByDocId(id);
                if (docTable.Rows.Count > 0)
                {
                    for (int cnt = 0; cnt < rawPages.Count; cnt++)
                    {

                        RawPage.RawPageRow rawPage = rawPages[cnt];

                        DirectoryInfo[] rawPageDirs = rawPageDirInfo.GetDirectories(rawPage.Id.ToString());

                        if (rawPageDirs.Length > 0)
                        {
                            DirectoryInfo rawPageDir = rawPageDirs[0];

                            // Get the raw page for download

                            bool useRawPage = false;

                            FileInfo[] rawPagePdfFiles = rawPageDir.GetFiles("*_s.pdf");

                            // If the raw page is not found, use the searcheable PDF
                            if (rawPagePdfFiles.Length > 0)
                                pageList.Add(rawPagePdfFiles[0].FullName);
                            else
                                useRawPage = true;

                            if (useRawPage)
                            {
                                FileInfo[] rawPageFiles = rawPageDir.GetFiles();
                                foreach (FileInfo rawPageFile in rawPageFiles)
                                {
                                    if (!rawPageFile.Extension.ToUpper().Equals(".DB") &&
                                        !rawPageFile.Name.ToUpper().EndsWith("_S.PDF") &&
                                        !rawPageFile.Name.ToUpper().EndsWith("_TH.JPG"))
                                    {
                                        if (rawPageFile.Extension.ToUpper().Equals(".PDF"))
                                        {
                                            //path = Util.CreatePdfFileFromImage(path);
                                            pageList.Add(rawPageFile.FullName);
                                            //hasRawPage = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                

                if (!Directory.Exists(saveDir))
                    Directory.CreateDirectory(saveDir);

                if (pageList.Count > 0)
                {
                    string docTypeDesc = doc.DocTypeCode;

                    DocType.DocTypeDataTable docTypeTable = docTypeDb.GetDocTypeByCode(doc.DocTypeCode);

                    if (docTypeTable.Rows.Count > 0)
                    {
                        DocType.DocTypeRow docType = docTypeTable[0];
                        docTypeDesc = docType.Description;
                    }

                    //string mergedFileName = Path.Combine(saveDir, docTypeDesc.Replace("/", "_") + " - " + doc.Id.ToString() + ".pdf");
                    string mergedFileName = Path.Combine(saveDir, doc.Id.ToString() + ".pdf");

                    try
                    {
                        if (File.Exists(mergedFileName))
                            File.Delete(mergedFileName);
                    }
                    catch (Exception)
                    {
                    }

                    string errorMessage = string.Empty;

                    Util.MergePdfFiles(pageList, mergedFileName, out errorMessage);

                    //docList.Add(mergedFileName);


                    if (String.IsNullOrEmpty(errorMessage))
                    {
                        FileInfo mergedPdf = new FileInfo(mergedFileName);

                        if (mergedPdf.Exists)
                        {
                            fileSize = mergedPdf.Length;
                            //Send only the file name only,
                            //At the DownloadImage.aspx, it will get the path of the temp folder.

                            //downloadLink = Retrieve.GetDownloadImagePageURLVerify() + "?file=" + mergedPdf.FullName;

                            fileName = Path.GetFileName(mergedFileName);

                            downloadLink = CDBVerifyUtil.GetDownloadImagePageURL() + "?file=" + fileName;

                            //File.Delete(mergedFileName);

                        }
                      
                    }
                   


                }
              
            }
            else
            {

            }
        
        }


        public static void MergePdfFiles(ArrayList inputPdfFiles, string destinationFile, out string errorMessage)
        {
            ErrorLogDb errorLogDb = new ErrorLogDb();
            errorMessage = string.Empty;

            PdfReader reader = null;
            Document document = null;
            PdfWriter writer = null;

            try
            {
                int f = 0;

                // if _.tiff_s.pdf is corrupted and for some reason not able to open, then get the main pdf file.
                try
                {
                    reader = new PdfReader(inputPdfFiles[f].ToString());
                }
                catch (Exception exception)
                {
                    reader = new PdfReader(inputPdfFiles[f].ToString().Replace("_.tiff_s.pdf", string.Empty));
                    errorLogDb.Insert(ErrorLogFunctionName.UnableToOpenPDFDocument.ToString(), exception.Message + "---InnerException: " + exception.InnerException + "---StackTrace: " + exception.StackTrace + "---FilePath: " + inputPdfFiles[f].ToString());
                }

                // we retrieve the total number of pages
                int n = reader.NumberOfPages;
                //Console.WriteLine("There are " + n + " pages in the original file.");
                // step 1: creation of a document-object
                document = new Document(reader.GetPageSizeWithRotation(1));
                // step 2: we create a writer that listens to the document
                writer = PdfWriter.GetInstance(document, new FileStream(destinationFile, FileMode.Create));
                // step 3: we open the document
                document.Open();
                PdfContentByte cb = writer.DirectContent;
                PdfImportedPage page;
                int rotation;
                // step 4: we add content
                while (f < inputPdfFiles.Count)
                {
                    int i = 0;
                    while (i < n)
                    {
                        i++;
                        page = writer.GetImportedPage(reader, i);
                        rotation = reader.GetPageRotation(i);
                        document.SetPageSize(reader.GetPageSizeWithRotation(i));
                        //document.SetPageSize(new iTextSharp.text.Rectangle(0.0F, 0.0F, page.Width, page.Height));
                        document.NewPage();

                        if (rotation == 90 || rotation == 270)
                        {
                            if (rotation == 90)
                                cb.AddTemplate(page, 0, -1f, 1f, 0, 0, reader.GetPageSizeWithRotation(i).Height);
                            else
                                cb.AddTemplate(page, 0, 1.0F, -1.0F, 0, reader.GetPageSizeWithRotation(i).Width, 0);
                        }
                        else
                        {
                            cb.AddTemplate(page, 1f, 0, 0, 1f, 0, 0);
                        }

                    }
                    f++;
                    if (f < inputPdfFiles.Count)
                    {
                        // if _.tiff_s.pdf is corrupted and for some reason not able to open, then get the main pdf file.
                        try
                        {
                            reader = new PdfReader(inputPdfFiles[f].ToString());
                        }
                        catch (Exception exception)
                        {
                            reader = new PdfReader(inputPdfFiles[f].ToString().Replace("_.tiff_s.pdf", string.Empty));
                            errorLogDb.Insert(ErrorLogFunctionName.UnableToOpenPDFDocument.ToString(), exception.Message + "---InnerException: " + exception.InnerException + "---StackTrace: " + exception.StackTrace + "---FilePath: " + inputPdfFiles[f].ToString());
                        }

                        // we retrieve the total number of pages
                        n = reader.NumberOfPages;
                        //Console.WriteLine("There are " + n + " pages in the original file.");
                    }
                }
                // step 5: we close the document, writer and reader

                if (document != null)
                    document.Close();
                if (writer != null)
                    writer.Close();
                if (reader != null)
                    reader.Close();

              
            }
            catch (Exception e)
            {
                errorMessage = e.Message + "<br><br>" + e.InnerException + "<br><br>" + e.StackTrace;
                errorLogDb.Insert(ErrorLogFunctionName.MergePDFDocument.ToString(), errorMessage);
               
            }
            finally
            {
                if (document != null)
                    document.Close();
                if (writer != null)
                    writer.Close();
                if (reader != null)
                    reader.Close();

               
            }

           
        }





    }



}
