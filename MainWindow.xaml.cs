using Microsoft.Win32;
using System;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WSLWpfApp.Models;
using WSLWpfApp.ViewModels;

namespace WSLWpfApp
{
    public partial class MainWindow : Window
    {
        private string activeDistro = null;
        private string importTarballPath = "";
        private string exportPath = "";
        private bool wslLaunched = false;
        private ObservableCollection<DistroModel> distros = new ObservableCollection<DistroModel>();
        MainWindowViewModel ViewModel = new MainWindowViewModel();
        public MainWindow()
        {
            InitializeComponent();
            LoadDistros();

            DataContext = ViewModel;
            ViewModel.LoadDistros();
        }

        private void LoadDistros()
        {
            distros.Clear();
            var output = RunCommandOutput("wsl -l");
            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                              .Skip(1)
                              .Select(line => line.Trim().Split(' ')[0])
                              .Where(name => !string.IsNullOrWhiteSpace(name))
                              .ToList();
            var distroModels = from distro in lines
                               select new DistroModel
                               {
                                   Name = distro,
                               };
            distros.AddRange(distroModels);
        }

        private string RunCommandOutput(string command)
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

        private async Task<string> RunCommandOutputAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Capture errors as well
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                var outputBuilder = new StringWriter();
                var errorBuilder = new StringWriter();

                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        outputBuilder.WriteLine(args.Data);
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                        errorBuilder.WriteLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit()); // Run WaitForExit on a background thread

                if (process.ExitCode != 0)
                {
                    return $"Error: {errorBuilder.ToString().Trim()}";
                }

                return outputBuilder.ToString().Trim();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return "";
            }
        }

        private void ListDistros_Click(object sender, RoutedEventArgs e) =>
            MessageBox.Show(RunCommandOutput("wsl -l"), "WSL Distributions");

        private async void ShutdownWsl_Click(object sender, RoutedEventArgs e)
        {
            string output = await RunCommandOutputAsync("wsl --shutdown");
            activeDistro = null; // Clear the active session
            MessageBox.Show(output, "WSL Shutdown");
        }



        private void WslStatus_Click(object sender, RoutedEventArgs e)
        {
            lblStatus.Text = RunCommandOutput("wsl --status");
        }

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
                MessageBox.Show(RunCommandOutput($"wsl --import {name} \"{installPath}\" \"{importTarballPath}\""), "Import Complete");
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
                MessageBox.Show(RunCommandOutput($"wsl --export {distro} \"{exportPath}\""), "Export Complete");
        }

        private void SetWslVersion_Click(object sender, RoutedEventArgs e)
        {
            if (cbWslVersion.SelectedItem is ComboBoxItem item)
                MessageBox.Show(RunCommandOutput($"wsl --set-default-version {item.Content}"), "Version Set");
        }

        private void MountPath_Click(object sender, RoutedEventArgs e)
        {
            var src = txtSourcePath.Text.Trim();
            var mount = txtMountPoint.Text.Trim();
            if (!string.IsNullOrEmpty(src) && !string.IsNullOrEmpty(mount))
                MessageBox.Show(RunCommandOutput($"wsl --mount \"{src}\" --mountpoint \"{mount}\""), "Mounted");
        }

        private void UnmountPath_Click(object sender, RoutedEventArgs e)
        {
            var mount = txtMountPoint.Text.Trim();
            if (!string.IsNullOrEmpty(mount))
                MessageBox.Show(RunCommandOutput($"wsl --unmount \"{mount}\""), "Unmounted");
        }

        private void InstallWslu_Click(object sender, RoutedEventArgs e)
        {
            if (cbExportDistro.SelectedItem is string distro)
            {
                string command = $"wsl -d {distro} --exec bash -c \"sudo apt-get update && sudo apt-get install -y wslu\"";
                string result = RunCommandOutput(command);
                MessageBox.Show(result, "wslu Installation");
            }

        }

        private async void LaunchDistro_Click(object sender, RoutedEventArgs e)
        {
            if (cbExportDistro.SelectedItem is string distro)
            {
                // Check if a WSL session is already active
                if (!string.IsNullOrEmpty(activeDistro))
                {
                    var result = MessageBox.Show(
                        $"A WSL session for '{activeDistro}' is already active. Do you want to terminate it and launch '{distro}'?",
                        "Active Session Detected",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );

                    if (result == MessageBoxResult.No)
                    {
                        return; // Do not launch the new session
                    }

                    // Terminate the active session
                    await RunCommandOutputAsync($"wsl --terminate {activeDistro}");
                }

                // Launch the new distribution
                string output = await RunCommandOutputAsync($"wsl -d {distro}");
                activeDistro = distro; // Update the active session
                MessageBox.Show(output, "WSL Launch");
            }
        }


    }
}
