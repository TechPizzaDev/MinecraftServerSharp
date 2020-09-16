using System;

namespace MCServerSharp
{
    public static class ArgumentGuard
    {
        /// <summary>
        /// Throws if the <paramref name="destination"/> span is 
        /// smaller than the <paramref name="source"/> span.
        /// </summary>
        public static void AssertSourceLargerThanDestination<T>(
            ReadOnlySpan<T> source, Span<T> destination)
        {
            if (source.IsEmpty) throw new ArgumentEmptyException(nameof(source));
            if (destination.IsEmpty) throw new ArgumentEmptyException(nameof(destination));

            if (destination.Length < source.Length)
                throw new ArgumentException(
                    $"The destination is smaller than the source.", nameof(destination));
        }

        /// <summary>
        /// Throws if the <paramref name="count"/> is <see langword="null"/>, less, or equal to zero.
        /// </summary>
        public static void AssertNonEmpty(long? count, string paramName, bool inlineParamName = true)
        {
            if (!count.HasValue)
                throw new ArgumentNullException(nameof(paramName));

            if (count.Value <= 0)
            {
                string name = inlineParamName ? paramName : "Collection";
                throw new ArgumentEmptyException(paramName, $"{name} may not be empty.");
            }
        }

        /// <summary>
        /// Throws if the <paramref name="value"/> is less or equal to zero.
        /// </summary>
        public static void AssertGreaterThanZero(long value, string paramName, bool inlineParamName = true)
        {
            if (value <= 0)
            {
                string name = inlineParamName ? paramName : "Value";
                string message = $"{name} must be greater than zero.";
                throw new ArgumentOutOfRangeException(message, paramName);
            }
        }

        /// <summary>
        /// Throws if the <paramref name="value"/> is less than zero.
        /// </summary>
        public static void AssertAtLeastZero(long value, string paramName, bool inlineParamName = true)
        {
            if (value < 0)
            {
                string name = inlineParamName ? paramName : "Value";
                string message = $"{name} must be equal to or greater than zero.";
                throw new ArgumentOutOfRangeException(message, paramName);
            }
        }

        /// <summary>
        /// Throws if the <see cref="string"/> is empty or consists only out of white-space characters.
        /// </summary>
        public static void AssertNotNullOrWhiteSpace(
            string value, string paramName, bool inlineParamName = true)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                string name = inlineParamName ? paramName : "Value";
                string message = $"{name} may not be empty or consist only out of white-space characters.";
                throw new ArgumentException(message, paramName);
            }
        }
    }
}
