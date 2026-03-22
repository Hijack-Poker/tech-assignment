using System;
using System.Collections.Generic;

namespace HijackPoker.Models
{
    public class HandHistoryEntry
    {
        public int HandNo;
        public int Step;
        public string StepLabel;
        public string[] WinnerNames;
        public float PotSize;
        public Dictionary<string, float> StackDeltas;
        public DateTime Timestamp;
    }
}
