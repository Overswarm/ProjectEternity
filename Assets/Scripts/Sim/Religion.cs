using UnityEngine;

namespace Eternity
{
    /// <summary>
    /// A faith born in a pious city. Fervent faiths unify and spread hard but drag
    /// science; contemplative faiths are mild in everything.
    /// Religion id -1 everywhere means "folk beliefs" (the default share).
    /// </summary>
    public class Religion
    {
        public int Id;
        public string Name;
        public Color Color;
        public float Fervor;       // 0 contemplative .. 1 fervent
        public int HolyCityId;
        public float FoundedYear;

        public string Temperament => Fervor > 0.66f ? "fervent" : Fervor > 0.33f ? "devout" : "contemplative";
    }
}
