using SERVER.Network;
using SERVER.Protocol;
using System;
using System.ComponentModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SERVER
{
    public partial class MainWindow : Window
    {
        private TcpServer? _tcpServer;
        private DatabaseManager? _dbManager; // DB 매니저 추가
        private const int Port = 7000;

        public MainWindow()
        {
            InitializeComponent();

            string connectionString = "Server=localhost;Port=3306;Database=rcs;User=root;Password=1234;";
            _dbManager = new DatabaseManager(connectionString);

            // 서버 설정
            _tcpServer = new TcpServer(Port);
            _tcpServer.OnMessageReceived += TcpServer_OnMessageReceived;
            _tcpServer.OnClientConnected += TcpServer_OnClientConnected;
            _tcpServer.OnClientDisconnected += TcpServer_OnClientDisconnected;
        }

        private async void ServerConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpServer == null) return;
            try
            {
                await _tcpServer.StartAsync();
                UpdateServerStatus(true);
                AppendLog("서버가 시작되었습니다.");
            }
            catch (Exception ex) { AppendLog($"서버 시작 실패: {ex.Message}"); }
        }

        private void ServerShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpServer == null) return;
            try
            {
                _tcpServer.Stop();
                UpdateServerStatus(false);
                AppendLog("서버가 중지되었습니다.");
            }
            catch (Exception ex) { AppendLog($"서버 중지 실패: {ex.Message}"); }
        }

        private void UpdateServerStatus(bool isRunning)
        {
            ServerStatusBar.Background = new SolidColorBrush(isRunning ? Colors.Green : Colors.Red);
            ServerStatusText.Text = isRunning ? "Server Start" : "Server Stop";
        }

        private void AppendLog(string message)
        {
            if (!Dispatcher.CheckAccess()) { Dispatcher.Invoke(() => AppendLog(message)); return; }
            LogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            LogTextBox.ScrollToEnd();
        }

        // 메시지 수신 핸들러
        private async void TcpServer_OnMessageReceived(string clientId, BaseMessage message)
        {
            // 1. 로그인/회원가입/식별은 즉시 처리
            if (message.msg == MsgType.CLIENT_IDENTIFY_REQ || message.msg == MsgType.LOGIN_REQ || message.msg == MsgType.ENROLL_REQ)
            {
                await RouteMessage(clientId, message);
                return;
            }

            // 2. 로그 출력 (빈번한 STATUS_REQ 등 제외)
            if (message.msg != MsgType.STATUS_REQ && message.msg != MsgType.LOGIN_RES)
            {
                string logMsg = FormatLogMessage(clientId, message, " [RECV]");
                if (!string.IsNullOrEmpty(logMsg)) AppendLog(logMsg);
            }

            // 3. 그 외 메시지 라우팅 처리
            await RouteMessage(clientId, message);
        }

        // 라우팅 및 로직 처리 (서버 로직 + 내 DB 연결)
        private async Task RouteMessage(string clientId, BaseMessage message)
        {
            BaseMessage? response = null;
            BaseMessage? forward = null;
            string? targetClientId = null;

            try
            {
                switch (message.msg)
                {
                    // [서버 기능] 식별
                    case MsgType.CLIENT_IDENTIFY_REQ:
                        if (message is ClientIdentifyReq identifyReq)
                        {
                            string name = identifyReq.client_name ?? "Unknown";
                            _tcpServer?.IdentifyClient(clientId, name);
                            AppendLog($"[{name}] 식별됨 (ID: {clientId})");
                            response = new ClientIdentifyRes { identified = true };
                            targetClientId = clientId;
                        }
                        break;

                    // [내 기능] 회원가입 (DB 연결)
                    case MsgType.ENROLL_REQ:
                        if (message is EnrollReq enrollReq && _dbManager != null)
                        {
                            int result = await _dbManager.EnrollUserAsync(enrollReq);
                            response = new EnrollRes { registered = (result > 0) };
                            targetClientId = clientId;
                            AppendLog($"[DB] 회원가입 결과: {(result > 0 ? "성공" : "실패")}");
                        }
                        break;

                    // [내 기능] 로그인 (DB 연결) + [서버 기능] 식별
                    case MsgType.LOGIN_REQ:
                        if (message is LoginReq loginReq && _dbManager != null)
                        {
                            int count = await _dbManager.LoginUserAsync(loginReq);
                            bool success = count > 0;
                            if (success)
                            {
                                _tcpServer?.IdentifyClient(clientId, "RCS"); // 로그인 성공 시 RCS로 식별
                                AppendLog($"[RCS] 로그인 성공: {loginReq.id}");
                            }
                            else
                            {
                                AppendLog($"[Unknown] 로그인 실패: {loginReq.id}");
                            }
                            response = new LoginRes { logined = success };
                            targetClientId = clientId;
                        }
                        break;

                    // [서버 기능] 제어 메시지 전달 (RCS -> DOBOT)
                    case MsgType.START_REQ:
                    case MsgType.DOOR_REQ:
                    case MsgType.TRUNK_REQ:
                    case MsgType.AIR_REQ:
                    case MsgType.CLI_REQ:
                    case MsgType.HEAT_REQ:
                    case MsgType.LIGHT_REQ:
                    case MsgType.CONTROL_REQ:
                    case MsgType.STOP_CHARGING_REQ:
                    case MsgType.STATUS_REQ:
                        targetClientId = _tcpServer?.GetClientByType("DOBOT");
                        if (targetClientId == null) targetClientId = _tcpServer?.GetClientByType("DOBOTLAB");

                        if (targetClientId != null) forward = message;
                        else if (message.msg != MsgType.STATUS_REQ) AppendLog($"[전달 실패] DOBOT 연결 안됨.");
                        break;

                    // [서버 기능] 응답 메시지 전달 (DOBOT -> RCS)
                    case MsgType.START_RES:
                    case MsgType.DOOR_RES:
                    case MsgType.TRUNK_RES:
                    case MsgType.AIR_RES:
                    case MsgType.CLI_RES:
                    case MsgType.HEAT_RES:
                    case MsgType.LIGHT_RES:
                    case MsgType.CONTROL_RES:
                    case MsgType.STOP_CHARGING_RES:
                    case MsgType.STATUS_RES:
                        targetClientId = _tcpServer?.GetClientByType("RCS");
                        if (targetClientId != null)
                        {
                            if (message is ControlRes cRes && !string.IsNullOrEmpty(cRes.reason))
                            {
                                if (cRes.reason.Contains("주차")) cRes.parking = true;
                                if (cRes.reason.Contains("출차")) cRes.driving = true;
                            }
                            forward = message;
                        }
                        break;
                }

                if (response != null && targetClientId != null)
                {
                    await _tcpServer?.SendToClientAsync(targetClientId, response)!;
                    LogSend(targetClientId, response);
                }
                if (forward != null && targetClientId != null)
                {
                    await _tcpServer?.SendToClientAsync(targetClientId, forward)!;
                    LogSend(targetClientId, forward);
                }
            }
            catch (Exception ex) { AppendLog($"[처리 오류] {ex.Message}"); }
        }

        private string FormatLogMessage(string clientId, BaseMessage message, string direction = "")
        {
            string clientType = _tcpServer?.GetClientType(clientId) ?? "Unknown";
            string content = "";
            switch (message.msg)
            {
                case MsgType.ENROLL_REQ: if (message is EnrollReq er) content = $"회원가입: {er.id}"; break;
                case MsgType.LOGIN_REQ: if (message is LoginReq lr) content = $"로그인: {lr.id}"; break;
                case MsgType.START_REQ: if (message is StartReq sr) content = $"시동: {(sr.active ? "ON" : "OFF")}"; break;
                case MsgType.DOOR_REQ: if (message is DoorReq dr) content = $"문: {(dr.door ? "Open" : "Close")}"; break;
                case MsgType.CONTROL_REQ: if (message is ControlReq cr) content = $"주차: {(cr.control ? "주차" : "출차")}"; break;
                case MsgType.STATUS_RES: if (message is StatusRes s) content = $"배터리: {s.battery}%"; break;
                default: content = $"{message.msg}"; break;
            }
            if (string.IsNullOrEmpty(content)) return "";
            return $"[{clientType}]{direction} {content}";
        }

        private void LogSend(string clientId, BaseMessage message)
        {
            if (message.msg == MsgType.STATUS_REQ) return;
            string logMsg = FormatLogMessage(clientId, message, " [SEND]");
            if (!string.IsNullOrEmpty(logMsg)) AppendLog(logMsg);
        }

        private void TcpServer_OnClientConnected(string clientId) => _tcpServer?.IdentifyClient(clientId, "Unknown");
        private void TcpServer_OnClientDisconnected(string clientId, string type) => AppendLog($"[{type}] 연결 해제");

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_tcpServer != null) { _tcpServer.OnMessageReceived -= TcpServer_OnMessageReceived; try { _tcpServer.Stop(); } catch { } }
            base.OnClosing(e);
        }
    }
}