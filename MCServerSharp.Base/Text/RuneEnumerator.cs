using System;
using System.Collections.Generic;
using System.Text;

namespace MCServerSharp.Text
{
    public ref struct RuneEnumerator
    {
        public delegate bool MoveNextDelegate(ref RuneEnumerator enumerator);

        private static MoveNextDelegate CachedSpanMoveNext { get; } = SpanMoveNext;
        private static MoveNextDelegate CachedStringMoveNext { get; } = StringMoveNext;
        private static MoveNextDelegate CachedUtf8MoveNext { get; } = Utf8MoveNext;
        private static MoveNextDelegate CachedBuilderMoveNext { get; } = BuilderMoveNext;
        private static MoveNextDelegate CachedInterfaceRuneMoveNext { get; } = InterfaceRuneMoveNext;
        private static MoveNextDelegate CachedInterfaceCharMoveNext { get; } = InterfaceCharMoveNext;

        private MoveNextDelegate _moveNext;

        private SpanRuneEnumerator _spanEnumerator;
        private StringRuneEnumerator _stringEnumerator;
        private Utf8RuneEnumerator _utf8Enumerator;
        private StringBuilder.ChunkEnumerator _builderEnumerator;

        public object? State { get; set; }
        public Rune Current { get; set; }

        public RuneEnumerator(MoveNextDelegate moveNext, object? state) : this()
        {
            _moveNext = moveNext;
            State = state;
        }

        public RuneEnumerator(SpanRuneEnumerator spanEnumerator) : this(CachedSpanMoveNext, null)
        {
            _spanEnumerator = spanEnumerator;
        }

        public RuneEnumerator(StringRuneEnumerator stringEnumerator) : this(CachedStringMoveNext, null)
        {
            _stringEnumerator = stringEnumerator;
        }

        public RuneEnumerator(Utf8RuneEnumerator utf8Enumerator) : this(CachedUtf8MoveNext, null)
        {
            _utf8Enumerator = utf8Enumerator;
        }

        public RuneEnumerator(StringBuilder.ChunkEnumerator builderEnumerator) : this(CachedBuilderMoveNext, null)
        {
            _builderEnumerator = builderEnumerator;
        }

        public RuneEnumerator(IEnumerator<Rune>? interfaceEnumerator) : this(CachedInterfaceRuneMoveNext, interfaceEnumerator)
        {
        }

        public RuneEnumerator(IEnumerator<char>? interfaceEnumerator) : this(CachedInterfaceCharMoveNext, interfaceEnumerator)
        {
        }

        public bool MoveNext()
        {
            return _moveNext.Invoke(ref this);
        }

        public RuneEnumerator GetEnumerator()
        {
            return this;
        }

        private static bool SpanMoveNext(ref RuneEnumerator e)
        {
            if (e._spanEnumerator.MoveNext())
            {
                e.Current = e._spanEnumerator.Current;
                return true;
            }
            return false;
        }

        private static bool StringMoveNext(ref RuneEnumerator e)
        {
            if (e._stringEnumerator.MoveNext())
            {
                e.Current = e._stringEnumerator.Current;
                return true;
            }
            return false;
        }

        private static bool Utf8MoveNext(ref RuneEnumerator e)
        {
            if (e._utf8Enumerator.MoveNext())
            {
                e.Current = e._utf8Enumerator.Current;
                return true;
            }
            return false;
        }

        private static bool BuilderMoveNext(ref RuneEnumerator e)
        {
            TryReturnFromChunk:
            if (e._spanEnumerator.MoveNext())
            {
                e.Current = e._spanEnumerator.Current;
                return true;
            }

            if (e._builderEnumerator.MoveNext())
            {
                var chunkSpan = e._builderEnumerator.Current.Span;
                e._spanEnumerator = chunkSpan.EnumerateRunes();
                goto TryReturnFromChunk;
            }
            return false;
        }

        private static bool InterfaceRuneMoveNext(ref RuneEnumerator e)
        {
            if (e.State is IEnumerator<Rune> enumerator && enumerator.MoveNext())
            {
                e.Current = enumerator.Current;
                return true;
            }
            return false;
        }

        private static bool InterfaceCharMoveNext(ref RuneEnumerator e)
        {
            if (e.State is IEnumerator<char> enumerator)
            {
                if (!enumerator.MoveNext())
                    return false;

                char highSurrogate = enumerator.Current;
                if (Rune.TryCreate(highSurrogate, out Rune rune))
                {
                    e.Current = rune;
                    return true;
                }

                if (!enumerator.MoveNext())
                    return false;

                char lowSurrogate = enumerator.Current;
                if (Rune.TryCreate(highSurrogate, lowSurrogate, out rune))
                {
                    e.Current = rune;
                    return true;
                }
            }
            return false;
        }

        public readonly override string ToString()
        {
            RuneEnumerator runes = this;
            StringBuilder builder = new();
            Span<char> buffer = stackalloc char[4];
            foreach (Rune rune in runes)
            {
                int encoded = rune.EncodeToUtf16(buffer);
                builder.Append(buffer.Slice(0, encoded));
            }
            return builder.ToString();
        }

        public static implicit operator RuneEnumerator(ReadOnlySpan<char> text)
        {
            return new RuneEnumerator(text.EnumerateRunes());
        }

        public static implicit operator RuneEnumerator(ReadOnlyMemory<char> text)
        {
            return new RuneEnumerator(text.Span.EnumerateRunes());
        }

        public static implicit operator RuneEnumerator(Utf8Memory text)
        {
            return new RuneEnumerator(text.EnumerateRunes());
        }

        public static implicit operator RuneEnumerator(string? text)
        {
            if (text == null)
                return default;
            return new RuneEnumerator(text.EnumerateRunes());
        }

        public static implicit operator RuneEnumerator(Utf8String? text)
        {
            if (text == null)
                return default;
            return new RuneEnumerator(text.EnumerateRunes());
        }

        public static implicit operator RuneEnumerator(StringBuilder? text)
        {
            if (text == null)
                return default;
            return new RuneEnumerator(text.GetChunks());
        }
    }
}
