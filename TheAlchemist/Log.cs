using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheAlchemist
{
    static class Log
    {
        static StreamWriter logFile;

        public static void Init(string path)
        {
            logFile = new StreamWriter(path /*new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read)*/);
            logFile.WriteLine("<head><title> Logfile </title></head><body>");
            logFile.WriteLine("<h1>" + DateTime.Now.ToLongDateString() + ", " + DateTime.Now.ToShortTimeString() + "</h1>");
            logFile.Flush();
        }

        public static void Message(string message)
        {
            TimeStamp();
            logFile.WriteLine(message);
            logFile.WriteLine("</br>");
            logFile.Flush();
        }

        public static void Warning(string message)
        {
            logFile.Write("<font color=\"orange\">");
            TimeStamp();
            logFile.Write("[WARNING] " + message);
            logFile.WriteLine("</font>");
            logFile.WriteLine("</br>");
            logFile.Flush();
        }

        public static void Error(string message)
        {
            logFile.Write("<font color=\"red\">");
            TimeStamp();
            logFile.Write("[ERROR] " + message);
            logFile.WriteLine("</font>");
            logFile.WriteLine("</br>");
            logFile.Flush();
        }

        public static void End()
        {
            logFile.WriteLine("</body>");
            logFile.Close();
            //logFile.Dispose();
        }

        static void TimeStamp()
        {
            logFile.Write("[" + DateTime.Now.ToLongTimeString() + "] ");
        }
    }
}
