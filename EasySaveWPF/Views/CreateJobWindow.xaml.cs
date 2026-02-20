using EasySave.Core.Domain;
using EasySave.WPF.Localization;
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
            if (string.IsNullOrWhiteSpace(JobNameTextBox.Text))
            {
                MessageBox.Show(LocalizationManager.T("Msg_EnterJobName"),
                    LocalizationManager.T("Msg_ValidationErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                JobNameTextBox.Focus();
                return;
            }

            string sourcePath = SourceTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                MessageBox.Show(LocalizationManager.T("Msg_EnterSourceDir"),
                    LocalizationManager.T("Msg_ValidationErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SourceTextBox.Focus();
                return;
            }

            if (!Directory.Exists(sourcePath))
            {
                var result = MessageBox.Show(
                    $"{LocalizationManager.T("Msg_SourceNotExist")}\n{sourcePath}\n\n{LocalizationManager.T("Msg_CreateDirQuestion")}",
                    LocalizationManager.T("Msg_DirNotFoundTitle"),
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
                        MessageBox.Show($"{LocalizationManager.T("Msg_FailedCreateDir")}\n{ex.Message}",
                            LocalizationManager.T("Msg_ErrorTitle"),
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

            string targetPath = TargetTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                MessageBox.Show(LocalizationManager.T("Msg_EnterTargetDir"),
                    LocalizationManager.T("Msg_ValidationErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                TargetTextBox.Focus();
                return;
            }

            if (sourcePath.Equals(targetPath, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(LocalizationManager.T("Msg_SourceTargetSame"),
                    LocalizationManager.T("Msg_ValidationErrorTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            List<string> extensions = new List<string>();
            if (EnableEncryptionCheckBox.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(EncryptionKeyTextBox.Text))
                {
                    MessageBox.Show(LocalizationManager.T("Msg_EnterEncryptionKey"),
                        LocalizationManager.T("Msg_ValidationErrorTitle"),
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

            CreatedJob = new BackupJob
            {
                Name = JobNameTextBox.Text.Trim(),
                SourceDirectory = sourcePath,
                TargetDirectory = targetPath,
                Type = ((ComboBoxItem)BackupTypeComboBox.SelectedItem).Content.ToString() == LocalizationManager.T("BackupType_Differential")
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
