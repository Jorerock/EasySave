using System.Windows;
using EasySave.WPF.ViewModels;

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
            // Ajoutez un breakpoint ici pour vérifier si la méthode est appelée
            MessageBox.Show("Button clicked!"); // Test temporaire
            
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
    }
}