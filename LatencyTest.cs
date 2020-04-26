using System;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;
using System.Net.NetworkInformation;
namespace LatencyLogger
{
    public partial class LatencyTest : Form
    {

        string[] schedule = { "08:00 AM", "01:00 PM", "02:30 PM" };
        const string DIR_NAME = "MyLatencyLogs";
        const string FILE_NAME = "LatencyLogs.dat";
        const string REGISTRY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        const string APP_NAME = "LatencyLogger";

        private String SaveLocFullPath
        {
            get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DIR_NAME, FILE_NAME); }
        }

        public LatencyTest()
        {
            InitializeComponent();
            chkStart.Checked = GetStartup();
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
            if (!AppSettings.instance.EnableVPNCheck) return true;


            if (NetworkInterface.GetIsNetworkAvailable())
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface Interface in interfaces)
                {            
                    if (Interface.Description.Contains(AppSettings.instance.VPNClientName)
                      && Interface.OperationalStatus == OperationalStatus.Up)
                    {
                        if (AppSettings.instance.CheckUsingConfigName)
                        {
                            if (Interface.Name.Equals(AppSettings.instance.ConfigName, StringComparison.InvariantCultureIgnoreCase))
                                return true;
                        }
                        else if (AppSettings.instance.CheckUsingDnsServer)
                            if (Interface.GetIPProperties().DnsAddresses.Any(x => x.ToString().Contains(AppSettings.instance.DNSServer)))
                                return true;
                    }
                }
            }
            return false;

        }

        private void Run()
        {
            AppSettings.Refresh();
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

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            SetStartup();
        }


        const String REG_EXCEPTION_MESSAGE = "Something is wrong while reading/writing to registry. Try run as administrator?";
        private void SetStartup()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    (REGISTRY, true);

                if (chkStart.Checked)
                    rk.SetValue(APP_NAME, Application.ExecutablePath);
                else
                    rk.DeleteValue(APP_NAME, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(REG_EXCEPTION_MESSAGE, "Registry error", MessageBoxButtons.OK);
            }

        }

        private bool GetStartup()
        {
            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey
                    (REGISTRY, true);

                return rk.GetValue(APP_NAME)!=null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(REG_EXCEPTION_MESSAGE, "Registry error", MessageBoxButtons.OK);
            }

            return false;
        }

        private void LatencyTest_FormClosing(object sender, FormClosingEventArgs e)
        {
            AppSettings.instance.Save();
        }

        private void lnkGSheet_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start(AppSettings.instance.GSheetLink);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            
            System.Diagnostics.Process.Start(AppSettings.instance.FilePath);
        }
    }
}
