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

using SERVER.Protocol;
using SERVER.Network;
using System.ComponentModel;
using System.Threading.Tasks; // async/await 사용을 위해 추가

namespace SERVER
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpServer? _tcpServer;
        private const int Port = 7000; //포트 번호
        private readonly DatabaseManager _dbManager;

        public MainWindow()
        {
            InitializeComponent();

            // Server=localhost 
            // Database=rcs 
            // User=root 
            // Password=; 
            string connectionString = "Server=localhost;Port=3306;Database=rcwUser=root;Password=1234;";

            _dbManager = new DatabaseManager(connectionString);

            _tcpServer = new TcpServer(Port);
            _tcpServer.OnMessageReceived += TcpServer_OnMessageReceived;
            _tcpServer.OnClientConnected += TcpServer_OnClientConnected;
            _tcpServer.OnClientDisconnected += TcpServer_OnClientDisconnected;
        }

        // 서버 연결 버튼
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

        // 상태바 업데이트
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

        // [수정] 로그 출력 메서드 (스레드 안전하게 변경)
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

        // 서버 종료 버튼
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

        // ** 병합 완료 ** (최신 코드: async, BaseMessage)
        private async void TcpServer_OnMessageReceived(string clientId, BaseMessage message)
        {
            string logMessage = FormatLogMessage(clientId, message, " [RECV]");
            AppendLog(logMessage);

            await RouteMessage(clientId, message);
        }

        private string FormatLogMessage(string clientId, BaseMessage message, string direction = "")
        {
            string clientName = clientId.Split(':')[0];
            string content = "";

            switch (message.msg)
            {
                // ** 병합 완료 ** (최신 코드: MsgType 기반 로깅)
                case MsgType.ENROLL_REQ:
                    if (message is EnrollReq req1) content = $"회원가입 요청: {req1.id}";
                    break;
                case MsgType.ENROLL_RES:
                    if (message is EnrollRes res1) content = $"회원가입 응답: {res1.registered}";
                    break;
                case MsgType.LOGIN_REQ:
                    if (message is LoginReq req2) content = $"로그인 요청: {req2.id}";
                    break;
                case MsgType.LOGIN_RES:
                    if (message is LoginRes res2) content = $"로그인 응답: {res2.logined}";
                    break;
                case MsgType.DOOR_REQ:
                    if (message is DoorReq req3) content = $"문 제어 요청: {req3.open}";
                    break;
                case MsgType.DOOR_RES:
                    if (message is DoorRes res3) content = $"문 제어 응답: {res3.doorstatus}";
                    break;
                case MsgType.STATUS_REQ:
                    if (message is StatusReq req4) content = $"상태 요청: CarStatus={req4.carstatus}";
                    break;
                case MsgType.STATUS_RES:
                    if (message is StatusRes res4) content = $"상태 응답: Charging={res4.charging}, Battery={res4.battery}";
                    break;
                default: content = $"[알 수 없는 메시지] Msg: {message.msg}"; break;
            }

            return $"{clientName}{direction} {content}";
        }

        // ** 병합 완료 ** (최신 코드: async Task, DB 연동 로직 유지)
        private async Task RouteMessage(string clientId, BaseMessage message)
        {
            BaseMessage? response = null;
            BaseMessage? forward = null;
            string? targetClientId = null;

            try
            {
                switch (message.msg)
                {
                    // [DB 연동] ENROLL_REQ
                    case MsgType.ENROLL_REQ:
                        if (message is EnrollReq enrollReq)
                        {
                            int rowsAffected = await _dbManager.EnrollUserAsync(enrollReq);
                            bool isEnrolled = rowsAffected > 0;

                            // [WPF 로그 출력] DB 작업 결과를 GUI 로그에 직접 기록
                            AppendLog($"[DB RESULT] ENROLL: {rowsAffected} rows affected.");

                            response = new EnrollRes { registered = isEnrolled };
                            targetClientId = clientId;
                        }
                        break;

                    // [DB 연동] LOGIN_REQ
                    case MsgType.LOGIN_REQ:
                        if (message is LoginReq loginReq)
                        {
                            int loginCount = await _dbManager.LoginUserAsync(loginReq);
                            bool isLogined = loginCount > 0;

                            // [WPF 로그 출력] DB 작업 결과를 GUI 로그에 직접 기록
                            AppendLog($"[DB RESULT] LOGIN: Found {loginCount} user(s).");

                            response = new LoginRes { logined = isLogined };
                            targetClientId = clientId;
                        }
                        break;

                    case MsgType.START_REQ:
                    case MsgType.DOOR_REQ:
                    case MsgType.TRUNK_REQ:
                    case MsgType.AIR_REQ:
                    case MsgType.CLI_REQ:
                    case MsgType.HEAT_REQ:
                    case MsgType.LIGHT_REQ:
                    case MsgType.CONTROL_REQ:
                    case MsgType.STATUS_REQ:
                    case MsgType.STOP_CHARGING_REQ:
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("DOBOTLAB");
                        break;

                    case MsgType.START_RES:
                    case MsgType.DOOR_RES:
                    case MsgType.TRUNK_RES:
                    case MsgType.AIR_RES:
                    case MsgType.CLI_RES:
                    case MsgType.HEAT_RES:
                    case MsgType.LIGHT_RES:
                    case MsgType.CONTROL_RES:
                    case MsgType.STATUS_RES:
                    case MsgType.STOP_CHARGING_RES:
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("RCS");
                        break;
                }

                if (response != null && targetClientId != null)
                {
                    _tcpServer?.SendToClientAsync(targetClientId, response);
                    LogSend(targetClientId, response);
                }

                if (forward != null && targetClientId != null)
                {
                    _tcpServer?.SendToClientAsync(targetClientId, forward);
                    LogSend(targetClientId, forward);
                }
                else if (forward != null && targetClientId == null)
                {
                    // ** 병합 완료 ** (최신 코드: 라우팅 실패 로깅)
                    string targetType = message.msg.ToString().EndsWith("_REQ") ? "DOBOTLAB" : "RCS";
                    AppendLog($"[라우팅 실패] 메시지 {message.msg}를 전달할 대상({targetType})을 찾을 수 없습니다.");
                }
            }
            catch (Exception ex)
            {
                AppendLog($"[메시지 처리 오류] {ex.Message}");
            }
        }

        private void LogSend(string clientId, BaseMessage message)
        {
            string logMessage = FormatLogMessage(clientId, message, " [SEND]");
            AppendLog(logMessage);
        }

        private void TcpServer_OnClientConnected(string clientId)
        {
            // 클라이언트 타입은 아직 식별되지 않았을 수 있으므로, 일단 "RCS"로 표시
            // 나중에 CLIENT_IDENTIFY 메시지를 받으면 타입이 업데이트됨
            AppendLog("{RCS} 접속");
        }

        private void TcpServer_OnClientDisconnected(string clientId, string clientType)
        {
            // 클라이언트 타입에 따라 로그 표시
            if (clientType == "RCS" || clientType == "Unknown")
            {
                AppendLog("{RCS} 로그아웃");
            }
            else
            {
                AppendLog($"{{{clientType}}} 연결 해제");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_tcpServer != null)
            {
                _tcpServer.OnMessageReceived -= TcpServer_OnMessageReceived;
                _tcpServer.OnClientConnected -= TcpServer_OnClientConnected;
                _tcpServer.OnClientDisconnected -= TcpServer_OnClientDisconnected;
                try { _tcpServer.Stop(); } catch { } // ** 병합 완료 ** (간결한 Stop 로직 유지)
            }
            base.OnClosing(e);
        }
    }
}