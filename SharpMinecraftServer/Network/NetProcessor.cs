using System;
using System.Net.Sockets;

namespace SharpMinecraftServer.Network
{
    /// <summary>
    /// Processes and network messages.
    /// </summary>
    public class NetProcessor
    {
        public void AddConnection(NetConnection connection)
        {
            var e = connection.SocketEvent;
            e.Completed += SocketEvent_Completed;

            // As soon as the client is connected, post a receive to the connection
            if (!connection.Socket.ReceiveAsync(e))
                ProcessReceive(connection);
        }

        private void SocketEvent_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive((NetConnection)e.UserToken);
                    break;

                case SocketAsyncOperation.Send:
                    ProcessSend((NetConnection)e.UserToken);
                    break;

                default:
                    throw new ArgumentException(
                        "The last operation completed on the socket was not a receive or send.");
            }
        }

        private void ProcessReceive(NetConnection connection)
        {
            var e = connection.SocketEvent;

        TryProcess:
            if (e.BytesTransferred > 0 &&
                e.SocketError == SocketError.Success)
            {
                // do stuff with data
                //Console.WriteLine("got: " + e.BytesTransferred);

                if (!connection.Socket.ReceiveAsync(e))
                    goto TryProcess;
            }
            else
            {
                connection.Close();
            }
        }

        private void ProcessSend(NetConnection connection)
        {
            var e = connection.SocketEvent;

            if (e.SocketError == SocketError.Success)
            {

            }
            else
            {
                connection.Close();
            }
        }
    }
}
