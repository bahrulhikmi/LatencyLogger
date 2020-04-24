using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LatencyLogger
{
    public class Latencies
    {
        public DateTime date;
        public List<LatencyRecord> latencies = new List<LatencyRecord>();
    }

    public class LatencyRecord
    {
        public DateTime schedule;
        public int seq;
        public bool? success;
        public string logfile;
        public DateTime date;
        public double latency;
    }

    public class LatencyRecords
    {
        public List<Latencies> Records = new List<Latencies>();

        public LatencyRecord Add(double latency)
        {
            var latRec = new LatencyRecord();
            latRec.date = DateTime.Today;
            latRec.latency = latency;
            latRec.success = latency > 0;

            var sameDateRecord = Records.Find(x => x.date.Date == latRec.date.Date);
            if (sameDateRecord == null)
            {
                sameDateRecord = new Latencies();
                sameDateRecord.date = latRec.date;                
            }

            latRec.seq = sameDateRecord.latencies.Count+1;
            sameDateRecord.latencies.Add(latRec);
            Records.Add(sameDateRecord);
            return latRec;

        }



    }
}
