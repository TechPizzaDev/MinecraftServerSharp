using System;
using MCServerSharp.Text;

namespace MCServerSharp
{
    public readonly struct Utf8Identifier : IIdentifier<Utf8Identifier>
    {
        // TODO: move this somewhere else?
        public static Utf8String DefaultNamespace { get; } = Identifier.DefaultNamespace.ToUtf8String();
        public static Utf8String Separator { get; } = Identifier.Separator.ToUtf8String();

        public Utf8String Value { get; }
        public Utf8String Namespace { get; }
        public Utf8String Location { get; }
        public int Hash { get; }

        public bool IsValid => Value != null;

        public Utf8Identifier(Utf8String value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));

            var parts = Value.EnumerateRangeSplit(Separator, StringSplitOptions.None);

            bool move1 = parts.MoveNext();
            Range part1 = parts.Current;

            bool move2 = parts.MoveNext();
            Range part2 = parts.Current;

            if (!(move1 && move2) || parts.MoveNext())
            {
                throw new ArgumentException(
                    "Could not separate identifier into a namespace and location.", nameof(value));
            }

            Namespace = value.Substring(part1);
            Identifier.ValidateNamespace(Namespace);

            Location = value.Substring(part2);
            Identifier.ValidateLocation(Location);

            Hash = Value.GetHashCode();
        }

        public Utf8Identifier(string value) : this(value.ToUtf8String())
        {
        }

        public Utf8Identifier(Utf8String @namespace, Utf8String location)
        {
            Identifier.ValidateNamespace(@namespace);
            Identifier.ValidateLocation(location);

            Namespace = @namespace ?? DefaultNamespace;
            Location = location;
            Value = Utf8String.Concat(Namespace, Separator, Location);
            Hash = Value.GetHashCode();
        }

        public static bool TryParse(Utf8String value, out Utf8Identifier identifier)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var parts = value.EnumerateRangeSplit(Separator, StringSplitOptions.None);

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

            Utf8String @namespace = value.Substring(part1);
            if (!Identifier.IsValidNamespace(@namespace))
                goto Fail;

            Utf8String location = value.Substring(part2);
            if (!Identifier.IsValidLocation(location))
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

        public bool Equals(Utf8Identifier other, StringComparison comparison) => Value.Equals(other.Value, comparison);

        public bool Equals(Utf8Identifier other) => Equals(other, StringComparison.Ordinal);

        public override bool Equals(object? obj)
        {
            return obj is Utf8Identifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Hash;
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
