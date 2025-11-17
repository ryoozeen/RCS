using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SERVER.Protocol;

namespace SERVER.Network
{
    public class TcpServer
    {
        private TcpListener? _listener;
        private bool _isRunning = false;
        private readonly int _port;
        private readonly List<TcpClient> _clients = new List<TcpClient>();
        private CancellationTokenSource? _cancellationTokenSource;
        private readonly List<ClientHandler> _clientHandlers = new List<ClientHandler>();
        // 이벤트: 클라이언트로부터 메시지 수신 시 발생
        public event Action<string, ControlMessage>? OnMessageReceived;

        // 생성자
        public TcpServer(int port) {
            _port = port;
        }

        // 서버 시작
        public Task StartAsync()
        {
            if (_isRunning)
            {
                return Task.CompletedTask; // 이미 실행 중
            }

            try
            {
                // CancellationTokenSource 생성
                _cancellationTokenSource = new CancellationTokenSource();
                var cancellationToken = _cancellationTokenSource.Token;

                // TcpListener 생성 및 시작
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                // 상태 플래그 설정
                _isRunning = true;

                // 연결 수신 루프 시작 (별도 Task로)
                _ = Task.Run(async () => await AcceptClientsAsync(cancellationToken));

                return Task.CompletedTask; // 성공적으로 시작됨을 반환
            }

            catch (Exception ex)
            {
                _isRunning = false;
                return Task.FromException(new Exception($"서버 시작 실패: {ex.Message}"));
            }
        }

        // 연결 수신 루프
        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener!.AcceptTcpClientAsync();
                    
                    // 클라이언트를 리스트에 추가
                    lock (_clients)
                    {
                        _clients.Add(client);
                    }
                    
                    // ClientHandler 생성 및 시작
                    ClientHandler handler = new ClientHandler(client, cancellationToken);

                    // 이벤트 구독: ClientHandler의 이벤트를 TcpServer 이벤트로 전달
                    handler.OnMessageReceived += (clientId, message) =>
                    {
                        // TcpServer의 이벤트로 전달
                        OnMessageReceived?.Invoke(clientId, message);
                    };

                    handler.OnClientDisconnected += (clientId) =>
                    {
                        // 클라이언트 핸들러 리스트에서 제거
                        lock (_clientHandlers)
                        {
                            _clientHandlers.Remove(handler);
                        }
                    };

                    // 클라이언트 핸들러 리스트에 추가
                    lock (_clientHandlers)
                    {
                        _clientHandlers.Add(handler);
                    }

                    // 클라이언트별 메시지 수신 루프 시작 (별도 Task로)
                    _ = Task.Run(async () => await handler.HandleClientAsync(), cancellationToken);

                }
                catch (ObjectDisposedException)
                {
                    // 서버가 중지되었을 때 정상 종료
                    break;
                }
                catch (Exception)
                {
                    // 에러 처리
                    break;
                }
            }
        }

        // 서버 중지
        public void Stop()
        {
            // 1. 이미 중지되었는지 확인
            if (!_isRunning)
            {
                return;
            }

            // 2. 상태 플래그 변경
            _isRunning = false;

            // 3. 연결 수신 루프 취소
            _cancellationTokenSource?.Cancel();

            // 4. TcpListener 중지
            _listener?.Stop();

            // 5. 모든 클라이언트 연결 정리
            lock (_clients)
            {
                int clientCount = _clients.Count;

                for (int i = 0; i < clientCount; i++)
                {
                    try
                    {
                        TcpClient client = _clients[i];
                        if (client != null)
                        {
                            // Close() 먼저 시도
                            try
                            {
                                client.Close();
                            }
                            catch
                            {
                                // Close() 실패 시 Dispose()로 리소스 정리
                                try
                                {
                                    client.Dispose();
                                }
                                catch
                                {
                                    // Dispose()도 실패하면 무시
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // 방어적 코딩: 클라이언트 접근 중 예외 발생 시
                        // 다음 클라이언트 정리 계속 진행
                    }
                }

                // 6. 클라이언트 리스트 비우기
                _clients.Clear();
            }

            // 7. 클라이언트 핸들러 정리
            lock (_clientHandlers)
            {
                foreach (var handler in _clientHandlers.ToList())
                {
                    // 핸들러는 내부적으로 클라이언트 연결을 정리함
                    // (HandleClientAsync의 finally 블록에서 처리)
                }
                _clientHandlers.Clear();
            }

            // 8. 리소스 정리
            _listener = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

    }
}
