using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AzureStorageBackupUtility
{
    public partial class BackupService : ServiceBase
    {
        private bool _isThreadRunning;
        public bool IsThreadRunning
        {
            get
            {
                return _isThreadRunning;
            }
            set
            {
                _isThreadRunning = value;
                if (!IsThreadRunning)
                {
                    InitiateBackupThread();
                }
            }
        }
        private Logging _logging;
        public BackupService()
        {
            InitializeComponent();
            _logging = new Logging();
            _logging.SourceClassName = "BackupService";
        }

        protected override void OnStart(string[] args)
        {
            Trace.WriteLine("Backup service started.");
            _logging.Log("OnStart", "Backup service started.", "");
            InitiateBackupThread();
        }

        private void InitiateBackupThread()
        {
            Trace.WriteLine("Initiating Backup Thread.");
            _logging.Log("InitiateBackupThread", "Initiating Backup Thread.", "");
            var backupThread = new Thread(BackupServiceThread);
            IsThreadRunning = true;
            backupThread.Start();
        }

        protected override void OnStop()
        {
            _logging.Log("OnStop", "Backup service stopped.", "");
        }


        public void BackupServiceThread()
        {
            var backupTime = TimeSpan.FromHours(Convert.ToDouble(ConfigurationManager.AppSettings["BackupTimeIn24HoursFormat"]));
            _logging.Log("BackupServiceThread", "Inside Backup Thread.", "");
            while (IsThreadRunning)
            {
                try
                {
                    _logging.Log("BackupServiceThread", "Logging time", "Hours: " + backupTime.Hours + "\t" + "Minutes: " + backupTime.Minutes);
                    Thread.Sleep(30000);
                    if (DateTime.Now.Hour == backupTime.Hours && DateTime.Now.Minute == backupTime.Minutes && DateTime.Now.Second >= 0 && DateTime.Now.Second <= 59)
                    {
                        _logging.Log("BackupServiceThread", "Initiating Backup.", "");
                        Backup backupService = new Backup();
                        backupService.InitiateBackup();
                    }
                }
                catch (Exception ex)
                {
                    _logging.LogException("BackupServiceThread", ex, "");
                    IsThreadRunning = false;
                }
            }
            _logging.Log("BackupServiceThread", "Exiting Backup Thread.", "");
        }

    }
}
