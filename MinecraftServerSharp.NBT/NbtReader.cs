using System;

namespace MinecraftServerSharp.NBT
{
    public struct NbtReaderState
    {
        public NbtReaderOptions Options { get; }

        public NbtReaderState(NbtReaderOptions options = default)
        {
            Options = options;
        }
    }

    public ref struct NbtReader
    {
        private NbtReaderState _state;

        // Summary:
        //     Initializes a new instance of the System.Text.Json.Utf8JsonReader structure that
        //     processes a read-only span of UTF-8 encoded text and indicates whether the input
        //     contains all the text to process.
        //
        // Parameters:
        //   jsonData:
        //     The UTF-8 encoded JSON text to process.
        //
        //   isFinalBlock:
        //     true to indicate that the input sequence contains the entire data to process;
        //     false to indicate that the input span contains partial data with more data to
        //     follow.
        //
        //   state:
        //     An object that contains the reader state. If this is the first call to the constructor,
        //     pass the default state; otherwise, pass the value of the System.Text.Json.Utf8JsonReader.CurrentState
        //     property from the previous instance of the System.Text.Json.Utf8JsonReader.
        public NbtReader(ReadOnlySpan<byte> data, bool isFinalBlock, NbtReaderState state)
        {
        }

        // Summary:
        //     Initializes a new instance of the System.Text.Json.Utf8JsonReader structure that
        //     processes a read-only span of UTF-8 encoded text using the specified options.
        //
        // Parameters:
        //   jsonData:
        //     The UTF-8 encoded JSON text to process.
        //
        //   options:
        //     Defines customized behavior of the System.Text.Json.Utf8JsonReader that differs
        //     from the JSON RFC (for example how to handle comments or maximum depth allowed
        //     when reading). By default, the System.Text.Json.Utf8JsonReader follows the JSON
        //     RFC strictly; comments within the JSON are invalid, and the maximum depth is
        //     64.
        public NbtReader(ReadOnlySpan<byte> data, NbtReaderOptions options = default) :
            this(data, true, new NbtReaderState(options))
        {
        }

        /// <summary>
        /// Gets the type of the last processed element.
        /// </summary>
        public NbtType TagType { get; }

        /// <summary>
        /// Gets the index that the last processed element starts at (within the given input data).
        /// </summary>
        public readonly long TagStartIndex { get; }

        /// <summary>
        /// Gets the mode of this instance of the <see cref="NbtReader"/> which indicates
        /// whether all the data was provided or there is more data to come.
        /// </summary>
        /// <returns>
        /// true if the reader was constructed with the input span containing
        /// the entire data to process; false if the reader was constructed with an
        /// input span that may contain partial data with more data to follow.
        /// </returns>
        public bool IsFinalBlock { get; }

        /// <summary>
        /// Gets the current <see cref="NbtReader"/> state to pass to a <see cref="NbtReader"/>
        /// constructor with more data.
        /// </summary>
        public NbtReaderState CurrentState => _state;

        /// <summary>
        /// Gets the depth of the current element.
        /// </summary>
        public int CurrentDepth => 0;

        /// <summary>
        /// Gets the total number of bytes consumed so far by this instance.
        /// </summary>
        public long BytesConsumed => 0;

        /// <summary>
        /// Gets the raw value of the last processed element as a slice of the input.
        /// </summary>
        public readonly ReadOnlySpan<byte> ValueSpan { get; }

        // Summary:
        //     Reads the next JSON token from the input source.
        //
        // Returns:
        //     true if the token was read successfully; otherwise, false.
        //
        // Exceptions:
        //   T:System.Text.Json.JsonException:
        //     An invalid JSON token according to the JSON RFC is encountered. -or- The current
        //     depth exceeds the recursive limit set by the maximum depth.
        public bool Read();

        // Summary:
        //     Skips the children of the current JSON token.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     The reader was given partial data with more data to follow (that is, System.Text.Json.Utf8JsonReader.IsFinalBlock
        //     is false).
        //
        //   T:System.Text.Json.JsonException:
        //     An invalid JSON token was encountered while skipping, according to the JSON RFC.
        //     -or- The current depth exceeds the recursive limit set by the maximum depth.
        public void Skip();

        public bool TrySkip();

        public sbyte GetSByte();

        public double GetDouble();

        public short GetInt16();

        public int GetInt32();

        public long GetInt64();

        public float GetSingle();

        public string GetString();

        public bool TryGetSByte(out sbyte value);

        public bool TryGetDouble(out double value);

        public bool TryGetInt16(out short value);

        public bool TryGetInt32(out int value);

        public bool TryGetInt64(out long value);

        public bool TryGetSByte(out sbyte value);

        public bool TryGetSingle(out float value);

        public bool ValueTextEquals(ReadOnlySpan<byte> utf8Text);

        public bool ValueTextEquals(ReadOnlySpan<char> text);

        public bool ValueTextEquals(string text);
    }
}
