using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace MCServerSharp
{
    public static class StringSnakeExtensions
    {
        [SkipLocalsInit]
        [return: NotNullIfNotNull("value")]
        public static string? ToSnake(this string? value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return string.Empty;

            Span<char> buffer = stackalloc char[1024];
            int bufferIndex = 0;

            StringBuilder? builder = null;
            bool modified = false;

            void Append(char c, Span<char> buffer)
            {
                if (bufferIndex == buffer.Length)
                {
                    if (builder == null)
                    {
                        builder = new StringBuilder((int)(value.Length * 1.5));
                        builder.Append(buffer);
                        builder.Append(c);
                    }
                    return;
                }
                buffer[bufferIndex++] = c;
            }

            char c = value[0];
            if (char.IsWhiteSpace(c) || c == '-')
            {
                Append('_', buffer);
                modified = true;
            }
            else
            {
                Append(c, buffer);
            }

            for (int i = 1; i < value.Length; i++)
            {
                c = value[i];
                if (char.IsUpper(c))
                {
                    Append('_', buffer);
                    modified = true;
                }
                else if (c == '-' || char.IsWhiteSpace(c))
                {
                    Append('_', buffer);
                    modified = true;
                    continue;
                }
                Append(c, buffer);
            }

            if (!modified)
                return value;

            if (builder == null)
                return buffer.Slice(0, bufferIndex).ToString();

            return builder.ToString();
        }
    }
}
