using System.Collections.Generic;
using UnityEngine;
using HijackPoker.Models;

namespace HijackPoker.Managers
{
    /// <summary>
    /// Snapshots per-table state so it can be saved/restored on table switch.
    /// </summary>
    public class TableContext
    {
        public int TableId;
        public bool WasAutoPlaying;
        public int SpeedIndex;
        public SessionTracker SessionTracker;
        public List<HandHistoryEntry> HandHistoryEntries = new();
        public int LastGameNo = -1;
        public Dictionary<int, float> StartOfHandStacks = new();
        public Dictionary<int, string> PrevPlayerActions = new();
        public TableResponse LastTableState;

        public struct HandHistoryEntry
        {
            public string Text;
            public Color Color;
        }
    }
}
