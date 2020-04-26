using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace LatencyLogger
{
    class TaskScheduler
    {
        static TaskScheduler _ts;
        
        public static TaskScheduler instance
        {
            get
            {
                if (_ts == null)
                {
                    _ts = new TaskScheduler();
                }

                return _ts;
            }
            
        }

        #region "UNUSED"
        
        List<Timer> timers = new List<Timer>();
        public LatencyRecords LatRecords;

        public TaskScheduler()
        {
            LatRecords = Tool.Load();
        }

        public void Run(params TimeSpan[] timeSpans)
        {
            List<Timer> timers = new List<Timer>();
            foreach(TimeSpan ts in timeSpans)
            {
                SetUpTimer(ts);
            }            
        }
        
        private void SetUpTimer(TimeSpan alertTime)
        {
            DateTime current = DateTime.Now;
            TimeSpan timeToGo = alertTime - current.TimeOfDay;
            if (timeToGo < TimeSpan.Zero)
            {
                return;//time already passed
            }
            var timer = new System.Threading.Timer(x =>
            {
                this.RunAndLogLatency();
            }, null, timeToGo, Timeout.InfiniteTimeSpan);

            timers.Add(timer);
        }


        public void RunAndLogLatency()
        {
            var logmsg = String.Empty;
            var lat = HttpsPingTimeAverage(AppSettings.instance.ServerToTest, 10, out logmsg);
            var latRec = LatRecords.Add(lat);
            Tool.Save(LatRecords);
            latRec.logfile = Log(logmsg, Guid.NewGuid().ToString());

        }

        #endregion

        public double RunAndLogLatencySimple()
        {
            var logmsg = String.Empty;
            double val = 0;
            if (AppSettings.instance.UseHttpsPingApi)
                val = HttpsPingTimeAverage(AppSettings.instance.ServerToTest, AppSettings.instance.TotalRequestPerTest, out logmsg);
            else
                val = PingTimeAverage(AppSettings.instance.ServerToTest, AppSettings.instance.TotalRequestPerTest, out logmsg);
            
            Log(logmsg, $"Log{DateTime.Now.ToString("dd-MM-yy hh-mm-ss")}");
            return val;

        }       

        public string Log(string msg, string logname)
        {
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            directory = Path.Combine(directory, "MyLatencyLogs", logname+".txt");
           

                Directory.CreateDirectory(Path.GetDirectoryName(directory));
    

            File.WriteAllText(directory, msg);

            return directory;

        }

        private double PingTimeAverage(string host, int echoNum, out string logMsg)
        {
            long totalTime = 0;
            int timeout = 200;
            Ping pingSender = new Ping();
            StringBuilder log = new StringBuilder($"Pinging to {host} at { DateTime.Now}\n");
            for (int i = 0; i < echoNum; i++)
            {
                try
                {

                    PingReply reply = pingSender.Send(host, timeout);
                    if (reply.Status == IPStatus.Success)
                    {
                        totalTime += reply.RoundtripTime;
                    }
                    log.AppendLine($"Response: {reply.Status.ToString()} at {DateTime.Now} - {reply.RoundtripTime} ms");
                }
                catch (Exception e)
                {
                    log.AppendLine($"Exception thrown on pinging: {e.Message} - {e.InnerException} ms");
                }

                
            }
            logMsg = log.ToString();
            return totalTime / echoNum;
        }
        

        private double HttpsPingTimeAverage(string domain, int echoNum, out string logMsg)
        {
            var url = String.Format(@"https://{0}/ping", domain);
            long totalTime = 0;

            System.Diagnostics.Stopwatch timer = new Stopwatch();
            StringBuilder log = new StringBuilder($"Requesting request {url} at { DateTime.Now}\n");
            bool warmedUp = false;
            for (int i = 0; i < echoNum; i++)
            {
                timer.Restart();
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    response.Close();

                    if(warmedUp)
                        log.AppendLine($"Response: {response.StatusCode} at {DateTime.Now} - {timer.ElapsedMilliseconds} ms");
                    else
                        log.AppendLine($"Warm UP - Response: {response.StatusCode} at {DateTime.Now} - {timer.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    log.AppendLine("Error: " + ex.Message + " at " + DateTime.Now);
                }


                if (!warmedUp)
                {
                    warmedUp = true;
                    i--;
                    Thread.Sleep(500);
                    continue;
                }

                timer.Stop();
                totalTime += timer.ElapsedMilliseconds;
            }

            logMsg = log.ToString();
            return totalTime / echoNum;
        }


    }
}
