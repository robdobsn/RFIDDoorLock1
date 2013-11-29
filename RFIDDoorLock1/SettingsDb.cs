// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.IO;

namespace RFIDDoorLock1
{
    class SettingsDb
    {
        private string mDbFname = "";
        private string mDbFullPath = "";

        public int mainAutoTimeout {get; set; }
        public int mainManualTimeout { get; set; }
        public int innerAutoTimeout { get; set; }
        public int innerManualTimeout { get; set; }

        private const string mainAutoString = "main-door-auto-timeout";
        private const string mainManualString = "main-door-manual-timeout";
        private const string innerAutoString = "inner-door-auto-timeout";
        private const string innerManualString = "inner-door-manual-timeout";

        public SettingsDb(String dbFname)
        {
            mDbFname = dbFname;
            mDbFullPath = @"\sd\" + mDbFname;
            mainAutoTimeout = 2;
            mainManualTimeout = 15;
            innerAutoTimeout = 30;
            innerManualTimeout = 30;
            ReadFromFile();
        }

        public void FromWebSerialized(string webSer)
        {
            string[] webSerArgs = webSer.Split('&');
            foreach (string s in webSerArgs)
            {
                string[] nameval = s.Split('=');
                switch (nameval[0])
                {
                    case mainAutoString:
                        mainAutoTimeout = Convert.ToInt16(nameval[1]);
                        break;
                    case mainManualString:
                        mainManualTimeout = Convert.ToInt16(nameval[1]);
                        break;
                    case innerAutoString:
                        innerAutoTimeout = Convert.ToInt16(nameval[1]);
                        break;
                    case innerManualString:
                        innerManualTimeout = Convert.ToInt16(nameval[1]);
                        break;
                }
            }
        }

        public string ToWebSerialized()
        {
            string s = "";
            s += mainAutoString + "=" + mainAutoTimeout.ToString();
            s += "&" + mainManualString + "=" + mainManualTimeout.ToString();
            s += "&" + innerAutoString + "=" + innerAutoTimeout.ToString();
            s += "&" + innerManualString + "=" + innerManualTimeout.ToString();
            return s;
        }

        public void WriteValuesToFile()
        {
            // Form to string to write
            string s = ToWebSerialized();

            // Overwrite data in file
            try
            {
                using (StreamWriter sw = new StreamWriter(mDbFullPath, false))
                {
                    sw.WriteLine(s);
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + mDbFullPath + " Exception: " + e.ToString());
            }
        }

        private void ReadFromFile()
        {
            // Open tags file
            try
            {
                using (StreamReader sr = new StreamReader(mDbFullPath))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        line = line.Trim();
                        if (line.Length < 0)
                            continue;
                        FromWebSerialized(line);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + mDbFullPath + " Exception: " + e.ToString());
            }
        }
    }
}
