using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
namespace LatencyLogger
{
    static class Tool
    {

        public static TimeSpan GetTimeSpanOfTime(this DateTime dt)
        {
            return new TimeSpan(dt.Hour, dt.Minute, dt.Second);
        }

        private static string SaveLoc
        {
            get
            {
                var directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

                directory = Path.Combine(directory, "MyLatencyLogs", "LogRecords.dat");

                return directory;
            }
        }

        public static void Save(LatencyRecords latRecs)
        {                     

            Save<LatencyRecords>(latRecs, SaveLoc);
        }

        public static LatencyRecords Load()
        {
            if (!File.Exists(SaveLoc))
            {
                return new LatencyRecords();
            }

             return Read<LatencyRecords>(SaveLoc);

        }

        public static void Save<T>(T file, String path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Create a new Serializer
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            // Create a new StreamWriter
            TextWriter writer = new StreamWriter(path);

            // Serialize the file
            serializer.Serialize(writer, file);

            // Close the writer
            writer.Close();
        }

        public static T Read<T>(String path)
        {

            if (!File.Exists(path))
            {
                return default(T);
            }

            TextReader reader = null;
            try
            {


                // Create a new serializer
                XmlSerializer serializer = new XmlSerializer(typeof(T));

                // Create a StreamReader
                reader = new StreamReader(path);

                T file;
                // Deserialize the file
                file = (T)serializer.Deserialize(reader);


                // Return the object
                return file;
            }
            finally
            {
                reader?.Close();
                reader?.Dispose();

            }
        }
    }
}
