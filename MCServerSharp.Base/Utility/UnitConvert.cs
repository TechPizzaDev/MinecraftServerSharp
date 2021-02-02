using System;

namespace MCServerSharp.Net
{
    public static class UnitConvert
    {
        private static string[] _byteSuffixes1000 = { "", "K", "M", "G", "T", "P" };

        public static string ToReadable(long byteCount)
        {
            int order = 0;
            double length = byteCount;
            while (length >= 1000 && order < _byteSuffixes1000.Length - 1)
            {
                order++;
                length /= 1000;
            }

            int decimalCount = Math.Max(0, (int)Math.Ceiling(2 - Math.Log10(length))); // length < 10 ? 2 : 1;
            string format = decimalCount switch
            {
                2 => "0.00",
                1 => "0.0",
                _ => "0"
            };
            string result = length.ToString(format) + _byteSuffixes1000[order];
            return result;
        }
    }
}
