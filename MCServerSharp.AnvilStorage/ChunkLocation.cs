using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MCServerSharp.Utility;

namespace MCServerSharp.AnvilStorage
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public readonly struct ChunkLocation : IEquatable<ChunkLocation>, IComparable<ChunkLocation>
    {
        private readonly byte _offset0;
        private readonly byte _offset1;
        private readonly byte _offset2;

        public byte SectorCount { get; }

        public int SectorOffset => _offset0 << 16 | _offset1 << 8 | _offset2;

        public int CompareTo(ChunkLocation other)
        {
            return SectorOffset.CompareTo(other.SectorOffset);
        }

        public bool Equals(ChunkLocation other)
        {
            return SectorCount == other.SectorCount
                && _offset0 == other._offset0
                && _offset1 == other._offset1
                && _offset2 == other._offset2;
        }

        private string GetDebuggerDisplay()
        {
            return ToString();
        }

        public override int GetHashCode()
        {
            return UnsafeR.As<ChunkLocation, int>(this);
        }

        public override string ToString()
        {
            return $"{SectorCount} @ [{SectorOffset}]";
        }
    }
}
