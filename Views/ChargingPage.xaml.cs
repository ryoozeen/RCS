using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using DotBotCarClient.Protocol;

namespace DotBotCarClient.Views
{
    public partial class ChargingPage : Page, IProtocolHandler
    {
        private readonly DispatcherTimer _statusTimer;

        public ChargingPage()
        {
            InitializeComponent();

            UpdateChargingStatus(false, 0);

            // 🔁 2초마다 상태 요청 보내도록 타이머 활성화
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }

        // 🔹 타이머가 실행될 때마다 서버에 STATUS_REQ 보내기
        private async void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (!App.Network.IsConnected)
                return;

            var req = new StatusReq(); // 🔥 상태 요청 메시지
            await App.Network.SendAsync(req);
        }

        // 🔹 서버 응답 (StatusRes) 받으면 UI 업데이트
        public void HandleProtocolMessage(BaseMessage msg)
        {
            if (msg is StatusRes status)
            {
                UpdateChargingStatus(status.Charging, status.Battery);
            }
        }

        public void UpdateChargingStatus(bool isCharging, double percent)
        {
            UpdateBattery(percent);

            if (isCharging)
            {
                ChargeStatusText.Text = percent >= 100 ? "충전 완료" : "충전 중…";
            }
            else
            {
                ChargeStatusText.Text = string.Empty;
            }
        }

        public void UpdateBattery(double percent)
        {
            BatteryPercentText.Text = $"Battery: {percent:F0}%";
            AnimateBattery(percent);

            double range = 400 * (percent / 100.0);
            DriveRangeText.Text = $"주행 가능 거리: {range:F0} km";
        }

        private void AnimateBattery(double percent)
        {
            double maxWidth = 260 - 6;
            double targetWidth = maxWidth * (percent / 100.0);

            var anim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            BatteryFill.BeginAnimation(WidthProperty, anim);
        }

        private async void StopCharging_Click(object sender, RoutedEventArgs e)
        {
            _statusTimer.Stop();

            if (App.Network.IsConnected)
            {
                var req = new StopChargingReq { Stop = true };
                await App.Network.SendAsync(req);
            }

            MessageBox.Show("충전 중지 요청을 보냈습니다.");
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _statusTimer.Stop();
            NavigationService?.GoBack();
        }
    }

}