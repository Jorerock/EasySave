using System.Collections.Generic;
using System.Windows;
using EasySave.Core.Domain;

namespace EasySave.WPF.Views
{
    public partial class AppSettingsWindow : Window
    {
        public AppSettings AppSettings { get; private set; }

        public AppSettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();
            LoadSettings(currentSettings);
        }

        private void LoadSettings(AppSettings settings)
        {
            //langage:  Auto fill the langage with the current settings
            LanguageComboBox.SelectedItem = settings.Language;

            // Log format
            LogFormatComboBox.SelectedIndex = settings.LogFormat == "xml" ? 1 : 0;

            // Business software path
            SourceTextBox.Text = settings.BusinessSoftwarePath ?? string.Empty;

            // Check the checkboxes based on the current settings
            if (settings.ExtensionsToEncrypt != null)
            {
                foreach (var ext in settings.ExtensionsToEncrypt)
                {
                    switch (ext.ToLower())
                    {
                        case ".txt":
                            TxtCheckBox.IsChecked = true;
                            break;
                        case ".csv":
                            CsvCheckBox.IsChecked = true;
                            break;
                        case ".png":
                            PngCheckBox.IsChecked = true;
                            break;
                        case ".jpg":
                            JpgCheckBox.IsChecked = true;
                            break;
                        case ".docx":
                            DocxCheckBox.IsChecked = true;
                            break;
                        case ".pdf":
                            PdfCheckBox.IsChecked = true;
                            break;
                    }
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Select the language from the combo box
            var selectedLanguage = (AppLanguage)LanguageComboBox.SelectedItem;

            // build the list of extensions to encrypt based on the checkboxes
            var extensions = new List<string>();
            if (TxtCheckBox.IsChecked == true) extensions.Add(".txt");
            if (CsvCheckBox.IsChecked == true) extensions.Add(".csv");
            if (PngCheckBox.IsChecked == true) extensions.Add(".png");
            if (JpgCheckBox.IsChecked == true) extensions.Add(".jpg");
            if (DocxCheckBox.IsChecked == true) extensions.Add(".docx");
            if (PdfCheckBox.IsChecked == true) extensions.Add(".pdf");

            AppSettings = new AppSettings()
            {
                LogFormat = LogFormatComboBox.SelectedIndex == 0 ? "json" : "xml",
                BusinessSoftwarePath = SourceTextBox.Text.Trim(),
                ExtensionsToEncrypt = extensions
            };

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}