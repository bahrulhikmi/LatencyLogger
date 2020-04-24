using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
namespace LatencyLogger
{
    public partial class LatencyTest : Form
    {

        string[] schedule = { "08:00 AM", "01:00 PM", "02:30 PM" };
        const string DIR_NAME = "MyLatencyLogs";
        const string FILE_NAME = "LatencyLogs.dat";

        private String SaveLocFullPath
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DIR_NAME, FILE_NAME); }
        }

        public LatencyTest()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Run();
        }

        private void LatencyTest_Load(object sender, EventArgs e)
        {
            saveAndReloadText(string.Empty);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            checkTimer();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (File.Exists(SaveLocFullPath))
            {
                try
                {
                    richTextBox1.Text = String.Empty;
                    File.Delete(SaveLocFullPath);
                }
                catch { }
            }
        }

        private void checkTimer()
        {
            var scheduleInd =0;
            var nextSec = getNextScheduleInSeconds(out scheduleInd);
            lblSchedule.Text = schedule[scheduleInd];
            button1.Text = $"Will be automatically run in {secondToString(nextSec)}";
            if(nextSec<=1)
            {
                System.Threading.Thread.Sleep(500);
                timer1.Stop();
                Run();
                timer1.Start();
            }
        }

        private string secondToString(double second)
        {
            TimeSpan t = TimeSpan.FromSeconds(second);

            string answer = string.Format("{0:D2}h:{1:D2}m:{2:D2}s",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);

            return answer;
        }


        private double getNextScheduleInSeconds(out int scheduleIndex)
        {        
            TimeSpan ts1 = new TimeSpan(8, 0, 0);
            TimeSpan ts2 = new TimeSpan(13, 0, 0);
            TimeSpan ts3 = new TimeSpan(14, 30, 0);
            TimeSpan ts4 = new TimeSpan(1,8, 0, 0);

            DateTime current = DateTime.Now;
            TimeSpan timeToGo = ts1 - current.TimeOfDay;

            //8:00 AM
            if (timeToGo >= TimeSpan.Zero)
            {
                scheduleIndex = 0;
                return timeToGo.TotalSeconds;
            }

            timeToGo = ts2 - current.TimeOfDay;
            if (timeToGo >= TimeSpan.Zero)
            {
                scheduleIndex = 1;
                return timeToGo.TotalSeconds;
            }

            timeToGo = ts3 - current.TimeOfDay;
            if (timeToGo >= TimeSpan.Zero)
            {
                scheduleIndex = 2;
                return timeToGo.TotalSeconds;
            }

            timeToGo = ts4 - current.TimeOfDay;

            scheduleIndex = 0;
            return timeToGo.TotalSeconds;
   

        }


        private bool checkVPNEnabled()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                    foreach (NetworkInterface Interface in interfaces)
                    {
                        // This is the OpenVPN driver for windows. 
                        if (Interface.Description.Contains("WireGuard")
                          && Interface.OperationalStatus == OperationalStatus.Up)
                        {
                            if (Interface.GetIPProperties().DnsAddresses.Any(x => x.ToString().Contains("1.1.1.1")))
                            return true;
                        }
                    }
                }
                return false;
            }

            return false;

        }

        private void Run()
        {

            if (!checkVPNEnabled())
            {
                notifyUser($"You are not connected through VPN (Wrong VPN) !", true);
                saveAndReloadText($"[{DateTime.Now.ToString("dd/MM/yy hh:mm")}] Failed - not connected to VPN\n");
                return;
            }
            
           
            var result = TaskScheduler.instance.RunAndLogLatencySimple();
            Log(result);
            button1.Text = String.Format("Done! (Result:{0} ms)", result);
            notifyUser($"Latency Test Result {result} ms ", false);
        }

        private void Log(double latency)
        {
            saveAndReloadText($"[{DateTime.Now.ToString("dd/MM/yy hh:mm")}] {latency} ms\n");     
        }

        public void saveAndReloadText(string newContent)
        {
            string savePath = SaveLocFullPath;
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            string currentContent = String.Empty;
            if (File.Exists(savePath))
            {
                currentContent = File.ReadAllText(savePath);
            }
             richTextBox1.Text = newContent + currentContent;
            File.WriteAllText(savePath, newContent + currentContent);
        }

        public void notifyUser(string message, bool messageBox)
        {
            notifyIcon1.Visible = true;
            notifyIcon1.BalloonTipText = message;
            notifyIcon1.ShowBalloonTip(500);
            //if(messageBox)
            //MessageBox.Show(message);

        }

    }
}
