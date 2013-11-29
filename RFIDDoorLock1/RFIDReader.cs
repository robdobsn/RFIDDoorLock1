// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using System.IO.Ports;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;


namespace RFIDDoorLock1
{
    class RFIDReader
    {
        static SerialPort serial;
        static DateTime lastChReceived = DateTime.Now;
        bool requiredCTSState = true;
        InputPort ctsFromReader;
        DateTime lastReqSent = DateTime.Now;
        const int RFID_ID_MAXLEN = 20;
        static byte[] rfidBuf = new byte[RFID_ID_MAXLEN];
        static int rfidBufPos = 0;
        String currentTagId = "";
        DateTime timeTagIdLastReceived = DateTime.Now;
        
        public RFIDReader()
        {
            // Serial port is used to communicate with the RFID reader
            // initialize the serial port for COM2 (using D2 & D3)
            serial = new SerialPort("COM2", 9600, Parity.None, 8, StopBits.One);
            // open the serial-port, so we can send & receive data
            serial.Open();
            // add an event-handler for handling incoming data
            serial.DataReceived += new SerialDataReceivedEventHandler(serial_DataReceived);

            // Reader module details here: http://www.ibtechnology.co.uk/pdf/MFprot_LP.PDF
            // Reader module can only be talked to when CTS is low - so use Pin 7 as input to read CTS line
            ctsFromReader = new InputPort(Pins.GPIO_PIN_D7, false, Port.ResistorMode.Disabled);
        }

        public void Service()
        {
            // Check if there is something in the buffer and nothing has been received for a while
            if (rfidBufPos > 0)
                if (DateTime.Now > lastChReceived + new TimeSpan(0, 0, 0, 0, 50))
                {
                    // Tag ID as a string of hex chars
                    String tagIdStr = "";
                    for (int i = 1; i < rfidBufPos; i++)
                        tagIdStr += rfidBuf[i].ToString("X2");
                    rfidBufPos = 0;
                    currentTagId = tagIdStr;
                    timeTagIdLastReceived = DateTime.Now;
                }

            // Check if enough time has elapsed since last request sent
            if (DateTime.Now < lastReqSent + new TimeSpan(0,0,0,0,500))
                return;

            // Wait for CTS to transition
            if (ctsFromReader.Read() == requiredCTSState)
            {
                if (requiredCTSState == false)
                {
                    // send an 0x55 which is the command to read the card ID
                    try
                    {
                        byte[] buf = new byte[1];
                        buf[0] = 0x55;
                        serial.Write(buf, 0, 1);
                        lastReqSent = DateTime.Now;
                    }
                    catch (Exception e)
                    {
                        Debug.Print(e.ToString());
                    }
                }

                // Now wait for the next cycle before sending another request
                requiredCTSState = !requiredCTSState;
            }

            // Check if the last time the tag id set was a long time ago - e.g. comms broken down
            // and reset the tagid if so
            if (currentTagId != "")
                if (DateTime.Now > timeTagIdLastReceived + new TimeSpan(0, 0, 10))
                    currentTagId = "";
        }

        public String GetRFIDTagId()
        {
            // Return ID of last tag read - or empty string if no tag present
            return currentTagId;
        }
    
        static void serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // create a single byte array
            byte[] bytes = new byte[1];

            // as long as there is data waiting to be read
            while (serial.BytesToRead > 0)
            {
                // read a single byte
                serial.Read(bytes, 0, bytes.Length);
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (rfidBufPos < RFID_ID_MAXLEN)
                        rfidBuf[rfidBufPos++] = bytes[i];
                }
                lastChReceived = DateTime.Now;
            }
        }
    }
}
