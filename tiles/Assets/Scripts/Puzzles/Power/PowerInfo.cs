using System.Collections.Generic;
using System.Linq;

namespace Tiles.Puzzles.Power
{
    public class PowerInfo
    {
        private struct SourceEntry
        {
            public PowerSourceFeature Source;
            public PowerType Type;
            public int Strength;
            public int Version;

            public SourceEntry(PowerSourceFeature source, PowerType type, int strength)
            {
                Source = source;
                Type = type;
                Strength = strength;
                Version = 1;
            }

            public bool Compare(PowerSourceFeature source, PowerType type) => Source == source && Type == type;
            public bool Compare(SourceEntry entry) => Compare(entry.Source, entry.Type);
        }

        private readonly List<SourceEntry> sources;

        private PowerInfo(List<SourceEntry> sources) => this.sources = sources;
        private PowerInfo() : this(new List<SourceEntry>()) {}
        private PowerInfo(PowerInfo other) : this(other.sources.ToList()) {}

        public PowerInfo WithSource(PowerSourceFeature source, PowerType type, int strength = 1)
        {
            PowerInfo pi = new PowerInfo(this);
            for (int i = 0; i < pi.sources.Count; i++)
            {
                SourceEntry entry = pi.sources[i];
                if (entry.Compare(source, type))
                {
                    if (entry.Strength == strength) return pi;
                    entry.Strength = strength;
                    entry.Version++;
                    pi.sources[i] = entry;
                    return pi;
                }
            }

            // Source not found, we need to add it
            pi.sources.Add(new SourceEntry(source, type, strength));
            return pi;
        }

        public PowerInfo Combine(PowerInfo other)
        {
            PowerInfo pi = new PowerInfo(this);
            for (int i = 0; i < other.sources.Count; i++)
            {
                SourceEntry otherEntry = other.sources[i];

                // If this source/type is already in `pi`, we have a conflict and must resolve it
                // To do so, we choose the entry with the highest version
                int thisIndex = sources.FindIndex(entry => entry.Compare(otherEntry));
                if (thisIndex >= 0)
                {
                    SourceEntry thisEntry = pi.sources[thisIndex];
                    if (thisEntry.Version == otherEntry.Version)
                        pi.sources[thisIndex] = thisEntry.Strength > otherEntry.Strength ? thisEntry : otherEntry;
                    else pi.sources[thisIndex] = thisEntry.Version > otherEntry.Version ? thisEntry : otherEntry;
                } else pi.sources.Add(otherEntry);
            }

            return pi;
        }

        public bool HasPower(PowerType type)
        {
            foreach (var src in sources)
            {
                if (src.Strength > 0 && src.Type == type) return true;
            }
            return false;
        }

        public int GetPower(PowerType type)
        {
            int sum = 0;
            foreach (var src in sources)
            {
                if (src.Type == type) sum += src.Strength;
            }
            return sum;
        }
    }
}