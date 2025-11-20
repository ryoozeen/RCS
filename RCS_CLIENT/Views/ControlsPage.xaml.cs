using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DotBotCarClient.Protocol;

namespace DotBotCarClient.Views
{
    public partial class ControlsPage : Page, IProtocolHandler
    {
        private int targetTemp = 22;

        // 토글 상태
        private bool _airOn = false;
        private bool _heatOn = false;
        private bool _lightOn = false;

        private readonly Brush OnColor = new SolidColorBrush(Color.FromRgb(76, 175, 80));
        private readonly Brush OffColor = new SolidColorBrush(Color.FromRgb(34, 34, 34));

        public ControlsPage()
        {
            InitializeComponent();
            UpdateTempUI();

            DriveBtn.IsEnabled = false;
            ParkBtn.IsEnabled = false;
        }

        // ====================================================
        // UI : 온도 변경
        // ====================================================
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

        // ====================================================
        // 서버 메시지 처리 (IProtocolHandler)
        // ====================================================
        public async void HandleProtocolMessage(BaseMessage msg)
        {
            switch (msg.msg)
            {
                case MsgType.AIR_RES:
                    var air = msg as AirRes;
                    if (air != null)
                    {
                        _airOn = air.air_status;
                        AirBtn.Background = _airOn ? OnColor : OffColor;
                    }
                    break;

                case MsgType.HEAT_RES:
                    var heat = msg as HeatRes;
                    if (heat != null)
                    {
                        _heatOn = heat.heat_status;
                        HeatBtn.Background = _heatOn ? OnColor : OffColor;
                    }
                    break;

                case MsgType.LIGHT_RES:
                    var light = msg as LightRes;
                    if (light != null)
                    {
                        _lightOn = light.light_status;
                        LightBtn.Background = _lightOn ? OnColor : OffColor;
                    }
                    break;

                case MsgType.CONTROL_RES:
                    var ctrl = msg as ControlRes;
                    if (ctrl != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (Application.Current.MainWindow is MainWindow mw &&
                            mw.ContentFrame.Content is StatusPage sp)
                            {
                                sp.ForceUpdateParkingTime();
                            }
                        });
                    }
                    break;
            }
        }

        // ====================================================
        // Air / Heat / Light 버튼 → 서버로 요청
        // ====================================================
        private async void Air_Click(object sender, RoutedEventArgs e)
        {
            _airOn = !_airOn;

            var req = new AirReq
            {
                air = _airOn
            };

            await App.Network.SendAsync(req);

            // 응답에서 실제 반영됨
        }

        private async void Heat_Click(object sender, RoutedEventArgs e)
        {
            _heatOn = !_heatOn;

            var req = new HeatReq
            {
                heat = _heatOn
            };

            await App.Network.SendAsync(req);
        }

        private async void Light_Click(object sender, RoutedEventArgs e)
        {
            _lightOn = !_lightOn;

            var req = new LightReq
            {
                light = _lightOn
            };

            await App.Network.SendAsync(req);
        }

        // ====================================================
        // Horn (서버 없이 1.5초 반짝)
        // ====================================================
        private async void Horn_Click(object sender, RoutedEventArgs e)
        {
            HornBtn.Background = OnColor;
            await Task.Delay(1500);
            HornBtn.Background = OffColor;
        }

        // ====================================================
        // Valet Mode
        // ====================================================
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

        // ====================================================
        // Drive / Park → 서버 전송 + 5초 유지
        // ====================================================
        private async void DriveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ValetToggle.IsChecked != true) return;

            ParkBtn.Background = OffColor;
            DriveBtn.Background = OnColor;

            var req = new ControlReq
            {
                control = false
            };
            await App.Network.SendAsync(req);

            App.IsParked = false;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mw &&
                mw.ContentFrame.Content is StatusPage sp)
                {
                    sp.ForceUpdateParkingTime();
                }
            });

            await Task.Delay(5000);
        }

        private async void ParkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (ValetToggle.IsChecked != true) return;

            DriveBtn.Background = OffColor;
            ParkBtn.Background = OnColor;

            var req = new ControlReq
            {
                control = true
            };
            await App.Network.SendAsync(req);

            App.ParkingStartTime = DateTime.Now;
            App.IsParked = true;
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.MainWindow is MainWindow mw &&
                mw.ContentFrame.Content is StatusPage sp)
                {
                    sp.ForceUpdateParkingTime();
                }
            });

            await Task.Delay(5000);
        }

        // ====================================================
        // Navigation
        // ====================================================
        private void Back_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.GoBack();
        }
    }
}
