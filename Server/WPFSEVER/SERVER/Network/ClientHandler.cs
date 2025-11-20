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
        private CancellationToken _cancellationToken; // readonly 제거 (RunAsync에서 받기 위해)
        private readonly string _clientId; // 클라이언트 식별자 

        private NetworkStream? _stream;

        public string ClientType { get; set; } = "Unknown";

        public event Action<string, BaseMessage>? OnMessageReceived;

        public event Action<string>? OnClientDisconnected;

        // [기존 생성자 유지하되, TcpServer 호환을 위해 오버로딩 추가]
        public ClientHandler(TcpClient client, CancellationToken cancellationToken)
        {
            _client = client;
            _cancellationToken = cancellationToken;
            _clientId = _client.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        // [추가됨] TcpServer에서 호출하는 생성자 (문자열 ID 버전)
        public ClientHandler(TcpClient client, string clientId)
        {
            _client = client;
            _clientId = clientId;
            // 토큰은 나중에 RunAsync에서 받음
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

        // [기존 메서드명 유지]
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
                        // 예외 발생 시 로그 출력을 위해 이벤트 발생
                        OnMessageReceived?.Invoke(_clientId, new BaseMessage
                        {
                            msg = MsgType.STATUS_REQ, // 임시로 사용
                            reason = $"[ERROR] {ex.Message}"
                        });
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
                // [수정] finally 블록 코드를 Disconnect 메서드로 감싸서 재사용
                Disconnect();
            }
        }

        // =======================================================================
        // [추가된 부분] TcpServer와의 호환성을 위해 추가된 메서드들
        // =======================================================================

        // 1. TcpServer가 호출하는 RunAsync (기존 HandleClientAsync로 연결)
        public async Task RunAsync(CancellationToken ct)
        {
            _cancellationToken = ct; // 토큰 설정
            await HandleClientAsync();
        }

        // 2. TcpServer가 호출하는 Disconnect (기존 finally 블록 로직 이동)
        public void Disconnect()
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