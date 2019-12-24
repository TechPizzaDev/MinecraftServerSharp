using System;
using System.Net;
using System.Net.Sockets;

namespace SharpMinecraftServer
{
    public class UserToken
    {
        public ConnectionListener Listener { get; }
        public Socket Socket { get; }

        public UserToken(ConnectionListener listener, Socket socket)
        {
            Listener = listener ?? throw new ArgumentNullException(nameof(listener));
            Socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public void Close()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Disconnect(reuseSocket: true);
        }
    }

    public class ConnectionListener
    {
        private Socket _listener;

        public event Action<ConnectionListener> Started;
        public event Action<ConnectionListener> Stopped;

        public event Action<UserToken> Connection;

        public ConnectionListener(IPEndPoint localEndPoint)
        {
            _listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
            _listener.Bind(localEndPoint);
        }

        public void Start(int backlog)
        {
            _listener.Listen(backlog);
            Started?.Invoke(this);

            var acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.Completed += AcceptEventArg_Completed;

            StartAccept(acceptEventArg);
        }

        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            // socket must be cleared since the context object is being reused
            acceptEventArg.AcceptSocket = null;

            if (!_listener.AcceptAsync(acceptEventArg))
                ProcessAccept(acceptEventArg);
        }

        private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }

        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            var readEventArgs = new SocketAsyncEventArgs(); // TODO: pool 
            readEventArgs.Completed += SocketAsync_Completed;

            byte[] buffer = new byte[4096];
            readEventArgs.SetBuffer(buffer, 0, buffer.Length);

            var userToken = new UserToken(this, e.AcceptSocket);
            userToken.Socket.NoDelay = true;
            readEventArgs.UserToken = userToken;

            Connection?.Invoke(userToken);

            // As soon as the client is connected, post a receive to the connection
            if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
                ProcessReceive(readEventArgs);

            // Accept the next connection request
            StartAccept(e);
        }

        private void SocketAsync_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive or send.");
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            var token = (UserToken)e.UserToken;

        TryProcess:
            if (e.BytesTransferred > 0 &&
                e.SocketError == SocketError.Success)
            {
                // do stuff with data
                Console.WriteLine("got: " + e.BytesTransferred);

                if (!token.Socket.ReceiveAsync(e))
                    goto TryProcess;
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                var token = (UserToken)e.UserToken;
            }
            else
            {
                CloseClientSocket(e);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            var token = (UserToken)e.UserToken;
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            catch (Exception) // throws if client process has already closed
            {
            }
            token.Socket.Close();

            // m_readWritePool.Push(e); // TODO: pool

            // TODO: disconnect event  
        }

        public void Stop()
        {
            _listener.Close();
            Stopped?.Invoke(this);
        }
    }
}
