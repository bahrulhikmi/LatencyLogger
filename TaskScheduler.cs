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
        const string PING_SERVER = "ec2.ap-southeast-1.amazonaws.com";

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
            var lat = HttpsPingTimeAverage(PING_SERVER, 10, out logmsg);
            var latRec = LatRecords.Add(lat);
            Tool.Save(LatRecords);
            latRec.logfile = Log(logmsg, Guid.NewGuid().ToString());

        }

        public double RunAndLogLatencySimple()
        {
            var logmsg = String.Empty;
            //return PingTimeAverage(PING_SERVER, 10, out logmsg);
            var val = HttpsPingTimeAverage(PING_SERVER, 10, out logmsg);
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

        private double PingTimeAverage(string host, int echoNum)
        {
            long totalTime = 0;
            int timeout = 120;
            Ping pingSender = new Ping();

            for (int i = 0; i < echoNum; i++)
            {
                PingReply reply = pingSender.Send(host, timeout);
                if (reply.Status == IPStatus.Success)
                {
                    totalTime += reply.RoundtripTime;
                }
            }
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
