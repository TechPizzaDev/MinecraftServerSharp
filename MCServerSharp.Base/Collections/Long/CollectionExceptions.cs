// Copied from .NET Foundation (and Modified)

using System;

namespace MCServerSharp.Collections
{
    internal class CollectionExceptions
    {
        public static Exception InvalidOperation_ConcurrentOperations()
        {
            return new InvalidOperationException(
                "A concurrent update was performed on the collection and corrupted its state.");
        }

        public static Exception InvalidOperation_EnumerationFailedVersion()
        {
            return new InvalidOperationException(
                "The collection was modified and enumeration may not continue.");
        }

        public static Exception InvalidOperation_EnumerationCantHappen()
        {
            return new InvalidOperationException(
                "Enumeration has either not started or has already finished.");
        }

        public static Exception NotSupported_KeyCollectionSet()
        {
            return new NotSupportedException(
                "Mutating a key collection derived from a dictionary is not allowed.");
        }

        public static Exception NotSupported_ValueCollectionSet()
        {
            return new NotSupportedException(
                "Mutating a value collection derived from a dictionary is not allowed.");
        }

        public static Exception Argument_ArrayPlusOffTooSmall()
        {
            return new ArgumentException(
                "Destination array is not long enough to copy all the items in the collection. " +
                "Check array index and length.");
        }

        public static Exception Argument_DuplicateKey(string? keyString, string? paramName)
        {
            return new ArgumentException(
                "An item with the same key has already been added. Key: " + keyString, paramName);
        }

        public static Exception Argument_InvalidArrayType(string? paramName)
        {
            return new ArgumentException(
                "Target array type is not compatible with the type of items in the collection.", paramName);
        }

        public static Exception Argument_MultiDimArrayNotSupported(string? paramName)
        {
            return new ArgumentException(
                "Only single dimensional arrays are supported for the requested action.", paramName);
        }

        public static Exception Argument_NonZeroLowerBound(string? paramName)
        {
            return new ArgumentException(
                "The lower bound of target array must be zero.", paramName);
        }
    }
}