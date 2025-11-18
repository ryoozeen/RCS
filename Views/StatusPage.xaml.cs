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

        // 토글 상태 저장
        private bool _isEngineOn = false;
        private bool _isDoorOpen = false;
        private bool _isTrunkOpen = false;

        public StatusPage()
        {
            InitializeComponent();

            // 첫 로드시 상태 요청
            RequestStatus();

            // 5초마다 지속 요청
            _statusTimer = new DispatcherTimer();
            _statusTimer.Interval = TimeSpan.FromSeconds(5);
            _statusTimer.Tick += (s, e) => RequestStatus();
            _statusTimer.Start();

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
        private async void RequestStatus()
        {
            var req = new StatusReq
            {
                car_status = true
            };

            await App.Network.SendAsync(req);
        }

        // ======================================================
        // STATUS_RES 처리 (UI 업데이트)
        // ======================================================
        private void HandleStatusRes(StatusRes? st)
        {
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