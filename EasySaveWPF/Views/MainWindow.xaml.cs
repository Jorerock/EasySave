using EasySave.Core.Domain;
using EasySave.Core.ViewModels;
using EasySave.WPF.Localization;
using System.Windows;

namespace EasySave.WPF.Views
{
    public partial class MainWindow : Window
    {
        private IMainViewModel ViewModel
        {
            get { return (IMainViewModel)DataContext; }
        }

        public MainWindow()
        {
            InitializeComponent();
            RefreshDataGridHeaders();
        }

        private void RefreshDataGridHeaders()
        {
            if (JobsDataGrid == null)
            {
                return;
            }

            if (JobsDataGrid.Columns == null || JobsDataGrid.Columns.Count < 5)
            {
                return;
            }

            JobsDataGrid.Columns[0].Header = LocalizationManager.T("Col_Name");
            JobsDataGrid.Columns[1].Header = LocalizationManager.T("Col_Type");
            JobsDataGrid.Columns[2].Header = LocalizationManager.T("Col_Source");
            JobsDataGrid.Columns[3].Header = LocalizationManager.T("Col_Target");
            JobsDataGrid.Columns[4].Header = LocalizationManager.T("Col_Encrypted");
        }

        private void CreateJob_Click(object sender, RoutedEventArgs e)
        {
            CreateJobWindow createWindow = new CreateJobWindow();
            createWindow.Owner = this;

            bool? result = createWindow.ShowDialog();
            if (result == true)
            {
                BackupJob job = createWindow.CreatedJob;
                ViewModel.AddJob(job);

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
                RefreshDataGridHeaders();

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
