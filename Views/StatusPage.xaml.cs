using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Windows.Media;
using DotBotCarClient.Protocol;
using DotBotCarClient.Network;

namespace DotBotCarClient.Views
{
    public partial class StatusPage : Page, IProtocolHandler
    {
        private DispatcherTimer _statusTimer;
        private DispatcherTimer _parkingTimer;

        // 토글 상태 저장
        private bool _isEngineOn = false;
        private bool _isDoorOpen = false;
        private bool _isTrunkOpen = false;

        public StatusPage()
        {
            InitializeComponent();

            // 첫 로드시 상태 요청
            RequestBatteryStatus();

            // 최신 시간 반영
            this.Loaded += (s, e) =>
            {
                _statusTimer?.Start();
                _parkingTimer?.Start();
                UpdateParkingTime();
            };
            // 5초마다 지속 요청
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(3);
            _statusTimer.Tick += (s, e) => RequestBatteryStatus();
            _statusTimer.Start();

            // 주차 시간 갱신
            _parkingTimer = new DispatcherTimer();
            _parkingTimer.Interval = TimeSpan.FromMinutes(1);
            _parkingTimer.Tick += (s, e) => UpdateParkingTime();
            _parkingTimer.Start();

            // 즉시 로드
            UpdateParkingTime();

            // 다른 페이지로 이동 시 타이머 종료
            this.Unloaded += StatusPage_Unloaded;
        }

        private void StatusPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_statusTimer != null)
                _statusTimer.Stop();
        }

        // ======================================================
        // 서버 응답 처리
        // ======================================================
        public async void HandleProtocolMessage(BaseMessage msg)
        {
            switch (msg.msg)
            {
                case MsgType.STATUS_RES:
                    HandleStatusRes(msg as StatusRes);
                    break;

                case MsgType.BATTERY_RES:
                    {
                        HandleBatteryRes(msg as BatteryRes);
                        break;
                    }

                case MsgType.START_RES:
                    {
                        var res = msg as StartRes;
                        if (res != null)
                        {
                            _isEngineOn = res.active_status;
                            await UpdateStartButtonColor(_isEngineOn);
                        }
                        break;
                    }

                case MsgType.DOOR_RES:
                    {
                        var res = msg as DoorRes;
                        if (res != null)
                        {
                            _isDoorOpen = res.door_status;
                            await UpdateDoorButtonColor(_isDoorOpen);
                        }
                        break;
                    }

                case MsgType.TRUNK_RES:
                    {
                        var res = msg as TrunkRes;
                        if (res != null)
                        {
                            _isTrunkOpen = res.trunk_status;
                            await UpdateTrunkButtonColor(_isTrunkOpen);
                        }
                        break;
                    }
            }
        }

        // ======================================================
        // 상태 요청
        // ======================================================
        private async void RequestBatteryStatus()
        {
            var req = new BatteryReq
            {
                battery = true
            };

            await App.Network.SendAsync(req);
        }


        // ======================================================
        // STATUS_RES / BATEERY_RES 처리 (UI 업데이트)
        // ======================================================
        private void HandleStatusRes(StatusRes? st)
        {
            if (st == null) return;

            Dispatcher.Invoke(() =>
            {
                string state = "대기";
                if (st.driving) state = "운행";

                CarStateText.Text = $"차량 상태 : {state}";
            });
        }
        private void HandleBatteryRes(BatteryRes? br)
        {
            if (br == null) return;
            Dispatcher.Invoke(() =>
            {
                BatteryText.Text = $"{(int)(br.battery_status * 100)}%";
                int range = (int)(400 * br.battery_status);
                RangeText.Text = $"Range : {range}km";
            });
        }
        // ======================================================
        // UI - 주차 업데이트
        // ======================================================
        private void UpdateParkingTime()
        {
            if (!App.IsParked || App.ParkingStartTime == DateTime.MinValue)
            {
                CarStateText.Text = "차량 상태 : 대기";
                LocationText.Text = "주차 위치: 없음";
                return;
            }

            int minutesPassed = (int)(DateTime.Now - App.ParkingStartTime).TotalMinutes;
            CarStateText.Text = "차량 상태 : 주차";

            if (minutesPassed >= 0 && minutesPassed < 60)
                LocationText.Text = $"주차 위치: 집 ({minutesPassed}분 전)";
            else
            {
                int hours = minutesPassed / 60;
                int mins = minutesPassed % 60;
                LocationText.Text = $"주차 위치: 집 ({hours}시간 {mins}분 전)";
            }
        }
        public void ForceUpdateParkingTime()
        {
            UpdateParkingTime();
        }

        // ======================================================
        // 버튼 색 변경 함수들
        // ======================================================

        private async Task UpdateStartButtonColor(bool isOn)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                StartBorder.Background = isOn
                    ? new SolidColorBrush(Color.FromRgb(41, 128, 255))
                    : new SolidColorBrush(Color.FromRgb(34, 34, 34));
            });
        }
        private async Task UpdateDoorButtonColor(bool isOpen)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                LockBorder.Background = isOpen
                    ? new SolidColorBrush(Color.FromRgb(39, 174, 96))
                    : new SolidColorBrush(Color.FromRgb(34, 34, 34));
            });
        }
        private async Task UpdateTrunkButtonColor(bool isOpen)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                TrunkBorder.Background = isOpen
                    ? new SolidColorBrush(Color.FromRgb(243, 156, 18))
                    : new SolidColorBrush(Color.FromRgb(34, 34, 34));
            });
        }

        // ======================================================
        // 버튼 클릭 → 서버 전송 (토글 방식)
        // ======================================================

        // ★ 시동 토글
        private async void start_Click(object sender, RoutedEventArgs e)
        {
            var req = new StartReq
            {
                active = !_isEngineOn
            };

            await App.Network.SendAsync(req);
        }

        // ★ 문 토글
        private async void Lock_Click(object sender, RoutedEventArgs e)
        {
            var req = new DoorReq
            {
                door = !_isDoorOpen
            };

            await App.Network.SendAsync(req);
        }

        // ★ 트렁크 토글
        private async void Trunk_Click(object sender, RoutedEventArgs e)
        {
            var req = new TrunkReq
            {
                trunk = !_isTrunkOpen
            };

            await App.Network.SendAsync(req);
        }

        // 네비게이션
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