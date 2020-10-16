using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MCServerSharp
{
    public readonly struct Identifier :
        IEquatable<Identifier>, IComparable<Identifier>,
        IEquatable<string>, IComparable<string>
    {
        private static HashSet<char> _validLocationCharacters;
        private static HashSet<char> _validNamespaceCharacters;

        public static ReadOnlyMemory<char> ValidLocationCharacters { get; } = new char[]
        {
            '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j',
            'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't',
            'u', 'v', 'w', 'x', 'y', 'z', '-', '_', '/', '.'
        };

        public static ReadOnlyMemory<char> ValidNamespaceCharacters { get; } = ValidLocationCharacters[0..^2];

        // TODO: move this somewhere else
        public const string DefaultNamespace = "minecraft";
        public const string Separator = ":";

        public string Value { get; }
        public string Namespace { get; }
        public string Location { get; }

        public bool IsValid => Value != null;

        #region Constructors

        static Identifier()
        {
            _validLocationCharacters = new HashSet<char>(ValidLocationCharacters.ToArray());
            _validNamespaceCharacters = new HashSet<char>(ValidNamespaceCharacters.ToArray());
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
            ValidateParts(parts[0], parts[1]);

            Namespace = parts[0];
            Location = parts[1];
        }

        public Identifier(string @namespace, string location)
        {
            ValidateParts(@namespace, location);

            Namespace = @namespace ?? DefaultNamespace;
            Location = location;
            Value = Namespace + Separator + Location;
        }

        #endregion

        public static bool TryParse(Utf8String value, out Identifier identifier)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            // TODO: Could be useful if we implement Utf8Identifier
            //int colonIndex = value.Bytes.IndexOf((byte)':');
            //if (colonIndex == -1)
            //{
            //    identifier = default;
            //    return false;
            //}
            //
            //int nextColonIndex = value.Bytes.Slice(colonIndex + 1).IndexOf((byte)':');
            //if (nextColonIndex != -1)
            //{
            //    identifier = default;
            //    return false;
            //}

            string value16 = value.ToString();

            string[] parts = value16.Split(Separator, StringSplitOptions.None);
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

        public bool Equals(Identifier other)
        {
            return Equals(other.Value);
        }

        public bool Equals(string? other)
        {
            return Value.Equals(other, StringComparison.Ordinal);
        }

        public int CompareTo(Identifier other)
        {
            return CompareTo(other.Value);
        }

        public int CompareTo(string? other)
        {
            return string.CompareOrdinal(Value, other);
        }

        private static void ValidateParts(string @namespace, string location)
        {
            if (!IsValidNamespace(@namespace))
                throw new ArgumentException("The namespace contains invalid characters.", nameof(@namespace));

            if (location == null)
                throw new ArgumentNullException(nameof(location));

            if (!IsValidLocation(location))
                throw new ArgumentException("The location contains invalid characters.", nameof(location));
        }

        public static bool IsValidNamespace(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!_validNamespaceCharacters.Contains(value[i]))
                    return false;
            }
            return true;
        }

        public static bool IsValidLocation(ReadOnlySpan<char> value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (!_validLocationCharacters.Contains(value[i]))
                    return false;
            }
            return true;
        }

        public override string ToString()
        {
            return Value;
        }

        public static implicit operator Identifier(string value)
        {
            return new Identifier(value);
        }
    }
}
