// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.Text.RegularExpressions;

namespace RFIDDoorLock1
{
    class TagInfo
    {
        public string mTagId;
        public string mHolderName;
        public bool mIsEnabled;

        public TagInfo()
        {
            mTagId = "";
            mHolderName = "";
            mIsEnabled = false;
        }

        public TagInfo(string tagId, string holderName, bool isEnabled)
        {
            mTagId = tagId;
            mHolderName = holderName;
            mIsEnabled = isEnabled;
        }

        public void FromWebSerialized(string webSer)
        {
            mIsEnabled = false;
            mHolderName = "";
            mTagId = "";
            string[] webSerArgs = webSer.Split('&');
            foreach (string s in webSerArgs)
            {
                string[] nameval = s.Split('=');
                switch (nameval[0])
                {
                    case "tag-tagid":
                        mTagId = URLDecode(nameval[1].Trim());
                        break;
                    case "tag-holdername":
                        mHolderName = URLDecode(nameval[1].Trim());
                        break;
                    case "tag-enabled": // Note that if the enabled is included then it is checked (not serialized if unchecked)
                        mIsEnabled = true;
                        break;
                }
            }
        }

        public static string URLDecode(string encodedString)
        {
            string outStr = string.Empty;

            int i = 0;
            while (i < encodedString.Length)
            {
                switch (encodedString[i])
                {
                    case '+': outStr += " "; break;
                    case '%':
                        string tempStr = encodedString.Substring(i + 1, 2);
                        outStr += Convert.ToChar((ushort)Convert.ToInt32(tempStr, 16));
                        i = i + 2;
                        break;
                    default:
                        outStr += encodedString[i];
                        break;
                }
                i++;
            }
            return outStr;
        }
    }
}
