using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine.Assertions;

namespace Tiles.Puzzles.Power
{
    public class PowerInfo : IEquatable<PowerInfo>
    {
        private readonly struct Entry : IEquatable<Entry>, IComparable<Entry>
        {
            public readonly PowerFeature Source;
            public readonly bool Power;
            public readonly int Version;

            public Entry(PowerFeature source, bool power, int version)
            {
                Source = source;
                Power = power;
                Version = version;
            }

            public int CompareTo(Entry other)
            {
                if (Power != other.Power) return Power.CompareTo(other.Power);
                if (Version != other.Version) return Version.CompareTo(other.Version);
                return Source.GetInstanceID().CompareTo(other.Source.GetInstanceID());
            }

            public bool Equals(Entry other) =>
                Source == other.Source &&
                Power == other.Power &&
                Version == other.Version;

            public override bool Equals(object obj) => obj is Entry e && Equals(e);
            public override int GetHashCode() => HashCode.Combine(Source, Power, Version);
            public static bool operator ==(Entry lhs, Entry rhs) => lhs.Equals(rhs);
            public static bool operator !=(Entry lhs, Entry rhs) => !(lhs == rhs);
        }

        /// <summary>
        /// Compares two <see cref="PowerInfo"/> on the basis of their public interface.
        /// This is the default comparer used to implement <see cref="operator=="/>
        /// </summary>
        internal class Comparer : EqualityComparer<PowerInfo>
        {
            public override bool Equals(PowerInfo x, PowerInfo y)
            {
                if (x is null && y is null) return true;
                if (x is null || y is null) return false;
                if (ReferenceEquals(x, y)) return true;
                return x.HasPower() == y.HasPower();
            }

            public override int GetHashCode(PowerInfo pi)
            {
                if (pi is null) return -1;
                return pi.HasPower().GetHashCode();
            }
        }

        /// <summary>
        /// Compares two <see cref="PowerInfo"/> on the basis of their contents.
        /// </summary>
        internal class StrictComparer : EqualityComparer<PowerInfo>
        {
            public override bool Equals(PowerInfo x, PowerInfo y)
            {
                if (x is null && y is null) return true;
                if (x is null || y is null) return false;
                if (ReferenceEquals(x, y)) return true;
                return x.entries.SequenceEqual(y.entries);
            }

            public override int GetHashCode(PowerInfo pi)
            {
                if (pi is null) return -1;
                int hash = 0;
                foreach (var entry in pi.entries) hash = HashCode.Combine(hash, entry);
                return hash;
            }
        }

        internal static readonly Comparer DefaultEquals = new Comparer();
        internal static readonly StrictComparer StrictEquals = new StrictComparer();

        public static readonly PowerInfo None = new PowerInfo();

        private readonly List<Entry> entries = new List<Entry>();

        private PowerInfo() : this(new List<Entry>()) {}

        private PowerInfo(List<Entry> entries)
        {
            entries.Sort();
            this.entries = entries;
        }

        public PowerInfo SetSource(PowerFeature source, bool power)
        {
            Assert.IsNotNull(source);

            int changedIndex = -1;

            for (int i = 0; i < entries.Count; i++)
            {
                Entry entry = entries[i];
                if (entry.Source == source)
                {
                    if (entry.Power == power) return this;
                    changedIndex = i;
                    break;
                }
            }

            List<Entry> newEntries = new(entries);
            if (changedIndex < 0) newEntries.Add(new Entry(source, power, 0));
            else newEntries[changedIndex] = new Entry(source, power, newEntries[changedIndex].Version + 1);
            return new PowerInfo(newEntries);
        }

        public PowerInfo Combine(PowerInfo other)
        {
            if (Equals(other)) return this;
            other ??= None;

            List<Entry> resulting = new(entries);

            void CombineEntry(Entry newEntry)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var oldEntry = resulting[i];
                    if (newEntry.Source == oldEntry.Source)
                    {
                        if (newEntry.Version >= oldEntry.Version)
                        {
                            resulting.RemoveAt(i);
                            resulting.Add(newEntry);
                        }
                        return;
                    }
                }

                resulting.Add(newEntry);
            }

            foreach (var newEntry in other.entries) CombineEntry(newEntry);
            return new PowerInfo(resulting);
        }

        public bool HasPower()
        {
            foreach (var entry in entries)
            {
                if (entry.Power) return true;
            }

            return false;
        }

        public bool Equals(PowerInfo other) => DefaultEquals.Equals(this, other);
        public override int GetHashCode() => DefaultEquals.GetHashCode(this);
        public override bool Equals(object obj) => Equals(obj as PowerInfo);
        public static bool operator ==(PowerInfo lhs, PowerInfo rhs) => DefaultEquals.Equals(lhs, rhs);
        public static bool operator !=(PowerInfo lhs, PowerInfo rhs) => !(lhs == rhs);
    }
}