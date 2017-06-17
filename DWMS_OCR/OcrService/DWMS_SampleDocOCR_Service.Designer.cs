namespace DWMS_OCR.OcrService
{
    partial class DWMS_SampleDocOCR_Service
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
            this.timer = new System.Timers.Timer();
            this.eventLog = new System.Diagnostics.EventLog();
            ((System.ComponentModel.ISupportInitialize)(this.timer)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).BeginInit();
            // 
            // timer
            // 
            this.timer.AutoReset = false;
            this.timer.Interval = 60000D;
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
            // 
            // DWMS_SampleDocOCR_Service
            // 
            this.ServiceName = "DWMS_SampleDocOCR_Service_New";
            ((System.ComponentModel.ISupportInitialize)(this.timer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

        }

        #endregion

        private System.Timers.Timer timer;
        private System.Diagnostics.EventLog eventLog;
    }
}
