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
using System.ComponentModel;  // CancelEventArgs 사용을 위해

namespace SERVER
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpServer? _tcpServer;
        private const int Port = 8888; //포트 번호

        public MainWindow()
        {
            InitializeComponent();

            // TcpServer 인스턴스 생성
            _tcpServer = new TcpServer(Port);

            // ⭐ 추가: 메시지 수신 이벤트 구독
            _tcpServer.OnMessageReceived += TcpServer_OnMessageReceived;

        }

        // 서버 연결 버튼
        private async void ServerConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpServer == null) return;

            try
            {
                // 서버 시작
                await _tcpServer.StartAsync();

                // UI 업데이트 (서버 시작 성공 시)
                UpdateServerStatus(true);

                // 로그 출력 (선택사항)
                AppendLog("서버가 시작되었습니다.");
            }
            catch (Exception ex)
            {
                AppendLog($"서버 시작 실패: {ex.Message}");
                // 에러 시 UI는 변경하지 않음
            }
        }

        // 상태바 업데이트
        private void UpdateServerStatus(bool isRunning)
        {
            if (isRunning)
            {
                // 서버 실행 중: 초록색 + "Server Start"
                ServerStatusBar.Background = new SolidColorBrush(Colors.Green);
                ServerStatusBar.BorderBrush = new SolidColorBrush(Colors.Green);
                ServerStatusText.Text = "Server Start";
            }
            
            else
            {
                // 서버 중지: 빨간색 + "Server Stop"
                ServerStatusBar.Background = new SolidColorBrush(Colors.Red);
                ServerStatusBar.BorderBrush = new SolidColorBrush(Colors.Red);
                ServerStatusText.Text = "Server Stop";
            }
        }

        // 로그 출력 메서드 추가
        private void AppendLog(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            LogTextBox.AppendText($"[{timestamp}] {message}\r\n");
            LogTextBox.ScrollToEnd(); // 자동 스크롤
        }

        // 서버 종료 버튼
        private void ServerShutdown_Click(object sender, RoutedEventArgs e)
        {
            if (_tcpServer == null) return;

            try
            {
                // 서버 중지
                _tcpServer.Stop();

                // UI 업데이트 (서버 중지 시)
                UpdateServerStatus(false);

                // 로그 출력
                AppendLog("서버가 중지되었습니다.");
            }
            catch (Exception ex)
            {
                AppendLog($"서버 중지 실패: {ex.Message}");
            }
        }

        // 메시지 수신 이벤트 핸들러
        private void TcpServer_OnMessageReceived(string clientId, ControlMessage message)
        {
            // UI 스레드에서 실행 (Dispatcher.Invoke 필요)
            Dispatcher.Invoke(() =>
            {
                // 로그 형식: [{timestamp}] {클라이언트 이름} {메시지 내용}
                string logMessage = FormatLogMessage(clientId, message);
                AppendLog(logMessage);
            });
        }

        // 메시지를 로그 형식으로 변환
        private string FormatLogMessage(string clientId, ControlMessage message)
        {
            // 클라이언트 ID에서 IP만 추출
            string clientName = clientId.Split(':')[0];  // "192.168.1.100:12345" → "192.168.1.100"

            // 메시지 타입에 따라 다른 로그 형식
            string content = "";

            if (message.Msg == "CONNECTION_SUCCESS")
            {
                content = "서버 연결 성공";
            }
            else if (message.Msg == "CONNECTION_FAILED")
            {
                content = $"서버 연결 실패: {message.Reason ?? ""}";
            }
            else if (message.Msg == "STATUS_UPDATE")
            {
                // 상태 업데이트 메시지
                content = $"상태 업데이트 - " +
                          $"문: {message.DoorStatus ?? "N/A"}, " +
                          $"시동: {message.StartStatus ?? "N/A"}, " +
                          $"에어컨: {message.AirStatus ?? "N/A"}, " +
                          $"난방: {message.HeatStatus ?? "N/A"}, " +
                          $"충전: {message.ChargingStatus ?? "N/A"}, " +
                          $"배터리: {message.BatteryLevel}%";
            }
            else if (!string.IsNullOrEmpty(message.Msg))
            {
                // 기타 메시지
                content = message.Msg;
                if (!string.IsNullOrEmpty(message.Reason))
                {
                    content += $" - {message.Reason}";
                }
            }
            else
            {
                // 메시지 타입이 없으면 기본 형식
                content = "알 수 없는 메시지";
            }

            return $"{clientName} {content}";
        }

        // 이벤트 구독 해제
        protected override void OnClosing(CancelEventArgs e)
        {
            if (_tcpServer != null)
            {
                // 메세지 구독 해제 (메모리 누수 방지)
                _tcpServer.OnMessageReceived -= TcpServer_OnMessageReceived;

                try
                {
                    // 서버 중지
                    _tcpServer.Stop();
                }
                catch (Exception)
                {
                    // 에러 처리
                }
            }

            // 기본 종료 처리 호출
            base.OnClosing(e);
        }
    }
    
}