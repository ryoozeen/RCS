using SERVER.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading; // CancellationToken 사용을 위해

namespace SERVER.Network
{
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly CancellationToken _cancellationToken;
        private readonly string _clientId; // 클라이언트 식별자 (IP:Port)

        // [수정] NetworkStream을 멤버 변수로 변경 (메시지 전송 시 사용)
        private NetworkStream? _stream;

        // [신규] 클라이언트 식별용 프로퍼티 (RCS, DOBOTLAB)
        public string ClientType { get; set; } = "Unknown";

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

        //  메시지 전송(Send) 메서드
        public async Task SendMessageAsync(ControlMessage message)
        {
            // 스트림이 없거나 연결이 끊겼으면 전송 불가
            if (_stream == null || !_client.Connected)
            {
                return;
            }

            try
            {
                // 1. 메시지 직렬화 (1단계 ControlMessage에서 만듦)
                byte[] messageBytes = ControlMessage.SerializeMessage(message);

                // 2. 비동기 전송
                // CancellationToken을 WriteAsync에도 전달
                await _stream.WriteAsync(messageBytes, 0, messageBytes.Length, _cancellationToken);
            }
            catch (Exception)
            {
                // 전송 오류 (연결 끊김 등)
                // HandleClientAsync의 finally 블록에서 어차피 정리됨
            }
        }

        public async Task HandleClientAsync()
        {
            // NetworkStream? stream = null; // [수정] 멤버 변수 _stream 사용

            try
            {
                // 1. NetworkStream 가져오기 (멤버 변수에 할당)
                _stream = _client.GetStream();

                // 2. 메시지 수신 루프
                while (!_cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // 3. 메시지 수신 및 파싱
                        // [수정] 1단계에서 변경한 CancellationToken 전달
                        ControlMessage? message = await ControlMessage.DeserializeMessageAsync(_stream, _cancellationToken);

                        // 4. 메시지가 null이면 연결 끊김
                        if (message == null)
                        {
                            break;
                        }

                        // 5. 메시지 수신 이벤트 발생
                        OnMessageReceived?.Invoke(_clientId, message);
                    }
                    catch (OperationCanceledException)
                    {
                        // [신규] 서버가 종료(Cancel)되면 정상적으로 루프 탈출
                        break;
                    }
                    catch (Exception ex)
                    {
                        // 메시지 수신 중 오류 발생
                        // (연결 끊김 또는 네트워크 오류)
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