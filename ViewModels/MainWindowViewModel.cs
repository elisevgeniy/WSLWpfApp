using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WSLWpfApp.Models;

namespace WSLWpfApp.ViewModels
{
    // Must be partial for Source Generator to work when using RelayCommand Attribute
    public partial class MainWindowViewModel : ObservableObject
    {
        private static readonly char[] NewLineChars = new char[] { '\r', '\n', };

        public ObservableCollection<DistroModel> Distros { get; set; } = new ObservableCollection<DistroModel>();
        public MainWindowViewModel()
        {
        }

        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public void LoadDistros()
        {
            Distros.Clear();
            var output = RunCommandOutput("wsl -l");
            var lines = output
                              .Skip(1)
                              .Select(line => line.Trim().Split(' ')[0])
                              .Where(name => !string.IsNullOrWhiteSpace(name))
                              .ToList();
            Distros.AddRange(GetDistrosWithStatus());
        }

        private bool CanLaunchWSL()
        {
            return Distros.Count > 0;
        }



        [RelayCommand()]
        public void ResetError()
        {
            ErrorMessage = string.Empty;
        }

        [RelayCommand(CanExecute = nameof(CanLaunchWSL))]
        public void LaunchWsl(string distro)
        {

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "wsl.exe",
                    Arguments = $"-d {distro}",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };
                var process = Process.Start(psi);
                process.WaitForExit();
                GetDistrosWithStatus();
            }
            catch (Exception ex)
            {
                ErrorMessage=ex.Message;
            }
        }

        [RelayCommand]
        public void TerminateDistro(string distro)
        {
            RunCommandOutput($"wsl --terminate {distro}");
            GetDistrosWithStatus();
        }

        [RelayCommand]
        public void SetDefaultDistro(string distro)
        {
            RunCommandOutput($"wsl --setdefault {distro}");
            GetDistrosWithStatus();
        }

        private IEnumerable<DistroModel> GetDistrosWithStatus()
        {
            IEnumerable<DistroModel> result = new List<DistroModel>();
            var output = RunCommandOutput("wsl -l -v");
            result = output.Skip(1)
                .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .Select(x => x.Length > 3 ? new DistroModel(name: x[1], x[3], x[2], true) : new DistroModel(name: x[0], x[2], x[1], false));

            return result;
        }

        private IEnumerable<string> RunCommandOutput(string command)
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
                string[] output = process.StandardOutput.ReadToEnd().Replace("\0", string.Empty)
                .Split(NewLineChars, StringSplitOptions.RemoveEmptyEntries);
                process.WaitForExit();
                return output;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            return new string[] { };
        }
    }
}
