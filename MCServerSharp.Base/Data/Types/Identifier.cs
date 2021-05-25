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

        private readonly int _namespaceEnd;

        public ReadOnlyMemory<char> Value { get; }
        public ReadOnlyMemory<char> Namespace => Value[0.._namespaceEnd];
        public ReadOnlyMemory<char> Location => Value[(_namespaceEnd + Separator.Length)..];

        public int Length => Value.Length;
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

        private Identifier(ReadOnlyMemory<char> value, int namespaceEnd)
        {
            Value = value;
            _namespaceEnd = namespaceEnd;
        }

        public Identifier(ReadOnlyMemory<char> value)
        {
            Value = value;

            Utf16Splitter parts = Value.EnumerateSplit(Separator, StringSplitOptions.None);

            bool namespaceMove = parts.MoveNext();
            _namespaceEnd = parts.Current.End.GetOffset(value.Length);

            bool locationMove = parts.MoveNext();

            if (!(namespaceMove && locationMove) || parts.MoveNext())
            {
                throw new ArgumentException(
                    $"Could not separate identifier \"{value}\" into a namespace and location.", nameof(value));
            }

            ValidateNamespace(Namespace);
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

            Value = string.Concat(@namespace.Span, Separator.AsSpan(), location.Span).AsMemory();
            _namespaceEnd = @namespace.Length;
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
            Utf16Splitter parts = value.EnumerateSplit(Separator, StringSplitOptions.None);

            bool namespaceMove = parts.MoveNext();
            if (!namespaceMove)
                goto Fail;
            int namespaceEnd = parts.Current.End.GetOffset(value.Length);

            bool locationMove = parts.MoveNext();
            if (!locationMove)
                goto Fail;

            if (parts.MoveNext())
                goto Fail;

            identifier = new Identifier(value, namespaceEnd);
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
