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
        private DatabaseManager? _dbManager; // [추가됨] DB 매니저
        private const int Port = 7000;

        public MainWindow()
        {
            InitializeComponent();

            // [추가됨] DB 연결 설정
            string connectionString = "Server=localhost;Port=3306;Database=rcs;User=root;Password=1234;";
            _dbManager = new DatabaseManager(connectionString);

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

        private async void TcpServer_OnMessageReceived(string clientId, BaseMessage message)
        {
            // 에러 메시지 처리 (임시로 STATUS_REQ에 reason으로 에러 메시지를 보낸 경우)
            if (message.msg == MsgType.STATUS_REQ && !string.IsNullOrEmpty(message.reason) && message.reason.StartsWith("[ERROR]"))
            {
                AppendLog($"[ERROR] {clientId}: {message.reason}");
                return;
            }

            // CLIENT_IDENTIFY_REQ는 먼저 처리
            if (message.msg == MsgType.CLIENT_IDENTIFY_REQ)
            {
                await RouteMessage(clientId, message);
                return;
            }

            // LOGIN_REQ는 RouteMessage에서 처리 (로그도 거기서 출력)
            if (message.msg == MsgType.LOGIN_REQ)
            {
                await RouteMessage(clientId, message);
                return;
            }

            // STATUS_REQ와 LOGIN_RES는 로그 출력하지 않음
            if (message.msg == MsgType.STATUS_REQ || message.msg == MsgType.LOGIN_RES)
            {
                await RouteMessage(clientId, message);
                return;
            }

            // 다른 메시지는 FormatLogMessage로 통일 처리
            string logMessage = FormatLogMessage(clientId, message, " [RECV]");
            // 빈 문자열인 경우 로그 출력하지 않음
            if (!string.IsNullOrEmpty(logMessage))
            {
                AppendLog(logMessage);
            }
            await RouteMessage(clientId, message);
        }

        private string FormatLogMessage(string clientId, BaseMessage message, string direction = "")
        {
            // 클라이언트 타입 가져오기 (CLIENT_IDENTIFY_REQ에서 저장된 client_name 사용)
            string clientType = _tcpServer?.GetClientType(clientId) ?? "Unknown";

            string content = "";

            switch (message.msg)
            {
                case MsgType.CLIENT_IDENTIFY_REQ:
                case MsgType.CLIENT_IDENTIFY_RES:
                    // CLIENT_IDENTIFY 메시지는 로그 출력하지 않음 (이미 RouteMessage에서 처리)
                    return "";
                case MsgType.START_REQ:
                    if (message is StartReq startReq)
                    {
                        // 형식: {메시지("시동")}
                        content = startReq.reason ?? "시동";
                    }
                    break;
                case MsgType.START_RES:
                    if (message is StartRes startRes)
                    {
                        if (!string.IsNullOrEmpty(message.reason))
                        {
                            content = message.reason;
                        }
                        else
                        {
                            // active_status에 따라 성공/실패 표시
                            string status = startRes.active_status ? "성공" : "실패";
                            content = $"시동 제어 응답 : {status}";
                        }
                    }
                    break;

                case MsgType.DOOR_REQ:
                    if (message is DoorReq req3)
                    {
                        string doorState = req3.door ? "열기" : "닫기";
                        content = $"문 제어 요청: {doorState} (door = {req3.door})";
                    }
                    break;

                case MsgType.DOOR_RES:
                    if (message is DoorRes res3) content = $"문 제어 응답: {res3.door_status}";
                    break;

                case MsgType.CONTROL_REQ:
                    if (message is ControlReq controlReq)
                    {
                        // control 값에 따라 주차/출차 요청 구분
                        content = controlReq.control ? "주차 요청" : "출차 요청";
                    }
                    break;

                case MsgType.CONTROL_RES:
                    if (message is ControlRes controlRes)
                    {
                        if (!string.IsNullOrEmpty(message.reason))
                        {
                            // reason 필드가 있으면 그 내용 표시 (예: "주차중...", "출차중...")
                            content = message.reason;
                        }
                        else
                        {
                            // reason이 없으면 control_status에 따라 표시
                            // 주차/출차 구분은 이전 메시지로 판단하거나 기본값으로 주차로 설정
                            content = controlRes.control_status ? "주차 완료" : "주차 실패";
                        }
                    }
                    break;
                case MsgType.STATUS_REQ:
                    // STATUS_REQ는 대기상태이므로 로그 출력하지 않음
                    return "";

                case MsgType.STATUS_RES:
                    if (message is StatusRes res4) content = $"상태 응답: Charging={res4.charging}, Battery={res4.battery}";
                    break;

                case MsgType.ENROLL_REQ:
                    if (message is EnrollReq enrollReq)
                    {
                        content = $"회원가입 요청: id={enrollReq.id}, username={enrollReq.username}";
                    }
                    break;

                case MsgType.ENROLL_RES:
                    if (message is EnrollRes enrollRes)
                    {
                        content = $"회원가입 응답: registered={enrollRes.registered}";
                    }
                    break;

                case MsgType.TRUNK_REQ:
                    if (message is TrunkReq trunkReq)
                    {
                        string trunkState = trunkReq.trunk ? "열기" : "닫기";
                        content = $"트렁크 제어 요청: {trunkState} (trunk = {trunkReq.trunk})";
                    }
                    break;

                case MsgType.TRUNK_RES:
                    if (message is TrunkRes trunkRes)
                    {
                        string trunkState = trunkRes.trunk_status ? "열림" : "닫힘";
                        content = $"트렁크 제어 응답: {trunkState}";
                    }
                    break;

                case MsgType.AIR_REQ:
                    if (message is AirReq airReq)
                    {
                        string airState = airReq.air ? "켜기" : "끄기";
                        content = $"에어컨 제어 요청: {airState} (air = {airReq.air})";
                    }
                    break;

                case MsgType.AIR_RES:
                    if (message is AirRes airRes)
                    {
                        content = $"에어컨 제어 응답: air_status={airRes.air_status}";
                    }
                    break;

                case MsgType.CLI_REQ:
                    if (message is CliReq cliReq)
                    {
                        content = $"온도 제어 요청: temp={cliReq.temp}";
                    }
                    break;

                case MsgType.CLI_RES:
                    if (message is CliRes cliRes)
                    {
                        content = $"온도 제어 응답: temp_status={cliRes.temp_status}";
                    }
                    break;

                case MsgType.HEAT_REQ:
                    if (message is HeatReq heatReq)
                    {
                        string heatState = heatReq.heat ? "켜기" : "끄기";
                        content = $"열선 제어 요청: {heatState} (heat = {heatReq.heat})";
                    }
                    break;

                case MsgType.HEAT_RES:
                    if (message is HeatRes heatRes)
                    {
                        content = $"열선 제어 응답: heat_status={heatRes.heat_status}";
                    }
                    break;

                case MsgType.LIGHT_REQ:
                    if (message is LightReq lightReq)
                    {
                        string lightState = lightReq.light ? "켜기" : "끄기";
                        content = $"라이트 제어 요청: {lightState} (light = {lightReq.light})";
                    }
                    break;

                case MsgType.LIGHT_RES:
                    if (message is LightRes lightRes)
                    {
                        content = $"라이트 제어 응답: light_status={lightRes.light_status}";
                    }
                    break;

                case MsgType.STOP_CHARGING_REQ:
                    if (message is StopChargingReq stopChargingReq)
                    {
                        content = $"충전 중지 요청: stop={stopChargingReq.stop}";
                    }
                    break;

                case MsgType.STOP_CHARGING_RES:
                    if (message is StopChargingRes stopChargingRes)
                    {
                        content = $"충전 중지 응답: stop_status={stopChargingRes.stop_status}";
                    }
                    break;

                case MsgType.LOGIN_RES:
                    // LOGIN_RES는 로그 출력하지 않음
                    return "";
                default: content = $"[알 수 없는 메시지] Msg: {message.msg}"; break;
            }

            // 빈 content인 경우 로그 출력하지 않음
            if (string.IsNullOrEmpty(content))
            {
                return "";
            }

            // 형식: [{클라이언트 타입명}] {direction} {메시지}
            // direction이 있으면 포함, 없으면 생략
            if (!string.IsNullOrEmpty(direction))
            {
                return $"[{clientType}]{direction} {content}";
            }
            else
            {
                return $"[{clientType}] {content}";
            }
        }

        private async Task RouteMessage(string clientId, BaseMessage message)
        {
            BaseMessage? response = null;
            BaseMessage? forward = null;
            string? targetClientId = null;

            try
            {
                switch (message.msg)
                {
                    case MsgType.CLIENT_IDENTIFY_REQ:
                        // 클라이언트 식별 요청 처리
                        if (message is ClientIdentifyReq identifyReq)
                        {
                            string clientName = identifyReq.client_name ?? "Unknown";
                            // 클라이언트 타입 설정 (RCS 또는 DOBOT)
                            _tcpServer?.IdentifyClient(clientId, clientName);

                            // 로그 출력
                            AppendLog($"[{clientName}] 서버 연결 성공");
                            AppendLog($"[{clientName}] 접속");

                            // 응답 전송
                            response = new ClientIdentifyRes
                            {
                                identified = true
                            };
                            targetClientId = clientId;
                        }
                        break;

                    case MsgType.ENROLL_REQ:
                        // [수정됨] 회원가입 요청 처리 (DB 연결)
                        if (message is EnrollReq enrollReq && _dbManager != null)
                        {
                            // DB에 회원가입 요청
                            int result = await _dbManager.EnrollUserAsync(enrollReq);
                            bool isRegistered = result > 0;

                            // 로그 출력
                            AppendLog($"[DB] 회원가입 요청 결과: {(isRegistered ? "성공" : "실패")} (ID: {enrollReq.id})");

                            // 응답 전송
                            response = new EnrollRes { registered = isRegistered };
                            targetClientId = clientId;
                        }
                        break;

                    case MsgType.LOGIN_REQ:
                        // [수정됨] 로그인 요청 처리 (DB 연결)
                        if (message is LoginReq loginReq && _dbManager != null)
                        {
                            // DB에서 로그인 확인
                            int count = await _dbManager.LoginUserAsync(loginReq);
                            bool loginSuccess = count > 0;

                            string clientType = _tcpServer?.GetClientType(clientId) ?? "Unknown";

                            // 로그인 성공/실패 로그 출력 및 식별
                            if (loginSuccess)
                            {
                                _tcpServer?.IdentifyClient(clientId, "RCS"); // 로그인 성공 시 RCS로 식별
                                AppendLog($"[RCS] 로그인 성공: {loginReq.id}");
                            }
                            else
                            {
                                AppendLog($"[{clientType}] 로그인 실패: {loginReq.id}");
                            }

                            // 로그인 응답 전송
                            response = new LoginRes
                            {
                                logined = loginSuccess
                            };
                            targetClientId = clientId;
                        }
                        break;

                    case MsgType.STATUS_REQ:
                        // 일반 STATUS_REQ는 DOBOT으로 전달
                        // DOBOT이 연결되어 있으면 전달, 없으면 무시 (대기상태)
                        targetClientId = _tcpServer?.GetClientByType("DOBOT");
                        if (targetClientId == null)
                        {
                            // DOBOTLAB도 확인 (하위 호환성)
                            targetClientId = _tcpServer?.GetClientByType("DOBOTLAB");
                        }
                        if (targetClientId != null)
                        {
                            forward = message;
                        }
                        // DOBOT이 없으면 라우팅 실패 메시지 출력하지 않음 (대기상태)
                        break;

                    case MsgType.START_REQ:
                        // START_REQ는 DOBOT으로 전달
                        // DOBOT이 START_RES를 보내면 서버가 클라이언트로 전달
                        targetClientId = _tcpServer?.GetClientByType("DOBOT");
                        if (targetClientId == null)
                        {
                            // DOBOTLAB도 확인 (하위 호환성)
                            targetClientId = _tcpServer?.GetClientByType("DOBOTLAB");
                        }
                        if (targetClientId != null)
                        {
                            forward = message;
                        }
                        // DOBOT이 없으면 라우팅 실패 메시지 출력하지 않음
                        break;

                    case MsgType.DOOR_REQ:
                    case MsgType.TRUNK_REQ:
                    case MsgType.AIR_REQ:
                    case MsgType.CLI_REQ:
                    case MsgType.HEAT_REQ:
                    case MsgType.LIGHT_REQ:
                    case MsgType.CONTROL_REQ:
                    case MsgType.STOP_CHARGING_REQ:
                        // DOBOT이 연결되어 있으면 전달, 없으면 무시
                        targetClientId = _tcpServer?.GetClientByType("DOBOT");
                        if (targetClientId == null)
                        {
                            // DOBOTLAB도 확인 (하위 호환성)
                            targetClientId = _tcpServer?.GetClientByType("DOBOTLAB");
                        }
                        if (targetClientId != null)
                        {
                            forward = message;
                        }
                        // DOBOT이 없으면 라우팅 실패 메시지 출력하지 않음
                        break;

                    // 제어 응답 (RCS로 전달)
                    case MsgType.START_RES:
                        // START_RES는 DOBOTLAB에서 온 것이므로 클라이언트로 전달
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("RCS");
                        break;

                    case MsgType.DOOR_RES:
                    case MsgType.TRUNK_RES:
                    case MsgType.AIR_RES:
                    case MsgType.CLI_RES:
                    case MsgType.HEAT_RES:
                    case MsgType.LIGHT_RES:
                    case MsgType.CONTROL_RES:
                        // CONTROL_RES 처리: reason에 따라 parking 또는 driving 설정
                        if (message is ControlRes controlResMsg)
                        {
                            if (!string.IsNullOrEmpty(controlResMsg.reason))
                            {
                                if (controlResMsg.reason.Contains("주차"))
                                {
                                    controlResMsg.parking = true;
                                }
                                else if (controlResMsg.reason.Contains("출차"))
                                {
                                    controlResMsg.driving = true;
                                }
                            }
                        }
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("RCS");
                        break;
                    case MsgType.STATUS_RES:
                    case MsgType.STOP_CHARGING_RES:
                        forward = message;
                        targetClientId = _tcpServer?.GetClientByType("RCS");
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
                // STATUS_REQ와 DOBOTLAB으로 전달하는 REQ 메시지는 대상이 없어도 라우팅 실패 메시지 출력하지 않음
                else if (forward != null && targetClientId == null)
                {
                    // DOBOTLAB으로 전달하는 REQ 메시지들은 대상이 없어도 조용히 무시
                    bool isDobotlabReq = message.msg == MsgType.STATUS_REQ ||
                                         message.msg == MsgType.START_REQ ||
                                         message.msg == MsgType.DOOR_REQ ||
                                         message.msg == MsgType.TRUNK_REQ ||
                                         message.msg == MsgType.AIR_REQ ||
                                         message.msg == MsgType.CLI_REQ ||
                                         message.msg == MsgType.HEAT_REQ ||
                                         message.msg == MsgType.LIGHT_REQ ||
                                         message.msg == MsgType.CONTROL_REQ ||
                                         message.msg == MsgType.STOP_CHARGING_REQ;

                    if (!isDobotlabReq)
                    {
                        string targetType = message.msg.ToString().EndsWith("_REQ") ? "DOBOTLAB" : "RCS";
                        AppendLog($"[라우팅 실패] 메시지 {message.msg}를 전달할 대상({targetType})을 찾을 수 없습니다.");
                    }
                }
            }
            catch (Exception ex)
            {
                // DB 연결 오류 등이 여기서 잡힙니다.
                AppendLog($"[메시지 처리 오류] {ex.Message}");
            }
        }

        private void LogSend(string clientId, BaseMessage message)
        {
            string logMessage = FormatLogMessage(clientId, message, " [SEND]");
            // 빈 문자열인 경우 로그 출력하지 않음
            if (!string.IsNullOrEmpty(logMessage))
            {
                AppendLog(logMessage);
            }
        }

        private void TcpServer_OnClientConnected(string clientId)
        {
            // 연결 시점에는 타입을 모르므로 기본값으로 "Unknown" 설정
            // CLIENT_IDENTIFY_REQ 메시지 수신 시 타입 식별 및 로그 출력
            _tcpServer?.IdentifyClient(clientId, "Unknown");
        }

        private void TcpServer_OnClientDisconnected(string clientId, string clientType)
        {
            // 클라이언트 타입에 따라 로그 표시
            AppendLog($"[{clientType}] 로그아웃");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_tcpServer != null)
            {
                _tcpServer.OnMessageReceived -= TcpServer_OnMessageReceived;
                _tcpServer.OnClientConnected -= TcpServer_OnClientConnected;
                _tcpServer.OnClientDisconnected -= TcpServer_OnClientDisconnected;
                try { _tcpServer.Stop(); } catch { }
            }
            base.OnClosing(e);
        }
    }
}