using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace DotBotCarClient.Views
{
    public partial class ChargingPage : Page
    {
        public ChargingPage()
        {
            InitializeComponent();

            // 테스트용
            UpdateChargingStatus(true, 60);     // 충전중, 60%
        }


        /// <summary>
        /// 충전 상태 업데이트
        /// </summary>
        public void UpdateChargingStatus(bool isCharging, double percent)
        {
            UpdateBattery(percent);

            // 충전 상태 텍스트 표시
            if (isCharging)
            {
                if (percent >= 100)
                    ChargeStatusText.Text = "충전 완료";
                else
                    ChargeStatusText.Text = "충전 중…";
            }
            else
            {
                ChargeStatusText.Text = "";
            }
        }


        /// <summary>
        /// 배터리 UI 업데이트
        /// </summary>
        public void UpdateBattery(double percent)
        {
            BatteryPercentText.Text = $"Battery: {percent}%";

            AnimateBattery(percent);

            double range = 400 * (percent / 100);
            DriveRangeText.Text = $"주행 가능 거리: {range:F0} km";
        }
        private void AnimateBattery(double percent)
        {
            double maxWidth = 260 - 6;
            double target = maxWidth * (percent / 100);

            // 애니메이션: Width 부드럽게 증가
            DoubleAnimation anim = new DoubleAnimation()
            {
                To = target,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase() { EasingMode = EasingMode.EaseOut }
            };

            BatteryFill.BeginAnimation(WidthProperty, anim);
        }


        private void StopCharging_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("충전 중지 (서버 연결 예정)");
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new StatusPage());
        }
    }
}
