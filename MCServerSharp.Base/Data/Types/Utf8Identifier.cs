using System;
using MCServerSharp.Text;

namespace MCServerSharp
{
    public readonly struct Utf8Identifier : IIdentifier<Utf8Identifier>
    {
        // TODO: move this somewhere else?
        public static Utf8String DefaultNamespace { get; } = Identifier.DefaultNamespace.ToUtf8String();
        public static Utf8String Separator { get; } = Identifier.Separator.ToUtf8String();

        public Utf8Memory Value { get; }
        public Utf8Memory Namespace { get; }
        public Utf8Memory Location { get; }

        public Utf8Identifier(Utf8Memory value)
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

            Namespace = value.Substring(part1);
            Identifier.ValidateNamespace(Namespace);

            Location = value.Substring(part2);
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
            Namespace = @namespace;
            Location = location;
            Value = Utf8String.Concat(Namespace, Separator, Location);
        }

        public Utf8Identifier(ReadOnlySpan<char> @namespace, ReadOnlySpan<char> value) : 
            this(Utf8String.Create(@namespace), Utf8String.Create(value))
        {
        }

        public static bool TryParse(Utf8Memory value, out Utf8Identifier identifier)
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

            Utf8Memory @namespace = value.Substring(part1);
            if (!Identifier.IsValidNamespace(@namespace, out _))
                goto Fail;

            Utf8Memory location = value.Substring(part2);
            if (!Identifier.IsValidLocation(location, out _))
                goto Fail;

            identifier = new Utf8Identifier(@namespace, location);
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
