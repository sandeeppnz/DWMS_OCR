using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Aquaforest.OCR.Api;
using System.IO;
using System.Drawing;
using DWMS_OCR.App_Code.Helper;
using System.Diagnostics;
using Aquaforest.OCR.Definitions;
using DWMS_OCR.App_Code.Dal;
using iTextSharp.text.pdf;

namespace DWMS_OCR.App_Code.Bll
{
    class OcrManager
    {
        /// <summary>
        /// Members
        /// </summary>
        private PreProcessor preProcessor;
        private Ocr ocr;

        private string sourceFilePath;
        private int setId;
        private int binarize;
        private int bgFactor;
        private int fgFactor;
        private int quality;
        private string morph;
        private bool dotMatrix;
        private int despeckle;

        //private bool ocrDone;
        private byte ocrRotate;

        private string ocrText;

        public string OcrText
        {
            get { return ocrText; }
            set { ocrText = value; }
        }

        public byte GetRotation()
        {
            return ocrRotate;
        }


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="eventLog"></param>
        //public OcrManager(string sourceFilePath, int setId, int binarize, int bgFactor, int fgFactor, int quality, string morph, bool dotMatrix, int despeckle)
        public OcrManager(string sourceFilePath, int setId, int binarize, int bgFactor, int fgFactor, int quality, string morph, bool dotMatrix, int despeckle)
        {
            preProcessor = new PreProcessor();
            ocr = new Ocr();

            this.sourceFilePath = sourceFilePath;
            this.setId = setId;
            this.binarize = binarize;
            this.bgFactor = bgFactor;
            this.fgFactor = fgFactor;
            this.quality = quality;
            this.morph = morph;
            this.dotMatrix = dotMatrix;
            this.despeckle = despeckle;
            //this.ocrDone = false;

            InitiateOcrEngine();
        }


        #region Edited by Edward 10.07.2013 to Cater JPG Files Added PDF Condition --Commented at 01.11.2013
        /// <summary>
        /// OCR the page 
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="outPdfFilePath"></param>
        /// <param name="outputTextFilePath"></param>
        /// <param name="noPictures"></param>
        /// <returns></returns>
        /// 
        public bool GetOcrText(out string ocrText, out string errorReason, out string errorException)
        {
            ParameterDb parameterDb = new ParameterDb();
            bool logging = parameterDb.Logging();
            bool detailLogging = parameterDb.DetailLogging();
            bool result = false;

            ocrText = string.Empty;
            errorReason = string.Empty;
            errorException = string.Empty;
            long fileSize = 0;

            string fileName = sourceFilePath.ToLower();
            Image image = null;
            try
            {
                result = true;

                // Set up the OCR engine
                #region Reading the Source
                if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Reading Image Source", EventLogEntryType.Information);
                if (fileName.EndsWith(".pdf"))
                {
                    ocr.ReadPDFSource(sourceFilePath);
                }
                else if (fileName.EndsWith(".tif") || fileName.EndsWith(".tiff"))
                {
                    ocr.ReadTIFFSource(sourceFilePath);
                }
                else if (fileName.EndsWith(".bmp"))
                {
                    ocr.ReadBMPSource(sourceFilePath);
                }
                else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif"))
                {
                    try
                    {
                        using (image = Image.FromFile(fileName))//causing out of memory issue
                        {
                            ocr.ReadImageSource(image);
                        }
                    }
                    catch (Exception e)
                    {
                        // Log the error in the windows service log
                        string errorSummary = string.Format("Warning (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
                            , sourceFilePath, e.Message, e.StackTrace);

                        errorReason = "Prepare image for OCR Failed.";
                        errorException = String.Format("File={0}, Message={1}",
                            (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath),
                            e.Message);

                        result = false;
                    }
                }
                else
                {
                    result = false;
                }
                #endregion

                if (result)
                {
                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Carry out the OCR processing", EventLogEntryType.Information);
                    // Carry out the OCR processing
                    if (ocr.Recognize(preProcessor))
                    {

                        // Return the OCR text
                        if (fileName.EndsWith(".pdf"))
                        {
                            #region FOR PDF
                            if (ocr.NumberPages == 1)
                            {
                                try
                                {
                                    ocrText = ocr.ReadDocumentString();

                                    if (ocrText.Length > 0) ocrText = ocr.ReadPageString(1);

                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Information);
                                }
                                catch (Exception e)
                                {
                                    ocrText = ocr.ReadDocumentString();
                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Error);
                                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize failed to read text for file: " + sourceFilePath + "Message: " + e.Message, EventLogEntryType.Error);
                                }

                                FileInfo pdfFile = new FileInfo(sourceFilePath);
                                string pdfPath = sourceFilePath + "_s.pdf";
                                pdfFile.CopyTo(pdfPath, true);

                                FileInfo newSearcheablePdf = new FileInfo(pdfPath);

                                if (newSearcheablePdf.Exists)
                                {
                                    if (newSearcheablePdf.Length == 0)
                                        result = false;
                                }

                                if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Save pdf: " + pdfPath, EventLogEntryType.Information);
                                //if (fileName.EndsWith(".pdf"))
                                if (!File.Exists(pdfPath.ToLower() + "_tmp.jpg_th.jpg"))
                                    File.Delete(pdfPath.ToLower() + "_tmp.jpg_th.jpg");
                                //else
                                if (File.Exists(pdfPath.ToLower() + "_th.jpg"))
                                    File.Delete(pdfPath.ToLower() + "_th.jpg");
                                try
                                {
                                    // Create the thumbnail file
                                    //ImageManager.Resize(newRawPageTempPath, 113, 160);
                                    string tempImagePath = Util.SaveAsTiffThumbnailImage(pdfPath);
                                    if (logging) Util.DWMSLog("DWMS_OCR_Service.GetOcrText", "Done create thumbnail(GetOcrText): " + tempImagePath, EventLogEntryType.Information);
                                    string thumbNailPath = ImageManager.Resize(tempImagePath);

                                    try
                                    {
                                        if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
                                    }
                                    catch
                                    {
                                    }
                                }
                                catch (Exception)
                                {
                                    // Log the error to show in the set action log
                                    LogActionDb logActionDb = new LogActionDb();
                                    logActionDb.Insert(Retrieve.GetSystemGuid(),
                                        LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
                                        LogActionEnum.Thumbnail_Creation_Error.ToString(),
                                        pdfPath.Contains("\\") ? pdfPath.Substring(pdfPath.LastIndexOf("\\") + 1) : pdfPath,
                                        string.Empty, string.Empty, LogTypeEnum.S, setId);
                                }
                            }
                            if (ocr.NumberPages > 1)
                            {
                                try
                                {
                                    ocrText = ocr.ReadDocumentString();
                                    if (ocrText.Length > 0) ocrText = ocr.ReadPageString(1);
                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Information);
                                }
                                catch (Exception e)
                                {
                                    ocrText = ocr.ReadDocumentString();
                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Error);
                                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize failed to read text for file: " + sourceFilePath + "Message: " + e.Message, EventLogEntryType.Error);
                                }

                                // Save the searchable PDF file
                                string pdfPath = sourceFilePath + "_s.pdf";
                                ocr.SavePDFOutput(pdfPath, true);

                                FileInfo newSearcheablePdf = new FileInfo(pdfPath);

                                if (newSearcheablePdf.Exists)
                                {
                                    if (newSearcheablePdf.Length == 0)
                                        result = false;
                                }

                                if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Save pdf: " + pdfPath, EventLogEntryType.Information);
                                //if (fileName.EndsWith(".pdf"))
                                if (!File.Exists(pdfPath.ToLower() + "_tmp.jpg_th.jpg"))
                                    File.Delete(pdfPath.ToLower() + "_tmp.jpg_th.jpg");
                                //else
                                if (File.Exists(pdfPath.ToLower() + "_th.jpg"))
                                    File.Delete(pdfPath.ToLower() + "_th.jpg");
                                try
                                {
                                    // Create the thumbnail file
                                    //ImageManager.Resize(newRawPageTempPath, 113, 160);
                                    string tempImagePath = Util.SaveAsTiffThumbnailImage(pdfPath);
                                    if (logging) Util.DWMSLog("DWMS_OCR_Service.GetOcrText", "Done create thumbnail(GetOcrText): " + tempImagePath, EventLogEntryType.Information);
                                    string thumbNailPath = ImageManager.Resize(tempImagePath);

                                    try
                                    {
                                        if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
                                    }
                                    catch
                                    {
                                    }
                                }
                                catch (Exception)
                                {
                                    // Log the error to show in the set action log
                                    LogActionDb logActionDb = new LogActionDb();
                                    logActionDb.Insert(Retrieve.GetSystemGuid(),
                                        LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
                                        LogActionEnum.Thumbnail_Creation_Error.ToString(),
                                        pdfPath.Contains("\\") ? pdfPath.Substring(pdfPath.LastIndexOf("\\") + 1) : pdfPath,
                                        string.Empty, string.Empty, LogTypeEnum.S, setId);
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            #region for Non-PDF
                            if (ocr.NumberPages > 0)
                            {
                                try
                                {
                                    ocrText = ocr.ReadDocumentString();
                                    if (ocrText.Length > 0) ocrText = ocr.ReadPageString(1);
                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Information);
                                }
                                catch (Exception e)
                                {
                                    ocrText = ocr.ReadDocumentString();
                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Error);
                                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize failed to read text for file: " + sourceFilePath + "Message: " + e.Message, EventLogEntryType.Error);
                                }

                                // Save the searchable PDF file
                                string pdfPath = sourceFilePath + "_s.pdf";
                                ocr.SavePDFOutput(pdfPath, true);

                                FileInfo newSearcheablePdf = new FileInfo(pdfPath);

                                if (newSearcheablePdf.Exists)
                                {
                                    if (newSearcheablePdf.Length == 0)
                                        result = false;
                                }

                                if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Save pdf: " + pdfPath, EventLogEntryType.Information);
                                //if (fileName.EndsWith(".pdf"))
                                if (!File.Exists(pdfPath.ToLower() + "_tmp.jpg_th.jpg"))
                                    File.Delete(pdfPath.ToLower() + "_tmp.jpg_th.jpg");
                                //else
                                if (File.Exists(pdfPath.ToLower() + "_th.jpg"))
                                    File.Delete(pdfPath.ToLower() + "_th.jpg");
                                try
                                {
                                    // Create the thumbnail file
                                    //ImageManager.Resize(newRawPageTempPath, 113, 160);
                                    string tempImagePath = Util.SaveAsTiffThumbnailImage(pdfPath);
                                    if (logging) Util.DWMSLog("DWMS_OCR_Service.GetOcrText", "Done create thumbnail(GetOcrText): " + tempImagePath, EventLogEntryType.Information);
                                    string thumbNailPath = ImageManager.Resize(tempImagePath);

                                    try
                                    {
                                        if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
                                    }
                                    catch
                                    {
                                    }
                                }
                                catch (Exception)
                                {
                                    // Log the error to show in the set action log
                                    LogActionDb logActionDb = new LogActionDb();
                                    logActionDb.Insert(Retrieve.GetSystemGuid(),
                                        LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
                                        LogActionEnum.Thumbnail_Creation_Error.ToString(),
                                        pdfPath.Contains("\\") ? pdfPath.Substring(pdfPath.LastIndexOf("\\") + 1) : pdfPath,
                                        string.Empty, string.Empty, LogTypeEnum.S, setId);
                                }
                            }
                            #endregion
                        }


                        //to rotate the original raw file, according to autorotated value, generated by OCR, saved into RawFiles table.
                        #region Rotate
                        if (ocrRotate > 0)
                        {
                            if (fileName.EndsWith(".pdf"))
                            {
                                string tempPdfPath = sourceFilePath + ".temp";
                                PdfReader reader = new PdfReader(sourceFilePath);
                                using (FileStream fs = new FileStream(tempPdfPath, FileMode.Create))
                                {
                                    PdfStamper stamper = new PdfStamper(reader, fs);
                                    PdfDictionary pageDictionary = reader.GetPageN(1); //only get first page (raw page only 1 page)
                                    int desiredRot = ocrRotate * 90;
                                    PdfNumber rotation = pageDictionary.GetAsNumber(PdfName.ROTATE);
                                    if (rotation != null)
                                    {
                                        desiredRot += rotation.IntValue;
                                        desiredRot %= 360;
                                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Rotate pdf image: " + fileName + " degree: " + desiredRot, EventLogEntryType.Warning);
                                    }
                                    pageDictionary.Put(PdfName.ROTATE, new PdfNumber(desiredRot));
                                    stamper.Close();
                                }
                                File.Replace(tempPdfPath, sourceFilePath, sourceFilePath + ".backup");
                                if (File.Exists(sourceFilePath + ".backup")) File.Delete(sourceFilePath + ".backup");
                            }
                            else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif") || fileName.EndsWith(".tif") || fileName.EndsWith(".tiff") || fileName.EndsWith(".bmp"))
                            {
                                using (image = Image.FromFile(fileName))
                                {
                                    switch (ocrRotate)
                                    {
                                        case 1:
                                            image.RotateFlip(RotateFlipType.Rotate90FlipNone);
                                            image.Save(fileName);
                                            break;
                                        case 2:
                                            image.RotateFlip(RotateFlipType.Rotate180FlipNone);
                                            image.Save(fileName);
                                            break;
                                        case 3:
                                            image.RotateFlip(RotateFlipType.Rotate270FlipNone);
                                            image.Save(fileName);
                                            break;
                                    }
                                    if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Rotate image: " + fileName + " degree: " + ocrRotate * 90, EventLogEntryType.Warning);
                                }
                            }
                        }
                        #endregion
                    }
                    else
                        Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize fail", EventLogEntryType.Warning);

                }
            }
            catch (Exception e)
            {
                // Log the error in the windows service log
                string errorSummary = string.Format("Warning (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
                    , sourceFilePath, e.Message, e.StackTrace);
                Util.DWMSLog("OcrManager.GetOcrText", errorSummary, EventLogEntryType.Warning);

                // Log the error to show in the set action log
                LogActionDb logActionDb = new LogActionDb();
                logActionDb.Insert(Retrieve.GetSystemGuid(),
                    LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3.ToString(),
                    LogActionEnum.File_Error.ToString(),
                    e.Message,
                    sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath,
                    string.Empty, LogTypeEnum.S, setId);

                //// Log the exception for the directory
                //multiple exception log
                errorReason = "OCR of file failed.";
                errorException = String.Format("File={0}, Message={1}",
                    (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath),
                    e.Message);

                result = false;
            }
            finally
            {
                if (image != null)
                    image.Dispose();

                // Delete the temporary files
                ocr.DeleteTemporaryFiles();
            }

            return result;
        }
        #endregion

        #region Edited By Edward 03.10.2013 to Cater Marriage certificate issue, just added a number of pages condition - Commented by Edward 07.10.2013
        ///// <summary>
        ///// OCR the page 
        ///// </summary>
        ///// <param name="sourceFilePath"></param>
        ///// <param name="outPdfFilePath"></param>
        ///// <param name="outputTextFilePath"></param>
        ///// <param name="noPictures"></param>
        ///// <returns></returns>
        ///// 
        //public bool GetOcrText(out string ocrText, out string errorReason, out string errorException)
        //{
        //    ParameterDb parameterDb = new ParameterDb();
        //    bool logging = parameterDb.Logging();
        //    bool detailLogging = parameterDb.DetailLogging();
        //    bool result = false;

        //    ocrText = string.Empty;
        //    errorReason = string.Empty;
        //    errorException = string.Empty;
        //    long fileSize = 0;

        //    string fileName = sourceFilePath.ToLower();
        //    Image image = null;
        //    try
        //    {
        //        result = true;

        //        // Set up the OCR engine
        //        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Reading Image Source", EventLogEntryType.Information);
        //        if (fileName.EndsWith(".pdf"))
        //        {
        //            ocr.ReadPDFSource(sourceFilePath);
        //        }
        //        else if (fileName.EndsWith(".tif") || fileName.EndsWith(".tiff"))
        //        {
        //            ocr.ReadTIFFSource(sourceFilePath);
        //        }
        //        else if (fileName.EndsWith(".bmp"))
        //        {
        //            ocr.ReadBMPSource(sourceFilePath);
        //        }
        //        else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif"))
        //        {
        //            try
        //            {
        //                using (image = Image.FromFile(fileName))//causing out of memory issue
        //                {
        //                    ocr.ReadImageSource(image);
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                // Log the error in the windows service log
        //                string errorSummary = string.Format("Warning (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
        //                    , sourceFilePath, e.Message, e.StackTrace);

        //                errorReason = "Prepare image for OCR Failed.";
        //                errorException = String.Format("File={0}, Message={1}",
        //                    (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath),
        //                    e.Message);

        //                result = false;
        //            }
        //        }
        //        else
        //        {
        //            result = false;
        //        }

        //        if (result)
        //        {
        //            if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Carry out the OCR processing", EventLogEntryType.Information);
        //            // Carry out the OCR processing
        //            if (ocr.Recognize(preProcessor))
        //            {

        //                // Return the OCR text
        //                if (ocr.NumberPages == 1)
        //                {
        //                    try
        //                    {
        //                        ocrText = ocr.ReadDocumentString();

        //                        if (ocrText.Length > 0) ocrText = ocr.ReadPageString(1);

        //                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Information);
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        ocrText = ocr.ReadDocumentString();
        //                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Error);
        //                        if (logging) Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize failed to read text for file: " + sourceFilePath + "Message: " + e.Message, EventLogEntryType.Error);
        //                    }

        //                    FileInfo pdfFile = new FileInfo(sourceFilePath);
        //                    string pdfPath = sourceFilePath + "_s.pdf";
        //                    pdfFile.CopyTo(pdfPath, true);

        //                    FileInfo newSearcheablePdf = new FileInfo(pdfPath);

        //                    if (newSearcheablePdf.Exists)
        //                    {
        //                        if (newSearcheablePdf.Length == 0)
        //                            result = false;
        //                    }

        //                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Save pdf: " + pdfPath, EventLogEntryType.Information);
        //                    //if (fileName.EndsWith(".pdf"))
        //                    if (!File.Exists(pdfPath.ToLower() + "_tmp.jpg_th.jpg"))
        //                        File.Delete(pdfPath.ToLower() + "_tmp.jpg_th.jpg");
        //                    //else
        //                    if (File.Exists(pdfPath.ToLower() + "_th.jpg"))
        //                        File.Delete(pdfPath.ToLower() + "_th.jpg");
        //                    try
        //                    {
        //                        // Create the thumbnail file
        //                        //ImageManager.Resize(newRawPageTempPath, 113, 160);
        //                        string tempImagePath = Util.SaveAsTiffThumbnailImage(pdfPath);
        //                        if (logging) Util.DWMSLog("DWMS_OCR_Service.GetOcrText", "Done create thumbnail(GetOcrText): " + tempImagePath, EventLogEntryType.Information);
        //                        string thumbNailPath = ImageManager.Resize(tempImagePath);

        //                        try
        //                        {
        //                            if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
        //                        }
        //                        catch
        //                        {
        //                        }
        //                    }
        //                    catch (Exception)
        //                    {
        //                        // Log the error to show in the set action log
        //                        LogActionDb logActionDb = new LogActionDb();
        //                        logActionDb.Insert(Retrieve.GetSystemGuid(),
        //                            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
        //                            LogActionEnum.Thumbnail_Creation_Error.ToString(),
        //                            pdfPath.Contains("\\") ? pdfPath.Substring(pdfPath.LastIndexOf("\\") + 1) : pdfPath,
        //                            string.Empty, string.Empty, LogTypeEnum.S, setId);
        //                    }
        //                }

        //                if (ocr.NumberPages > 1)
        //                {
        //                    try
        //                    {
        //                        ocrText = ocr.ReadDocumentString();
        //                        if (ocrText.Length > 0) ocrText = ocr.ReadPageString(1);
        //                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Information);
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        ocrText = ocr.ReadDocumentString();
        //                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Error);
        //                        if (logging) Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize failed to read text for file: " + sourceFilePath + "Message: " + e.Message, EventLogEntryType.Error);
        //                    }

        //                    // Save the searchable PDF file
        //                    string pdfPath = sourceFilePath + "_s.pdf";
        //                    ocr.SavePDFOutput(pdfPath, true);

        //                    FileInfo newSearcheablePdf = new FileInfo(pdfPath);

        //                    if (newSearcheablePdf.Exists)
        //                    {
        //                        if (newSearcheablePdf.Length == 0)
        //                            result = false;
        //                    }

        //                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Save pdf: " + pdfPath, EventLogEntryType.Information);
        //                    //if (fileName.EndsWith(".pdf"))
        //                    if (!File.Exists(pdfPath.ToLower() + "_tmp.jpg_th.jpg"))
        //                        File.Delete(pdfPath.ToLower() + "_tmp.jpg_th.jpg");
        //                    //else
        //                    if (File.Exists(pdfPath.ToLower() + "_th.jpg"))
        //                        File.Delete(pdfPath.ToLower() + "_th.jpg");
        //                    try
        //                    {
        //                        // Create the thumbnail file
        //                        //ImageManager.Resize(newRawPageTempPath, 113, 160);
        //                        string tempImagePath = Util.SaveAsTiffThumbnailImage(pdfPath);
        //                        if (logging) Util.DWMSLog("DWMS_OCR_Service.GetOcrText", "Done create thumbnail(GetOcrText): " + tempImagePath, EventLogEntryType.Information);
        //                        string thumbNailPath = ImageManager.Resize(tempImagePath);

        //                        try
        //                        {
        //                            if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
        //                        }
        //                        catch
        //                        {
        //                        }
        //                    }
        //                    catch (Exception)
        //                    {
        //                        // Log the error to show in the set action log
        //                        LogActionDb logActionDb = new LogActionDb();
        //                        logActionDb.Insert(Retrieve.GetSystemGuid(),
        //                            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
        //                            LogActionEnum.Thumbnail_Creation_Error.ToString(),
        //                            pdfPath.Contains("\\") ? pdfPath.Substring(pdfPath.LastIndexOf("\\") + 1) : pdfPath,
        //                            string.Empty, string.Empty, LogTypeEnum.S, setId);
        //                    }
        //                }

        //                //to rotate the original raw file, according to autorotated value, generated by OCR, saved into RawFiles table.
        //                if (ocrRotate > 0)
        //                {
        //                    if (fileName.EndsWith(".pdf"))
        //                    {
        //                        string tempPdfPath = sourceFilePath + ".temp";
        //                        PdfReader reader = new PdfReader(sourceFilePath);
        //                        using (FileStream fs = new FileStream(tempPdfPath, FileMode.Create))
        //                        {
        //                            PdfStamper stamper = new PdfStamper(reader, fs);
        //                            PdfDictionary pageDictionary = reader.GetPageN(1); //only get first page (raw page only 1 page)
        //                            int desiredRot = ocrRotate * 90;
        //                            PdfNumber rotation = pageDictionary.GetAsNumber(PdfName.ROTATE);
        //                            if (rotation != null)
        //                            {
        //                                desiredRot += rotation.IntValue;
        //                                desiredRot %= 360;
        //                                if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Rotate pdf image: " + fileName + " degree: " + desiredRot, EventLogEntryType.Warning);
        //                            }
        //                            pageDictionary.Put(PdfName.ROTATE, new PdfNumber(desiredRot));
        //                            stamper.Close();
        //                        }
        //                        File.Replace(tempPdfPath, sourceFilePath, sourceFilePath + ".backup");
        //                        if (File.Exists(sourceFilePath + ".backup")) File.Delete(sourceFilePath + ".backup");
        //                    }
        //                    else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif") || fileName.EndsWith(".tif") || fileName.EndsWith(".tiff") || fileName.EndsWith(".bmp"))
        //                    {
        //                        using (image = Image.FromFile(fileName))
        //                        {
        //                            switch (ocrRotate)
        //                            {
        //                                case 1:
        //                                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        //                                    image.Save(fileName);
        //                                    break;
        //                                case 2:
        //                                    image.RotateFlip(RotateFlipType.Rotate180FlipNone);
        //                                    image.Save(fileName);
        //                                    break;
        //                                case 3:
        //                                    image.RotateFlip(RotateFlipType.Rotate270FlipNone);
        //                                    image.Save(fileName);
        //                                    break;
        //                            }
        //                            if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Rotate image: " + fileName + " degree: " + ocrRotate * 90, EventLogEntryType.Warning);
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //                Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize fail", EventLogEntryType.Warning);

        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // Log the error in the windows service log
        //        string errorSummary = string.Format("Warning (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
        //            , sourceFilePath, e.Message, e.StackTrace);
        //        Util.DWMSLog("OcrManager.GetOcrText", errorSummary, EventLogEntryType.Warning);

        //        // Log the error to show in the set action log
        //        LogActionDb logActionDb = new LogActionDb();
        //        logActionDb.Insert(Retrieve.GetSystemGuid(),
        //            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3.ToString(),
        //            LogActionEnum.File_Error.ToString(),
        //            e.Message,
        //            sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath,
        //            string.Empty, LogTypeEnum.S, setId);

        //        //// Log the exception for the directory
        //        //multiple exception log
        //        errorReason = "OCR of file failed.";
        //        errorException = String.Format("File={0}, Message={1}",
        //            (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath),
        //            e.Message);

        //        result = false;
        //    }
        //    finally
        //    {
        //        if (image != null)
        //            image.Dispose();

        //        // Delete the temporary files
        //        ocr.DeleteTemporaryFiles();
        //    }

        //    return result;
        //}
        #endregion


        #region GetOcrText Commented by Edward 03.10.2013
        /// <summary>
        /// OCR the page
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="outPdfFilePath"></param>
        /// <param name="outputTextFilePath"></param>
        /// <param name="noPictures"></param>
        /// <returns></returns>
        //public bool GetOcrText(out string ocrText, out string errorReason, out string errorException)
        //{
        //    ParameterDb parameterDb = new ParameterDb();
        //    bool logging = parameterDb.Logging();
        //    bool detailLogging = parameterDb.DetailLogging();
        //    bool result = false;

        //    ocrText = string.Empty;
        //    errorReason = string.Empty;
        //    errorException = string.Empty;
        //    long fileSize = 0;

        //    string fileName = sourceFilePath.ToLower();
        //    Image image = null;
        //    try
        //    {
        //        result = true;

        //        // Set up the OCR engine
        //        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Reading Image Source", EventLogEntryType.Information);
        //        if (fileName.EndsWith(".pdf"))
        //        {
        //            ocr.ReadPDFSource(sourceFilePath);
        //        }
        //        else if (fileName.EndsWith(".tif") || fileName.EndsWith(".tiff"))
        //        {
        //            ocr.ReadTIFFSource(sourceFilePath);
        //        }
        //        else if (fileName.EndsWith(".bmp"))
        //        {
        //            ocr.ReadBMPSource(sourceFilePath);
        //        }
        //        else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif"))
        //        {
        //            try
        //            {
        //                using (image = Image.FromFile(fileName))//causing out of memory issue
        //                {
        //                    ocr.ReadImageSource(image);
        //                }
        //            }
        //            catch (Exception e)
        //            {
        //                // Log the error in the windows service log
        //                string errorSummary = string.Format("Warning (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
        //                    , sourceFilePath, e.Message, e.StackTrace);

        //                errorReason = "Prepare image for OCR Failed.";
        //                errorException = String.Format("File={0}, Message={1}",
        //                    (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath),
        //                    e.Message);

        //                result = false;
        //            }
        //        }
        //        else
        //        {
        //            result = false;
        //        }

        //        if (result)
        //        {
        //            if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Carry out the OCR processing", EventLogEntryType.Information);
        //            // Carry out the OCR processing
        //            if (ocr.Recognize(preProcessor))
        //            {
        //                //while (!this.ocrDone)
        //                //    continue;

        //                //if (this.ocrDone)
        //                //{
        //                // Return the OCR text
        //                if (ocr.NumberPages > 0)
        //                {
        //                    try
        //                    {
        //                        ocrText = ocr.ReadDocumentString();
        //                        if (ocrText.Length > 0) ocrText = ocr.ReadPageString(1);
        //                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Information);
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        ocrText = ocr.ReadDocumentString();
        //                        if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "File: " + fileName + "\nocrText: " + ocrText, EventLogEntryType.Error);
        //                        if (logging) Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize failed to read text for file: " + sourceFilePath + "Message: " + e.Message, EventLogEntryType.Error);
        //                    }

        //                    // Save the searchable PDF file
        //                    string pdfPath = sourceFilePath + "_s.pdf";
        //                    ocr.SavePDFOutput(pdfPath, true);

        //                    FileInfo newSearcheablePdf = new FileInfo(pdfPath);

        //                    if (newSearcheablePdf.Exists)
        //                    {
        //                        if (newSearcheablePdf.Length == 0)
        //                            result = false;
        //                    }

        //                    if (logging) Util.DWMSLog("OcrManager.GetOcrText", "Save pdf: " + pdfPath, EventLogEntryType.Information);
        //                    //if (fileName.EndsWith(".pdf"))
        //                    if (!File.Exists(pdfPath.ToLower() + "_tmp.jpg_th.jpg"))
        //                        File.Delete(pdfPath.ToLower() + "_tmp.jpg_th.jpg");
        //                    //else
        //                    if (File.Exists(pdfPath.ToLower() + "_th.jpg"))
        //                        File.Delete(pdfPath.ToLower() + "_th.jpg");
        //                    try
        //                    {
        //                        // Create the thumbnail file
        //                        //ImageManager.Resize(newRawPageTempPath, 113, 160);
        //                        string tempImagePath = Util.SaveAsTiffThumbnailImage(pdfPath);
        //                        if (logging) Util.DWMSLog("DWMS_OCR_Service.GetOcrText", "Done create thumbnail(GetOcrText): " + tempImagePath, EventLogEntryType.Information);
        //                        string thumbNailPath = ImageManager.Resize(tempImagePath);

        //                        try
        //                        {
        //                            if (File.Exists(tempImagePath)) File.Delete(tempImagePath);
        //                        }
        //                        catch
        //                        {
        //                        }
        //                    }
        //                    catch (Exception)
        //                    {
        //                        // Log the error to show in the set action log
        //                        LogActionDb logActionDb = new LogActionDb();
        //                        logActionDb.Insert(Retrieve.GetSystemGuid(),
        //                            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_Unable_to_create_thumbnail_for_the_file_PERIOD_SEMICOLON_File_EQUALSSIGN_REPLACE2.ToString(),
        //                            LogActionEnum.Thumbnail_Creation_Error.ToString(),
        //                            pdfPath.Contains("\\") ? pdfPath.Substring(pdfPath.LastIndexOf("\\") + 1) : pdfPath,
        //                            string.Empty, string.Empty, LogTypeEnum.S, setId);
        //                    }
        //                }

        //                //to rotate the original raw file, according to autorotated value, generated by OCR, saved into RawFiles table.
        //                if (ocrRotate > 0)
        //                {
        //                    if (fileName.EndsWith(".pdf"))
        //                    {
        //                        string tempPdfPath = sourceFilePath + ".temp";
        //                        PdfReader reader = new PdfReader(sourceFilePath);
        //                        using (FileStream fs = new FileStream(tempPdfPath, FileMode.Create))
        //                        {
        //                            PdfStamper stamper = new PdfStamper(reader, fs);
        //                            PdfDictionary pageDictionary = reader.GetPageN(1); //only get first page (raw page only 1 page)
        //                            int desiredRot = ocrRotate * 90;
        //                            PdfNumber rotation = pageDictionary.GetAsNumber(PdfName.ROTATE);
        //                            if (rotation != null)
        //                            {
        //                                desiredRot += rotation.IntValue;
        //                                desiredRot %= 360;
        //                                if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Rotate pdf image: " + fileName + " degree: " + desiredRot, EventLogEntryType.Warning);
        //                            }
        //                            pageDictionary.Put(PdfName.ROTATE, new PdfNumber(desiredRot));
        //                            stamper.Close();
        //                        }
        //                        File.Replace(tempPdfPath, sourceFilePath, sourceFilePath + ".backup");
        //                        if (File.Exists(sourceFilePath + ".backup")) File.Delete(sourceFilePath + ".backup");
        //                    }
        //                    else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif") || fileName.EndsWith(".tif") || fileName.EndsWith(".tiff") || fileName.EndsWith(".bmp"))
        //                    {
        //                        using (image = Image.FromFile(fileName))
        //                        {
        //                            switch (ocrRotate)
        //                            {
        //                                case 1:
        //                                    image.RotateFlip(RotateFlipType.Rotate90FlipNone);
        //                                    image.Save(fileName);
        //                                    break;
        //                                case 2:
        //                                    image.RotateFlip(RotateFlipType.Rotate180FlipNone);
        //                                    image.Save(fileName);
        //                                    break;
        //                                case 3:
        //                                    image.RotateFlip(RotateFlipType.Rotate270FlipNone);
        //                                    image.Save(fileName);
        //                                    break;
        //                            }
        //                            if (detailLogging) Util.DWMSLog("OcrManager.GetOcrText", "Rotate image: " + fileName + " degree: " + ocrRotate * 90, EventLogEntryType.Warning);
        //                        }
        //                    }
        //                }
        //            }
        //            else
        //                Util.DWMSLog("OcrManager.GetOcrText", "ocr.Recognize fail", EventLogEntryType.Warning);

        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        // Log the error in the windows service log
        //        string errorSummary = string.Format("Warning (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
        //            , sourceFilePath, e.Message, e.StackTrace);
        //        Util.DWMSLog("OcrManager.GetOcrText", errorSummary, EventLogEntryType.Warning);

        //        // Log the error to show in the set action log
        //        LogActionDb logActionDb = new LogActionDb();
        //        logActionDb.Insert(Retrieve.GetSystemGuid(),
        //            LogActionEnum.REPLACE1_COLON_Message_EQUALSSIGN_REPLACE2_SEMICOLON_File_EQUALSSIGN_REPLACE3.ToString(),
        //            LogActionEnum.File_Error.ToString(),
        //            e.Message,
        //            sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath,
        //            string.Empty, LogTypeEnum.S, setId);

        //        //// Log the exception for the directory
        //        //DocSetDb docSetDb = new DocSetDb();
        //        //ExceptionLogDb exceptionLogDb = new ExceptionLogDb();

        //        //string channel = string.Empty;
        //        //string refNo = string.Empty;
        //        //string reason = "OCR of file failed.";
        //        //string errorMessage = String.Format("File={0}, Message={1}", 
        //        //    (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath), 
        //        //    e.Message);

        //        //DocSet.vDocSetDataTable vDocSetTable = docSetDb.GetvDocSetById(setId);

        //        //if (vDocSetTable.Rows.Count > 0)
        //        //{
        //        //    DocSet.vDocSetRow vDocSet = vDocSetTable[0];

        //        //    channel = (vDocSet.IsChannelNull() ? string.Empty : vDocSet.Channel);
        //        //    refNo = (vDocSet.IsRefNoNull() ? string.Empty : vDocSet.RefNo);
        //        //}

        //        //exceptionLogDb.Insert(channel, refNo, DateTime.Now, reason, errorMessage, setId.ToString(), false);
        //        //multiple exception log
        //        errorReason = "OCR of file failed.";
        //        errorException = String.Format("File={0}, Message={1}",
        //            (sourceFilePath.Contains("\\") ? sourceFilePath.Substring(sourceFilePath.LastIndexOf("\\") + 1) : sourceFilePath),
        //            e.Message);

        //        result = false;
        //    }
        //    finally
        //    {
        //        if (image != null)
        //            image.Dispose();

        //        // Delete the temporary files
        //        ocr.DeleteTemporaryFiles();
        //    }

        //    return result;
        //}
        #endregion


        /// <summary>
        /// OCR the page
        /// </summary>
        /// <param name="sourceFilePath"></param>
        /// <param name="outPdfFilePath"></param>
        /// <param name="outputTextFilePath"></param>
        /// <param name="noPictures"></param>
        /// <returns></returns>
        public bool GetOcrTextWithoutPdf(string sourceFilePath, out string ocrText)
        {
            DateTime start = DateTime.Now;
            string cr = Environment.NewLine;
            bool result = false;

            ocrText = string.Empty;

            string fileName = sourceFilePath.ToLower();

            Util.DWMSLog("", "Filename: " + fileName + File.Exists(fileName).ToString(), EventLogEntryType.Warning);
            Image image = null;
            try
            {
                result = true;

                // Set up the OCR engine
                if (fileName.EndsWith(".pdf"))
                {
                    ocr.ReadPDFSource(sourceFilePath);
                }
                else if (fileName.EndsWith(".tif") || fileName.EndsWith(".tiff"))
                {
                    ocr.ReadTIFFSource(sourceFilePath);
                }
                else if (fileName.EndsWith(".bmp"))
                {
                    ocr.ReadBMPSource(sourceFilePath);
                }
                else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".gif"))
                {
                    using (image = Image.FromFile(sourceFilePath))
                    {
                        ocr.ReadImageSource(image);
                    }
                }
                else
                {
                    result = false;
                }

                // Carry out the OCR processing
                if (ocr.Recognize(preProcessor))
                {
                    // Return the OCR text
                    if (ocr.NumberPages > 0)
                    {
                        try
                        {
                            ocrText = ocr.ReadPageString(1);
                        }
                        catch (Exception)
                        {
                            ocrText = string.Empty;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string errorSummary = string.Format("Error (OcrManager.GetOcrText): File={0}, Message={1}, StackTrace={2}"
                    , sourceFilePath, e.Message, e.StackTrace);

                Util.DWMSLog("OcrManager.GetOcrText", errorSummary, EventLogEntryType.Warning);

                result = false;
            }
            finally
            {
                if (image != null)
                    image.Dispose();

                // Delete the temporary files
                ocr.DeleteTemporaryFiles();
            }

            return result;
        }

        /// <summary>
        /// Initiative OCR Engine
        /// </summary>
        private void InitiateOcrEngine()
        {
            // Settings
            preProcessor.Autorotate = true;
            //preProcessor.Deskew = true;
            preProcessor.NoPictures = true;
            preProcessor.Tables = true;
            preProcessor.RemoveLines = true;
            //preProcessor.MRC = true;
            preProcessor.Despeckle = despeckle;
            preProcessor.Binarize = binarize;
            preProcessor.Morph = morph;
            preProcessor.MRCBackgroundFactor = bgFactor;
            preProcessor.MRCForegroundFactor = fgFactor;
            //preProcessor.MRCQuality = quality;

            string resourceFolder = @"C:\Aquaforest\OCRSDK\bin";

            // Add the resource folder to the environment variable.
            // But only add if the variable has not yet been added.
            string environmentPathVariable = System.Environment.GetEnvironmentVariable("PATH");

            if (!environmentPathVariable.ToUpper().Contains(resourceFolder.ToUpper()))
                System.Environment.SetEnvironmentVariable("PATH", System.Environment.GetEnvironmentVariable("PATH") + ";" + resourceFolder);

            ocr.Dotmatrix = dotMatrix;
            ocr.ResourceFolder = resourceFolder;
            ocr.License = Util.GetOcrLicense();
            ocr.EnablePdfOutput = true;
            ocr.EnableTextOutput = true;
            ocr.Language = SupportedLanguages.English;
            ocr.RemoveExistingPDFText = false;
            //ocr.OptimiseOcr = true;
            ocr.StatusUpdate += OcrStatusUpdate;

            string tempFolder = Path.Combine(Retrieve.GetTempDirPath(), Guid.NewGuid().ToString()); // multiple thread need a different temporary folder for each thread
            ocr.TempFolder = tempFolder;

            //ocr.PageCompleted += OcrPageCompleted;
        }

        public void Dispose()
        {
            if (ocr != null)
            {
                ocr.Dispose();
                ocr = null;
            }
        }

        private void OcrStatusUpdate(object sender, StatusUpdateEventArgs statusUpdateEventArgs)
        {
            //this.ocrDone = true;
            this.ocrRotate = (byte)statusUpdateEventArgs.Rotation;
        }

        private void OcrPageCompleted(int pageNumber, bool textAvailable, bool imageAvailable, bool blankPage)
        {
            //this.ocrDone = true;
            Util.DWMSLog("", "OCR Done!", EventLogEntryType.Warning);
        }
    }
}
