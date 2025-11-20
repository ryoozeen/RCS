using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SERVER.Protocol; // BaseMessage 사용을 위해 필수

namespace SERVER.Network
{
    public class ClientHandler
    {
        private TcpClient _client;
        private string _clientId;
        private NetworkStream _stream;

        // TcpServer에서 구독할 이벤트들
        public event Action<BaseMessage>? OnMessageReceived;
        public event Action? OnDisconnected;

        public ClientHandler(TcpClient client, string clientId)
        {
            _client = client;
            _clientId = clientId;
            _stream = client.GetStream();
        }

        // 클라이언트와 데이터를 주고받는 메인 루프
        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested && _client.Connected)
                {
                    // Protocol.cs에 있는 DeserializeMessageAsync 사용
                    // 스트림에서 메시지를 읽어서 BaseMessage 객체로 변환
                    BaseMessage? message = await BaseMessage.DeserializeMessageAsync(_stream, ct);

                    if (message == null)
                    {
                        // 연결이 끊기거나 잘못된 데이터 수신 시 종료
                        break;
                    }

                    // 메시지 수신 알림 (TcpServer로 전달됨)
                    OnMessageReceived?.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                // 연결 종료 등의 일반적인 예외는 무시하거나 로그 출력
                Console.WriteLine($"Client Loop Error ({_clientId}): {ex.Message}");
            }
            finally
            {
                Disconnect();
            }
        }

        // 메시지 전송 (서버 -> 클라이언트)
        public async Task SendMessageAsync(BaseMessage message)
        {
            if (!_client.Connected) return;

            try
            {
                // Protocol.cs의 SerializeMessage 사용
                byte[] packet = BaseMessage.SerializeMessage(message);
                await _stream.WriteAsync(packet, 0, packet.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send Error ({_clientId}): {ex.Message}");
            }
        }

        // 연결 종료 처리
        public void Disconnect()
        {
            try
            {
                if (_client.Connected)
                {
                    _client.Close();
                }
                // 연결 해제 사실을 TcpServer에 알림
                OnDisconnected?.Invoke();
            }
            catch
            {
                // 이미 닫힌 경우 무시
            }
        }
    }
}