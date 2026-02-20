using EasySave.Core.Domain;
using EasySave.Core.ViewModels;  // ← IMainViewModel
using System.Windows;

namespace EasySave.WPF.Views
{
    public partial class MainWindow : Window
    {
        // ✅ Utilise l'interface, mais le DataContext est WpfMainViewModel
        private IMainViewModel ViewModel => (IMainViewModel)DataContext;

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