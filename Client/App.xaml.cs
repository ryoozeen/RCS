using DotBotCarClient.Network;
using DotBotCarClient.Protocol;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DotBotCarClient
{
    public partial class App : Application
    {
        public static NetworkClient Network { get; private set; } = new NetworkClient();
        public static bool IsParked = false;
        public static DateTime ParkingStartTime;
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                //await Network.ConnectAsync("10.10.21.122", 7000); // 명진
                //await Network.ConnectAsync("10.10.21.107", 7000); // 연주
                //await Network.ConnectAsync("10.10.21.111", 7000); // 진리
                await Network.ConnectAsync("192.168.0.196", 7000); // 유진

                // 여기서 한번만 등록
                Network.OnMessageReceived += Network_OnMessageReceived;

                // 클라이언트 식별자 전송
                var req = new ClientIdentifyReq
                {
                    client_name = "RCS"
                };

                await Network.SendAsync(req);
            }
            catch
            {
                MessageBox.Show("서버 연결 실패", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        // 서버에서 메시지 올 때마다 실행되는 전역 라우터
        private void Network_OnMessageReceived(BaseMessage msg)
        {
            Dispatcher.Invoke(() =>
            {
                var mainWindow = Current.MainWindow as MainWindow;
                if (mainWindow?.ContentFrame?.Content is IProtocolHandler handler)
                {
                    handler.HandleProtocolMessage(msg);
                }
                else
                {
                    MessageBox.Show(
                        $"[DEBUG] 현재 View는 IProtocolHandler 아님\n" +
                        $"타입 = {mainWindow?.ContentFrame?.Content?.GetType().Name}\n" +
                        $"Msg = {msg.msg}");
                }
            });
        }
    }
}
