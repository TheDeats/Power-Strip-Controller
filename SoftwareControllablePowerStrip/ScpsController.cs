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
        private const int RebootDelayMs = 100;

        public bool IsConnected { get; set; }

        public static string Name => "Software Controllable Power Strip";

        public ScpsController()
        {
			_serialPortComms = new SerialPortCommunicator();
			_responseStopwatch = new Stopwatch();
        }

        /// <summary>
        /// Combine two arrays into a new array
        /// </summary>
        /// <param name="arr1">the first array</param>
        /// <param name="arr2">the second array</param>
        /// <returns>The combined array</returns>
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

        /// <summary>
        /// Connect to the com port
        /// </summary>
        /// <param name="comPort">the com port to connect to</param>
        /// <param name="avoidReset">attempt to avoid resetting the arduino during connection</param>
        /// <returns>True if successfully connected to the com port</returns>
        public bool Connect(string comPort, bool avoidReset = true)
        {
            if (!IsConnected)
            {
				IsConnected = _serialPortComms.Connect(comPort, avoidReset);
			}
            return IsConnected && _serialPortComms.PortName == comPort;
        }

		/// <summary>
		/// Connect to the com port asynchronously
		/// </summary>
		/// <param name="comPort">the com port to connect to</param>
		/// <param name="avoidReset">attempt to avoid resetting the arduino during connection</param>
		/// <returns>True if successfully connected to the com port</returns>
		public async Task<bool> ConnectAsync(string comPort, bool avoidReset = true)
        {
            if (!IsConnected)
            {
				await Task.Run(() =>
				{
					IsConnected = _serialPortComms.Connect(comPort, avoidReset);
				});
			}
			
            return IsConnected && _serialPortComms.PortName == comPort;
        }

		/// <summary>
		/// Connect to the com port and test the connection
		/// </summary>
		/// <param name="comPort">the com port to connect to</param>
		/// <param name="avoidReset">attempt to avoid resetting the arduino during connection</param>
		/// <returns>True if the connection test returns the correct response</returns>
		public bool ConnectAndTest(string comPort, bool avoidReset = true)
        {
			bool success = Connect(comPort, avoidReset);
            if (success)
            {
				if (!avoidReset)
				{
					Thread.Sleep(RebootDelayMs); // needed for connection after Arduino initial boot when DTR = true. It causes a reboot so we gotta wait
				}
				success = VerifyConnection();

				if (!success && avoidReset)
				{
                    Disconnect();
					success = Connect(comPort, false);
					if (success)
					{
						Thread.Sleep(RebootDelayMs); // needed for connection after Arduino initial boot when DTR = true. It causes a reboot so we gotta wait
						success = VerifyConnection();
					}
				}
			}
			
            return success;
        }

		/// <summary>
		/// Connect to the com port asynchronously and test the connection
		/// </summary>
		/// <param name="comPort">the com port to connect to</param>
		/// <param name="avoidReset">attempt to avoid resetting the arduino during connection</param>
		/// <returns>True if the connection test returns the correct response</returns>
		public async Task<bool> ConnectAndTestAsync(string comPort, bool avoidReset = true)
        {
			bool success = await ConnectAsync(comPort, avoidReset);
            if (success)
            {
                if (!avoidReset)
                {
					await Task.Delay(RebootDelayMs); // needed for connection after Arduino initial boot when DTR = true. It causes a reboot so we gotta wait
				}
				success = await VerifyConnectionAsync();

				if (!success && avoidReset)
				{
                    await DisconnectAsync();
					success = await ConnectAsync(comPort, false);
					if (success)
					{
						await Task.Delay(RebootDelayMs); // needed for connection after Arduino initial boot when DTR = true. It causes a reboot so we gotta wait
						success = await VerifyConnectionAsync();
					}
				}
			}
			
            return success;
        }

        /// <summary>
        /// Disconnect from the com port
        /// </summary>
        /// <returns>true if successfully disconnected from the com port</returns>
        public bool Disconnect()
        {
            if (IsConnected)
            {
                if (_serialPortComms.Disconnect())
                {
                    IsConnected = false;
                    return true;
                }
			}
            return false;
        }

		/// <summary>
		/// Disconnect from the com port asynchronously
		/// </summary>
		/// <returns>true if successfully disconnected from the com port</returns>
		public async Task<bool> DisconnectAsync()
		{
            bool success = false;
			if (IsConnected)
			{
                await Task.Run(() =>
                {
					if (_serialPortComms.Disconnect())
                    {
                        IsConnected = false;
                        success = true;
					}
                });
			}
			return success;
		}

        /// <summary>
        /// Get the state of the power port
        /// </summary>
        /// <returns>True if powered ON, False if OFF</returns>
        public bool? GetState()
        {
			string response = SendCommandAndWaitForResponse(ScpsConstants.GetState);
			if (response.Contains(ScpsConstants.GetState))
			{
                if (response.Contains("true"))
                {
                    return true;
                }
                return false;
			}
            return null;
		}

		/// <summary>
		/// Get the state of the power port asynchronously
		/// </summary>
		/// <returns>True if powered ON, False if OFF</returns>
		public async Task<bool?> GetStateAsync()
		{
            bool? state = null;
            await Task.Run(() =>
            {
				string response = SendCommandAndWaitForResponse(ScpsConstants.GetState);
				if (response.Contains(ScpsConstants.GetState))
				{
					if (response.Contains("true"))
					{
						state = true;
					}
                    else
                    {
						state = false;
					}
				}
			});
			return state;
		}

		/// <summary>
		/// Power Off Port
		/// </summary>
		/// <returns>true if received correct power off response, otherwise false</returns>
		public bool PowerOff()
		{
			string response = SendCommandAndWaitForResponse(ScpsConstants.PowerOff);
			if (response.Contains(ScpsConstants.PowerOff))
			{
				return true;
			}
			return false;
		}


		/// <summary>
		/// Power Off Port asynchronously
		/// </summary>
		/// <returns>true if received correct power off response, otherwise false</returns>
		public async Task<bool> PowerOffAsync()
        {
            string response = await SendCommandAndWaitForResponseAsync(ScpsConstants.PowerOff);
            if (response.Contains(ScpsConstants.PowerOff))
            {
                return true;
            }
            return false;
        }

		/// <summary>
		/// Power On Port
		/// </summary>
		/// <returns>true if received correct power on response, otherwise false</returns>
		public bool PowerOn()
		{
			string response = SendCommandAndWaitForResponse(ScpsConstants.PowerOn);
			if (response.Contains(ScpsConstants.PowerOn))
			{
				return true;
			}
			return false;
		}

		/// <summary>
		/// Power On Port asynchronously
		/// </summary>
		/// <returns>true if received correct power on response, otherwise false</returns>
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
        /// <returns>the response received</returns>
        public string SendCommandAndWaitForResponse(string command)
        {
            if (!IsConnected)
            {
                return string.Empty;
            }
            _response = [];
            string responseText = string.Empty;
            bool responseReceived = false;

            Action<byte[]> handler = (byte[] data) =>
            {
                try
                {
                    _response = CombineArrays(_response, data);
                    responseText = Encoding.ASCII.GetString(_response);
                    if (responseText.Contains(ScpsConstants.EndOfMessage))
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
                byte[] commandBytes = Encoding.ASCII.GetBytes(command + " " + ScpsConstants.EndOfMessage);
                _serialPortComms.SendCommand(commandBytes);

                // Wait for responses
                _responseStopwatch = Stopwatch.StartNew();
                while (!responseReceived && _responseStopwatch.ElapsedMilliseconds < ResponseWaitTimeMsec) { }

                if (!responseReceived)
                {
                    throw new Exception($"Response never received from SCPS. {responseText}");
                }

                return responseText;
            }
            finally
            {
                _responseStopwatch.Stop();
                _isBusy = false;
				_serialPortComms.SerialPacketReceived -= handler;
			}
        }

        /// <summary>
        /// Send the command and wait for a response asynchronously
        /// </summary>
        /// <param name="command">The command to send</param>
        /// <returns>the response received</returns>
        public async Task<string> SendCommandAndWaitForResponseAsync(string command)
        {
			if (!IsConnected)
			{
				return string.Empty;
			}
			_response = [];
            string responseText = string.Empty;
            bool responseReceived = false;

            Action<byte[]> handler = (byte[] data) =>
            {
                try
                {
                    _response = CombineArrays(_response, data);
                    responseText = Encoding.ASCII.GetString(_response);
                    if (responseText.Contains(ScpsConstants.EndOfMessage))
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
                byte[] commandBytes = Encoding.ASCII.GetBytes(command + " " + ScpsConstants.EndOfMessage);
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
        /// Sends a test command to see if it will respond with the correct message
        /// </summary>
        /// <returns>true if we receive the correct response</returns>
        public bool VerifyConnection()
        {
            try
            {
                string response = SendCommandAndWaitForResponse(ScpsConstants.ConnectionTest);
				if (response.Contains(ScpsConstants.ConnectionTestResponse))
				{
					return true;
				}
				return false;
			}
            catch
            {
				return false;
			}
		}

		/// <summary>
		/// Sends a test command to see if it will respond with the correct message asynchronously
		/// </summary>
		/// <returns>true if we receive the correct response</returns>
		public async Task<bool> VerifyConnectionAsync()
        {
			try
			{
				string response = await SendCommandAndWaitForResponseAsync(ScpsConstants.ConnectionTest);
				if (response.Contains(ScpsConstants.ConnectionTestResponse))
				{
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
        }
    }
}
