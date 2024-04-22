using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateTags)]
    public readonly struct ServerUpdateTags
    {
        [DataProperty(0)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))] 
        public TagContainer[] Containers { get; }

        public ServerUpdateTags(TagContainer[] containers)
        {
            Containers = containers;
        }
    }

    [DataObject]
    public readonly struct TagContainer
    {
        [DataProperty(0)]
        public Utf8Identifier Identifier { get; }

        [DataProperty(1)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public Tag[] Tags { get; }

        public TagContainer(Utf8Identifier identifier, Tag[] tags)
        {
            Identifier = identifier;
            Tags = tags ?? throw new ArgumentNullException(nameof(tags));
        }
    }

    [DataObject]
    public readonly struct Tag
    {
        [DataProperty(0)]
        public Utf8Identifier Identifier { get; }

        [DataProperty(1)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public VarInt[] Entries { get; }

        public Tag(Utf8Identifier identifier, VarInt[] entries)
        {
            Identifier = identifier;
            Entries = entries ?? throw new ArgumentNullException(nameof(entries));
        }
    }
}
