using System;
using System.Collections.Generic;

namespace PaperShift.Runtime
{
    public sealed class WeightedPicker<T>
    {
        private readonly List<Entry> entries = new List<Entry>();

        public int Count
        {
            get { return entries.Count; }
        }

        public void Add(T value, int weight)
        {
            if (weight <= 0)
            {
                return;
            }

            entries.Add(new Entry { Value = value, Weight = weight });
        }

        public T Pick(Random random)
        {
            if (entries.Count == 0)
            {
                return default(T);
            }

            var total = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                total += entries[i].Weight;
            }

            var roll = random.Next(0, total);
            var cursor = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                cursor += entries[i].Weight;
                if (roll < cursor)
                {
                    return entries[i].Value;
                }
            }

            return entries[entries.Count - 1].Value;
        }

        private sealed class Entry
        {
            public T Value;
            public int Weight;
        }
    }
}
