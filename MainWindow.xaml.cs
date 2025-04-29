using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WSLWpfApp
{
    public partial class MainWindow : Window
    {
        private string importTarballPath = "";
        private string exportPath = "";

        public MainWindow()
        {
            InitializeComponent();
            LoadDistros();
        }

        private void LoadDistros()
        {
            var output = RunCommand("wsl -l");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                              .Skip(1) // skip header
                              .Select(line => line.Trim().Split(' ')[0]) // distro name
                              .Where(name => !string.IsNullOrWhiteSpace(name))
                              .ToList();

            cbDistros.ItemsSource = lines;
            cbExportDistro.ItemsSource = lines;
        }

        private string RunCommand(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                var process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }
        }

        private void LaunchWsl_Click(object sender, RoutedEventArgs e) => RunCommand("wsl");

        private void ListDistros_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show(RunCommand("wsl -l"), "WSL Distributions");

        private void ShutdownWsl_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show(RunCommand("wsl --shutdown"), "WSL Shutdown");

        private void TerminateDistro_Click(object sender, RoutedEventArgs e)
        {
            if (cbDistros.SelectedItem is string distro)
                MessageBox.Show(RunCommand($"wsl --terminate {distro}"), "Terminated");
        }

        private void SetDefaultDistro_Click(object sender, RoutedEventArgs e)
        {
            if (cbDistros.SelectedItem is string distro)
                MessageBox.Show(RunCommand($"wsl --set-default {distro}"), "Default Distro Set");
        }

        private void WslStatus_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show(RunCommand("wsl --status"), "WSL Status");

        private void SelectImportTarball_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "Tar Files (*.tar)|*.tar" };
            if (dialog.ShowDialog() == true)
                importTarballPath = dialog.FileName;
        }

        private void ImportDistro_Click(object sender, RoutedEventArgs e)
        {
            var name = txtImportName.Text.Trim();
            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(importTarballPath))
            {
                string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WSL", name);
                Directory.CreateDirectory(installPath);
                MessageBox.Show(RunCommand($"wsl --import {name} \"{installPath}\" \"{importTarballPath}\""), "Import Complete");
                LoadDistros();
            }
        }

        private void SelectExportPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Filter = "Tar Files (*.tar)|*.tar" };
            if (dialog.ShowDialog() == true)
                exportPath = dialog.FileName;
        }

        private void ExportDistro_Click(object sender, RoutedEventArgs e)
        {
            if (cbExportDistro.SelectedItem is string distro && !string.IsNullOrEmpty(exportPath))
                MessageBox.Show(RunCommand($"wsl --export {distro} \"{exportPath}\""), "Export Complete");
        }

        private void SetWslVersion_Click(object sender, RoutedEventArgs e)
        {
            if (cbWslVersion.SelectedItem is ComboBoxItem item)
                MessageBox.Show(RunCommand($"wsl --set-default-version {item.Content}"), "Version Set");
        }

        private void MountPath_Click(object sender, RoutedEventArgs e)
        {
            var src = txtSourcePath.Text.Trim();
            var mount = txtMountPoint.Text.Trim();
            if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(mount))
                MessageBox.Show(RunCommand($"wsl --mount \"{src}\" --mountpoint \"{mount}\""), "Mounted");
        }

        private void UnmountPath_Click(object sender, RoutedEventArgs e)
        {
            var mount = txtMountPoint.Text.Trim();
            if (!string.IsNullOrEmpty(mount))
                MessageBox.Show(RunCommand($"wsl --unmount \"{mount}\""), "Unmounted");
        }
    }
}
