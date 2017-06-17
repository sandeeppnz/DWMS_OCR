using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DWMS_OCR.App_Code.Helper
{
    static class Logger
    {

        public static void WriteToLogFile(string logFilePath, string contents)
        {
            StreamWriter w;
            try
            {
               
                w = File.AppendText(logFilePath);
                w.Write(contents + "\r\n");
                w.Flush();
                w.Close();
            }
            catch (Exception)
            {
                w = File.AppendText(logFilePath);
                w.Write("Error writing to file" + "\r\n");
                w.Flush();
                w.Close();
            }
        }

    }
}
