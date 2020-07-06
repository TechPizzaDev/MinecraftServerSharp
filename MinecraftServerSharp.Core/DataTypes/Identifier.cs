using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MinecraftServerSharp
{
    public readonly struct Identifier
    {
        private static HashSet<char> _validLocationCharacters = new HashSet<char>(ValidLocationCharacters.ToArray());
        private static HashSet<char> _validNamespaceCharacters = new HashSet<char>(ValidNamespaceCharacters.ToArray());

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

        #region Constructors

        public Identifier(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));

            string[] parts = Value.Split(Separator, StringSplitOptions.None);
            if (parts.Length != 0)
                throw new ArgumentException(
                    "Could not separate identifier into a namespace and location.", nameof(value));

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

        [DebuggerHidden]
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
    }
}
