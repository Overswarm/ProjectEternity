using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public class GameEvent
    {
        public float Year;
        public string Text;
        public Color Color;
        public int TileX = -1, TileY = -1;   // optional world location (camera jump)
        public bool HasLocation => TileX >= 0;
    }

    /// <summary>
    /// The Chronicle — every autonomous act of the simulation is narrated here.
    /// "Legible emergence" pillar: history should read like history.
    /// </summary>
    public class EventLog
    {
        public readonly List<GameEvent> Entries = new List<GameEvent>();
        const int MaxEntries = 400;

        public void Log(float year, string text, Color color, int tx = -1, int ty = -1)
        {
            Entries.Add(new GameEvent { Year = year, Text = text, Color = color, TileX = tx, TileY = ty });
            if (Entries.Count > MaxEntries) Entries.RemoveRange(0, Entries.Count - MaxEntries);
        }

        public static string FormatYear(float year)
        {
            int y = Mathf.RoundToInt(year);
            return y < 0 ? (-y) + " BC" : y + " AD";
        }
    }
}
