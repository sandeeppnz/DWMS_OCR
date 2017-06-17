namespace DWMS_OCR
{
    partial class ProjectInstaller
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
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            this.serviceInstaller2 = new System.ServiceProcess.ServiceInstaller();
            this.serviceInstaller3 = new System.ServiceProcess.ServiceInstaller();
            this.serviceInstaller4 = new System.ServiceProcess.ServiceInstaller();
            this.serviceInstaller5 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.ServiceName = "DWMS_OCR_Service_New";
            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller1.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller1_AfterInstall);
            // 
            // serviceInstaller2
            // 
            this.serviceInstaller2.ServiceName = "DWMS_SampleDocOCR_Service_New";
            this.serviceInstaller2.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller2.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller2_AfterInstall);
            // 
            // serviceInstaller3
            // 
            this.serviceInstaller3.ServiceName = "DWMS_Maintenance";
            this.serviceInstaller3.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller3.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller3_AfterInstall);
            // 
            // serviceInstaller4
            // 
            this.serviceInstaller4.ServiceName = "DWMS_CDB_Service";
            this.serviceInstaller4.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.serviceInstaller4.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller4_AfterInstall);
            // 
            // serviceInstaller5
            // 
            this.serviceInstaller5.ServiceName = "DWMS_LEAS_Service";
            this.serviceInstaller5.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1,
            this.serviceInstaller2,
            this.serviceInstaller3,
            this.serviceInstaller4,
            this.serviceInstaller5});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller2;
        private System.ServiceProcess.ServiceInstaller serviceInstaller3;
        private System.ServiceProcess.ServiceInstaller serviceInstaller4;
        private System.ServiceProcess.ServiceInstaller serviceInstaller5;
    }
}