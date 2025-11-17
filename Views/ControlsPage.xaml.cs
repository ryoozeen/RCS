using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DotBotCarClient.Views
{
    public partial class ControlsPage : Page
    {
        private int interiorTemp = 23;
        private int targetTemp = 22;

        private bool airOn = false;
        private bool heatOn = false;
        private bool lightOn = false;
        private bool hornOn = false;
        private bool driveOn = false;
        private bool parkOn = false;

        public ControlsPage()
        {
            InitializeComponent();
            ValetToggle.IsChecked = true;
        }

        private Brush OnColor = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        private Brush OffColor = new SolidColorBrush(Color.FromRgb(34, 34, 34));

        // ================== 온도 조절 ==================
        private void DecreaseTemp_Click(object sender, MouseButtonEventArgs e)
        {
            if (targetTemp > 16)
                targetTemp--;

            TargetTempText.Text = $"{targetTemp}°C";
        }

        private void IncreaseTemp_Click(object sender, MouseButtonEventArgs e)
        {
            if (targetTemp < 30)
                targetTemp++;

            TargetTempText.Text = $"{targetTemp}°C";
        }

        // ================== 토글 헬퍼 ==================
        private void ToggleFeature(Border border, ref bool state)
        {
            state = !state;
            border.Background = state ? OnColor : OffColor;
        }

        // ================== 아이콘 클릭 ==================
        private void Air_Click(object sender, MouseButtonEventArgs e)
            => ToggleFeature(AirBtn, ref airOn);

        private void Heat_Click(object sender, MouseButtonEventArgs e)
            => ToggleFeature(HeatBtn, ref heatOn);

        private void Light_Click(object sender, MouseButtonEventArgs e)
            => ToggleFeature(LightBtn, ref lightOn);

        private void Horn_Click(object sender, MouseButtonEventArgs e)
            => ToggleFeature(HornBtn, ref hornOn);

        // ================== 발렛 모드 ==================
        private void ValetToggle_Checked(object sender, RoutedEventArgs e)
        {
            // 발렛모드 ON → Drive & Park 활성화
            DriveBtn.IsEnabled = true;
            ParkBtn.IsEnabled = true;
        }

        private void ValetToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // --------------------------
            // 발렛모드 OFF → Drive, Park만 OFF
            // --------------------------
            driveOn = false;
            parkOn = false;

            DriveBtn.Background = OffColor;
            ParkBtn.Background = OffColor;

            // Drive / Park 비활성화
            DriveBtn.IsEnabled = false;
            ParkBtn.IsEnabled = false;
        }

        // ================== DRIVE / PARK ==================
        private void DriveBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (ValetToggle.IsChecked == true)
            {
                DriveBtn.Background = OnColor;
                ParkBtn.Background = OffColor;
            }
        }

        private void ParkBtn_Click(object sender, MouseButtonEventArgs e)
        {
            if (ValetToggle.IsChecked == true)
            {
                ParkBtn.Background = OnColor;
                DriveBtn.Background = OffColor;
            }
        }

        // ================== BACK ==================
        private void Back_Click(object sender, MouseButtonEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
