using SERVER.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading; 

namespace SERVER.Network
{
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly CancellationToken _cancellationToken;
        private readonly string _clientId; // 클라이언트 식별자 


        private NetworkStream? _stream;

        public string ClientType { get; set; } = "Unknown";

        public event Action<string, BaseMessage>? OnMessageReceived;

        public event Action<string>? OnClientDisconnected;

        public ClientHandler(TcpClient client, CancellationToken cancellationToken)
        {
            _client = client;
            _cancellationToken = cancellationToken;

            _clientId = _client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        public async Task SendMessageAsync(BaseMessage message)
        {
            if (_stream == null || !_client.Connected)
            {
                return;
            }

            try
            {
                byte[] messageBytes = BaseMessage.SerializeMessage(message);

                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length, _cancellationToken);
            }
            catch (Exception)
            {
                // 전송 오류
            }
        }

        public async Task HandleClientAsync()
        {

            try
            {
                _stream = _client.GetStream();

                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        BaseMessage? message = await BaseMessage.DeserializeMessageAsync(_stream, _cancellationToken);

                        if (message == null)
                        {
                            break;
                        }
                        OnMessageReceived?.Invoke(_clientId, message);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {

                        break;
                    }
                }
            }
            catch (Exception)
            {
                // 연결 오류
            }
            finally
            {
                OnClientDisconnected?.Invoke(_clientId);

                try
                {
                    _stream?.Close();
                }
                catch { } // 무시

                try
                {
                    _client?.Close();
                }
                catch { } // 무시

                try
                {
                    _client?.Dispose();
                }
                catch { } // 무시
            }
        }
    }
}