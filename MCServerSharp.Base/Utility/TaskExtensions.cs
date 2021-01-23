using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace MCServerSharp
{
    public static class TaskExtensions
    {
        public static ConfiguredTaskAwaitable Unchain(this Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static ConfiguredTaskAwaitable<T> Unchain<T>(this Task<T> task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static ConfiguredValueTaskAwaitable Unchain(this ValueTask task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }

        public static ConfiguredValueTaskAwaitable<T> Unchain<T>(this ValueTask<T> task)
        {
            return task.ConfigureAwait(continueOnCapturedContext: false);
        }
    }
}
