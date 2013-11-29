// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.IO;

namespace RFIDDoorLock1
{
    class EventLogger
    {
        string mEventLogFname = "";
        string mEventLogFullPath = "";
        string mDebugLogFname = "";
        string mDebugLogFullPath = "";

        public EventLogger(String eventLogFname, string debugLogFname)
        {
            mEventLogFname = eventLogFname;
            mEventLogFullPath = @"\sd\" + mEventLogFname;
            mDebugLogFname = debugLogFname;
            mDebugLogFullPath = @"\sd\" + mDebugLogFname;
        }

        public void LogEntryEvent(string tagId, string holderName)
        {
            // Append to data in file
            try
            {
                using (StreamWriter sw = new StreamWriter(mEventLogFullPath, true))
                {
                    string s = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + "\tEntry\t" + tagId + "\t" + holderName;
                    sw.WriteLine(s);
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + mEventLogFullPath + " Exception: " + e.ToString());
            }

        }

        public void LogDebug(string param1, string param2)
        {
            // Append to data in file
            try
            {
                using (StreamWriter sw = new StreamWriter(mDebugLogFullPath, true))
                {
                    string s = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + "\t" + param1 + "\t" + param2;
                    sw.WriteLine(s);
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + mDebugLogFullPath + " Exception: " + e.ToString());
            }

        }

        public string ToWebHTML(int numEvents, bool fromDebugLog)
        {
            string fName = fromDebugLog ? mEventLogFullPath : mDebugLogFullPath;

            // Read file and count lines
            int lineCount = 0;
            try
            {
                using (StreamReader sr = new StreamReader(fName))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                        lineCount++;
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + fName + " Exception: " + e.ToString());
            }

            if (lineCount <= 0)
                return "No Events";

            string s = "<div class=\"eventslist\">";

            int lineCount2 = 0;
            try
            {
                using (StreamReader sr = new StreamReader(fName))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        lineCount2++;
                        if (lineCount2 > lineCount - numEvents)
                        {
                            string[] lc = line.Split(new char[] { '\t' });
                            s += "<div class=\"eventsrow\">";
                            foreach (string lss in lc)
                            {
                                s += "<div class=\"eventscol\">" + lss + "</div>";
                            }
                            s += "</div>\n";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + fName + " Exception: " + e.ToString());
            }

            s += "</div>";
//            Debug.Print("Event list HTML: " + s);
            return s;
        }

    }
}
