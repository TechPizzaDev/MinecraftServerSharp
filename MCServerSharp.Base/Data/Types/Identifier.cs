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

        public string Value { get; }
        public string Namespace { get; }
        public string Location { get; }

        public bool IsValid => Value != null;

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

        public Identifier(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));

            string[] parts = Value.Split(Separator, StringSplitOptions.None);
            if (parts.Length != 2)
            {
                throw new ArgumentException(
                    "Could not separate identifier into a namespace and location.", nameof(value));
            }
            ValidateNamespace(parts[0]);
            ValidateLocation(parts[1]);

            Namespace = string.IsInterned(parts[0]) ?? parts[0];
            Location = parts[1];
        }

        public Identifier(string @namespace, string location)
        {
            ValidateNamespace(@namespace);
            ValidateLocation(location);
            
            Namespace = @namespace ?? DefaultNamespace;
            Location = location;
            Value = Namespace + Separator + Location;
        }

        #endregion

        public Utf8Identifier ToUtf8Identifier()
        {
            return new Utf8Identifier(Value);
        }

        public static bool TryParse(string value, out Identifier identifier)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            string[] parts = value.Split(Separator, StringSplitOptions.None);
            if (parts.Length != 2)
                goto Fail;

            string @namespace = parts[0];
            string location = parts[1];
            if (!IsValidNamespace(@namespace) ||
                !IsValidLocation(location))
                goto Fail;

            identifier = new Identifier(@namespace, location);
            return true;

            Fail:
            identifier = default;
            return false;
        }

        public static void ValidateNamespace(RuneEnumerator runes)
        {
            if (!IsValidNamespace(runes))
                throw new ArgumentException("The namespace contains invalid characters.", nameof(runes));
        }

        public static void ValidateLocation(RuneEnumerator runes)
        {
            if (!IsValidLocation(runes))
                throw new ArgumentException("The location contains invalid characters.", nameof(runes));
        }

        public static bool IsValidNamespace(RuneEnumerator runes)
        {
            ReadOnlySpan<Rune> validChars = ValidNamespaceCharacters.Span;
            foreach (Rune c in runes)
            {
                if (!validChars.Contains(c))
                    return false;
            }
            return true;
        }

        public static bool IsValidLocation(RuneEnumerator runes)
        {
            ReadOnlySpan<Rune> validChars = ValidLocationCharacters.Span;
            foreach (Rune c in runes)
            {
                if (!validChars.Contains(c))
                    return false;
            }
            return true;
        }

        public RuneEnumerator EnumerateValue() => Value;
        public RuneEnumerator EnumerateNamespace() => Namespace;
        public RuneEnumerator EnumerateLocation() => Location;

        public bool Equals(Identifier other, StringComparison comparison)
        {
            return Value.Equals(other.Value, comparison);
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
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value;
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
