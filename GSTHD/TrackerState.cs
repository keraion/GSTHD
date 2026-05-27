using System;
using System.Collections.Generic;

namespace GSTHD
{
    public class TrackerState
    {
        public const int CurrentVersion = 1;
        public const string DefaultFileName = "tracker_state.json";

        public int Version { get; set; } = CurrentVersion;
        public string LayoutPath { get; set; } = string.Empty;
        public DateTime SavedAtUtc { get; set; } = DateTime.UtcNow;

        public Dictionary<string, int> Items { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CollectedItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DoubleItems { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Medallions { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MedallionDungeons { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Songs { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, SongMarkerState> SongMarkers { get; set; } = new Dictionary<string, SongMarkerState>();
        public Dictionary<string, GossipStoneState> GossipStones { get; set; } = new Dictionary<string, GossipStoneState>();
        public Dictionary<string, PanelWothBarrenState> PanelStates { get; set; } = new Dictionary<string, PanelWothBarrenState>();
    }

    public class PanelWothBarrenState
    {
        public List<WothEntryState> WothEntries { get; set; } = new List<WothEntryState>();
        public List<BarrenEntryState> BarrenEntries { get; set; } = new List<BarrenEntryState>();
    }

    public class WothEntryState
    {
        public string Name { get; set; } = string.Empty;
        public int ColorIndex { get; set; } = 0;
        public List<GossipStoneState> GossipStoneStates { get; set; } = new List<GossipStoneState>();
    }

    public class BarrenEntryState
    {
        public string Name { get; set; } = string.Empty;
        public int ColorIndex { get; set; } = 0;
    }
}
