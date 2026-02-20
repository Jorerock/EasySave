using EasySave.Core.Domain;
using EasySave.WPF.ViewModels; 
using System.Windows;

namespace EasySave.WPF.Views
{
    public partial class MainWindow : Window
    {
        // ✅ CORRECTION : Cast vers WpfMainViewModel au lieu de IMainViewModel
        private WpfMainViewModel ViewModel => (WpfMainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateJob_Click(object sender, RoutedEventArgs e)
        {
            var createWindow = new CreateJobWindow
            {
                Owner = this
            };

            if (createWindow.ShowDialog() == true)
            {
                var job = createWindow.CreatedJob;
                ViewModel.AddJob(job);  // ✅ Fonctionne car WpfMainViewModel a AddJob
                
                if (!string.IsNullOrEmpty(ViewModel.StatusMessage))
                {
                    MessageBox.Show(ViewModel.StatusMessage, "Success",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void AppSettings_Click(object sender, RoutedEventArgs e)
        {
            var currentSettings = ViewModel.GetCurrentSettings();

            var settingsWindow = new AppSettingsWindow(currentSettings)
            {
                Owner = this
            };

            if (settingsWindow.ShowDialog() == true)
            {
                ViewModel.ApplySettings(settingsWindow.AppSettings);

                MessageBox.Show("Settings saved successfully.", "Settings",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}