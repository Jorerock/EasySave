using EasySave.Core.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EasySave.WPF.Views
{
    public partial class CreateJobWindow : Window
    {
        public BackupJob CreatedJob { get; private set; }

        public CreateJobWindow()
        {
            InitializeComponent();
        }

        private void EnableEncryption_Changed(object sender, RoutedEventArgs e)
        {
            if (EncryptionPanel != null)
            {
                EncryptionPanel.IsEnabled = EnableEncryptionCheckBox.IsChecked == true;
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            // Validation du nom
            if (string.IsNullOrWhiteSpace(JobNameTextBox.Text))
            {
                MessageBox.Show("Please enter a job name.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                JobNameTextBox.Focus();
                return;
            }

            // Validation de la source
            string sourcePath = SourceTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                MessageBox.Show("Please enter a source directory.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SourceTextBox.Focus();
                return;
            }

            // Vérifier que le répertoire source existe
            if (!Directory.Exists(sourcePath))
            {
                var result = MessageBox.Show(
                    $"The source directory does not exist:\n{sourcePath}\n\nDo you want to create it?",
                    "Directory Not Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Directory.CreateDirectory(sourcePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to create directory:\n{ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    SourceTextBox.Focus();
                    return;
                }
            }

            // Validation de la cible
            string targetPath = TargetTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                MessageBox.Show("Please enter a target directory.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TargetTextBox.Focus();
                return;
            }

            // Vérifier que source et cible ne sont pas identiques
            if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Source and target directories cannot be the same.", "Validation Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Parse extensions
            List<string> extensions = new List<string>();
            if (EnableEncryptionCheckBox.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(EncryptionKeyTextBox.Text))
                {
                    MessageBox.Show("Please enter an encryption key.", "Validation Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EncryptionKeyTextBox.Focus();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(ExtensionsTextBox.Text))
                {
                    extensions = ExtensionsTextBox.Text
                        .Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(ext => ext.Trim())
                        .Select(ext => ext.StartsWith(".") ? ext : "." + ext)
                        .Select(ext => ext.ToLowerInvariant())
                        .Distinct()
                        .ToList();
                }
            }

            // Create job
            CreatedJob = new BackupJob
            {
                Name = JobNameTextBox.Text.Trim(),
                SourceDirectory = sourcePath,
                TargetDirectory = targetPath,
                Type = ((ComboBoxItem)BackupTypeComboBox.SelectedItem).Content.ToString() == "Differential"
                    ? BackupType.Differential
                    : BackupType.Full,
                EnableEncryption = EnableEncryptionCheckBox.IsChecked == true,
                EncryptionKey = EncryptionKeyTextBox.Text?.Trim(),
                ExtensionsToEncrypt = extensions
            };

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}