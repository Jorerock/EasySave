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
            LanguageComboBox.SelectedItem = settings.Language;
            LogFormatComboBox.SelectedIndex = settings.LogFormat == "xml" ? 1 : 0;
            SourceTextBox.Text = settings.BusinessSoftwarePath ?? string.Empty;

            if (settings.ExtensionsToEncrypt != null)
            {
                foreach (string ext in settings.ExtensionsToEncrypt)
                {
                    string lower = ext.ToLower();

                    if (lower == ".txt") TxtCheckBox.IsChecked = true;
                    else if (lower == ".csv") CsvCheckBox.IsChecked = true;
                    else if (lower == ".png") PngCheckBox.IsChecked = true;
                    else if (lower == ".jpg") JpgCheckBox.IsChecked = true;
                    else if (lower == ".docx") DocxCheckBox.IsChecked = true;
                    else if (lower == ".pdf") PdfCheckBox.IsChecked = true;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            AppLanguage selectedLanguage = (AppLanguage)LanguageComboBox.SelectedItem;

            List<string> extensions = new List<string>();
            if (TxtCheckBox.IsChecked == true) extensions.Add(".txt");
            if (CsvCheckBox.IsChecked == true) extensions.Add(".csv");
            if (PngCheckBox.IsChecked == true) extensions.Add(".png");
            if (JpgCheckBox.IsChecked == true) extensions.Add(".jpg");
            if (DocxCheckBox.IsChecked == true) extensions.Add(".docx");
            if (PdfCheckBox.IsChecked == true) extensions.Add(".pdf");

            AppSettings settings = new AppSettings();
            settings.SetLanguage(selectedLanguage); // ✅ IMPORTANT
            settings.LogFormat = LogFormatComboBox.SelectedIndex == 0 ? "json" : "xml";
            settings.BusinessSoftwarePath = SourceTextBox.Text.Trim();
            settings.ExtensionsToEncrypt = extensions;

            AppSettings = settings;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
