using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Sharpotify.Util
{
    public class SocketClient
    {
        /// <summary>
        /// Cached Socket object that will be used by each call for the lifetime of this class
        /// </summary>
        private Socket socket;

        /// <summary>
        /// Signaling object used to notify when an asynchronous operation is completed
        /// </summary>
        private ManualResetEvent clientDone = new ManualResetEvent(false);

        /// <summary>
        /// Define a timeout in milliseconds for each asynchronous call. If a response is not received within this timeout period, the call is aborted.
        /// </summary>
        private const int TIMEOUT_MILLISECONDS = 5000;

        /// <summary>
        /// The maximum size of the data buffer to use with the asynchronous socket methods
        /// </summary>
        private const int MAX_BUFFER_SIZE = 2048;

        /// <summary>
        /// Attempt a TCP socket connection to the given host over the given port
        /// </summary>
        /// <param name="hostName">The name of the host</param>
        /// <param name="portNumber">The port number to connect</param>
        /// <returns>A string representing the result of this connection attempt</returns>
        public void Connect(string hostName, int portNumber)
        {
            // Create DnsEndPoint. The hostName and port are passed in to this method.
            DnsEndPoint hostEntry = new DnsEndPoint(hostName, portNumber);

            // Create a stream-based, TCP socket using the InterNetwork Address Family. 
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            
            // Create a SocketAsyncEventArgs object to be used in the connection request
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;

            // Inline event handler for the Completed event.
            // Note: This even handler was implemented inline in order to make this method self-contained.
            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                {
                    // Retrieve the result of this request
                    throw new Exception(e.SocketError.ToString());
                }

                // Signal that the request is complete, unblocking the UI thread
                clientDone.Set();
            });

            // Sets the state of the event to nonsignaled, causing threads to block
            clientDone.Reset();

            // // Make an asynchronous Connect request over the socket
            socket.ConnectAsync(socketEventArg);

            // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS seconds.
            // If no response comes back within this time then proceed
            clientDone.WaitOne(TIMEOUT_MILLISECONDS);
        }

        /// <summary>
        /// Closes this instance.
        /// </summary>
        public void Close()
        {
            socket.Close();
        }
        /// <summary>
        /// Send the given data to the server using the established connection
        /// </summary>
        /// <param name="data">The data to send to the server</param>
        public void Send(byte[] data)
        {
            // We are re-using the _socket object that was initialized in the Connect method
            if (socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

                // Set properties on context object
                socketEventArg.RemoteEndPoint = socket.RemoteEndPoint;
                socketEventArg.UserToken = null;

                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    // Unblock the UI thread
                    clientDone.Set();
                    //throw new Exception(e.SocketError.ToString());
                });

                // Add the data to be sent into the buffer
                socketEventArg.SetBuffer(data, 0, data.Length);

                // Sets the state of the event to nonsignaled, causing threads to block
                clientDone.Reset();

                // Make an asynchronous Send request over the socket
                socket.SendAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS seconds.
                // If no response comes back within this time then proceed
                clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
            else
            {
                throw new Exception("Socket is not initialized");
            }
        }

        /// <summary>
        /// Receive data from the server using the established socket connection
        /// </summary>
        /// <returns>The data received from the server</returns>
        public int Read(byte[] target, int offset, int lenght)
        {
            int result = 0;
            // We are receiving over an established socket connection
            if (socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = socket.RemoteEndPoint;

                // Setup the buffer to receive the data
                socketEventArg.SetBuffer(target, offset, lenght);

                // Inline event handler for the Completed event.
                EventHandler<SocketAsyncEventArgs> callbackevent = null;

                // Note: This even handler was implemented inline in order to make this method self-contained.
                callbackevent = new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        result += e.BytesTransferred;
                        if (result == lenght)
                        {
                            socketEventArg.Completed -= callbackevent;
                            clientDone.Set();
                        }
                    }
                    else
                    {
                        clientDone.Set();
                        throw new Exception(e.SocketError.ToString());
                    }
                });
                
                socketEventArg.Completed += callbackevent;

                // Sets the state of the event to nonsignaled, causing threads to block
                clientDone.Reset();

                // Make an asynchronous Receive request over the socket
                socket.ReceiveAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS seconds.
                // If no response comes back within this time then proceed
                clientDone.WaitOne(TIMEOUT_MILLISECONDS);
            }
            else
            {
                throw new Exception("Socket is not initialized");
            }

            return result;
        }

        /// <summary>
        /// Begins the write.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="lenght">The lenght.</param>
        /// <param name="callback">The callback.</param>
        public void BeginWrite(byte[] data, int offset, int lenght, Action callback)
        {
            int result = 0;
            // Create SocketAsyncEventArgs context object
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();

            // Set properties on context object
            socketEventArg.RemoteEndPoint = socket.RemoteEndPoint;
            socketEventArg.UserToken = null;

            // Inline event handler for the Completed event.
            EventHandler<SocketAsyncEventArgs> callbackevent = null;

            // Note: This even handler was implemented inline in order to make this method self-contained.
            callbackevent  = new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
            {
                if (e.SocketError != SocketError.Success)
                {
                    throw new Exception(e.SocketError.ToString());
                }
                else
                {
                     result += e.BytesTransferred;
                     if (result == lenght)
                     {
                         socketEventArg.Completed -= callbackevent;
                         callback();
                     }
                }
            });

            socketEventArg.Completed += callbackevent;
            // Add the data to be sent into the buffer
            socketEventArg.SetBuffer(data, offset, lenght);

            // Make an asynchronous Send request over the socket
            socket.SendAsync(socketEventArg);
        }

        /// <summary>
        /// Begins the read.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="lenght">The lenght.</param>
        /// <param name="callback">The callback.</param>
        public void BeginRead(byte[] target, int offset, int lenght, Action<byte[]> callback)
        {
            // Create SocketAsyncEventArgs context object
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = socket.RemoteEndPoint;

            // Setup the buffer to receive the data
            socketEventArg.SetBuffer(target, offset, lenght);

            // Inline event handler for the Completed event.
            EventHandler<SocketAsyncEventArgs> callbackevent = null;
            // Note: This even handler was implemented inline in order to make this method self-contained.
            callbackevent  = new EventHandler<SocketAsyncEventArgs>(delegate(object s, SocketAsyncEventArgs e)
            {
                if (e.SocketError == SocketError.Success)
                {
                    if (e.BytesTransferred == lenght - offset)
                    {
                        socketEventArg.Completed -= callbackevent;
                        // Retrieve the data from the buffer
                        callback(e.Buffer);
                    }
                }
                else
                {
                    throw new Exception(e.SocketError.ToString());
                }
            });

            socketEventArg.Completed += callbackevent;

            // Make an asynchronous Receive request over the socket
            socket.ReceiveAsync(socketEventArg);
        }
    }
}
