using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using MCServerSharp;

namespace Sandbox
{
    class Sandbox
    {
        static void Main(string[] args)
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
            //for (int i = 0; i < 1000_1000; i++)
            //{
            //    Digest(hasher, "Notch");
            //    Digest(hasher, "jeb_");
            //    Digest(hasher, "simon");
            //}

            Console.WriteLine("Notch: " + Digest(hasher, "Notch"));
            Console.WriteLine("jeb_: " + Digest(hasher, "jeb_"));
            Console.WriteLine("simon: " + Digest(hasher, "simon"));
        }

        public static string Digest(IncrementalHash hasher, string input)
        {
            Span<byte> buffer = stackalloc byte[1024];

            var src = input.AsSpan();
            while (src.Length > 0)
            {
                if (Utf8.FromUtf16(src, buffer, out int read, out int written) != OperationStatus.Done)
                    throw new Exception();

                hasher.AppendData(buffer.Slice(0, written));
                src = src[read..];
            }

            if (!hasher.TryGetHashAndReset(buffer, out int hashWritten))
                throw new Exception();

            Span<byte> remainingBuffer = buffer[hashWritten..];
            int charCount = HexUtility.GetHexCharCount(hashWritten);
            Span<char> hexBuffer = MemoryMarshal.Cast<byte, char>(remainingBuffer).Slice(0, charCount + 1);
            hexBuffer[0] = '0'; // reserve one char for minus sign

            Span<byte> hash = buffer.Slice(0, hashWritten);
            int signBit = hash[^1] & 0x1;
            if (signBit == 1)
            {
                for (int i = 0; i < hash.Length - 1; i++)
                    hash[i] = (byte)~hash[i];

                hash[^1] ^= 0xfe;
            }

            HexUtility.ToHexString(hash, hexBuffer[1..]);

            int zeroCount = 0;
            while (zeroCount < hexBuffer.Length && hexBuffer[zeroCount] == '0')
                zeroCount++;

            if (signBit == 1)
            {
                hexBuffer = hexBuffer[(zeroCount - 1)..];
                hexBuffer[0] = '-';
            }
            else
            {
                hexBuffer = hexBuffer[zeroCount..];
            }

            string hex = "";
            //string hex = hexBuffer.ToString();
            return hex;

            //int charCount = HexUtility.GetHexCharCount(hash.Length);
            //Span<char> hex = stackalloc char[charCount];
            //HexUtility.ToHexString(hash, hex);
            //
            //// BigInteger tries to be smart and puts in a leading 0 when 
            //// formatting as a hex number to allow roundtripping 
            //// of negative numbers, thus we have to trim it off.
            //if (bigInt < 0)
            //{
            //    // toss in a negative sign if the interpreted number is negative
            //    return "-" + (-bigInt).ToString("x").TrimStart('0');
            //}
            //else
            //{
            //    return bigInt.ToString("x").TrimStart('0');
            //}
        }

        /*
        class what
        {
            public int order;
        }

        static void Main(string[] args)
        {
            var xd = new List<what>();

            var rand = new Random();
            for (int i = 0; i < 64; i++)
            {
                xd.Add(new what { order = rand.Next(64) });
            }

            var watch = new Stopwatch();

            static int comp(what x, what y)
            {
                return x.order.CompareTo(y.order);
            }

            for (int i = 0; i < 100 * 10_000; i++)
            {
                for (int j= 0; j < 64; j++)
                    xd[j].order = rand.Next(64);
                
                watch.Start();
                xd.Sort(comp);
                watch.Stop();
            }

            Console.WriteLine(watch.Elapsed.TotalMilliseconds + "ms total = " + watch.Elapsed.TotalMilliseconds / 1_000_000);
        }
        */
    }
}
