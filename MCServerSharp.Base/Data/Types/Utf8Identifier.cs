using System;
using MCServerSharp.Text;

namespace MCServerSharp
{
    public readonly struct Utf8Identifier : IIdentifier<Utf8Identifier>
    {
        // TODO: move this somewhere else?
        public static Utf8String DefaultNamespace { get; } = Identifier.DefaultNamespace.ToUtf8String();
        public static Utf8String Separator { get; } = Identifier.Separator.ToUtf8String();

        private readonly int _namespaceEnd;

        public Utf8Memory Value { get; }
        public Utf8Memory Namespace => Value[0.._namespaceEnd];
        public Utf8Memory Location => Value[(_namespaceEnd + Identifier.Separator.Length)..];
        
        private Utf8Identifier(Utf8Memory value, int namespaceEnd)
        {
            Value = value;
            _namespaceEnd = namespaceEnd;
        }

        public Utf8Identifier(Utf8Memory value)
        {
            Value = value;

            Utf8Splitter parts = Value.EnumerateSplit(Separator, StringSplitOptions.None);

            bool namespaceMove = parts.MoveNext();
            _namespaceEnd = parts.Current.End.GetOffset(value.Length);
            
            bool locationMove = parts.MoveNext();
            
            if (!(namespaceMove && locationMove) || parts.MoveNext())
            {
                throw new ArgumentException(
                    $"Could not separate identifier \"{value}\" into a namespace and location.", nameof(value));
            }

            Identifier.ValidateNamespace(Namespace);
            Identifier.ValidateLocation(Location);
        }

        public Utf8Identifier(ReadOnlySpan<char> value) : this(Utf8String.Create(value))
        {
        }

        public Utf8Identifier(Utf8Memory @namespace, Utf8Memory location)
        {
            Identifier.ValidateNamespace(@namespace);
            Identifier.ValidateLocation(location);

            if (@namespace.IsEmpty)
                @namespace = DefaultNamespace;

            Value = Utf8String.Concat(@namespace, Separator, location);
            _namespaceEnd = @namespace.Length;
        }

        public Utf8Identifier(ReadOnlySpan<char> @namespace, ReadOnlySpan<char> value) :
            this(Utf8String.Create(@namespace), Utf8String.Create(value))
        {
        }

        public static bool TryParse(Utf8Memory value, out Utf8Identifier identifier)
        {
            Utf8Splitter parts = value.EnumerateSplit(Separator, StringSplitOptions.None);

            bool namespaceMove = parts.MoveNext();
            if (!namespaceMove)
                goto Fail;
            int namespaceEnd = parts.Current.End.GetOffset(value.Length);

            bool locationMove = parts.MoveNext();
            if (!locationMove)
                goto Fail;

            if (parts.MoveNext())
                goto Fail;

            identifier = new Utf8Identifier(value, namespaceEnd);
            return true;

            Fail:
            identifier = default;
            return false;
        }

        public RuneEnumerator EnumerateValue() => Value;
        public RuneEnumerator EnumerateNamespace() => Namespace;
        public RuneEnumerator EnumerateLocation() => Location;

        public bool Equals(Utf8Identifier other, StringComparison comparison)
        {
            return Value.Equals(other.Value, comparison);
        }

        public bool Equals(Utf8Identifier other)
        {
            return Equals(other, StringComparison.Ordinal);
        }

        public override bool Equals(object? obj)
        {
            return obj is Utf8Identifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override string ToString()
        {
            return Value.ToString();
        }

        public static bool operator ==(Utf8Identifier left, Utf8Identifier right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Utf8Identifier left, Utf8Identifier right)
        {
            return !(left == right);
        }
    }
}
