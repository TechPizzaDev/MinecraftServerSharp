// Copied from .NET Foundation (and Modified)

using System;
using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using MinecraftServerSharp.Collections;

namespace MinecraftServerSharp
{
    public struct LongHashCode
    {
        private static ulong Seed { get; } = MarvinHash64.GenerateSeed();

        private const ulong Prime1 = 11400714785074694791UL;
        private const ulong Prime2 = 14029467366897019727UL;
        private const ulong Prime3 = 1609587929392839161UL;
        private const ulong Prime4 = 9650029242287828579UL;
        private const ulong Prime5 = 2870177450012600261UL;

        private ulong _v1, _v2, _v3, _v4;
        private ulong _queue1, _queue2, _queue3;
        private ulong _length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetLongHashCode<T>(T value)
        {
            return value != null ? LongEqualityComparer<T>.Default.GetLongHashCode(value) : 0;
        }

        public static long Combine<T1>(
            T1 value1)
        {
            // Provide a way of diffusing bits from something with a limited
            // input hash space. For example, many enums only have a few
            // possible hashes, only using the bottom few bits of the code. Some
            // collections are built on the assumption that hashes are spread
            // over a larger space, so diffusing the bits may help the
            // collection work more efficiently.

            ulong hc1 = (ulong)GetLongHashCode(value1);

            ulong hash = MixEmptyState();
            hash += sizeof(ulong);

            hash = QueueRound(hash, hc1);

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2>(
            T1 value1, T2 value2)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);

            ulong hash = MixEmptyState();
            hash += sizeof(ulong) * 2;

            hash = QueueRound(hash, hc1);
            hash = QueueRound(hash, hc2);

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2, T3>(
            T1 value1, T2 value2, T3 value3)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);
            ulong hc3 = (ulong)GetLongHashCode(value3);

            ulong hash = MixEmptyState();
            hash += sizeof(ulong) * 3;

            hash = QueueRound(hash, hc1);
            hash = QueueRound(hash, hc2);
            hash = QueueRound(hash, hc3);

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2, T3, T4>(
            T1 value1, T2 value2, T3 value3, T4 value4)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);
            ulong hc3 = (ulong)GetLongHashCode(value3);
            ulong hc4 = (ulong)GetLongHashCode(value4);

            Initialize(out ulong v1, out ulong v2, out ulong v3, out ulong v4);

            v1 = Round(v1, hc1);
            v2 = Round(v2, hc2);
            v3 = Round(v3, hc3);
            v4 = Round(v4, hc4);

            ulong hash = MixState(v1, v2, v3, v4);
            hash += sizeof(ulong) * 4;

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2, T3, T4, T5>(
            T1 value1, T2 value2, T3 value3, T4 value4, T5 value5)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);
            ulong hc3 = (ulong)GetLongHashCode(value3);
            ulong hc4 = (ulong)GetLongHashCode(value4);
            ulong hc5 = (ulong)GetLongHashCode(value5);

            Initialize(out ulong v1, out ulong v2, out ulong v3, out ulong v4);

            v1 = Round(v1, hc1);
            v2 = Round(v2, hc2);
            v3 = Round(v3, hc3);
            v4 = Round(v4, hc4);

            ulong hash = MixState(v1, v2, v3, v4);
            hash += sizeof(ulong) * 5;

            hash = QueueRound(hash, hc5);

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2, T3, T4, T5, T6>(
            T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);
            ulong hc3 = (ulong)GetLongHashCode(value3);
            ulong hc4 = (ulong)GetLongHashCode(value4);
            ulong hc5 = (ulong)GetLongHashCode(value5);
            ulong hc6 = (ulong)GetLongHashCode(value6);

            Initialize(out ulong v1, out ulong v2, out ulong v3, out ulong v4);

            v1 = Round(v1, hc1);
            v2 = Round(v2, hc2);
            v3 = Round(v3, hc3);
            v4 = Round(v4, hc4);

            ulong hash = MixState(v1, v2, v3, v4);
            hash += sizeof(ulong) * 6;

            hash = QueueRound(hash, hc5);
            hash = QueueRound(hash, hc6);

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2, T3, T4, T5, T6, T7>(
            T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);
            ulong hc3 = (ulong)GetLongHashCode(value3);
            ulong hc4 = (ulong)GetLongHashCode(value4);
            ulong hc5 = (ulong)GetLongHashCode(value5);
            ulong hc6 = (ulong)GetLongHashCode(value6);
            ulong hc7 = (ulong)GetLongHashCode(value7);

            Initialize(out ulong v1, out ulong v2, out ulong v3, out ulong v4);

            v1 = Round(v1, hc1);
            v2 = Round(v2, hc2);
            v3 = Round(v3, hc3);
            v4 = Round(v4, hc4);

            ulong hash = MixState(v1, v2, v3, v4);
            hash += sizeof(ulong) * 7;

            hash = QueueRound(hash, hc5);
            hash = QueueRound(hash, hc6);
            hash = QueueRound(hash, hc7);

            hash = MixFinal(hash);
            return (long)hash;
        }

        public static long Combine<T1, T2, T3, T4, T5, T6, T7, T8>(
            T1 value1, T2 value2, T3 value3, T4 value4, T5 value5, T6 value6, T7 value7, T8 value8)
        {
            ulong hc1 = (ulong)GetLongHashCode(value1);
            ulong hc2 = (ulong)GetLongHashCode(value2);
            ulong hc3 = (ulong)GetLongHashCode(value3);
            ulong hc4 = (ulong)GetLongHashCode(value4);
            ulong hc5 = (ulong)GetLongHashCode(value5);
            ulong hc6 = (ulong)GetLongHashCode(value6);
            ulong hc7 = (ulong)GetLongHashCode(value7);
            ulong hc8 = (ulong)GetLongHashCode(value8);

            Initialize(out ulong v1, out ulong v2, out ulong v3, out ulong v4);

            v1 = Round(v1, hc1);
            v2 = Round(v2, hc2);
            v3 = Round(v3, hc3);
            v4 = Round(v4, hc4);

            v1 = Round(v1, hc5);
            v2 = Round(v2, hc6);
            v3 = Round(v3, hc7);
            v4 = Round(v4, hc8);

            ulong hash = MixState(v1, v2, v3, v4);
            hash += sizeof(ulong) * 8;

            hash = MixFinal(hash);
            return (long)hash;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Initialize(out ulong v1, out ulong v2, out ulong v3, out ulong v4)
        {
            v1 = Seed + Prime1 + Prime2;
            v2 = Seed + Prime2;
            v3 = Seed;
            v4 = Seed - Prime1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Round(ulong hash, ulong input)
        {
            return BitOperations.RotateLeft(hash + input * Prime2, 31) * Prime1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong QueueRound(ulong hash, ulong queuedValue)
        {
            hash ^= BitOperations.RotateLeft(queuedValue * Prime2, 31) * Prime1;
            return BitOperations.RotateLeft(hash, 27) * Prime1 + Prime4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MixState(ulong v1, ulong v2, ulong v3, ulong v4)
        {
            return
                BitOperations.RotateLeft(v1, 1) +
                BitOperations.RotateLeft(v2, 7) +
                BitOperations.RotateLeft(v3, 12) +
                BitOperations.RotateLeft(v4, 18);
        }

        private static ulong MixEmptyState()
        {
            return Seed + Prime5;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MixFinal(ulong hash)
        {
            hash ^= hash >> 33;
            hash *= Prime2;
            hash ^= hash >> 29;
            hash *= Prime3;
            hash ^= hash >> 32;
            return hash;
        }

        public void Add<T>(T value)
        {
            Add(GetLongHashCode(value));
        }

        public void Add<T>(T value, ILongEqualityComparer<T>? comparer)
        {
            comparer ??= LongEqualityComparer<T>.Default;
            Add(value == null ? 0 : comparer.GetLongHashCode(value));
        }

        private void Add(long value)
        {
            // The original xxHash works as follows:
            // 0. Initialize immediately. We can't do this in a struct (no
            //    default ctor).
            // 1. Accumulate blocks of length 32 (4 ulongs) into 4 accumulators.
            // 2. Accumulate remaining blocks of length 8 (1 ulong) into the
            //    hash.
            // 3. Accumulate remaining blocks of length 1 into the hash.

            // There is no need for #3 as this type only accepts longs. _queue1,
            // _queue2 and _queue3 are basically a buffer so that when
            // ToHashCode is called we can execute #2 correctly.

            // We need to initialize the xxHash64 state (_v1 to _v4) lazily (see
            // #0) nd the last place that can be done if you look at the
            // original code is just before the first block of 16 bytes is mixed
            // in. The xxHash64 state is never used for streams containing fewer
            // than 32 bytes.

            // To see what's really going on here, have a look at the Combine
            // methods.

            ulong val = (ulong)value;

            // Storing the value of _length locally shaves of quite a few bytes
            // in the resulting machine code.
            ulong previousLength = _length++;
            ulong position = previousLength % 4;

            // Switch can't be inlined.

            if (position == 0)
                _queue1 = val;
            else if (position == 1)
                _queue2 = val;
            else if (position == 2)
                _queue3 = val;
            else // position == 3
            {
                if (previousLength == 3)
                    Initialize(out _v1, out _v2, out _v3, out _v4);

                _v1 = Round(_v1, _queue1);
                _v2 = Round(_v2, _queue2);
                _v3 = Round(_v3, _queue3);
                _v4 = Round(_v4, val);
            }
        }

        public long ToHashCode()
        {
            // Storing the value of _length locally shaves of quite a few bytes
            // in the resulting machine code.
            ulong length = _length;

            // position refers to the *next* queue position in this method, so
            // position == 1 means that _queue1 is populated; _queue2 would have
            // been populated on the next call to Add.
            ulong position = length % 4;

            // If the length is less than 8, _v1 to _v4 don't contain anything
            // yet. xxHash64 treats this differently.

            ulong hash = length < 4 ? MixEmptyState() : MixState(_v1, _v2, _v3, _v4);

            // _length is incremented once per Add(Int64) and is therefore 8
            // times too small (xxHash length is in bytes, not ints).

            hash += sizeof(ulong) * length;

            // Mix what remains in the queue

            // Switch can't be inlined right now, so use as few branches as
            // possible by manually excluding impossible scenarios (position > 1
            // is always false if position is not > 0).
            if (position > 0)
            {
                hash = QueueRound(hash, _queue1);
                if (position > 1)
                {
                    hash = QueueRound(hash, _queue2);
                    if (position > 2)
                        hash = QueueRound(hash, _queue3);
                }
            }

            hash = MixFinal(hash);
            return (long)hash;
        }

#pragma warning disable CA1065 // Do not raise exceptions in unexpected locations
#pragma warning disable 0809
        // Obsolete member 'memberA' overrides non-obsolete member 'memberB'.
        // Disallowing GetHashCode and Equals is by design

        // * We decided to not override GetHashCode() to produce the hash code
        //   as this would be weird, both naming-wise as well as from a
        //   behavioral standpoint (GetHashCode() should return the object's
        //   hash code, not the one being computed).

        // * Even though ToHashCode() can be called safely multiple times on
        //   this implementation, it is not part of the contract. If the
        //   implementation has to change in the future we don't want to worry
        //   about people who might have incorrectly used this type.

        [Obsolete(
            "LongHashCode is a mutable struct and should not be compared with other LongHashCodes. " +
            "Use ToHashCode to retrieve the computed hash code.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override int GetHashCode()
        {
            throw new NotSupportedException();
        }

        [Obsolete("LongHashCode is a mutable struct and should not be compared with other LongHashCodes.", error: true)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override bool Equals(object? obj)
        {
            throw new NotSupportedException();
        }
#pragma warning restore 0809
#pragma warning restore CA1065
    }
}
