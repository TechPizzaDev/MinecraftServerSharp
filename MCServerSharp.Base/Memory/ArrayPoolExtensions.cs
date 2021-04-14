using System;
using System.Buffers;

namespace MCServerSharp
{
    public static class ArrayPoolExtensions
    {
        public static T[] Resize<T>(this ArrayPool<T> pool, T[] array, int newMinimumLength)
        {
            T[] newArray = pool.Rent(newMinimumLength);

            int toCopy = Math.Min(newArray.Length, newMinimumLength);
            Buffer.BlockCopy(array, 0, newArray, 0, toCopy);

            pool.Return(array);
            return newArray;
        }

        public static void Resize<T>(this ArrayPool<T> pool, ref T[] array, int newMinimumLength)
        {
            array = pool.Resize(array, newMinimumLength);
        }

        public static T[] ReturnRent<T>(this ArrayPool<T> pool, T[] array, int newMinimumLength)
        {
            pool.Return(array);
            return pool.Rent(newMinimumLength);
        }

        public static void ReturnRent<T>(this ArrayPool<T> pool, ref T[] array, int newMinimumLength)
        {
            array = pool.ReturnRent(array, newMinimumLength);
        }
    }
}
