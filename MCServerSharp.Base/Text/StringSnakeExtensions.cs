using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MCServerSharp
{
    public static class StringSnakeExtensions
    {
        [return: NotNullIfNotNull("value")]
        public static string? ToSnake(this string? value)
        {
            if (value == null)
                return null;

            if (value.Length == 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder(value.Length);
            bool modified = false;

            char c = value[0];
            if (char.IsWhiteSpace(c) || c == '-')
            {
                builder.Append('_');
                modified = true;
            }
            else
            {
                builder.Append(c);
            }

            for (int i = 1; i < value.Length; i++)
            {
                c = value[i];
                if (char.IsUpper(c))
                {
                    builder.Append('_');
                    modified = true;
                }
                else if (c == '-' || char.IsWhiteSpace(c))
                {
                    builder.Append('_');
                    modified = true;
                    continue;
                }
                builder.Append(c);
            }

            if (!modified)
                return value;
            return builder.ToString();
        }
    }
}
