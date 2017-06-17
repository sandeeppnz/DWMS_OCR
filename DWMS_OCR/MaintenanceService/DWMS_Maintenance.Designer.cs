namespace DWMS_OCR.MaintenanceService
{
    partial class DWMS_Maintenance
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
            this.timer.Enabled = false;
            this.timer.Interval = 45000D;//30 sec interval
            this.timer.Elapsed += new System.Timers.ElapsedEventHandler(this.timer_Elapsed);
            // 
            // DWMS_Maintenance
            // 
            this.ServiceName = "DWMS_Maintenance";
            ((System.ComponentModel.ISupportInitialize)(this.timer)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.eventLog)).EndInit();

        }

        #endregion

        private System.Timers.Timer timer;
        private System.Diagnostics.EventLog eventLog;
    }
}
