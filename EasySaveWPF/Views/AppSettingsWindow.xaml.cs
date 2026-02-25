using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EasySave.Core.Domain;

namespace EasySave.WPF.Views
{
    public partial class AppSettingsWindow : Window
    {
        public AppSettings AppSettings { get; private set; }

        // On garde une copie des settings existants
        private readonly AppSettings _original;

        public AppSettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();

            _original = currentSettings ?? new AppSettings();
            LoadSettings(_original);
        }

        private void LoadSettings(AppSettings settings)
        {
            // Language
            LanguageComboBox.SelectedItem = settings.Language;

            // LogFormat
            LogFormatComboBox.SelectedIndex = (settings.LogFormat?.ToLowerInvariant() == "xml") ? 1 : 0;

            // Business software
            SourceTextBox.Text = settings.BusinessSoftwarePath ?? string.Empty;

            // Extensions to encrypt
            SetEncryptCheckboxes(settings.ExtensionsToEncrypt);

            // Priority extensions
            SetPriorityCheckboxes(settings.PriorityExtensions);

            // Log mode
            SetLogMode(settings.LogMode);

            // Central URL
            CentralUrlTextBox.Text = string.IsNullOrWhiteSpace(settings.CentralLogUrl)
                ? "http://localhost:5080"
                : settings.CentralLogUrl.Trim();
        }

        private void SetEncryptCheckboxes(List<string> exts)
        {
            TxtCheckBox.IsChecked = false;
            CsvCheckBox.IsChecked = false;
            PngCheckBox.IsChecked = false;
            JpgCheckBox.IsChecked = false;
            DocxCheckBox.IsChecked = false;
            PdfCheckBox.IsChecked = false;

            if (exts == null) return;

            foreach (string ext in exts.Select(x => x?.ToLowerInvariant()))
            {
                if (ext == ".txt") TxtCheckBox.IsChecked = true;
                else if (ext == ".csv") CsvCheckBox.IsChecked = true;
                else if (ext == ".png") PngCheckBox.IsChecked = true;
                else if (ext == ".jpg") JpgCheckBox.IsChecked = true;
                else if (ext == ".docx") DocxCheckBox.IsChecked = true;
                else if (ext == ".pdf") PdfCheckBox.IsChecked = true;
            }
        }

        private void SetPriorityCheckboxes(List<string> exts)
        {
            PriorityPdfCheckBox.IsChecked = false;
            PriorityDocxCheckBox.IsChecked = false;
            PriorityTxtCheckBox.IsChecked = false;

            if (exts == null) return;

            foreach (string ext in exts.Select(x => x?.ToLowerInvariant()))
            {
                if (ext == ".pdf") PriorityPdfCheckBox.IsChecked = true;
                else if (ext == ".docx") PriorityDocxCheckBox.IsChecked = true;
                else if (ext == ".txt") PriorityTxtCheckBox.IsChecked = true;
            }
        }

        private void SetLogMode(string mode)
        {
            string m = (mode ?? "local").ToLowerInvariant();
            // items: local/central/both via Tag
            foreach (var item in LogModeComboBox.Items)
            {
                if (item is System.Windows.Controls.ComboBoxItem cbi)
                {
                    var tag = (cbi.Tag?.ToString() ?? "").ToLowerInvariant();
                    if (tag == m)
                    {
                        LogModeComboBox.SelectedItem = cbi;
                        return;
                    }
                }
            }

            // défaut
            LogModeComboBox.SelectedIndex = 0;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // ✅ on part d'une copie de l'original pour ne pas perdre les champs
            AppSettings settings = CloneSettings(_original);

            // Language
            if (LanguageComboBox.SelectedItem is AppLanguage lang)
                settings.SetLanguage(lang);

            // LogFormat
            settings.LogFormat = (LogFormatComboBox.SelectedIndex == 1) ? "xml" : "json";

            // Business software
            settings.BusinessSoftwarePath = SourceTextBox.Text?.Trim() ?? "";

            // Extensions to encrypt
            settings.ExtensionsToEncrypt = GetEncryptExtensionsFromUI();

            // Priority extensions
            settings.PriorityExtensions = GetPriorityExtensionsFromUI();

            // Log mode
            settings.LogMode = GetSelectedLogMode();

            // Central URL
            settings.CentralLogUrl = (CentralUrlTextBox.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(settings.CentralLogUrl))
                settings.CentralLogUrl = "http://localhost:5080";

            AppSettings = settings;
            DialogResult = true;
        }

        private List<string> GetEncryptExtensionsFromUI()
        {
            List<string> extensions = new List<string>();
            if (TxtCheckBox.IsChecked == true) extensions.Add(".txt");
            if (CsvCheckBox.IsChecked == true) extensions.Add(".csv");
            if (PngCheckBox.IsChecked == true) extensions.Add(".png");
            if (JpgCheckBox.IsChecked == true) extensions.Add(".jpg");
            if (DocxCheckBox.IsChecked == true) extensions.Add(".docx");
            if (PdfCheckBox.IsChecked == true) extensions.Add(".pdf");
            return extensions;
        }

        private List<string> GetPriorityExtensionsFromUI()
        {
            List<string> extensions = new List<string>();
            if (PriorityPdfCheckBox.IsChecked == true) extensions.Add(".pdf");
            if (PriorityDocxCheckBox.IsChecked == true) extensions.Add(".docx");
            if (PriorityTxtCheckBox.IsChecked == true) extensions.Add(".txt");
            return extensions;
        }

        private string GetSelectedLogMode()
        {
            if (LogModeComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem cbi)
            {
                var tag = cbi.Tag?.ToString();
                if (!string.IsNullOrWhiteSpace(tag))
                    return tag.Trim().ToLowerInvariant();
            }
            return "local";
        }

        private static AppSettings CloneSettings(AppSettings s)
        {
            return new AppSettings
            {
                Language = s.Language,
                LogFormat = s.LogFormat ?? "json",
                ExtensionsToEncrypt = s.ExtensionsToEncrypt != null ? new List<string>(s.ExtensionsToEncrypt) : new List<string>(),
                BusinessSoftwarePath = s.BusinessSoftwarePath ?? "",

                PriorityExtensions = s.PriorityExtensions != null ? new List<string>(s.PriorityExtensions) : new List<string>(),
                LargeFileThresholdKo = s.LargeFileThresholdKo,

                LogMode = string.IsNullOrWhiteSpace(s.LogMode) ? "local" : s.LogMode,
                CentralLogUrl = string.IsNullOrWhiteSpace(s.CentralLogUrl) ? "http://localhost:5080" : s.CentralLogUrl
            };
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}