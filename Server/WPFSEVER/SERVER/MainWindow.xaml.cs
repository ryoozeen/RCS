using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.ComponentModel;

using SERVER.Protocol;
using SERVER.Network;

namespace SERVER
{
    public partial class MainWindow : Window
    {
        private TcpServer? _tcpServer;
        private const int Port = 7000;

        private readonly DatabaseManager _dbManager; // [필수] DB 변수 선언

        public MainWindow()
        {
            InitializeComponent();

            // [DB 연결 문자열] Password는 본인 환경에 맞게 설정하세요.
            string connectionString = "Server=localhost;Port=3306;Database=rcs;User=root;Password=;";
            _dbManager = new DatabaseManager(connectionString);

            _tcpServer = new TcpServer(Port);
            _tcpServer.OnMessageReceived += TcpServer_OnMessageReceived;
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
            catch (Exception ex)
            {
                AppendLog($"서버 시작 실패: {ex.Message}");
            }
        }

        private void UpdateServerStatus(bool isRunning)
        {
            if (isRunning)
            {
                ServerStatusBar.Background = new SolidColorBrush(Colors.Green);
                ServerStatusBar.BorderBrush = new SolidColorBrush(Colors.Green);
                ServerStatusText.Text = "Server Start";
            }
            else
            {
                ServerStatusBar.Background = new SolidColorBrush(Colors.Red);
                ServerStatusBar.BorderBrush = new SolidColorBrush(Colors.Red);
                ServerStatusText.Text = "Server Stop";
            }
        }

        private void AppendLog(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendLog(message));
                return;
            }
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogTextBox.AppendText($"[{timestamp}] {message}\r\n");
            LogTextBox.ScrollToEnd();
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
            catch (Exception ex)
            {
                AppendLog($"서버 중지 실패: {ex.Message}");
            }
        }

        private async void TcpServer_OnMessageReceived(string clientId, ControlMessage message)
        {
            string logMessage = FormatLogMessage(clientId, message, " [RECV]");
            AppendLog(logMessage);
            await RouteMessage(clientId, message);
        }

        private string FormatLogMessage(string clientId, ControlMessage message, string direction = "")
        {
            string clientName = clientId.Split(':')[0];
            string content = "";

            switch (message.msg)
            {
                case "CLIENT_IDENTIFY": content = $"클라이언트 식별: {message.command}"; break;
                case "ENROLL_REQ": content = $"회원가입 요청: {message.id}"; break;
                case "ENROLL_RES": content = $"회원가입 응답: {message.registered}"; break;
                case "LOGIN_REQ": content = $"로그인 요청: {message.id}"; break;
                case "LOGIN_RES": content = $"로그인 응답: {message.logined}"; break;
                case "DOOR_REQ": content = $"문 제어 요청: {message.open}"; break;
                case "DOOR_RES": content = $"문 제어 응답: {message.doorStatus}"; break;
                case "STATUS_REQ": content = $"상태 요청: Charging={message.charging}"; break;
                case "STATUS_RES": content = $"상태 응답: {message.resulted}"; break;
                default: content = $"[알 수 없는 메시지] Msg: {message.msg}"; break;
            }
            return $"{clientName}{direction} {content}";
        }

        private async Task RouteMessage(string clientId, ControlMessage message)
        {
            ControlMessage? response = null;
            ControlMessage? forward = null;
            string? targetClientId = null;

            try
            {
                switch (message.msg)
                {
                    case "CLIENT_IDENTIFY":
                        string clientType = message.command ?? "Unknown";
                        _tcpServer?.IdentifyClient(clientId, clientType);
                        AppendLog($"[{clientId}] 식별 완료: {clientType}");
                        break;

                    // [DB 연동] ENROLL_REQ
                    case "ENROLL_REQ":
                        int rowsAffected = await _dbManager.EnrollUserAsync(message);
                        bool isEnrolled = rowsAffected > 0;

                        // [WPF 로그 출력] DB 작업 결과를 GUI 로그에 직접 기록
                        AppendLog($"[DB RESULT] ENROLL: {rowsAffected} rows affected.");

                        response = new ControlMessage { msg = "ENROLL_RES", registered = isEnrolled };
                        targetClientId = clientId;
                        break;

                    // [DB 연동] LOGIN_REQ
                    case "LOGIN_REQ":
                        int loginCount = await _dbManager.LoginUserAsync(message);
                        bool isLogined = loginCount > 0;

                        // [WPF 로그 출력] DB 작업 결과를 GUI 로그에 직접 기록
                        AppendLog($"[DB RESULT] LOGIN: Found {loginCount} user(s).");

                        response = new ControlMessage { msg = "LOGIN_RES", logined = isLogined };
                        targetClientId = clientId;
                        break;

                    case "START_REQ":
                    case "DOOR_REQ":
                    case "TRUNK_REQ":
                    case "AIR_REQ":
                    case "CLI_REQ":
                    case "HEAT_REQ":
                    case "LIGHT_REQ":
                    case "CONTROL_REQ":
                    case "STATUS_REQ":
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("DOBOTLAB");
                        break;

                    case "START_RES":
                    case "DOOR_RES":
                    case "TRUNK_RES":
                    case "AIR_RES":
                    case "CLI_RES":
                    case "HEAT_RES":
                    case "LIGHT_RES":
                    case "CONTROL_RES":
                    case "STATUS_RES":
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("RCS");
                        break;
                }

                if (response != null && targetClientId != null)
                {
                    await _tcpServer.SendToClientAsync(targetClientId, response);
                    LogSend(targetClientId, response);
                }

                if (forward != null && targetClientId != null)
                {
                    await _tcpServer.SendToClientAsync(targetClientId, forward);
                    LogSend(targetClientId, forward);
                }
                else if (forward != null && targetClientId == null)
                {
                    AppendLog($"[라우팅 실패] 메시지 {message.msg}를 전달할 대상({(message.msg?.EndsWith("_REQ") == true ? "DOBOTLAB" : "RCS")})을 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[메시지 처리 오류] {ex.Message}");
            }
        }

        private void LogSend(string clientId, ControlMessage message)
        {
            string logMessage = FormatLogMessage(clientId, message, " [SEND]");
            AppendLog(logMessage);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_tcpServer != null)
            {
                _tcpServer.OnMessageReceived -= TcpServer_OnMessageReceived;
                try { _tcpServer.Stop(); } catch { }
            }
            base.OnClosing(e);
        }
    }
}