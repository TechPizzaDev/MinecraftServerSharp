
using System;
using System.Collections.Generic;

namespace MCServerSharp.Collections
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> SelectWhere<TSource, TValue, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TValue> valueSelector,
            Func<TSource, TValue, bool> predicate,
            Func<TSource, TValue, TResult> resultSelector)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (valueSelector == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(source));
            if (resultSelector == null)
                throw new ArgumentNullException(nameof(source));

            foreach (var item in source)
            {
                var value = valueSelector(item);
                if (predicate(item, value))
                    yield return resultSelector(item, value);
            }
        }

        public static IEnumerable<TValue> SelectWhere<TSource, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TValue> valueSelector,
            Func<TSource, TValue, bool> predicate)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (valueSelector == null)
                throw new ArgumentNullException(nameof(source));
            if (predicate == null)
                throw new ArgumentNullException(nameof(source));

            foreach (var item in source)
            {
                var value = valueSelector(item);
                if (predicate(item, value))
                    yield return value;
            }
        }
    }
}
