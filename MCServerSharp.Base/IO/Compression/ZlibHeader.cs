using System;
using System.IO.Compression;

namespace MCServerSharp.IO.Compression
{
    public readonly struct ZlibHeader
    {
        public enum FLevel : byte
        {
            Faster = 0,
            Fast = 1,
            Default = 2,
            Optimal = 3,
        }

        /// <summary>
        /// CMF 0-3
        /// </summary>
        public byte CompressionMethod { get; }

        /// <summary>
        /// CMF 4-7
        /// </summary>
        public byte CompressionInfo { get; }

        /// <summary>
        /// Flag 0-4 (Check bits for CMF and FLG)
        /// </summary>
        public byte Check { get; }

        /// <summary>
        /// Flag 5 (Preset dictionary)
        /// </summary>
        public bool Dict { get; }

        /// <summary>
        /// Flag 6-7 (Compression level)
        /// </summary>
        public FLevel Level { get; }

        public bool IsValid =>
            CompressionMethod == 8 && 
            CompressionInfo == 7 && 
            !Dict &&
            (GetCMF() * 256 + GetFLG()) % 31 == 0;

        private ZlibHeader(
            byte compressionMethod, byte compressionInfo, byte check, bool dict, FLevel level)
        {
            CompressionMethod = compressionMethod;
            CompressionInfo = compressionInfo;
            Check = check;
            Dict = dict;
            Level = level;
        }

        public ZlibHeader(byte compressionMethod, byte compressionInfo, bool dict, FLevel level) : this()
        {
            if (compressionMethod > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(compressionMethod), "Argument cannot be greater than 15.");
            if (compressionInfo > 15)
                throw new ArgumentOutOfRangeException(
                    nameof(compressionInfo), "Argument cannot be greater than 15.");

            CompressionMethod = compressionMethod;
            CompressionInfo = compressionInfo;
            Dict = dict;
            Level = level;

            byte byteFLG = (byte)((byte)Level << 1);
            byteFLG |= Convert.ToByte(Dict);
            Check = Convert.ToByte(31 - Convert.ToByte((GetCMF() * 256 + byteFLG) % 31));
        }

        public byte GetCMF()
        {
            byte byteCMF = (byte)(CompressionInfo << 4);
            byteCMF |= CompressionMethod;
            return byteCMF;
        }

        public byte GetFLG()
        {
            byte byteFLG = (byte)((byte)Level << 6);
            byteFLG |= (byte)(Convert.ToByte(Dict) << 5);
            byteFLG |= Check;
            return byteFLG;
        }

        /// <summary>
        /// Creates a header with values applicable to a 
        /// <see cref="DeflateStream"/> (.NET framework 4.5 and above).
        /// </summary>
        public static ZlibHeader CreateForDeflateStream(CompressionLevel level)
        {
            return new ZlibHeader(8, 7, false, ConvertLevel(level));
        }

        public static ZlibHeader Decode(byte cmf, byte flg)
        {
            byte CompressionMethod = Convert.ToByte(cmf & 0x0F);
            byte CompressionInfo = Convert.ToByte((cmf & 0xF0) >> 4);
            byte FCheck = Convert.ToByte(flg & 0x1F);
            bool FDict = Convert.ToBoolean(Convert.ToByte((flg & 0x20) >> 5));
            var Level = (FLevel)Convert.ToByte((flg & 0xC0) >> 6);

            return new ZlibHeader(CompressionMethod, CompressionInfo, FCheck, FDict, Level);
        }

        public static FLevel ConvertLevel(CompressionLevel level)
        {
            return level switch
            {
                CompressionLevel.NoCompression => FLevel.Faster,
                CompressionLevel.Fastest => FLevel.Default,
                CompressionLevel.Optimal => FLevel.Optimal,
                _ => throw new ArgumentOutOfRangeException(nameof(level)),
            };
        }

        public static CompressionLevel ConvertLevel(FLevel level)
        {
            return level switch
            {
                FLevel.Faster => CompressionLevel.NoCompression,
                FLevel.Default => CompressionLevel.Fastest,
                FLevel.Optimal => CompressionLevel.Optimal,
                _ => throw new ArgumentOutOfRangeException(nameof(level)),
            };
        }
    }
}
