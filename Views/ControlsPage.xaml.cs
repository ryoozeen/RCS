using System.Windows;
using System.Windows.Controls;

namespace DotBotCarClient.Views
{
    public partial class ControlsPage : Page
    {
        public ControlsPage()
        {
            InitializeComponent();
        }

        private void BtnLock_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("문 잠금/해제 요청 (서버 연동 예정)");
        }

        private void BtnFlash_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("플래시 요청");
        }

        private void BtnHonk_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("경적!");
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("시동 ON/OFF 요청");
        }

        private void ToggleValet_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Valet Mode ON");
        }

        private void ToggleValet_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Valet Mode OFF");
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new StatusPage());
        }
    }
}
