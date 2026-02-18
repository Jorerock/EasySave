using EasySave.Core.Domain;
using EasySave.WPF.ViewModels;
using System.Windows;

namespace EasySave.WPF.Views  // ← Doit correspondre au x:Class dans XAML
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateJob_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new Views.CreateJobWindow
            {
                Owner = this
            };

            if (createWindow.ShowDialog() == true)
            {
                var job = createWindow.CreatedJob;
                ViewModel.AddJob(job);

                if (!string.IsNullOrEmpty(ViewModel.StatusMessage))
                {
                    MessageBox.Show(ViewModel.StatusMessage, "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }



        private void AppSettings_Click(object sender, RoutedEventArgs e)
        {
            // get the settings from the ViewModel to pass to the settings window
            var currentSettings = ViewModel.CurrentSettings
                ?? new AppSettings();

            var settingsWindow = new AppSettingsWindow(currentSettings)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true)
            {
                // send the new settings back to the ViewModel to apply them
                ViewModel.ApplySettings(settingsWindow.AppSettings);

                MessageBox.Show("Settings saved successfully.", "Settings",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

    }
}