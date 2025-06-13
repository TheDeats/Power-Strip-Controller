using System.Diagnostics;
using System.IO.Ports;

namespace SoftwareControllablePowerStrip
{
    public class SerialPortCommunicator
    {
        private readonly SerialPort _serialPort;
        public event Action<byte[]> SerialPacketReceived = delegate { };
        private const int SerialPortBufferSize = 1024;

        public string PortName
        {
            get => _serialPort.PortName; 
        }

        public SerialPortCommunicator()
        {
            _serialPort = new SerialPort()
            {
                BaudRate = 9600,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = true
                // RTS = false for Arduino Nano Every
                // Enabling Dtr causes Arduino Every to reset on connection
                // DTR must be enabled for first time connection after Arduino initial boot
                // then you can leave DTR = false for subsequent connections to avoid Arduino reboot
            };
        }

        /// <summary>
        /// Connects the com port
        /// </summary>
        /// <param name="comPort">the com port to connect to ex: COM3</param>
        /// <returns>True if the serial port opened successfully, false otherwise</returns>
        public bool Connect(string portName, bool avoidReset)
        {
            if (!_serialPort.IsOpen)
            {
				_serialPort.PortName = portName;
                _ = avoidReset ? _serialPort.DtrEnable = false : _serialPort.DtrEnable = true;
                _serialPort.Open();

				if (_serialPort.IsOpen)
				{
					_ = StartReadingAsync();
				}
			}

            return _serialPort.IsOpen;
        }

        /// <summary>
        /// Disconnects the com port
        /// </summary>
        /// <returns>true if disconnected, false otherwise.</returns>
        public bool Disconnect()
        {
			if (_serialPort.IsOpen)
			{
				_serialPort.Close();
			}

			return !_serialPort.IsOpen;
        }

        /// <summary>
        /// Sends the command data.
        /// </summary>
        /// <param name="data">The command data.</param>
        public void SendCommand(byte[] data)
        {
            if (data.Length == 0 || _serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            _serialPort.Write(data, 0, data.Length);
        }

        /// <summary>
        /// Sends the command data asynchronously.
        /// </summary>
        /// <param name="data">The command data.</param>
        public async Task SendCommandAsync(byte[] data)
        {
            if (data.Length == 0 || _serialPort == null || !_serialPort.IsOpen)
            {
                return;
            }

            await this._serialPort.BaseStream.WriteAsync(data, 0, data.Length).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads all data from the ComPort as it is received.
        /// </remarks>
        private async Task StartReadingAsync()
        {
            byte[] buffer = new byte[SerialPortBufferSize];

            try
            {
                while (_serialPort!.IsOpen)
                {
                    int actualLength;

                    try
                    {
                        actualLength = await _serialPort.BaseStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (_serialPort?.IsOpen ?? false)
                        {
                            Debug.WriteLine($"Error receiving data on port {_serialPort.PortName}. {ex.Message}");
                        }
                        continue;
                    }

                    byte[] received = new byte[actualLength];
                    Buffer.BlockCopy(buffer, 0, received, 0, actualLength);
                    SerialPacketReceived?.Invoke(received);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in serial port data received handler for port {_serialPort.PortName}. {ex.Message}");
                _ = StartReadingAsync();
            }
        }
    }
}
