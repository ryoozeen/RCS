using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SERVER.Protocol;
using System.Collections.Concurrent;

namespace SERVER.Network
{
    public class TcpServer
    {
        private TcpListener? _listener;
        private bool _isRunning = false;
        private readonly int _port;
        // private readonly List<TcpClient> _clients = new List<TcpClient>();
        private CancellationTokenSource? _cancellationTokenSource;


        // (Key: clientId, Value: ClientHandler)
        private readonly ConcurrentDictionary<string, ClientHandler> _clientHandlers = new ConcurrentDictionary<string, ClientHandler>();

        // [추가] 클라이언트 타입 관리 (ID -> Type 문자열) : 원본 코드 유지하며 기능 추가를 위해 별도 딕셔너리 사용하지 않고 Handler 속성 활용
        // (하지만 원본 Handler에 ClientType 속성이 있다는 가정하에 아래 IdentifyClient 등에서 활용)

        // 이벤트: 클라이언트로부터 메시지 수신 시 발생
        public event Action<string, BaseMessage>? OnMessageReceived;

        // 이벤트: 클라이언트 연결 시 발생
        public event Action<string>? OnClientConnected;

        // 이벤트: 클라이언트 연결 해제 시 발생 (clientId, clientType)
        public event Action<string, string>? OnClientDisconnected;

        // 생성자
        public TcpServer(int port)
        {
            _port = port;
        }

        // [신규] 특정 클라이언트에게 메시지 전송 (MainWindow에서 사용)
        public async Task SendToClientAsync(string clientId, BaseMessage message)
        {
            if (_clientHandlers.TryGetValue(clientId, out ClientHandler? handler))
            {

                await handler.SendMessageAsync(message);
            }
        }

        public void IdentifyClient(string clientId, string clientType)
        {
            if (_clientHandlers.TryGetValue(clientId, out ClientHandler? handler))
            {
                handler.ClientType = clientType; // 2단계에서 만든 ClientType 프로퍼티에 할당
            }
        }

        public string GetClientType(string clientId)
        {
            if (_clientHandlers.TryGetValue(clientId, out ClientHandler? handler))
            {
                return handler.ClientType;
            }
            return "Unknown";
        }

        public string? GetClientByType(string clientType)
        {
            // ClientType이 일치하는 첫 번째 KeyValuePair를 찾음
            var pair = _clientHandlers.FirstOrDefault(p => p.Value.ClientType == clientType);

            // Key(clientId)를 반환. 없으면 null
            return pair.Key;
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

                    TcpClient client = await _listener!.AcceptTcpClientAsync(cancellationToken);

                    // clients 리스트에 추가하는 로직 삭제

                    // ClientHandler 생성 및 시작
                    ClientHandler handler = new ClientHandler(client, cancellationToken);

                    // ClientId 가져오기
                    string clientId = client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

                    // 이벤트 구독: ClientHandler의 이벤트를 TcpServer 이벤트로 전달
                    handler.OnMessageReceived += (id, message) =>
                    {
                        OnMessageReceived?.Invoke(id, message);
                    };

                    handler.OnClientDisconnected += (id) =>
                    {
                        // 클라이언트 타입 가져오기
                        string clientType = "Unknown";
                        if (_clientHandlers.TryGetValue(id, out ClientHandler? disconnectedHandler))
                        {
                            clientType = disconnectedHandler.ClientType;
                        }

                        _clientHandlers.TryRemove(id, out _);

                        // 연결 해제 이벤트 발생
                        OnClientDisconnected?.Invoke(id, clientType);
                    };

                    _clientHandlers.TryAdd(clientId, handler);

                    // 클라이언트 연결 이벤트 발생
                    OnClientConnected?.Invoke(clientId);

                    // 클라이언트별 메시지 수신 루프 시작 (별도 Task로)
                    _ = Task.Run(async () => await handler.HandleClientAsync(), cancellationToken);

                }
                catch (OperationCanceledException)
                {

                    break;
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
            //이미 중지되었는지 확인
            if (!_isRunning)
            {
                return;
            }

            // 상태 플래그 변경
            _isRunning = false;

            // 연결 수신 루프 취소
            _cancellationTokenSource?.Cancel();

            // TcpListener 중지
            _listener?.Stop();


            _clientHandlers.Clear();

            //리소스 정리
            _listener = null;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

    }
}