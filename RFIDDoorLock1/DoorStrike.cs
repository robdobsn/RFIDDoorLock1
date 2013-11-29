// RFID Door Control Software
// Rob Dobson 2012-2013

using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace RFIDDoorLock1
{
    class DoorStrike
    {
        OutputPort doorStrikePort = null;
        InputPort doorOpenSensePort = null;
        TimeSpan mTimeOutOnUnlock = new TimeSpan(0,0,1);
        DateTime unlockedTime = DateTime.MaxValue;
        TimeSpan MIN_TIMOUT_AFTER_UNLOCK = new TimeSpan(0,0,1);

        public enum DoorOpenSense
        {
            Unknown,
            Closed,
            Open
        }

        public DoorStrike(Cpu.Pin doorStrikePin, Cpu.Pin doorOpenSensePin)
        {
            doorStrikePort = new OutputPort(doorStrikePin, true);
            if (doorOpenSensePin != Cpu.Pin.GPIO_NONE)
                doorOpenSensePort = new InputPort(doorOpenSensePin, true, Port.ResistorMode.PullUp);
        }

        public void Service()
        {
            // Handle re-locking door
            if (unlockedTime != DateTime.MaxValue)
            {
                // Two cases are that the door is now open and unlock occurred more than a minimum time ago
                // OR the maximum time limit for door unlocked has been exceeded
                bool case1 = (DateTime.Now > unlockedTime + MIN_TIMOUT_AFTER_UNLOCK) && (IsOpen() == DoorOpenSense.Open);
                bool case2 = (DateTime.Now > unlockedTime + mTimeOutOnUnlock);
                if (case1 || case2)
                    Lock();
            }
        }

        public bool Unlock(int timeoutInSecs)
        {
            // Check if already unlocked (for return value)
            bool wasLocked = IsLocked();

            // Allow door to open
            doorStrikePort.Write(false);

            // Store timeout
            mTimeOutOnUnlock = new TimeSpan(0,0,timeoutInSecs);

            // Set timer to time how long to leave door unlocked
            unlockedTime = DateTime.Now;

            return wasLocked;
        }

        public void Lock()
        {
            // Lock door
            doorStrikePort.Write(true);

            // Set timer to time how long to leave door unlocked
            unlockedTime = DateTime.MaxValue;
        }

        public bool IsLocked()
        {
            return (unlockedTime == DateTime.MaxValue);
        }

        public DoorOpenSense IsOpen()
        {
            if (doorOpenSensePort == null)
                return DoorOpenSense.Unknown;

            if (doorOpenSensePort.Read())
                return DoorOpenSense.Closed;
            return DoorOpenSense.Open;
        }
    }
}
