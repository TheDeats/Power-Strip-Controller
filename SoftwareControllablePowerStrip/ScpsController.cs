using System.Diagnostics;
using System.IO.Ports;
using System.Text;

namespace SoftwareControllablePowerStrip
{
    public class ScpsController
    {
        private SerialPortCommunicator _serialPortComms;
        private Stopwatch _responseStopwatch;

        private byte[] _response = [];
        private bool _isBusy = false;
        private const int ResponseWaitTimeMsec = 2000;

        public bool IsConnected { get; set; }

        public static string Name => "Software Controllable Power Strip";

        public ScpsController()
        {
            _serialPortComms = new SerialPortCommunicator(
                new SerialPort()
                {
                    BaudRate = 9600,
                    DataBits = 8,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    RtsEnable = true,
                    //DtrEnable = true 
                    // Opening the USB serial port toggles the DTR (Data Terminal Ready) line. This line is connected to the RESET pin on the Arduino via a capacitor
                    // When the serial port is opened, DTR is pulled low → triggers a hardware reset
                    // Enabling Dtr causes the Arduino to reset on connection
                }
            );
            _responseStopwatch = new Stopwatch();
        }

        private static byte[] CombineArrays(byte[] arr1, byte[] arr2)
        {
            if (arr1 == null || arr1.Length < 1)
            {
                return arr2;
            }
            else if (arr2 == null || arr2.Length < 1)
            {
                return arr1;
            }
            byte[] combined = new byte[arr1.Length + arr2.Length];
            Buffer.BlockCopy(arr1, 0, combined, 0, arr1.Length);
            Buffer.BlockCopy(arr2, 0, combined, arr1.Length, arr2.Length);
            return combined;
        }

        public bool Connect(string comPort)
        {
            IsConnected = _serialPortComms.Connect(comPort);
            return IsConnected;
        }

        public async Task<bool> ConnectAsync(string comPort)
        {
            await Task.Run(() =>
            {
                IsConnected = _serialPortComms.Connect(comPort);
            });
            return IsConnected;
        }

        public bool ConnectAndTest(string comPort)
        {
            bool success = Connect(comPort);
            if (success)
            {
                success = VerifyConnection();
            }
            return success;
        }

        public async Task<bool> ConnectAndTestAsync(string comPort)
        {
            bool success = await ConnectAsync(comPort);
            if (success)
            {
                success = await VerifyConnectionAsync();
            }
            return success;
        }

        public async Task<bool> PowerOffAsync()
        {
            string response = await SendCommandAndWaitForResponseAsync(ScpsConstants.PowerOff);
            if (response.Contains(ScpsConstants.PowerOff))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> PowerOnAsync()
        {
            string response = await SendCommandAndWaitForResponseAsync(ScpsConstants.PowerOn);
            if (response.Contains(ScpsConstants.PowerOn))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Send the command and wait for a response
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <returns>async Task</returns>
        /// <exception cref="Exception"></exception>
        public string SendCommandAndWaitForResponse(string command)
        {
            _response = [];
            string responseText = string.Empty;
            bool responseReceived = false;

            Action<byte[]> handler = (byte[] data) =>
            {
                try
                {
                    _response = CombineArrays(_response, data);
                    responseText = Encoding.ASCII.GetString(_response);
                    if (responseText.Contains(ScpsConstants.EndOfMessage + Environment.NewLine))
                    {
                        responseReceived = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error occurred while getting the response to the command {command}. {ex.Message}");
                }
            };

            try
            {
                while (_isBusy) { }
                _isBusy = true;
                _serialPortComms.SerialPacketReceived += handler;

                // Send the command
                byte[] commandBytes = Encoding.ASCII.GetBytes(command + Environment.NewLine);
                _serialPortComms.SendCommand(commandBytes);

                // Wait for responses
                _responseStopwatch = Stopwatch.StartNew();
                while (!responseReceived && _responseStopwatch.ElapsedMilliseconds < ResponseWaitTimeMsec) { }

                if (!responseReceived)
                {
                    throw new Exception($"Response never received from SCPS. {responseText}");
                }

                // TODO verify the response is for the command sent

                return responseText;
            }
            finally
            {
                _serialPortComms.SerialPacketReceived -= handler;
                _responseStopwatch.Stop();
                _isBusy = false;
            }
        }

        /// <summary>
        /// Send the command and wait for a response
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <returns>async Task</returns>
        /// <exception cref="Exception"></exception>
        public async Task<string> SendCommandAndWaitForResponseAsync(string command)
        {
            _response = [];
            string responseText = string.Empty;
            bool responseReceived = false;

            Action<byte[]> handler = (byte[] data) =>
            {
                try
                {
                    _response = CombineArrays(_response, data);
                    responseText = Encoding.ASCII.GetString(_response);
                    if (responseText.Contains(Environment.NewLine)) // ScpsConstants.EndOfMessage + 
                    {
                        responseReceived = true;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error occurred while getting the response to the command {command}. {ex.Message}");
                }
            };

            try
            {
                while (_isBusy) { }
                _isBusy = true;
                _serialPortComms.SerialPacketReceived += handler;

                // Send the command
                byte[] commandBytes = Encoding.ASCII.GetBytes(command + Environment.NewLine);
                await _serialPortComms.SendCommandAsync(commandBytes);

                // Wait for responses
                _responseStopwatch = Stopwatch.StartNew();
                while (!responseReceived && _responseStopwatch.ElapsedMilliseconds < ResponseWaitTimeMsec) 
                {
                    await Task.Delay(10); // let other threads run
                }

                if (!responseReceived)
                {
                    throw new Exception($"Response never received from SCPS. {responseText}");
                }

                // TODO verify the response is for the command sent

                return responseText;
            }
            finally
            {
                _serialPortComms.SerialPacketReceived -= handler;
                _responseStopwatch.Stop();
                _isBusy = false;
            }
        }

        public bool VerifyConnection()
        {
            string response = SendCommandAndWaitForResponse(ScpsConstants.ConnectionTest);
            if (response.Contains(ScpsConstants.ConnectionTestResponse))
            {
                return true;
            }
            return false;
        }

        public async Task<bool> VerifyConnectionAsync()
        {
            string response = await SendCommandAndWaitForResponseAsync(ScpsConstants.ConnectionTest);
            if (response.Contains(ScpsConstants.ConnectionTestResponse))
            {
                return true;
            }
            return false;
        }
    }
}
