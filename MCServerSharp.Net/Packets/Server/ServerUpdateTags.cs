using System;

namespace MCServerSharp.Net.Packets
{
    [PacketStruct(ServerPacketId.UpdateTags)]
    public readonly struct ServerUpdateTags
    {
        [DataProperty(0)] public TagContainer BlockTags { get; }
        [DataProperty(1)] public TagContainer ItemTags { get; }
        [DataProperty(2)] public TagContainer FluidTags { get; }
        [DataProperty(3)] public TagContainer EntityTypeTags { get; }

        public ServerUpdateTags(
            TagContainer blockTags, TagContainer itemTags, TagContainer fluidTags, TagContainer entityTypeTags)
        {
            BlockTags = blockTags;
            ItemTags = itemTags;
            FluidTags = fluidTags;
            EntityTypeTags = entityTypeTags;
        }
    }

    [DataObject]
    public readonly struct TagContainer
    {
        public static TagContainer Empty => new TagContainer(Array.Empty<Tag>());

        [DataProperty(0)]
        [DataEnumerable]
        [DataLengthPrefixed(typeof(VarInt))]
        public Tag[] Tags { get; }

        public TagContainer(Tag[] tags)
        {
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
