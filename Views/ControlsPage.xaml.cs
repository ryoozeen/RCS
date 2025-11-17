using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DotBotCarClient.Views
{
    public partial class ControlsPage : Page
    {
        private int targetTemp = 22;

        private bool airOn = false;
        private bool heatOn = false;
        private bool lightOn = false;
        private bool hornOn = false;

        private bool driveOn = false;
        private bool parkOn = false;

        private readonly Brush OnColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 초록색
        private readonly Brush OffColor = new SolidColorBrush(Color.FromRgb(34, 34, 34)); // #222222

        public ControlsPage()
        {
            InitializeComponent();
            UpdateTempUI();

            // ✅ 수정: ControlsPage 처음 진입 시 Drive/Park 버튼 비활성화
            DriveBtn.IsEnabled = false;
            ParkBtn.IsEnabled = false;
        }

        // ==========================
        //    🔥 온도 UI 업데이트
        // ==========================
        private void UpdateTempUI()
        {
            TargetTempText.Text = $"{targetTemp}°C";
        }

        private void IncreaseTemp_Click(object sender, RoutedEventArgs e)
        {
            if (targetTemp < 30)
            {
                targetTemp++;
                UpdateTempUI();
            }
        }

        private void DecreaseTemp_Click(object sender, RoutedEventArgs e)
        {
            if (targetTemp > 16)
            {
                targetTemp--;
                UpdateTempUI();
            }
        }


        // ==========================
        //    🔥 아이콘 버튼들
        // ==========================
        private void Air_Click(object sender, RoutedEventArgs e)
        {
            airOn = !airOn;
            AirBtn.Background = airOn ? OnColor : OffColor;
        }

        private void Heat_Click(object sender, RoutedEventArgs e)
        {
            heatOn = !heatOn;
            HeatBtn.Background = heatOn ? OnColor : OffColor;
        }

        private void Light_Click(object sender, RoutedEventArgs e)
        {
            lightOn = !lightOn;
            LightBtn.Background = lightOn ? OnColor : OffColor;
        }

        private void Horn_Click(object sender, RoutedEventArgs e)
        {
            hornOn = !hornOn;
            HornBtn.Background = hornOn ? OnColor : OffColor;
        }


        // ==========================
        //    🔥 발렛 모드
        // ==========================
        private void ValetToggle_Checked(object sender, RoutedEventArgs e)
        {
            DriveBtn.IsEnabled = true;
            ParkBtn.IsEnabled = true;
        }

        private void ValetToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            driveOn = false;
            parkOn = false;

            DriveBtn.Background = OffColor;
            ParkBtn.Background = OffColor;

            DriveBtn.IsEnabled = false;
            ParkBtn.IsEnabled = false;
        }


        // ==========================
        //    🔥 Drive / Park 버튼
        // ==========================
        private void DriveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ValetToggle.IsChecked != true)
                return;

            driveOn = !driveOn;
            DriveBtn.Background = driveOn ? OnColor : OffColor;
        }

        private void ParkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ValetToggle.IsChecked != true)
                return;

            parkOn = !parkOn;
            ParkBtn.Background = parkOn ? OnColor : OffColor;
        }


        // ==========================
        //    🔙 BACK 버튼
        // ==========================
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}