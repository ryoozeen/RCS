using DotBotCarClient.Views;
using System.Windows;

namespace DotBotCarClient
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ContentFrame.Navigate(new LoginPage());
        }
    }
}
