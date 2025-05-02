using System.Diagnostics;
using System.IO.Ports;

namespace SoftwareControllablePowerStrip
{
    public class SerialPortCommunicator
    {
        private readonly SerialPort _serialPort;
        public event Action<byte[]> SerialPacketReceived = delegate { };

        public SerialPortCommunicator(SerialPort serialPort)
        {
            _serialPort = serialPort;
        }

        public bool Connect(string comPort)
        {
            _serialPort.PortName = comPort;
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }

            if (_serialPort.IsOpen)
            {
                _ = StartReadingAsync();
            }
            return _serialPort.IsOpen;
        }

        /// <summary>
        /// Disconnects the com port
        /// </summary>
        /// <returns>true if disconnected, false otherwise.</returns>
        public bool Disconnect()
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.Close();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error closing COM port {_serialPort.PortName}. {ex.Message}");
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
            byte[] buffer = new byte[1024];

            try
            {
                while (_serialPort.IsOpen)
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
