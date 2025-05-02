using CommunityToolkit.Mvvm.Input;
using Scps_Control_App.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace Scps_Control_App.ViewModels
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly MainWindowModel _model;
        private List<string> _availableComPorts;
        private string _selectedComPort = string.Empty;
        private bool _isConnected;
        private bool _isNotConnected = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public List<string> AvailableComPorts
        {
            get
            {
                return _availableComPorts;
            }
            set
            {
                _availableComPorts = value;
                RaisePropertyChanged(nameof(AvailableComPorts));
            }
        }

        public bool IsConnected {
            get => _isConnected;
            set
            {
                _isConnected = value;
                IsNotConnected = !value;
                RaisePropertyChanged(nameof(IsConnected));
            }
        }

        public bool IsNotConnected
        {
            get => _isNotConnected;
            set
            {
                _isNotConnected = value;
                RaisePropertyChanged(nameof(IsNotConnected));
            }
        }

        public string SelectedComPort
        {
            get => _selectedComPort;
            set
            {
                if (_selectedComPort != value)
                {
                    _selectedComPort = value;
                    RaisePropertyChanged(nameof(SelectedComPort));
                }
            }
        }

        public string SelectedComPortConnectableName
        {
            get
            {
                if (string.IsNullOrEmpty(SelectedComPort))
                {
                    return string.Empty;
                }
                Match match = Regex.Match(SelectedComPort, @"\((COM\d+)\)");
                if (match.Success)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public MainWindowViewModel()
        {
            _model = new MainWindowModel();
            _availableComPorts = [];
        }

        /// <summary>
        /// Every time the dropdown is opened, update the list of COM ports
        /// to pick from.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task ComPortsDropDownOpened()
        {
            await UpdateAvailableComPorts();
        }

        /// <summary>
        /// Power OFF
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task PowerOff()
        {
            try
            {
                await _model.PowerOffAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Power ON
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task PowerOn()
        {
            try
            {
                await _model.PowerOnAsync();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The name of the property that change.</param>
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// When a COM port is selected, connect to it.
        /// </summary>
        /// <returns></returns>
        [RelayCommand]
        private async Task SelectedComPortChanged()
        {
            IsConnected = await _model.ConnectAsync(SelectedComPortConnectableName);
        }

        private async Task UpdateAvailableComPorts()
        {
            await Task.Run(() =>
            {
                List<string> ports = new List<string>();
                var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string pDescription = obj["Caption"].ToString(); // e.g. "USB Serial Device (COM9)"
                    ports.Add(pDescription);
                }
                AvailableComPorts = ports.ToList();
            });
        }
    }
}
