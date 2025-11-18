using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using DotBotCarClient.Protocol;
using DotBotCarClient.Network;

namespace DotBotCarClient.Views
{
    public partial class StatusPage : Page, IProtocolHandler
    {
        private DispatcherTimer _statusTimer;

        public StatusPage()
        {
            InitializeComponent();

            // 첫 로드시 상태 요청
            RequestStatus();

            // 타이머로 5초마다 요청
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(5);
            _statusTimer.Tick += (s, e) => RequestStatus();
            _statusTimer.Start();

            // 페이지 벗어날 때 정지되도록
            this.Unloaded += StatusPage_Unloaded;
        }

        // 페이지 Unloaded 시 타이머 정지
        private void StatusPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_statusTimer != null)
                _statusTimer.Stop();
        }

        // ======================================================
        // 서버 응답 처리
        // ======================================================
        public void HandleProtocolMessage(BaseMessage msg)
        {
            if (msg.msg != MsgType.STATUS_RES)
                return;

            var st = msg as StatusRes;
            if (st == null) return;

            Dispatcher.Invoke(() =>
            {
                BatteryText.Text = $"{st.battery}%";

                string state = "대기 중";
                if (st.charging) state = "충전 중";
                else if (st.driving) state = "주행 중";
                else if (st.parking) state = "주차 중";

                CarStateText.Text = $"차량 상태 : {state}";
            });
        }

        // ======================================================
        // 상태 요청
        // ======================================================
        private async void RequestStatus()
        {
            var req = new StatusReq
            {
                carstatus = true
            };

            await App.Network.SendAsync(req);
        }

        // ==========================================================
        // 버튼 → 서버 전송
        // ==========================================================
        private async void start_Click(object sender, RoutedEventArgs e)
        {
            var req = new StartReq { active = true };
            await App.Network.SendAsync(req);
        }

        private async void Lock_Click(object sender, RoutedEventArgs e)
        {
            var req = new DoorReq { open = true };
            await App.Network.SendAsync(req);
        }

        private async void Trunk_Click(object sender, RoutedEventArgs e)
        {
            var req = new TrunkReq { open = true };
            await App.Network.SendAsync(req);
        }

        // 하단 네비게이션
        private void GoCharging(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ChargingPage());
        }

        private void GoControls(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ControlsPage());
        }



    }
}
