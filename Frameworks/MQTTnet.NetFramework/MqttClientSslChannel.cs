﻿using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading.Tasks;
using MQTTnet.Core.Channel;
using MQTTnet.Core.Client;
using MQTTnet.Core.Exceptions;

namespace MQTTnet
{
    /// <summary>
    /// Describes an SSL channel to an MQTT server.
    /// </summary>
    public class MqttClientSslChannel : IMqttCommunicationChannel, IDisposable
    {
        private readonly Socket _socket;
        private SslStream _sslStream;

        /// <summary>
        /// Creates a new <see cref="MqttClientSslChannel"/>.
        /// </summary>
        public MqttClientSslChannel()
        {
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Creates a new <see cref="MqttClientSslChannel"/> with a predefined <paramref name="socket"/>.
        /// </summary>
        /// <param name="socket"></param>
        public MqttClientSslChannel(Socket socket)
        {
            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        /// <summary>
        /// Asynchronously connects to the host described in the <see cref="MqttClientOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="MqttClientOptions"/> describing the connection.</param>
        public async Task ConnectAsync(MqttClientOptions options)
        {
            try
            {
                await Task.Factory.FromAsync(_socket.BeginConnect, _socket.EndConnect, options.Server, options.Port,
                    null);

                NetworkStream ns = new NetworkStream(_socket, true);
                _sslStream = new SslStream(ns);

                await _sslStream.AuthenticateAsClientAsync(options.Server, null, SslProtocols.Tls12, false);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        /// <summary>
        /// Asynchronously disconnects the client from the server.
        /// </summary>
        public Task DisconnectAsync()
        {
            try
            {
                return Task.Factory.FromAsync(_socket.BeginDisconnect, _socket.EndDisconnect, true, null);
            }
            catch (SocketException exception)
            {
                throw new MqttCommunicationException(exception);
            }
        }

        /// <summary>
        /// Asynchronously writes a sequence of bytes to the socket.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        public Task WriteAsync(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            try
            {
                return _sslStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
                when (ex is SocketException || ex is IOException)
            {
                throw new MqttCommunicationException(ex);
            }
        }

        /// <summary>
        /// Asynchronously reads a sequence of bytes from the socket.
        /// </summary>
        /// <param name="buffer">The buffer to write the data into.</param>
        public Task ReadAsync(byte[] buffer)
        {
            try
            {
                return _sslStream.ReadAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
                when (ex is SocketException || ex is IOException)
            {
                throw new MqttCommunicationException(ex);
            }
        }

        /// <summary>
        /// Releases all resources used by the <see cref="MqttClientSslChannel"/>.
        /// </summary>
        public void Dispose()
        {
            _sslStream?.Dispose();
            _socket?.Dispose();
        }
    }
}