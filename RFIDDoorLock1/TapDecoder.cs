// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace RFIDDoorLock1
{
    class TapDecoder
    {
        const int BETWEEN_GROUPS_MS = 1500;
        const int BREAKS_BEFORE_ABANDON = 5;
        const int MAX_DIGITS = 4;
        const int MAX_TAP_TIMINGS = MAX_DIGITS * 10 * 2;
        TimeSpan DEBOUNCE_TIME = new TimeSpan(0, 0, 0, 0, 400);

        private int requiredDigits = MAX_DIGITS;
        private InterruptPort tapSensePort;
        private Timer intrTimer;
        private int taps = 0;
        private int consecutiveBreaks = 0;
        private int digitsTappedOut = 0;
        private int curDigitIdx = 0;
        private bool codeComplete = false;

        private int[] tapTimings = new int [MAX_TAP_TIMINGS];
        private int curTapTimingIdx = -1;
        
        private DateTime lastTapTime = DateTime.Now;

        public TapDecoder(int reqDigits, Cpu.Pin tapSensePin)
        {
            requiredDigits = reqDigits;
            tapSensePort = new InterruptPort(tapSensePin, true, Port.ResistorMode.PullUp, Port.InterruptMode.InterruptEdgeHigh);
            tapSensePort.OnInterrupt += new NativeEventHandler(port_OnInterrupt);
            intrTimer = new Timer(new TimerCallback(intr_OnTimer), null, -1, -1);
            Reset();
        }

        public void Reset()
        {
            intrTimer.Change(-1, -1);
            digitsTappedOut = 0;
            consecutiveBreaks = 0;
            curDigitIdx = 0;
            codeComplete = false;
            curTapTimingIdx = -1;
        }

        public bool IsCodeReady()
        {
            return codeComplete;
        }

        public int GetCode()
        {
            return digitsTappedOut;
        }

        void port_OnInterrupt(uint portN, uint pinState, DateTime time)
        {
            // Ignore taps if code already complete
            if (codeComplete)
                return;

            // Debounce
            if (time < lastTapTime + DEBOUNCE_TIME)
                return;

            // Reset the count of breaks and set the timer for a break
            consecutiveBreaks = 0;
            intrTimer.Change(BETWEEN_GROUPS_MS, -1);

            // Count the taps
//            if (data2 == 1)
                if (taps < 9)
                    taps++;
            //            String str = taps.ToString() + ", " + data1.ToString() + ", " + data2.ToString() + ", " + time.ToString();
            //            Debug.Print(str);
                Debug.Print(taps.ToString());

            // Record tap
            if (curTapTimingIdx < MAX_TAP_TIMINGS)
            {
                if (curTapTimingIdx != -1)
                    tapTimings[curTapTimingIdx] = (int)((DateTime.Now - lastTapTime).Ticks / TimeSpan.TicksPerMillisecond);
                curTapTimingIdx++;
            }

            lastTapTime = time;
        }

        void intr_OnTimer(Object state)
        {
            // Check if first break after some taps
            if (consecutiveBreaks == 0)
            {
                // We have a digit (number of taps)
                digitsTappedOut = digitsTappedOut * 10 + taps;
                taps = 0;
                curDigitIdx++;

                Debug.Print("Code " + digitsTappedOut);

                // Check for required digits
                if (curDigitIdx >= requiredDigits)
                {
                    codeComplete = true;
                    Debug.Print("Complete");
                    intrTimer.Change(-1, -1);
                    return;
                }
            }

            // Inc number of consecutive breaks
            consecutiveBreaks++;

            // Check for abandon - a long delay with no taps
            if (consecutiveBreaks >= BREAKS_BEFORE_ABANDON)
            {
                Reset();
                Debug.Print("ABANDON");
                return;
            }

            // Restart timer
            intrTimer.Change(BETWEEN_GROUPS_MS, -1);

                        Debug.Print("More than INTERVAL breaks " + consecutiveBreaks.ToString());
        }

        public bool IsTapperPressed()
        {
            return tapSensePort.Read();
        }

        public int[] GetTapTimings()
        {
            return tapTimings;
        }

        public string GetTapTimingsStr()
        {
            string s = "";
            for (int i = 0; i < curTapTimingIdx; i++)
                s += tapTimings[i].ToString() + " ";
            return s;
        }
    }
}
