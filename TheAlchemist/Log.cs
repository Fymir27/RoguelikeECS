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
            Newline();
            logFile.Flush();
        }

        public static void Data(string data)
        {
            TimeStamp();
            logFile.WriteLine("[DATA]");
            Newline();
            logFile.Write(GetHTMLString("pre", null, data));
            Newline();
            logFile.Flush();
        }

        public static void Warning(string message)
        {
            TimeStamp();
            logFile.Write(GetHTMLString("font", new Dictionary<string, string>() { { "color", "orange"} }, "[WARNING] " + message));
            Newline();
            logFile.Flush();
        }

        public static void Error(string message)
        {
            TimeStamp();
            logFile.Write(GetHTMLString("font", new Dictionary<string, string>() { { "color", "red" } }, "[ERROR] " + message));
            Newline();
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

        static string GetHTMLString(string tag, Dictionary<string, string> attributes, string message)
        {
            var sb = new StringBuilder();
            sb.Append("<" + tag);

            if(attributes != null)
                attributes.Keys.ToList().ForEach(key => sb.Append(" " + key + "=" + attributes[key]));

            sb.Append(">");
            sb.Append(message);
            sb.Append("</" + tag + ">");
            return sb.ToString();
        }

        static void Newline()
        {
            logFile.WriteLine("</br>");
        }

    }
}
