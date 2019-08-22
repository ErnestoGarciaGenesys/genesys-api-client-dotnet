using Genesys.ApiClient.Sample.Agent.Wpf.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Genesys.ApiClient.Sample.Agent.Wpf
{
    public partial class App : Application
    {
        public App()
        {
            Settings.Default.PropertyChanged += (sender, e) => Settings.Default.Save();

            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            foreach (var inner in e.Exception.InnerExceptions)
            {
                ShowException("TaskScheduler_UnobservedTaskException", inner);
            }
        }

        void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            ShowException("Application_DispatcherUnhandledException", e.Exception);
        }

        void ShowException(string caughtBy, Exception e)
        {
            Trace.TraceError(caughtBy + ": " + e);
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
