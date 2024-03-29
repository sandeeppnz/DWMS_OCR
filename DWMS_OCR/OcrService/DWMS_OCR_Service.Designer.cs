﻿using DWMS_OCR.App_Code.Helper;
using System.Diagnostics;

namespace DWMS_OCR.OcrService
{
    partial class DWMS_OCR_Service
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.eventLog = new System.Diagnostics.EventLog();
            this.timer = new System.Timers.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.timer)).BeginInit();
            // 
            // timer
            // 

            this.timer.AutoReset = false;
            this.timer.Enabled = false;
            this.timer.Interval = 5000D;//5 sec interval
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);

            // 
            // DWMS_OCR_Service
            // 
            this.ServiceName = "DWMS_OCR_Service_New";
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.timer)).EndInit();

        }

        #endregion

        private System.Diagnostics.EventLog eventLog;
        private System.Timers.Timer timer;
    }
}
