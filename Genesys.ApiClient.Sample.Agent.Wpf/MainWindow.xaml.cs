using Genesys.ApiClient.Components;
using System;
using System.Runtime.ExceptionServices;
using System.Windows;

namespace Genesys.ApiClient.Sample.Agent.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GenesysConnection genesysConnection;
        private readonly GenesysAgent genesysAgent;
        private readonly GenesysDN genesysDN;

        public MainWindow()
        {
            InitializeComponent();

            genesysConnection = new GenesysConnection();
            ConnectionPanel.DataContext = genesysConnection;

            genesysAgent = new GenesysAgent() { Connection = genesysConnection };
            UserPanel.DataContext = genesysAgent;

            genesysDN = new GenesysDN() { User = genesysAgent };
            DNPanel.DataContext = genesysDN;
        }

        async void OpenConnection_Click(object sender, RoutedEventArgs e)
        {
            genesysConnection.ApiBaseUrl = ApiBaseUrl.Text;
            genesysConnection.ApiKey = ApiKey.Text;
            genesysConnection.UserName = UserName.Text;
            genesysConnection.Password = Password.Text;
            genesysConnection.ClientId = ClientId.Text;
            genesysConnection.ClientSecret = ClientSecret.Text;

            try
            {
                await genesysConnection.StartAsync();
                //await genesysUser.StartAsync();
            }
            catch (Exception ex)
            {
                genesysConnection.Stop();

                // OperationCanceledException is thrown if Close was requested while Opening. That's OK
                if (!(ex is OperationCanceledException))
                    throw;
            }
        }

        void CloseConnection_Click(object sender, RoutedEventArgs e)
        {
            genesysConnection.Dispose();
        }

        void ActivateChannels_Click(object sender, RoutedEventArgs e)
        {
            genesysAgent.DN = Dn.Text;
            genesysAgent.ActivateChannels();
        }

        void Ready_Click(object sender, RoutedEventArgs e)
        {
            genesysDN.Ready();
        }

        void NotReady_Click(object sender, RoutedEventArgs e)
        {
            genesysDN.NotReady();
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            genesysAgent.Logout();
        }
    }
}
