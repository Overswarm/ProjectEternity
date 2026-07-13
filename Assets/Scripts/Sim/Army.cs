using System.Collections.Generic;
using UnityEngine;

namespace Eternity
{
    public enum Mission { Raid, Conquer, Return, Settle }

    /// <summary>
    /// Abstract strength-stack marching overland. Settlers are armies with a
    /// different mission and no teeth.
    /// </summary>
    public class Army
    {
        public int Id;
        public int CivId;
        public float Strength;
        public Mission Mission;
        public City TargetCity;         // for Raid/Conquer
        public City HomeCity;           // garrison returns here
        public Tile TargetTile;         // for Settle / Return
        public bool Autonomous;         // launched by the city itself, not the leader

        public List<Tile> Path;
        public int PathIndex;
        public Vector2 Pos;             // fractional grid position for smooth marching
        public bool Dead;

        public Tile CurrentTile(WorldMap map) =>
            map.GetSafe(Mathf.RoundToInt(Pos.x), Mathf.RoundToInt(Pos.y));
    }
}
