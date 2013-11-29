// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.IO;

namespace RFIDDoorLock1
{
    class RfidTagDb
    {
        const int MAX_TAGS = 100;
        string mDbFname = "";
        string mDbFullPath = "";
        TagInfo[] validTagInfos = new TagInfo[MAX_TAGS];
        int numValidTags = 0;

        public RfidTagDb(String dbFname)
        {
            mDbFname = dbFname;
            mDbFullPath = @"\sd\" + mDbFname;
            ReadValidTags();
        }

        private void ReadValidTags()
        {
            // Set to zero tags
            numValidTags = 0;

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

                        // Store tag data
                        string[] lineSplit = line.Split(new char [] {'\t'});
                        if (lineSplit.Length == 0)
                            continue;

                        string tagId = lineSplit[0].Trim();
                        string holderName = "";
                        bool isEnabled = true;
                        if (lineSplit.Length > 1)
                            holderName = lineSplit[1].Trim();
                        if (lineSplit.Length > 2)
                            isEnabled = (lineSplit[2][0] == 'E');
                        TagInfo tagInfo = new TagInfo(tagId, holderName, isEnabled);
                        validTagInfos[numValidTags] = tagInfo;

                        // Next tag
                        numValidTags++;
                        if (numValidTags >= MAX_TAGS)
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Print("Opening: " + mDbFullPath + " Exception: " + e.ToString());
            }
           // validTagIds[numValidTags++] = "42F770DA000000";
        }

        public bool IsTagKnown(string tagId, out string holderName, out bool isEnabled)
        {
            // Find tag id in database
            isEnabled = false;
            holderName = "";
            for (int i = 0; i < numValidTags; i++)
            {
                TagInfo ti = validTagInfos[i];
                if (tagId.Trim() == ti.mTagId)
                {
                    holderName = ti.mHolderName;
                    isEnabled = ti.mIsEnabled;
                    return true;
                }
            }
            return false;
        }

        public bool AddOrChangeTagInfo(string tagId, string holderName, bool isEnabled)
        {
            bool changesMade = false;

            // Find tagId if present
            for (int i = 0; i < numValidTags; i++)
            {
                TagInfo ti = validTagInfos[i];
                if (tagId.Trim() == ti.mTagId)
                {
                    if (ti.mHolderName != holderName)
                    {
                        ti.mHolderName = holderName.Trim();
                        changesMade = true;
                    }
                    if (ti.mIsEnabled != isEnabled)
                    {
                        ti.mIsEnabled = isEnabled;
                        changesMade = true;
                    }
                    return changesMade;
                }
            }

            // Not found so add tag to db
            TagInfo tagInfo = new TagInfo(tagId.Trim(), holderName, isEnabled);
            validTagInfos[numValidTags] = tagInfo;
            numValidTags++;
            changesMade = true;

            return changesMade;
        }

        public void AddTagToDb(string tagId, string holderName, bool isEnabled)
        {
            if (tagId.Trim() == "")
                return;

            // Add or change tag
            bool changesMade = AddOrChangeTagInfo(tagId, holderName, isEnabled);

            Debug.Print("AddTagToDb: " + tagId + " name = " + holderName + " en=" + isEnabled.ToString() + " changes = " + changesMade.ToString());

            // If any changes made rewrite the db file
            if (changesMade)
            {
                try
                {
                    using (StreamWriter sw = new StreamWriter(mDbFullPath, false))
                    {
                        for (int i = 0; i < numValidTags; i++)
                        {
                            sw.WriteLine(validTagInfos[i].mTagId.Trim() + "\t" +
                                    validTagInfos[i].mHolderName.Trim() + "\t" +
                                    (validTagInfos[i].mIsEnabled ? "Enabled" : "Disabled"));
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.Print("Opening: " + mDbFullPath + " Exception: " + e.ToString());
                }
            }
        }

        public string ToWebHTML()
        {
            string s = "<div class=\"tagslist\">";
            for (int i = 0; i < numValidTags; i++)
            {
                s += "<div class=\"tagsrow\">" +
                    "<div class=\"tagid\">" + validTagInfos[i].mTagId.Trim() + "</div>" +
                    "<div class=\"tagholdername\">" + validTagInfos[i].mHolderName.Trim() + "</div>" +
                    "<div class=\"tagenabled\">" + (validTagInfos[i].mIsEnabled ? "Enabled" : "Disabled") + "</div>" +
                    "</div>\n";
            }
            s += "</div>";
//            Debug.Print("Tag list HTML: " + s);
            return s;
        }
    }
}
