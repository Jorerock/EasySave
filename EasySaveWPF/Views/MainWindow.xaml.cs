using EasySave.Core.Domain;
using EasySave.WPF.Localization;
using EasySave.WPF.ViewModels;
using System.Windows;

namespace EasySave.WPF.Views
{
    public partial class MainWindow : Window
    {
        private WpfMainViewModel ViewModel
        {
            get { return (WpfMainViewModel)DataContext; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void CreateJob_Click(object sender, RoutedEventArgs e)
        {
            CreateJobWindow createWindow = new CreateJobWindow();
            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();
            if (result == true)
            {
                ViewModel.AddJob(createWindow.CreatedJob);

                if (!string.IsNullOrEmpty(ViewModel.StatusMessage))
                {
                    MessageBox.Show(
                        ViewModel.StatusMessage,
                        LocalizationManager.T("Msg_Success"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
        }

        private void AppSettings_Click(object sender, RoutedEventArgs e)
        {
            AppSettings currentSettings = ViewModel.GetCurrentSettings();

            AppSettingsWindow settingsWindow = new AppSettingsWindow(currentSettings);
            settingsWindow.Owner = this;

            bool? result = settingsWindow.ShowDialog();
            if (result == true)
            {
                ViewModel.ApplySettings(settingsWindow.AppSettings);

                if (settingsWindow.AppSettings.Language == AppLanguage.Francais)
                {
                    LocalizationManager.SetCulture("fr-FR");
                }
                else
                {
                    LocalizationManager.SetCulture("en-US");
                }

                MessageBox.Show(
                    LocalizationManager.T("Msg_SettingsSaved"),
                    LocalizationManager.T("Msg_Settings"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }
    }
}
