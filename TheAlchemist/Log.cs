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
        static StreamWriter UIMessageLog;

        static string lastUIMessage;
        static int lastUIMessageCount = 1;
        static int unflushedMessageCount = 0;

        public static void Init(string path)
        {
            if (path[path.Length - 1] != '\\')
            {
                path += '\\';
            }

            logFile = new StreamWriter(path + "Log.html");
            logFile.WriteLine("<head><title> Logfile </title></head><body>");
            //logFile.WriteLine("<meta http-equiv=\"refresh\" content=\"3\"/>"); // automatic refresh after 3 sec
            string dateTime = DateTime.Now.ToLongDateString() + ", " + DateTime.Now.ToShortTimeString();
            logFile.WriteLine("<h1>" + dateTime + "</h1>");
            logFile.Flush();

            UIMessageLog = new StreamWriter(path + "MessageLog.txt");
            UIMessageLog.AutoFlush = true;
            UIMessageLog.WriteLine("Run startet at: " + dateTime);
            lastUIMessage = "---";

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
            Data("[DATA]", data);
        }

        public static void Data(string label, string data)
        {
            TimeStamp();
            logFile.WriteLine(label);
            Newline();
            logFile.Write(GetHTMLString("pre", null, data));
            Newline();
            logFile.Flush();
        }

        public static void Warning(string message)
        {
            TimeStamp();
            logFile.Write(GetHTMLString("font", new Dictionary<string, string>() { { "color", "orange" } }, "[WARNING] " + message));
            Newline();
            logFile.Flush();
        }

        public static void Error(string message, bool stacktrace = true)
        {
            TimeStamp();
            logFile.Write(GetHTMLString("font", new Dictionary<string, string>() { { "color", "red" } }, "[ERROR] " + message));
            Newline();
            
            if (stacktrace) {
                logFile.Write(GetHTMLString("pre", null, Environment.StackTrace));
            }
                
            logFile.Flush();

            Util.ErrorOccured = true;
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

            if (attributes != null)
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

        /// <summary>
        /// This method is lazy
        /// It only writes the last message to file (unless close is set), 
        /// because it waits if the current message gets repeated
        /// </summary>
        /// <param name="message">message to save (will be saved on next call to this function)</param>
        /// <param name="close">wether to flush and close the filestream</param>
        public static void SaveUIMessage(string message, bool close = false)
        {
            try
            {
                if (lastUIMessageCount > 1)
                {
                    UIMessageLog.WriteLine(String.Format("{0} (x{1})", lastUIMessage, lastUIMessageCount));
                }
                else
                {
                    UIMessageLog.WriteLine(lastUIMessage);
                }

                lastUIMessage = message;
                lastUIMessageCount = 1;

                //if (unflushedMessageCount++ >= 5)
                //{
                //    UIMessageLog.Flush();
                //}
            }
            catch (IOException e)
            {
                Log.Error("Failed to write to MessageLog!\n" + e.ToString());
            }

            if (close)
            {
                UIMessageLog.WriteLine(message);
                UIMessageLog.Flush();
                UIMessageLog.Close();
            }
        }

        /// <summary>
        /// increases the counter of repetitions for the last message
        /// </summary>
        public static void RepeatUIMessage()
        {
            lastUIMessageCount++;
        }
    }
}
