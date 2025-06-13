using CommunityToolkit.Mvvm.Input;
using Scps_Control_App.Models;
using System.ComponentModel;
using System.Management;
using System.Text.RegularExpressions;
using System.Windows;

namespace Scps_Control_App.ViewModels
{
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
		private const string CycleText = "Cycle";
		public const int MinCycleDelay = 1000;
        private const string OFF = "OFF";
        private const string ON = "ON";
        public const int ScrollWheelCycleDelayIncement = 100;
		private const string StopText = "Stop";

		private readonly MainWindowModel _model;
        private List<string> _availableComPorts;
        private string _cycleButtonText = CycleText;
        private int _cycleDelay = 2000;
		private bool _isConnected;
		private bool _isNotConnected = true;
		private bool? _port1State = false;
		private string _selectedComPort = string.Empty;
        private string _onOffButtonText = ON;

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

        public string CycleButtonText
        {
            get => _cycleButtonText;
            set
            {
                _cycleButtonText = value;
                RaisePropertyChanged(nameof(CycleButtonText));
            }
		}

        public int CycleDelay
        {
            get => _cycleDelay;
            set
            {
                _cycleDelay = value;
                RaisePropertyChanged(nameof(CycleDelay));
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

        public string OnOffButtonText
        {
            get => _onOffButtonText;
            set
            {
                _onOffButtonText = value;
                RaisePropertyChanged(nameof(OnOffButtonText));
			}
		}


		public bool? Port1State
        {
            get => _port1State;
            set
            {
                _port1State = value;
                RaisePropertyChanged(nameof(Port1State));
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

		[RelayCommand]
		private async Task CyclePower()
        {
            if (CycleDelay < MinCycleDelay)
            {
                CycleDelay = MinCycleDelay;
            }

            if (_port1State != null)
            {
                if (CycleButtonText == CycleText)
                {
                    CycleButtonText = StopText;
                    _ = StartCycle();
				}
                else
                {
                    CycleButtonText = CycleText;
                }
			}
            await Task.CompletedTask;
        }

		[RelayCommand]
		private async Task GetState()
		{
            try
            {
                await GetStateAsync();
			}
            catch (Exception ex)
            {
                MessageBox.Show($"{ex.Message} {ex.InnerException?.Message}");
            }
		}

		private async Task StartCycle()
        {
			while (CycleButtonText == StopText)
			{
                if (!(bool)Port1State!)
                {
					if (await _model.PowerOnAsync())
					{
						Port1State = true;
						OnOffButtonText = OFF;
					}
				}
                else
                {
					if (await _model.PowerOffAsync())
					{
						Port1State = false;
						OnOffButtonText = ON;
					}
				}
                await Task.Delay(CycleDelay);
			}
		}

        public async Task<bool?> GetStateAsync()
        {
			Port1State = await _model.GetStateAsync();
			if (Port1State != null)
			{
				_ = (bool)Port1State ? OnOffButtonText = OFF : OnOffButtonText = ON;
			}
            return Port1State;
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
				CycleButtonText = CycleText;
				if (await _model.PowerOffAsync())
                {
					Port1State = false;
                    OnOffButtonText = ON;
				}
            }
            catch (Exception ex)
            {
				MessageBox.Show($"{ex.Message} {ex.InnerException?.Message}");
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
				CycleButtonText = CycleText;
				if (await _model.PowerOnAsync())
                {
					Port1State = true;
                    OnOffButtonText = OFF;
				}
            }
            catch(Exception ex)
            {
				MessageBox.Show($"{ex.Message} {ex.InnerException?.Message}");
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
            try
            {
				IsConnected = await _model.ConnectAsync(SelectedComPortConnectableName);
                if (!IsConnected)
                {
                    MessageBox.Show($"Failed to connect to {SelectedComPortConnectableName}");
				}
                else
                {
					await GetStateAsync();
				}
			}
            catch (Exception ex)
            {
				MessageBox.Show($"{ex.Message} {ex.InnerException?.Message}");
			}
        }

        [RelayCommand]
        private async Task TogglePower()
        {
            try
            {
				if (Port1State == true)
				{
					await PowerOff();
				}
				else if (Port1State == false)
				{
					await PowerOn();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"{ex.Message} {ex.InnerException?.Message}");
			}
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
