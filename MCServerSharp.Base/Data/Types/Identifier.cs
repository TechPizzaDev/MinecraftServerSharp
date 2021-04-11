using System;
using System.Linq;
using System.Text;
using MCServerSharp.Text;

namespace MCServerSharp
{
    public readonly struct Identifier : IIdentifier<Identifier>
    {
        public static ReadOnlyMemory<Rune> ValidLocationCharacters { get; }
        public static ReadOnlyMemory<Rune> ValidNamespaceCharacters { get; }

        // TODO: move this somewhere else?
        public static string DefaultNamespace { get; } = string.Intern("minecraft");
        public static string Separator { get; } = ":";

        public ReadOnlyMemory<char> Value { get; }
        public ReadOnlyMemory<char> Namespace { get; }
        public ReadOnlyMemory<char> Location { get; }

        public bool IsValid => !Value.IsEmpty;

        #region Constructors

        static Identifier()
        {
            char[] validLocationCharacters = new char[]
            {
                '/', '.',
                '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
                'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
                'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
                'u', 'v', 'w', 'x', 'y', 'z', '-', '_'
            };
            ValidLocationCharacters = validLocationCharacters.Select(c => new Rune(c)).ToArray();
            ValidNamespaceCharacters = ValidLocationCharacters[2..];
        }

        public Identifier(ReadOnlyMemory<char> value)
        {
            Value = value;

            var parts = Value.EnumerateSplit(Separator, StringSplitOptions.None);

            bool move1 = parts.MoveNext();
            Range part1 = parts.Current;

            bool move2 = parts.MoveNext();
            Range part2 = parts.Current;

            if (!(move1 && move2) || parts.MoveNext())
            {
                throw new ArgumentException(
                    $"Could not separate identifier \"{value}\" into a namespace and location.", nameof(value));
            }

            Namespace = value[part1];
            ValidateNamespace(Namespace);

            Location = value[part2];
            ValidateLocation(Location);
        }

        public Identifier(string? value) : this(value.AsMemory())
        {
        }

        public Identifier(ReadOnlySpan<char> value) : this(value.ToString().AsMemory())
        {
        }

        public Identifier(ReadOnlyMemory<char> @namespace, ReadOnlyMemory<char> location)
        {
            ValidateNamespace(@namespace);
            ValidateLocation(location);

            if (@namespace.IsEmpty)
                @namespace = DefaultNamespace.AsMemory();
            Namespace = @namespace;
            Location = location;
            Value = string.Concat(Namespace + Separator + Location).AsMemory();
        }

        public Identifier(ReadOnlySpan<char> @namespace, ReadOnlySpan<char> location) :
            this(@namespace.ToString().AsMemory(), location.ToString().AsMemory())
        {
        }

        #endregion

        public Utf8Identifier ToUtf8Identifier()
        {
            return new Utf8Identifier(Value.Span);
        }

        public static bool TryParse(ReadOnlyMemory<char> value, out Identifier identifier)
        {
            var parts = value.EnumerateSplit(Separator, StringSplitOptions.None);

            bool move1 = parts.MoveNext();
            if (!move1)
                goto Fail;
            Range part1 = parts.Current;

            bool move2 = parts.MoveNext();
            if (!move2)
                goto Fail;
            Range part2 = parts.Current;

            if (parts.MoveNext())
                goto Fail;

            ReadOnlyMemory<char> @namespace = value[part1];
            if (!IsValidNamespace(@namespace, out _))
                goto Fail;

            ReadOnlyMemory<char> location = value[part2];
            if (!IsValidLocation(location, out _))
                goto Fail;

            identifier = new Identifier(@namespace, location);
            return true;

            Fail:
            identifier = default;
            return false;
        }

        public static bool TryParse(string value, out Identifier identifier)
        {
            return TryParse(value.AsMemory(), out identifier);
        }

        public static bool TryParse(ReadOnlySpan<char> value, out Identifier identifier)
        {
            return TryParse(value.ToString().AsMemory(), out identifier);
        }

        public static void ValidateNamespace(RuneEnumerator runes)
        {
            if (!IsValidNamespace(runes, out (int Index, Rune Rune) invalidRune))
            {
                throw new ArgumentException(
                    $"The namespace \"{runes.ToString()}\" contains an " +
                    $"invalid character \"{invalidRune.Rune}\" at index {invalidRune.Index}.", nameof(runes));
            }
        }

        public static void ValidateLocation(RuneEnumerator runes)
        {
            if (!IsValidLocation(runes, out (int Index, Rune Rune) invalidRune))
            {
                throw new ArgumentException(
                    $"The location \"{runes.ToString()}\" contains an " +
                    $"invalid character \"{invalidRune.Rune}\" at index {invalidRune.Index}.", nameof(runes));
            }
        }

        public static bool IsValidNamespace(RuneEnumerator runes, out (int Index, Rune Rune) invalidIndex)
        {
            ReadOnlySpan<Rune> validChars = ValidNamespaceCharacters.Span;
            invalidIndex = default;
            foreach (Rune c in runes)
            {
                invalidIndex.Rune = c;
                if (!validChars.Contains(c))
                    return false;
                invalidIndex.Index++;
            }
            invalidIndex = (-1, default);
            return true;
        }

        public static bool IsValidLocation(RuneEnumerator runes, out (int Index, Rune Rune) invalidIndex)
        {
            ReadOnlySpan<Rune> validChars = ValidLocationCharacters.Span;
            invalidIndex = default;
            foreach (Rune c in runes)
            {
                invalidIndex.Rune = c;
                if (!validChars.Contains(c))
                    return false;
                invalidIndex.Index++;
            }
            invalidIndex = (-1, default);
            return true;
        }

        public RuneEnumerator EnumerateValue() => Value;
        public RuneEnumerator EnumerateNamespace() => Namespace;
        public RuneEnumerator EnumerateLocation() => Location;

        public bool Equals(Identifier other, StringComparison comparison)
        {
            return Value.Span.Equals(other.Value.Span, comparison);
        }

        public bool Equals(Identifier other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is Identifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return string.GetHashCode(Value.Span);
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(Identifier left, Identifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Identifier left, Identifier right)
        {
            return !(left == right);
        }
    }
}
