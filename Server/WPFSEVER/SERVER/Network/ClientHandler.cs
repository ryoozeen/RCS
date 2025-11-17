using SERVER.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;  // CancellationToken 사용을 위해

namespace SERVER.Network
{
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly CancellationToken _cancellationToken;
        private readonly string _clientId; // 클라이언트 식별자 (IP:Port)

        // 이벤트: 메시지 수신 시 발생
        public event Action<string, ControlMessage>? OnMessageReceived;

        // 이벤트: 클라이언트 연결 해제 시 발생
        public event Action<string>? OnClientDisconnected;

        public ClientHandler(TcpClient client, CancellationToken cancellationToken)
        {
            _client = client;
            _cancellationToken = cancellationToken;

            // 클라이언트 식별자 생성 (IP:Port)
            _clientId = _client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        public async Task HandleClientAsync()
        {
            NetworkStream? stream = null;

            try
            {
                // 1. NetworkStream 가져오기
                stream = _client.GetStream();

                // 2. 메시지 수신 루프
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 3. 메시지 수신 및 파싱
                        ControlMessage? message = await ControlMessage.DeserializeMessageAsync(stream);

                        // 4. 메시지가 null이면 연결 끊김
                        if (message == null)
                        {
                            break;
                        }

                        // 5. 메시지 수신 이벤트 발생
                        OnMessageReceived?.Invoke(_clientId, message);
                    }
                    catch (Exception ex)
                    {
                        // 메시지 수신 중 오류 발생
                        // 연결 끊김 또는 네트워크 오류
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
                // 6. 클라이언트 연결 해제 이벤트 발생
                OnClientDisconnected?.Invoke(_clientId);

                // 7. 리소스 정리
                try
                {
                    stream?.Close();
                }
                catch
                {
                    // 무시
                }

                try
                {
                    _client?.Close();
                }
                catch
                {
                    // 무시
                }

                try
                {
                    _client?.Dispose();
                }
                catch
                {
                    // 무시
                }
            }
        }
    }
}
