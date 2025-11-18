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
        // 주기적으로 CHARGING_REQ를 보내기 위한 타이머
        private readonly DispatcherTimer _statusTimer;

        public ChargingPage()
        {
            InitializeComponent();

            // 초기 UI: 충전 안 함, 0%
            UpdateChargingStatus(false, 0);
        }

        // ======================================
        //   2) 서버에서 온 메시지 처리 (IProtocolHandler)
        // ======================================
        public void HandleProtocolMessage(BaseMessage msg)
        {
            // 배터리 상태 응답만 처리
            if (msg is StatusReq status)
            {
                // App.xaml.cs에서 이미 Dispatcher.Invoke 한 상태로 들어오기 때문에
                // 여기서는 바로 UI를 건드려도 됨.
                UpdateChargingStatus(status.Charging, status.Battery);
            }
        }

        // ======================================
        //   3) 배터리/충전 상태 UI 업데이트
        // ======================================

        /// <summary>
        /// 충전 상태 + 퍼센트에 따라 UI 업데이트
        /// </summary>
        public void UpdateChargingStatus(bool isCharging, double percent)
        {
            UpdateBattery(percent);

            if (isCharging)
            {
                if (percent >= 100)
                    ChargeStatusText.Text = "충전 완료";
                else
                    ChargeStatusText.Text = "충전 중…";
            }
            else
            {
                ChargeStatusText.Text = string.Empty;
            }
        }

        /// <summary>
        /// 배터리 수치 / 주행 가능 거리 표시
        /// </summary>
        public void UpdateBattery(double percent)
        {
            BatteryPercentText.Text = $"Battery: {percent:F0}%";

            AnimateBattery(percent);

            double range = 400 * (percent / 100.0);
            DriveRangeText.Text = $"주행 가능 거리: {range:F0} km";
        }

        /// <summary>
        /// 배터리 바 애니메이션 (가로 폭)
        /// </summary>
        private void AnimateBattery(double percent)
        {
            // XAML에서 배터리 내부 너비가 260 정도라고 가정 (테두리 제외)
            double maxWidth = 260 - 6;
            double targetWidth = maxWidth * (percent / 100.0);

            var anim = new DoubleAnimation
            {
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase
                {
                    EasingMode = EasingMode.EaseOut
                }
            };

            BatteryFill.BeginAnimation(WidthProperty, anim);
        }

        // ======================================
        //   4) 버튼들
        // ======================================

        // "충전 중지" 버튼
        private async void StopCharging_Click(object sender, RoutedEventArgs e)
        {
            // 원하면 서버에 "충전 중지" 같은 제어 명령도 함께 보낼 수 있음
            if (App.Network.IsConnected)
            {
                var req = new StopChargingReq
                {
                    Stop = true
                };
                await App.Network.SendAsync(req);
            }

            MessageBox.Show("충전 중지 요청을 보냈습니다.");
        }

        // 뒤로가기 버튼
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}