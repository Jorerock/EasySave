using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EasySave.Core.Domain;

namespace EasySave.WPF.Views
{
    public partial class AppSettingsWindow : Window
    {
        // Le résultat exposé après fermeture de la fenêtre
        public AppSettings AppSettings { get; private set; }

        // Collection observable pour les extensions (ajout/suppression dynamique)
        private readonly ObservableCollection<string> _extensions = new();

        public AppSettingsWindow(AppSettings currentSettings)
        {
            InitializeComponent();

            // Bind la liste des extensions à l'ItemsControl
            ExtensionsListBox.ItemsSource = _extensions;

            // Pré-remplir avec les settings existants
            LoadSettings(currentSettings);
        }

        private void LoadSettings(AppSettings settings)
        {
            // Langue : sélectionne la valeur de l'enum courante dans la ComboBox
            LanguageComboBox.SelectedItem = settings.Language;

            // Log format
            LogFormatComboBox.SelectedIndex = settings.LogFormat == "xml" ? 1 : 0;

            // Business software path
            SourceTextBox.Text = settings.BusinessSoftwarePath ?? string.Empty;

            // Extensions
            foreach (var ext in settings.ExtensionsToEncrypt)
                _extensions.Add(ext);
        }

        // Ajoute une extension en appuyant sur Entrée dans le TextBox
        private void ExtensionTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var ext = JobNameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(ext)) return;

            // Normalise (ajoute le point si absent)
            if (!ext.StartsWith("."))
                ext = "." + ext;

            if (!_extensions.Contains(ext))
                _extensions.Add(ext);

            JobNameTextBox.Clear();
        }

        // Supprime une extension quand la CheckBox est décochée
        private void ExtensionCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Content is string ext)
                _extensions.Remove(ext);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Récupère la langue sélectionnée — c'est directement la valeur de l'enum
            // Cast vers AppLanguage — plus aucun conflit avec XmlLanguage
            var selectedLanguage = (AppLanguage)LanguageComboBox.SelectedItem;

            AppSettings = new AppSettings(selectedLanguage)
            {
                LogFormat = LogFormatComboBox.SelectedIndex == 0 ? "json" : "xml",
                BusinessSoftwarePath = SourceTextBox.Text.Trim(),
                ExtensionsToEncrypt = new System.Collections.Generic.List<string>(_extensions)
            };

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}