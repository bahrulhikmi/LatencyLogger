using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatencyLogger
{
   public class AppSettings
    {
        public string GSheetLink;
        public int TotalRequestPerTest;
        public string ServerToTest;
        public bool UseHttpsPingApi;
        public bool EnableVPNCheck;
        public string VPNClientName;
        public bool CheckUsingDnsServer;
        public string DNSServer;
        public bool CheckUsingConfigName;
        public string ConfigName;
        private static AppSettings _instance;
        public static AppSettings instance
        {
            get
            {
                if(_instance==null)
                {
                    _instance = Tool.Read<AppSettings>(SaveLoc);

                    if(_instance==null)
                    {
                        _instance = new AppSettings();
                        _instance.GetDefault();
                        _instance.Save();
                    }

                }
                return _instance;
            }
        }

        private static string SaveLoc
        {
            get
            {
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                directory = Path.Combine(directory, "MyLatencyLogs", "AppSettings.txt");

                return directory;
            }
        }

        public string FilePath
        {
            get { return SaveLoc; }
        }


        public void Save()
        {
            Tool.Save<AppSettings>(this, SaveLoc);
        }

        public static void Refresh()
        {
            _instance = null;
        }

        private void GetDefault()
        {
            GSheetLink = @"https://docs.google.com/spreadsheets/d/1IGk7Ll9je9qbmJrSZd8Y3ne6DOsG7I0nbbDXzv629Kk/edit#gid=0";
            TotalRequestPerTest = 10;            
            ServerToTest = "ec2.ap-southeast-1.amazonaws.com";
            UseHttpsPingApi = false;
            EnableVPNCheck = true;
            VPNClientName = "WireGuard";
            CheckUsingDnsServer = true;
            DNSServer = "1.1.1.1";
            CheckUsingConfigName = false;
            ConfigName = "yourwireguardconfigname";

        }
    }
}
