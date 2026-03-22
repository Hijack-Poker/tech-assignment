using UnityEngine;

namespace HijackPoker.Analytics
{
    public enum PlayStyle
    {
        Unknown,
        TAG,          // Tight Aggressive
        LAG,          // Loose Aggressive
        Nit,          // Very tight, very passive
        Rock,         // Tight passive
        Fish,         // Loose passive
        Maniac,       // Very loose, very aggressive
        CallingStation // Loose, rarely raises
    }

    public static class PlayStyleHelper
    {
        public static string GetLabel(PlayStyle style)
        {
            switch (style)
            {
                case PlayStyle.TAG: return "TAG";
                case PlayStyle.LAG: return "LAG";
                case PlayStyle.Nit: return "NIT";
                case PlayStyle.Rock: return "ROCK";
                case PlayStyle.Fish: return "FISH";
                case PlayStyle.Maniac: return "MANIAC";
                case PlayStyle.CallingStation: return "CALL STN";
                default: return "";
            }
        }

        public static Color GetColor(PlayStyle style)
        {
            switch (style)
            {
                case PlayStyle.TAG: return new Color(0.20f, 0.60f, 0.86f);     // Blue
                case PlayStyle.LAG: return new Color(0.90f, 0.49f, 0.13f);     // Orange
                case PlayStyle.Nit: return new Color(0.55f, 0.55f, 0.62f);     // Gray
                case PlayStyle.Rock: return new Color(0.55f, 0.55f, 0.62f);    // Gray
                case PlayStyle.Fish: return new Color(0.06f, 0.72f, 0.50f);    // Green
                case PlayStyle.Maniac: return new Color(0.94f, 0.27f, 0.27f);  // Red
                case PlayStyle.CallingStation: return new Color(0.74f, 0.57f, 0.91f); // Purple
                default: return Color.clear;
            }
        }

        public static string GetDescription(PlayStyle style)
        {
            switch (style)
            {
                case PlayStyle.TAG: return "Tight Aggressive: selective but assertive";
                case PlayStyle.LAG: return "Loose Aggressive: plays many hands aggressively";
                case PlayStyle.Nit: return "Nit: extremely tight, folds almost everything";
                case PlayStyle.Rock: return "Rock: tight and passive, rarely bluffs";
                case PlayStyle.Fish: return "Fish: plays too many hands passively";
                case PlayStyle.Maniac: return "Maniac: hyper-aggressive, bets and raises constantly";
                case PlayStyle.CallingStation: return "Calling Station: calls everything, rarely raises";
                default: return "Not enough data to classify";
            }
        }
    }
}
