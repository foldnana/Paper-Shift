using PaperShift.Domain;

namespace PaperShift.Runtime
{
    public sealed class FitProfile
    {
        private readonly int[] values = new int[8];

        public int Get(FitDimension dimension)
        {
            return values[(int)dimension];
        }

        public void Set(FitDimension dimension, int value)
        {
            values[(int)dimension] = Clamp(value);
        }

        public void Add(FitDimension dimension, int delta)
        {
            Set(dimension, Get(dimension) + delta);
        }

        private static int Clamp(int value)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > 100 ? 100 : value;
        }
    }
}
