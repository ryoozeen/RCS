using System;
using System.DirectoryServices.ActiveDirectory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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

            UpdateBattery(0); // 초기화

            SendInitialBatteryStatusRequest(); // 페이지 변경 시 상태 요청

            // 🔁 2초마다 상태 요청 보내도록 타이머 활성화
            _statusTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _statusTimer.Tick += StatusTimer_Tick;
            _statusTimer.Start();
        }
        // 초기 요청
        private async void SendInitialBatteryStatusRequest()
        {
            if (!App.Network.IsConnected)
                return;

            var req = new BatteryReq();
            await App.Network.SendAsync(req);
        }
        // 🔹 타이머가 실행될 때마다 서버에 STATUS_REQ 보내기
        private async void StatusTimer_Tick(object? sender, EventArgs e)
        {
            if (!App.Network.IsConnected)
                return;

            var req = new BatteryReq(); // 🔥 상태 요청 메시지
            await App.Network.SendAsync(req);
        }

        // 🔹 서버 응답 (StatusRes) 받으면 UI 업데이트
        public void HandleProtocolMessage(BaseMessage msg)
        {
            if (msg is BatteryRes status)
            {
                UpdateBattery(status.battery_status);
            }
        }

        public void UpdateBattery(double percent)
        {
            BatteryPercentText.Text = $"Battery: {(int)(percent * 100)}%";

            int totalPercent = (int)(percent * 100);
            int hour = totalPercent / 60;
            int minute = totalPercent % 60;
            EstTimeText.Text = $"{hour}hr {minute}min remaining";

            AnimateBattery(percent * 100);

            double range = 400 * percent;
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

            StopChargingBtn.Background = new SolidColorBrush(Color.FromRgb(200, 50, 50));

            if (App.Network.IsConnected)
            {
                var req = new StopChargingReq { stop = true };
                await App.Network.SendAsync(req);
            }

            await Task.Delay(5000);
            StopChargingBtn.Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _statusTimer.Stop();
            NavigationService?.GoBack();
        }
    }

}