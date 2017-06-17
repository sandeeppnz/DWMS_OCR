using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using DWMS_OCR.App_Code.Dal;
using System.Globalization;
#region Creating the PDF
using its = iTextSharp;
using itsImage = iTextSharp.text.Image;
using itsFont = iTextSharp.text.Font;
using itsRectangle = iTextSharp.text.Rectangle;
using iTextSharp.text;
using iTextSharp.text.html;
using iTextSharp.text.html.simpleparser;
using iTextSharp.text.pdf;
using System.IO;
#endregion

namespace DWMS_OCR.App_Code.Bll
{
    class IncomeAssessmentDb
    {
        /// <summary>
        /// Gets the DocAppId and RefNo 
        /// </summary>
        /// <param name="status">Assessment Status - Extracted</param>
        /// <param name="LeasStatus">SentToLEASStatus</param>
        /// <returns></returns>
        public static DataTable GetDocAppByStatus(string status,string LeasStatus)
        {
            return IncomeAssessmentDs.GetDocAppByStatus(status, LeasStatus );
        }

        public static DataTable GetAppPersonalByDocAppId(int docAppId)
        {
            return IncomeAssessmentDs.GetAppPersonalByDocAppId(docAppId);
        }

        public static DataTable GetDataForIncomeAssessment(int docAppId, int appPersonalId)
        {
            return IncomeAssessmentDs.GetDataForIncomeAssessment(docAppId, appPersonalId);
        }

        public static DataTable GetDataForIncomeAssessment(int docAppId, string nric)
        {
            return IncomeAssessmentDs.GetDataForIncomeAssessment(docAppId, nric);
        }

        public static DataTable GetDataForIncomeAssessment(int appPersonalId)
        {
            return IncomeAssessmentDs.GetDataForIncomeAssessment(appPersonalId);
        }

        public static DataTable GetIncomeAmount(int docAppId, int appPersonalId, string TypeOfIncome)
        {
            return IncomeAssessmentDs.GetIncomeAmount(docAppId, appPersonalId, TypeOfIncome);
        }


        public static DataTable GetDescendingMonthYear(int docAppId, string nric)
        {
            return IncomeAssessmentDs.GetDescendingMonthYear(docAppId, nric);
        }


        public static DataTable GetDistinctIncomeItemByAppPersonalId(int id)
        {
            return IncomeAssessmentDs.GetDistinctIncomeItemByAppPersonalId(id);
        }

        public static DataTable GetIncomeDetailsByIncomeIdAndIncomeItem(int IncomeId, string IncomeItem)
        {
            return IncomeAssessmentDs.GetIncomeDetailsByIncomeIdAndIncomeItem(IncomeId, IncomeItem);
        }

        public static string GetUserNameByAssessmentStaffId(Guid userId)
        {
            return IncomeAssessmentDs.GetUserNameByAssessmentStaffId(userId);
        }


        #region Creating the PDF
    //    private static string strRefNo = string.Empty;
    //    private static string strAssessmentDateOut = string.Empty;
    //    public static MemoryStream GeneratePDFHousingGrant(int docAppId)
    //    {
    //        MemoryStream pdfStream = new MemoryStream();
    //        Document pdfDoc = new Document(PageSize.A4.Rotate());
    //        pdfDoc.SetMargins(pdfDoc.LeftMargin, pdfDoc.RightMargin, 80, 80);


    //        Dictionary<string, IncomeWorksheet> dicIncomeWorksheet = null;
    //        IncomeWorksheet clsIncomeWorksheet;
    //        List<string> listMonthYear = new List<string>();
    //        List<int> listIncomeId = new List<int>();
    //        decimal decIncomeAmount = 0;
    //        string strLowestIncomeAmount = string.Empty;
    //        int intYes = 0;
    //        int intNo = 0;


    //        try
    //        {
    //            PdfWriter pdfWriter = PdfWriter.GetInstance(pdfDoc, pdfStream);
    //            PdfPTable pdfpTable = null;
    //            itsFont bold = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);



    //            #region Get RefNo From DocApp to get the number of columns to create for the PDF table.


    //            DocAppDb docAppDb = new DocAppDb();
    //            DocApp.DocAppDataTable docApps = docAppDb.GetDocAppIncomeExtractionById(docAppId);
    //            if (docApps.Rows.Count > 0)
    //            {
    //                DocApp.DocAppRow docAppRow = docApps[0];
    //                strRefNo = docAppRow.RefNo;
    //                strAssessmentDateOut = !docAppRow.IsAssessmentDateOutNull() ? docAppRow.AssessmentDateOut.ToShortDateString() : " - ";
    //                pdfWriter.PageEvent = new PDFFooter();
    //                pdfDoc.Open();
    //                Paragraph TitleParagraph = new Paragraph(new Chunk("Income Extraction Worksheet", bold));
    //                TitleParagraph.Font.Size = 16;
    //                pdfDoc.Add(TitleParagraph);
    //                string refType = docAppRow.RefType.ToUpper().Trim();
    //                if (refType.Contains(ReferenceTypeEnum.HLE.ToString()))
    //                {
    //                    HleInterfaceDb hleInterfaceDb = new HleInterfaceDb();
    //                    HleInterface.HleInterfaceDataTable hleInterfaceDt = hleInterfaceDb.GetHleInterfaceByHleNumber(docAppRow.RefNo);
    //                    TitleParagraph = new Paragraph(new Chunk("Application Number: " + docAppRow.RefNo, bold));
    //                    TitleParagraph.Font.Size = 14;
    //                    pdfDoc.Add(TitleParagraph);
    //                    if (hleInterfaceDt.Rows.Count > 0)
    //                    {
    //                        if (dicIncomeWorksheet == null)
    //                            dicIncomeWorksheet = new Dictionary<string, IncomeWorksheet>();
    //                        foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                        {
    //                            #region Application number, summary and Summary Headers
    //                            pdfpTable = new PdfPTable(4);
    //                            pdfpTable.SpacingBefore = 30f;
    //                            pdfpTable.WidthPercentage = 100;
    //                            Phrase p = new Phrase();

    //                            #region Setting the Summary Header
    //                            string strSummary = string.Empty;
    //                            if (hleInterfaceRow.ApplicantType.Equals(PersonalTypeEnum.HA.ToString()))
    //                                strSummary = string.Format("Summary for Applicant {0} - {1} ({2})", hleInterfaceRow.OrderNo, hleInterfaceRow.Name, hleInterfaceRow.Nric);
    //                            else if (hleInterfaceRow.ApplicantType.Equals(PersonalTypeEnum.OC.ToString()))
    //                                strSummary = string.Format("Summary for Occupier {0} - {1} ({2})", hleInterfaceRow.OrderNo, hleInterfaceRow.Name, hleInterfaceRow.Nric);
    //                            else
    //                                strSummary = string.Format("Summary for {1} ({2})", hleInterfaceRow.Name, hleInterfaceRow.Nric);
    //                            #endregion

    //                            p.Add(new Chunk(strSummary));
    //                            PdfPCell cell = new PdfPCell(p);
    //                            cell.Colspan = 4;
    //                            cell.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //                            cell.BackgroundColor = its.text.Color.CYAN;
    //                            pdfpTable.AddCell(cell);
    //                            pdfpTable.AddCell(PopulatePDFCell("Month", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("Gross Income", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("Allce", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("OT", true, true));
    //                            #endregion


    //                            DataTable IncomeDt = IncomeAssessmentDb.GetDataForIncomeAssessment(docAppId, hleInterfaceRow.Nric);
    //                            if (IncomeDt.Rows.Count > 0)
    //                            {
    //                                decimal TotalGrossIncome = 0;
    //                                decimal TotalAllowance = 0;
    //                                decimal TotalOT = 0;
    //                                int i = 0;
    //                                foreach (DataRow IncomeRow in IncomeDt.Rows)
    //                                {
    //                                    decimal a = decimal.Parse(!string.IsNullOrEmpty(IncomeRow["GrossIncome"].ToString()) ?
    //                                        IncomeRow["GrossIncome"].ToString() : "0") / decimal.Parse(IncomeRow["CurrencyRate"].ToString());
    //                                    decimal b = decimal.Parse(!string.IsNullOrEmpty(IncomeRow["Allowance"].ToString()) ?
    //                                        IncomeRow["Allowance"].ToString() : "0") / decimal.Parse(IncomeRow["CurrencyRate"].ToString());
    //                                    decimal c = decimal.Parse(!string.IsNullOrEmpty(IncomeRow["Overtime"].ToString()) ?
    //                                        IncomeRow["Overtime"].ToString() : "0") / decimal.Parse(IncomeRow["CurrencyRate"].ToString());

    //                                    pdfpTable.AddCell(PopulatePDFCell(IncomeRow["MonthYear"].ToString(), false, false));
    //                                    bool IsExists = false;
    //                                    foreach (string str in listMonthYear)
    //                                    {
    //                                        if (str == IncomeRow["MonthYear"].ToString())
    //                                        {
    //                                            IsExists = true;
    //                                            break;
    //                                        }
    //                                    }
    //                                    if (IsExists == false)
    //                                        listMonthYear.Add(IncomeRow["MonthYear"].ToString());

    //                                    pdfpTable.AddCell(PopulatePDFCell(a.ToString("N0"), false, false)); //GrossIncome                                    
    //                                    pdfpTable.AddCell(PopulatePDFCell(b.ToString("N0"), false, false)); //Allowance
    //                                    pdfpTable.AddCell(PopulatePDFCell(c.ToString("N0"), false, false)); //Overtime
    //                                    TotalGrossIncome = TotalGrossIncome + a;
    //                                    TotalAllowance = TotalAllowance + b;
    //                                    TotalOT = TotalOT + c;
    //                                    clsIncomeWorksheet = new IncomeWorksheet();
    //                                    clsIncomeWorksheet.AverageGrossIncome = a;
    //                                    clsIncomeWorksheet.AverageAllowance = b;
    //                                    clsIncomeWorksheet.AverageOT = c;
    //                                    clsIncomeWorksheet.MonthYear = IncomeRow["MonthYear"].ToString();
    //                                    clsIncomeWorksheet.MonthYearId = int.Parse(IncomeRow["Id"].ToString());
    //                                    if (!dicIncomeWorksheet.ContainsKey(hleInterfaceRow.Nric + IncomeRow["MonthYear"].ToString()))
    //                                        dicIncomeWorksheet.Add(hleInterfaceRow.Nric + IncomeRow["MonthYear"].ToString(), clsIncomeWorksheet);
    //                                    i++;
    //                                }

    //                                #region Average for each Person
    //                                pdfpTable.AddCell(PopulatePDFCell("Average", true, false));
    //                                pdfpTable.AddCell(PopulatePDFCell((TotalGrossIncome / IncomeDt.Rows.Count).ToString("N0"), false, false));
    //                                pdfpTable.AddCell(PopulatePDFCell((TotalAllowance / IncomeDt.Rows.Count).ToString("N0"), false, false));
    //                                pdfpTable.AddCell(PopulatePDFCell((TotalOT / IncomeDt.Rows.Count).ToString("N0"), false, false));
    //                                #endregion

    //                                #region Total for each Person
    //                                pdfpTable.AddCell(PopulatePDFCell("Total", true, false));
    //                                pdfpTable.AddCell(PopulatePDFCell(TotalGrossIncome.ToString("N0"), false, false));
    //                                pdfpTable.AddCell(PopulatePDFCell(TotalAllowance.ToString("N0"), false, false));
    //                                pdfpTable.AddCell(PopulatePDFCell(TotalOT.ToString("N0"), false, false));
    //                                #endregion

    //                            }
    //                            pdfDoc.Add(pdfpTable);
    //                        }
    //                        decimal TotalAverageGrossIncome = 0;
    //                        listMonthYear = SortMonthYear(listMonthYear);

    //                        pdfpTable = new PdfPTable(hleInterfaceDt.Rows.Count + 1);
    //                        pdfpTable.SpacingBefore = 30f;
    //                        pdfpTable.SpacingAfter = 30f;
    //                        pdfpTable.WidthPercentage = 100;
    //                        Phrase p1 = new Phrase();
    //                        p1.Add(new Chunk(string.Format("Summary for {0}", docAppRow.RefNo, bold)));
    //                        PdfPCell cell1 = new PdfPCell(p1);
    //                        cell1.Colspan = hleInterfaceDt.Rows.Count + 1;
    //                        cell1.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //                        cell1.BackgroundColor = its.text.Color.CYAN;
    //                        pdfpTable.AddCell(cell1);
    //                        p1 = new Phrase();
    //                        p1.Add(new Chunk("Items", bold));
    //                        cell1 = new PdfPCell(p1);
    //                        cell1.HorizontalAlignment = PdfPCell.ALIGN_CENTER;
    //                        pdfpTable.AddCell(cell1);
    //                        foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                        {
    //                            pdfpTable.AddCell(PopulatePDFCell(string.Format("{0} / {1}", hleInterfaceRow.Name, hleInterfaceRow.Nric), true, false));
    //                        }

    //                        foreach (string str in listMonthYear)
    //                        {
    //                            pdfpTable.AddCell(PopulatePDFCell(str, false, false));
    //                            foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                            {

    //                                if (dicIncomeWorksheet.ContainsKey(hleInterfaceRow.Nric + str))
    //                                {
    //                                    pdfpTable.AddCell(PopulatePDFCell(dicIncomeWorksheet[hleInterfaceRow.Nric + str].AverageGrossIncome.ToString("N0"), false, false));
    //                                }
    //                                else
    //                                {
    //                                    pdfpTable.AddCell(PopulatePDFCell(" - ", false, false));
    //                                }
    //                            }
    //                        }
    //                        pdfpTable.AddCell(PopulatePDFCell("Average Gross Income", false, false));
    //                        foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                        {

    //                            DataTable IncomeDt = IncomeAssessmentDb.GetDataForIncomeAssessment(docAppId, hleInterfaceRow.Nric);
    //                            if (IncomeDt.Rows.Count > 0)
    //                            {
    //                                decimal avgGrossIncome = 0;

    //                                foreach (DataRow IncomeRow in IncomeDt.Rows)
    //                                {
    //                                    decimal a = decimal.Parse(!string.IsNullOrEmpty(IncomeRow["GrossIncome"].ToString()) ?
    //                                        IncomeRow["GrossIncome"].ToString() : "0") / decimal.Parse(IncomeRow["CurrencyRate"].ToString());

    //                                    avgGrossIncome = avgGrossIncome + a;

    //                                }
    //                                avgGrossIncome = avgGrossIncome / IncomeDt.Rows.Count;
    //                                TotalAverageGrossIncome = TotalAverageGrossIncome + avgGrossIncome;
    //                                pdfpTable.AddCell(PopulatePDFCell(avgGrossIncome.ToString("N0"), false, false));
    //                            }
    //                            else
    //                                pdfpTable.AddCell(PopulatePDFCell(" - ", false, false));
    //                        }
    //                        pdfpTable.AddCell(PopulatePDFCell("Average Allowance", false, false));
    //                        foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                        {
    //                            DataTable IncomeDt = IncomeAssessmentDb.GetDataForIncomeAssessment(docAppId, hleInterfaceRow.Nric);
    //                            if (IncomeDt.Rows.Count > 0)
    //                            {
    //                                decimal avgAllowance = 0;

    //                                foreach (DataRow IncomeRow in IncomeDt.Rows)
    //                                {

    //                                    decimal b = decimal.Parse(!string.IsNullOrEmpty(IncomeRow["Allowance"].ToString()) ?
    //                                        IncomeRow["Allowance"].ToString() : "0") / decimal.Parse(IncomeRow["CurrencyRate"].ToString());
    //                                    avgAllowance = avgAllowance + b;
    //                                }
    //                                avgAllowance = avgAllowance / IncomeDt.Rows.Count;
    //                                pdfpTable.AddCell(PopulatePDFCell(avgAllowance.ToString("N0"), false, false));
    //                            }
    //                            else
    //                                pdfpTable.AddCell(PopulatePDFCell(" - ", false, false));
    //                        }
    //                        pdfpTable.AddCell(PopulatePDFCell("Average Overtime", false, false));
    //                        foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                        {
    //                            DataTable IncomeDt = IncomeAssessmentDb.GetDataForIncomeAssessment(docAppId, hleInterfaceRow.Nric);
    //                            if (IncomeDt.Rows.Count > 0)
    //                            {
    //                                decimal avgOT = 0;

    //                                foreach (DataRow IncomeRow in IncomeDt.Rows)
    //                                {
    //                                    decimal c = decimal.Parse(!string.IsNullOrEmpty(IncomeRow["Overtime"].ToString()) ?
    //                                        IncomeRow["Overtime"].ToString() : "0") / decimal.Parse(IncomeRow["CurrencyRate"].ToString());
    //                                    avgOT = avgOT + c;

    //                                }
    //                                avgOT = avgOT / IncomeDt.Rows.Count;
    //                                pdfpTable.AddCell(PopulatePDFCell(avgOT.ToString("N0"), false, false));
    //                            }
    //                            else
    //                                pdfpTable.AddCell(PopulatePDFCell(" - ", false, false));
    //                        }
    //                        pdfDoc.Add(pdfpTable);

    //                        TitleParagraph = new Paragraph(new Chunk("Total Average Gross Income: " + TotalAverageGrossIncome, bold));
    //                        TitleParagraph.Font.Size = 13;
    //                        TitleParagraph.SpacingAfter = 20f;
    //                        pdfDoc.Add(TitleParagraph);

    //                        //this is the new enhancement from the excelsheet
    //                        foreach (HleInterface.HleInterfaceRow hleInterfaceRow in hleInterfaceDt.Rows)
    //                        {
    //                            AppPersonalDb appPersonalDb = new AppPersonalDb();
    //                            AppPersonal.AppPersonalDataTable appPersonalDt = appPersonalDb.GetAppPersonalByNricAndDocAppId(hleInterfaceRow.Nric, docAppId);
    //                            AppPersonal.AppPersonalRow appPersonalRow;
    //                            if (appPersonalDt.Rows.Count > 0)
    //                            {


    //                                appPersonalRow = appPersonalDt[0];

    //                                string strSummary = string.Empty;
    //                                if (hleInterfaceRow.ApplicantType.Equals(PersonalTypeEnum.HA.ToString()))
    //                                    strSummary = string.Format("Applicant {0} Name: {1} ({2})", hleInterfaceRow.OrderNo, hleInterfaceRow.Name, hleInterfaceRow.Nric);
    //                                else if (hleInterfaceRow.ApplicantType.Equals(PersonalTypeEnum.OC.ToString()))
    //                                    strSummary = string.Format("Occupier {0} Name: {1} ({2})", hleInterfaceRow.OrderNo, hleInterfaceRow.Name, hleInterfaceRow.Nric);
    //                                else
    //                                    strSummary = string.Format("Name: {1} ({2})", hleInterfaceRow.OrderNo, hleInterfaceRow.Name, hleInterfaceRow.Nric);

    //                                TitleParagraph = new Paragraph(new Chunk(strSummary, bold));
    //                                TitleParagraph.Font.Size = 14;
    //                                pdfDoc.Add(TitleParagraph);

    //                                DataTable IncomeDt = IncomeAssessmentDb.GetDescendingMonthYear(docAppId, hleInterfaceRow.Nric);
    //                                listIncomeId.Clear();
    //                                pdfpTable = new PdfPTable(7 + IncomeDt.Rows.Count);
    //                                pdfpTable.SpacingBefore = 10f;
    //                                pdfpTable.WidthPercentage = 100;

    //                                #region the column headers
    //                                pdfpTable.AddCell(PopulatePDFCellFont10("Income Component", true, true));
    //                                pdfpTable.AddCell(PopulatePDFCellFont10("Type of Income", true, true));

    //                                // The below code will populate the months

    //                                if (IncomeDt.Rows.Count > 0)
    //                                {
    //                                    foreach (DataRow IncomeRow in IncomeDt.Rows)
    //                                    {
    //                                        DateTimeFormatInfo info = new DateTimeFormatInfo();

    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("{0} {1}",
    //                                            info.GetAbbreviatedMonthName(int.Parse(IncomeRow["IncomeMonth"].ToString())), IncomeRow["IncomeYear"].ToString()), true, true));
    //                                        listIncomeId.Add(int.Parse(IncomeRow["Id"].ToString()));
    //                                    }
    //                                }

    //                                DataTable NumberOfMonthsDt = IncomeAssessmentDb.GetDescendingMonthYear(docAppId, hleInterfaceRow.Nric);
    //                                pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("Avg in past {0} mths", NumberOfMonthsDt.Rows.Count < 3 ? NumberOfMonthsDt.Rows.Count.ToString() : "3"), true, true));
    //                                pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("Avg in past {0} mths", NumberOfMonthsDt.Rows.Count < 12 ? NumberOfMonthsDt.Rows.Count.ToString() : "12"), true, true));
    //                                pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("Lowest in past {0} mths", NumberOfMonthsDt.Rows.Count < 12 ? NumberOfMonthsDt.Rows.Count.ToString() : "12"), true, true));
    //                                pdfpTable.AddCell(PopulatePDFCellFont10("Yes/No", true, true));
    //                                pdfpTable.AddCell(PopulatePDFCellFont10("Credit Assessment", true, true));

    //                                //The below code will get the IncomeItems and then generate the amount for each month
    //                                DataTable IncomeItemsDt = IncomeAssessmentDb.GetDistinctIncomeItemByAppPersonalId(appPersonalRow.Id);
    //                                foreach (DataRow IncomeItemsRow in IncomeItemsDt.Rows)
    //                                {
    //                                    decimal past3 = 0;
    //                                    decimal past12 = 0;
    //                                    int i = 0;
    //                                    if (!string.IsNullOrEmpty(IncomeItemsRow["IncomeType"].ToString()))
    //                                    {
    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(IncomeItemsRow["IncomeItem"].ToString(), false, false));
    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(IncomeItemsRow["IncomeType"].ToString(), false, false));
    //                                        foreach (int intIncomeId in listIncomeId)
    //                                        {
    //                                            DataTable IncomeItemDt = IncomeAssessmentDb.GetIncomeDetailsByIncomeIdAndIncomeItem(intIncomeId, IncomeItemsRow["IncomeItem"].ToString());
    //                                            if (IncomeItemDt.Rows.Count > 0)
    //                                            {
    //                                                DataRow IncomeItemRow = IncomeItemDt.Rows[0];
    //                                                if (IncomeItemsRow["IncomeType"].ToString().ToLower().Trim().Equals(IncomeItemRow["IncomeType"].ToString().ToLower().Trim()))
    //                                                {
    //                                                    i++;
    //                                                    decIncomeAmount = decIncomeAmount + (decimal.Parse(IncomeItemRow["IncomeAmount"].ToString()) / decimal.Parse(IncomeItemRow["CurrencyRate"].ToString()));
    //                                                    if (i == 3)
    //                                                        past3 = decIncomeAmount;
    //                                                    if (i == 12)
    //                                                        past12 = decIncomeAmount;
    //                                                    if (string.IsNullOrEmpty(strLowestIncomeAmount))
    //                                                        strLowestIncomeAmount = (decimal.Parse(IncomeItemRow["IncomeAmount"].ToString()) / decimal.Parse(IncomeItemRow["CurrencyRate"].ToString())).ToString();
    //                                                    else if (decimal.Parse(strLowestIncomeAmount) > (decimal.Parse(IncomeItemRow["IncomeAmount"].ToString()) / decimal.Parse(IncomeItemRow["CurrencyRate"].ToString())))
    //                                                        strLowestIncomeAmount = (decimal.Parse(IncomeItemRow["IncomeAmount"].ToString()) / decimal.Parse(IncomeItemRow["CurrencyRate"].ToString())).ToString();

    //                                                    intYes = intYes + 1;
    //                                                    pdfpTable.AddCell(PopulatePDFCellFont10((decimal.Parse(IncomeItemRow["IncomeAmount"].ToString()) / decimal.Parse(IncomeItemRow["CurrencyRate"].ToString())).ToString(), false, false));
    //                                                }
    //                                                else
    //                                                {
    //                                                    intNo = intNo + 1;
    //                                                    pdfpTable.AddCell(PopulatePDFCellFont10(" - ", false, false));
    //                                                }
    //                                            }
    //                                            else
    //                                            {
    //                                                intNo = intNo + 1;
    //                                                pdfpTable.AddCell(PopulatePDFCellFont10(" - ", false, false));
    //                                            }

    //                                        }
    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("{0}", i < 3 ? (decIncomeAmount / i).ToString("N2") : (past3 / 3).ToString("N2")), false, false));
    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("{0}", i < 12 ? (decIncomeAmount / i).ToString("N2") : (past12 / 12).ToString("N2")), false, false));
    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(strLowestIncomeAmount, false, false));
    //                                        pdfpTable.AddCell(PopulatePDFCellFont10(string.Format("{0}/{1}", intYes, intNo), false, false));

    //                                        //CreditAssessmentDb CAdb = new CreditAssessmentDb();
    //                                        //CreditAssessment.CreditAssessmentDataTable CADt = CAdb.GetCAByAppPersonalIdByIncomeItemType(appPersonalRow.Id, IncomeItemsRow["IncomeItem"].ToString(), IncomeItemsRow["IncomeType"].ToString());
    //                                        //if (CADt.Rows.Count > 0)
    //                                        //{
    //                                        //    CreditAssessment.CreditAssessmentRow CARow = CADt[0];
    //                                        //    pdfpTable.AddCell(PopulatePDFCellFont10(string.Format(CARow.CreditAssessmentAmount.ToString()), false, false));
    //                                        //}
    //                                        //else
    //                                        //    pdfpTable.AddCell(PopulatePDFCellFont10(string.Format(""), false, false));
    //                                    }
    //                                    strLowestIncomeAmount = string.Empty;
    //                                    intYes = 0;
    //                                    intNo = 0;
    //                                    decIncomeAmount = 0;
    //                                }

    //                            }

    //                            pdfpTable.AddCell(PopulatePDFCell("Avg in past 12 mths", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("Average in past 3 mths", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("Lowest in past mths", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("Yes/No", true, true));
    //                            pdfpTable.AddCell(PopulatePDFCell("Credit Assessment", true, true));
    //                            pdfpTable.SpacingBefore = 10f;
    //                            pdfpTable.SpacingAfter = 10f;
    //                                #endregion

    //                            pdfDoc.Add(pdfpTable);
    //                        }
    //                    }
    //                }
    //            }

    //            #endregion



    //            pdfDoc.Close();
    //        }
    //        catch (Exception ex)
    //        {

    //        }

    //        return pdfStream;
    //    }

    //    private static PdfPCell PopulatePDFCell(string value, bool IsBold, bool IsGray)
    //    {
    //        itsFont bold = !IsBold ? FontFactory.GetFont(FontFactory.HELVETICA, 12) : FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12);
    //        PdfPCell pdfpCell = new PdfPCell(new Phrase(new Chunk(value, bold)));
    //        pdfpCell.BackgroundColor = !IsGray ? its.text.Color.WHITE : its.text.Color.LIGHT_GRAY;
    //        return pdfpCell;
    //    }

    //    private static PdfPCell PopulatePDFCellFont10(string value, bool IsBold, bool IsGray)
    //    {
    //        itsFont bold = !IsBold ? FontFactory.GetFont(FontFactory.HELVETICA, 9) : FontFactory.GetFont(FontFactory.HELVETICA, 9);
    //        PdfPCell pdfpCell = new PdfPCell(new Phrase(new Chunk(value, bold)));
    //        pdfpCell.BackgroundColor = !IsGray ? its.text.Color.WHITE : its.text.Color.LIGHT_GRAY;
    //        pdfpCell.HorizontalAlignment = Cell.ALIGN_CENTER;
    //        return pdfpCell;
    //    }


    //    private static List<string> SortMonthYear(List<string> li)
    //    {
    //        string[] mth = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
    //        List<string> newList = new List<string>();

    //        foreach (string str in mth)
    //        {
    //            foreach (string str1 in li)
    //            {
    //                if (str == str1.Substring(0, str1.Length - 5))
    //                    newList.Add(str1);
    //            }
    //        }

    //        return newList;
    //    }

    //    //http://www.codeproject.com/Tips/573907/Generating-PDF-using-ItextSharp-with-Footer-in-Csh
    //    public class PDFFooter : PdfPageEventHelper
    //    {
    //        // write on top of document
    //        public override void OnOpenDocument(PdfWriter writer, Document document)
    //        {
    //            //base.OnOpenDocument(writer, document);
    //            //PdfPTable tabFot = new PdfPTable(new float[] { 1F });
    //            //tabFot.SpacingAfter = 10F;
    //            //PdfPCell cell;
    //            //tabFot.TotalWidth = 300F;
    //            //cell = new PdfPCell(new Phrase("Header"));
    //            //tabFot.AddCell(cell);
    //            //tabFot.WriteSelectedRows(0, -1, 150, document.Top, writer.DirectContent);
    //        }

    //        // write on start of each page
    //        public override void OnStartPage(PdfWriter writer, Document document)
    //        {
    //            base.OnStartPage(writer, document);
    //        }

    //        // write on end of each page
    //        public override void OnEndPage(PdfWriter writer, Document document)
    //        {
    //            base.OnEndPage(writer, document);
    //            PdfPTable tabFot = new PdfPTable(new float[] { 1F });
    //            PdfPCell cell;
    //            tabFot.TotalWidth = document.PageSize.Width - document.RightMargin;
    //            cell = new PdfPCell(new Phrase(string.Format("{0} / {1}", strRefNo, strAssessmentDateOut)));
    //            cell.Border = 0;
    //            cell.HorizontalAlignment = PdfCell.ALIGN_RIGHT;
    //            tabFot.AddCell(cell);
    //            tabFot.WriteSelectedRows(0, -50, 0, document.Bottom - 50, writer.DirectContent);
    //        }

    //        //write on close of document
    //        public override void OnCloseDocument(PdfWriter writer, Document document)
    //        {
    //            base.OnCloseDocument(writer, document);
    //        }
    //    }



    //    private class IncomeWorksheet
    //    {
    //        private int _MonthYearId;

    //        public int MonthYearId
    //        {
    //            get { return _MonthYearId; }
    //            set { _MonthYearId = value; }
    //        }

    //        private string _MonthYear;

    //        public string MonthYear
    //        {
    //            get { return _MonthYear; }
    //            set { _MonthYear = value; }
    //        }

    //        private decimal _AverageGrossIncome;

    //        public decimal AverageGrossIncome
    //        {
    //            get { return _AverageGrossIncome; }
    //            set { _AverageGrossIncome = value; }
    //        }

    //        private decimal _AverageAllowance;

    //        public decimal AverageAllowance
    //        {
    //            get { return _AverageAllowance; }
    //            set { _AverageAllowance = value; }
    //        }

    //        private decimal _AverageOT;

    //        public decimal AverageOT
    //        {
    //            get { return _AverageOT; }
    //            set { _AverageOT = value; }
    //        }


    //    }

    //
        #endregion
    }
}
