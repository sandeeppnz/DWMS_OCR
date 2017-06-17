using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;
using System.ServiceProcess;


namespace DWMS_OCR
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller1_AfterInstall(object sender, InstallEventArgs e)
        {
            //ServiceController objServiceController = new ServiceController();
            //objServiceController.ServiceName = serviceInstaller1.ServiceName;
            //objServiceController.Start();
        }

        private void serviceInstaller2_AfterInstall(object sender, InstallEventArgs e)
        {
            //ServiceController objServiceController = new ServiceController();
            //objServiceController.ServiceName = serviceInstaller2.ServiceName;
            //objServiceController.Start();
        }

        private void serviceInstaller3_AfterInstall(object sender, InstallEventArgs e)
        {

        }

        private void serviceInstaller4_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
