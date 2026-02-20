using EasySave.Core.Domain;
using EasySave.WPF.Localization;
using EasySave.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace EasySave.WPF.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel
        {
            get { return (MainViewModel)DataContext; }
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null) return;

            ComboBoxItem item = cb.SelectedItem as ComboBoxItem;
            if (item == null) return;

            string culture = item.Tag as string;
            if (string.IsNullOrWhiteSpace(culture)) return;

            LocalizationManager.SetCulture(culture);
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
                        LocalizationManager.T("Msg_SuccessTitle"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
    }
}
