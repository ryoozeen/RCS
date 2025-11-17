using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Threading.Tasks;

namespace DotBotCarClient.Views
{
    public partial class ControlsPage : Page
    {
        private int targetTemp = 22;

        private bool airOn = false;
        private bool heatOn = false;
        private bool lightOn = false;

        private readonly Brush OnColor = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // 초록색
        private readonly Brush OffColor = new SolidColorBrush(Color.FromRgb(34, 34, 34)); // #222222

        public ControlsPage()
        {
            InitializeComponent();
            UpdateTempUI();

            DriveBtn.IsEnabled = false;
            ParkBtn.IsEnabled = false;
        }

        private void UpdateTempUI()
        {
            TargetTempText.Text = $"{targetTemp}°C";
        }

        private void IncreaseTemp_Click(object sender, RoutedEventArgs e)
        {
            if (targetTemp < 30)
                targetTemp++;

            UpdateTempUI();
        }

        private void DecreaseTemp_Click(object sender, RoutedEventArgs e)
        {
            if (targetTemp > 16)
                targetTemp--;

            UpdateTempUI();
        }


        // ==========================
        //   ❄️ Air / Heat / Light
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


        // ==========================
        //   🔊 Horn (1~2초 후 복귀)
        // ==========================

        private async void Horn_Click(object sender, RoutedEventArgs e)
        {
            HornBtn.Background = OnColor;  // 초록색 활성 표시
            await Task.Delay(1500);        // 1.5초 유지
            HornBtn.Background = OffColor; // 다시 원상태
        }


        // ==========================
        //   Valet Mode
        // ==========================

        private void ValetToggle_Checked(object sender, RoutedEventArgs e)
        {
            DriveBtn.IsEnabled = true;
            ParkBtn.IsEnabled = true;
        }

        private void ValetToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            DriveBtn.IsEnabled = false;
            ParkBtn.IsEnabled = false;

            DriveBtn.Background = OffColor;
            ParkBtn.Background = OffColor;
        }


        // ==========================
        //   Drive (5초 후 원상 복귀)
        // ==========================

        private async void DriveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ValetToggle.IsChecked != true)
                return;

            DriveBtn.Background = OnColor;    // 초록색
            await Task.Delay(5000);           // 5초 유지
            DriveBtn.Background = OffColor;   // 원상 복귀
        }


        // ==========================
        //   Park (5초 후 원상 복귀)
        // ==========================

        private async void ParkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ValetToggle.IsChecked != true)
                return;

            ParkBtn.Background = OnColor;     // 초록색
            await Task.Delay(5000);           // 5초 유지
            ParkBtn.Background = OffColor;    // 원상 복귀
        }


        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
