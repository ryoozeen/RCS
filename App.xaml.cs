using DotBotCarClient.Network;
using DotBotCarClient.Protocol;
using System.Windows;
using System.Windows.Controls;

namespace DotBotCarClient
{
    public partial class App : Application
    {
        public static NetworkClient Network { get; private set; } = new NetworkClient();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                await Network.ConnectAsync("10.10.21.122", 7000);

                // 여기서 한번만 등록
                Network.OnMessageReceived += Network_OnMessageReceived;
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
                        $"Msg = {msg.Msg}");
                }
            });
        }
    }
}
