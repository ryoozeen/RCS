using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using SERVER.Protocol;

namespace SERVER.Network
{
    public class TcpServer
    {
        private TcpListener _listener;

        // 클라이언트 핸들러 목록 (ID -> Handler)
        private ConcurrentDictionary<string, ClientHandler> _clients = new ConcurrentDictionary<string, ClientHandler>();

        // ★ 추가된 부분: 클라이언트 타입 관리 (ID -> Type 문자열)
        private ConcurrentDictionary<string, string> _clientTypes = new ConcurrentDictionary<string, string>();

        public event Action<string, BaseMessage>? OnMessageReceived;
        public event Action<string>? OnClientConnected;
        public event Action<string, string>? OnClientDisconnected; // 타입 정보 포함

        private CancellationTokenSource _cts = new CancellationTokenSource();

        public TcpServer(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public async Task StartAsync()
        {
            _listener.Start();
            _cts = new CancellationTokenSource();

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(_cts.Token);
                    HandleNewClient(client);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { Console.WriteLine($"Accept Error: {ex.Message}"); }
            }
        }

        private void HandleNewClient(TcpClient client)
        {
            string clientId = client.Client.RemoteEndPoint?.ToString() ?? Guid.NewGuid().ToString();
            var handler = new ClientHandler(client, clientId);

            // 메시지 수신 이벤트 연결
            handler.OnMessageReceived += (msg) => OnMessageReceived?.Invoke(clientId, msg);

            // 연결 해제 이벤트 연결
            handler.OnDisconnected += () =>
            {
                _clients.TryRemove(clientId, out _);
                _clientTypes.TryRemove(clientId, out var type);
                // 연결 해제 시 클라이언트 타입 정보도 같이 보냄
                OnClientDisconnected?.Invoke(clientId, type ?? "Unknown");
            };

            _clients.TryAdd(clientId, handler);

            // 처음 접속 시엔 누군지 모르므로 Unknown으로 설정
            _clientTypes.TryAdd(clientId, "Unknown");

            OnClientConnected?.Invoke(clientId);

            // 핸들러 실행
            _ = handler.RunAsync(_cts.Token);
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener.Stop();
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();
            _clientTypes.Clear();
        }

        // 특정 클라이언트에게 메시지 전송
        public async Task SendToClientAsync(string clientId, BaseMessage message)
        {
            if (_clients.TryGetValue(clientId, out var handler))
            {
                await handler.SendMessageAsync(message);
            }
        }

        public string GetClientType(string clientId)
        {
            if (_clientTypes.TryGetValue(clientId, out string? type))
            {
                return type;
            }
            return "Unknown";
        }

        // 2. 클라이언트 식별 (로그인/식별 요청 성공 시 호출)
        public void IdentifyClient(string clientId, string type)
        {
            _clientTypes.AddOrUpdate(clientId, type, (key, oldVal) => type);
        }

        // 3. 타입으로 클라이언트 ID 찾기 (라우팅용)
        public string? GetClientByType(string type)
        {
            // 해당 타입을 가진 첫 번째 클라이언트의 키(ID)를 반환
            return _clientTypes.FirstOrDefault(x => x.Value == type).Key;
        }
    }
}