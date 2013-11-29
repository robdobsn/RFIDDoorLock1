// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Time;
using System.Threading;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using Microsoft.SPOT.Hardware;
using Blinq.Netduino.Web;

namespace RFIDDoorLock1
{
    public class Program
    {
        const int NUM_DIGITS_IN_CODE = 4;
        static EventLogger eventLogger;
        static SettingsDb settingsDb;
        static RFIDReader rfidReader;
        static RfidTagDb rfidTagDb;
        static DoorStrike mainDoorStrike;
        static DoorStrike innerDoorStrike;
        static TapDecoder tapDecoder;
        static DateTime rfidCheckLoopTimer = DateTime.Now;
        static DateTime testTimer = DateTime.Now;
        static string testTagId = "";
        static TimeSpan timeoutOnRfidCheckLoop = new TimeSpan(0, 0, 0, 0, 250);
        static TimeSpan timeoutOnTest = new TimeSpan(0, 0, 20);
        static DateTime doorBellLastRing = DateTime.MinValue;
        static TimeSpan DOOR_BELL_TIMEOUT_TS = new TimeSpan(0,0,5);
        static OutputPort onboardLed;
        static int onboardLedCount = 0;
        static int onboardLedRate = 250;
        static OutputPort watchdog;

        public static void Main()
        {
            // Onboard LED
            onboardLed = new OutputPort(Pins.ONBOARD_LED, false);

            // Watchdog
            watchdog = new OutputPort(Pins.GPIO_PIN_D8, false);

            // Get network time
            DateTimeSetter.SetTimeFromNTPWhenPossible();

            // Event log
            eventLogger = new EventLogger(@"events.txt", @"debug.txt");

            // Settings
            settingsDb = new SettingsDb(@"Settings.txt");

            // RFID reader
            rfidReader = new RFIDReader();

            // RFID Tag Database
            rfidTagDb = new RfidTagDb(@"TagIds.txt");

            // Door strikes
            mainDoorStrike = new DoorStrike(Pins.GPIO_PIN_D1, Pins.GPIO_PIN_D4);
            innerDoorStrike = new DoorStrike(Pins.GPIO_PIN_D0, Pins.GPIO_NONE);

            // Tap decoder
            tapDecoder = new TapDecoder(NUM_DIGITS_IN_CODE, Pins.GPIO_PIN_D5);

            // Instantiate a new web server on port 80.
            WebServer server = new WebServer(80);

            // Add a handler for commands that are received by the server.
            server.CommandReceived += new WebServer.CommandReceivedHandler(server_CommandReceived);

            // Command to receive the index page
            server.AllowedCommands.Add(new WebCommand("", 0));
            server.AllowedCommands.Add(new WebCommand("setup", 0));
            server.AllowedCommands.Add(new WebCommand("setup.html", 0));
            server.AllowedCommands.Add(new WebCommand("status", 0));
            server.AllowedCommands.Add(new WebCommand("addtag", 1));
            server.AllowedCommands.Add(new WebCommand("testtag", 1));
            server.AllowedCommands.Add(new WebCommand("main-unlock", 0));
            server.AllowedCommands.Add(new WebCommand("main-lock", 0));
            server.AllowedCommands.Add(new WebCommand("inner-unlock", 0));
            server.AllowedCommands.Add(new WebCommand("inner-lock", 0));
            server.AllowedCommands.Add(new WebCommand("submit-settings", 1));
            server.AllowedCommands.Add(new WebCommand("get-settings", 0));
            server.AllowedCommands.Add(new WebCommand("get-tags", 0));
            server.AllowedCommands.Add(new WebCommand("get-events", 0));
            server.AllowedCommands.Add(new WebCommand("get-debug", 0));

            // Start the server.
            server.Start();

            // loop forever...
            while (true)
            {
                // Toggle LED
                onboardLedCount++;
                if (onboardLedCount > onboardLedRate)
                {
                    onboardLedCount = 0;
                    onboardLed.Write(!onboardLed.Read());
                }

                // Pulse watchdog to keep alive
                watchdog.Write(true);
                Thread.Sleep(1);
                watchdog.Write(false);

                // Service the rfid reader - this allows it to get characters and interpret them
                rfidReader.Service();

                // Service the door strikes - allowing them to time-out, etc
                mainDoorStrike.Service();
                innerDoorStrike.Service();

                // Check for doorbell ring
                if (tapDecoder.IsTapperPressed())
                {
                    // Check we don't broadcast constantly
                    if ((DateTime.Now > doorBellLastRing + DOOR_BELL_TIMEOUT_TS))
                    {
                        BroadcastBellRing.Send();
                        doorBellLastRing = DateTime.Now;
                    }
                }

                // Check if there is a tag present
                if (DateTime.Now > rfidCheckLoopTimer + timeoutOnRfidCheckLoop)
                {
                    String tagId = rfidReader.GetRFIDTagId();

                    // Test code to inject a tag
                    if (tagId.Length == 0)
                    {
                        if (testTagId.Length > 0)
                        {
                            tagId = testTagId;
                            if (DateTime.Now > testTimer + timeoutOnTest)
                            {
                                testTagId = "";
                            }
                        }
                    }

                    // Door strike action
                    if (tagId.Length > 0)
                    {
                        string holderName = "";
                        bool isEnabled = false;
                        if (rfidTagDb.IsTagKnown(tagId, out holderName, out isEnabled))
                        {
                            if (isEnabled)
                            {
                                // Unlock main door - and log event if it was previously locked
                                if (mainDoorStrike.Unlock(settingsDb.mainAutoTimeout))
                                    eventLogger.LogEntryEvent(tagId, holderName);

                                // Unlock inner too
                                innerDoorStrike.Unlock(settingsDb.innerAutoTimeout);
                            }
                        }
                        //Debug.Print(tagID);
                    }
                    rfidCheckLoopTimer = DateTime.Now;
                }

                // The following code attempts to emulate the "tap-lock" that I previously
                // implemented

                // Check for tapper
                if (tapDecoder.IsCodeReady())
                {
                    int theCode = tapDecoder.GetCode();
                    Debug.Print("Code is " + theCode.ToString());

                    // Currently the code is fixed here - but the digit 0 isn't valid so this
                    // needs to be changed to a 4 digit code using digits 1..9
                    if (theCode == 0000)
                    {
                        // Unlock main door - and log event if it was previously locked
                        if (mainDoorStrike.Unlock(settingsDb.mainAutoTimeout))
                            eventLogger.LogEntryEvent("Tapper", "Unknown");

                        // Unlock inner too
                        innerDoorStrike.Unlock(settingsDb.innerAutoTimeout);
                    }
                    eventLogger.LogDebug("Code Entry " + theCode, tapDecoder.GetTapTimingsStr());
                    tapDecoder.Reset();
                }

            }

        }

        /// <summary>
        /// Handles the CommandReceived event.
        /// </summary>
        private static void server_CommandReceived(object source, WebCommandEventArgs e)
        {

            try
            {

                if (e.Command.CommandString != "status")
                    Debug.Print("Command received:" + e.Command.CommandString);

                // Check for home page
                bool bReturnStatus = false;
                if (e.Command.CommandString == "")
                {
                    e.ReturnString = Resources.GetString(Resources.StringResources.index);
                }
                else if ((e.Command.CommandString == "setup") || (e.Command.CommandString == "setup.html"))
                {
                    e.ReturnString = Resources.GetString(Resources.StringResources.setup);
                }
                else if (e.Command.CommandString == "status")
                {
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "addtag")
                {
                    TagInfo tagInfo = new TagInfo();
                    tagInfo.FromWebSerialized(e.Command.Arguments[0].ToString());
                    rfidTagDb.AddTagToDb(tagInfo.mTagId, tagInfo.mHolderName, tagInfo.mIsEnabled);
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "testtag")
                {
                    TagInfo tagInfo = new TagInfo();
                    tagInfo.FromWebSerialized(e.Command.Arguments[0].ToString());
                    testTagId = tagInfo.mTagId;
                    testTimer = DateTime.Now;
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "main-unlock")
                {
                    mainDoorStrike.Unlock(settingsDb.mainManualTimeout);
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "main-lock")
                {
                    mainDoorStrike.Lock();
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "inner-unlock")
                {
                    innerDoorStrike.Unlock(settingsDb.innerManualTimeout);
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "inner-lock")
                {
                    innerDoorStrike.Lock();
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "submit-settings")
                {
                    settingsDb.FromWebSerialized(e.Command.Arguments[0].ToString());
                    settingsDb.WriteValuesToFile();
                    Debug.Print("Submit settings " + e.Command.Arguments[0].ToString());
                    bReturnStatus = true;
                }
                else if (e.Command.CommandString == "get-settings")
                {
                    e.ReturnString = settingsDb.ToWebSerialized();
                }
                else if (e.Command.CommandString == "get-tags")
                {
                    e.ReturnString = rfidTagDb.ToWebHTML();
                }
                else if (e.Command.CommandString == "get-events")
                {
                    e.ReturnString = eventLogger.ToWebHTML(20, true);
                }
                else if (e.Command.CommandString == "get-debug")
                {
                    e.ReturnString = eventLogger.ToWebHTML(20, false);
                }
                if (e.ReturnString == null || e.ReturnString == "")
                {
                    e.ReturnString = "<html><body></body></html>";
                }

                // Handle commands that require status to be returned
                if (bReturnStatus)
                {
                    string tagId = rfidReader.GetRFIDTagId().Trim();
                    // Test inject tag id
                    if (tagId.Length == 0)
                        if (testTagId.Length > 0)
                            tagId = testTagId;
                    string holderName = "";
                    bool isEnabled = false;
                    bool tagKnown = rfidTagDb.IsTagKnown(tagId, out holderName, out isEnabled);
                    //               e.ReturnString = "<html><body>";
                    string mainDoorOpenStr = "Unknown";
                    switch (mainDoorStrike.IsOpen())
                    {
                        case DoorStrike.DoorOpenSense.Open:
                            mainDoorOpenStr = "Open";
                            break;
                        case DoorStrike.DoorOpenSense.Closed:
                            mainDoorOpenStr = "Closed";
                            break;
                    }
                    e.ReturnString += tagId;
                    e.ReturnString += "," + (tagId == "" ? "NoTagPresent" : tagKnown ? ((holderName == "" ? "Known" : holderName) + " " + (isEnabled ? "" : "(DISABLED)")) : "Unknown");
                    e.ReturnString += "," + (mainDoorStrike.IsLocked() ? "Locked" : "Unlocked");
                    e.ReturnString += "," + mainDoorOpenStr;
                    e.ReturnString += "," + (innerDoorStrike.IsLocked() ? "Locked" : "Unlocked");
                    e.ReturnString += "," + (tapDecoder.IsTapperPressed() ? "Ring" : "No");

                    //              e.ReturnString += "</body></html>";
                }
            }
            catch (Exception excp)
            {
                Debug.Print("Exception in server_CommandReceived " + excp.Message);
            }
        }
    }
}
