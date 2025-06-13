using SoftwareControllablePowerStrip;

namespace Scps_Control_App.Models
{
    public class MainWindowModel
    {
        private readonly ScpsController _scpsController;
        public MainWindowModel()
        {
            _scpsController = new ScpsController();
        }

        public async Task<bool> ConnectAsync(string comPort)
        {
            return await _scpsController.ConnectAndTestAsync(comPort);
        }

		public async Task<bool?> GetStateAsync()
		{
			return await _scpsController.GetStateAsync();
		}

		public async Task<bool> PowerOffAsync()
        {
            return await _scpsController.PowerOffAsync();
        }

        public async Task<bool> PowerOnAsync()
        {
            return await _scpsController.PowerOnAsync();
        }
    }
}
