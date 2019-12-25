using System;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace SharpMinecraftServer
{
    public class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action EmptyAction = () => { };

        private Action _continuation;

        public Action Continuation => _continuation;
        public bool IsCompleted { get; set; }
        public SocketAsyncEventArgs EventArgs { get; }

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            EventArgs = eventArgs ?? throw new ArgumentNullException(nameof(eventArgs));

            EventArgs.Completed += delegate
            {
                (_continuation ?? Interlocked.CompareExchange(
                    ref _continuation, EmptyAction, null))?.Invoke();
            };
        }

        public SocketAwaitable GetAwaiter() => this;

        public void OnCompleted(Action continuation)
        {
            if (_continuation == EmptyAction ||
                Interlocked.CompareExchange(ref _continuation, continuation, null) == EmptyAction)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (EventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)EventArgs.SocketError);
        }

        public void Reset()
        {
            IsCompleted = false;
            _continuation = null;
        }
    }
}
