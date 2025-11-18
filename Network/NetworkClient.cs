using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using DotBotCarClient.Protocol;

namespace DotBotCarClient.Network
{
    public class NetworkClient
    {
        private TcpClient? _client;
        private NetworkStream? _stream;

        public bool IsConnected => _client != null && _client.Connected;

        // 나중에 서버에서 오는 메시지를 처리할 때 쓰는 이벤트
        public event Action<ControlMessage>? OnMessageReceived;
        public event Action? OnDisconnected;

        public async Task ConnectAsync(string host, int port)
        {
            if (IsConnected) return;

            _client = new TcpClient();
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();

            // 연결 후 수신 루프 시작
            _ = Task.Run(ReceiveLoop);
        }

        public async Task SendAsync(ControlMessage message)
        {
            if (_stream == null)
                throw new InvalidOperationException("서버에 연결되지 않았습니다.");

            byte[] buffer = ControlMessage.SerializeMessage(message);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task ReceiveLoop()
        {
            try
            {
                while (true)
                {
                    if (_stream == null) break;

                    var msg = await ControlMessage.DeserializeMessageAsync(_stream);
                    if (msg == null)
                        break; // 연결 끊김

                    OnMessageReceived?.Invoke(msg);
                }
            }
            catch
            {
                // 통신 중 예외 → 연결 종료 처리
            }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }

        public void Close()
        {
            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
        }
    }
}
