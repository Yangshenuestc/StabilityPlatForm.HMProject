using StabilityPlatForm.HMProject.Models.Enumeration;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class Semiconductor : ISemiconductor
    {
        public Semiconductor() { }

        private TemperatureInfo _currentTemperature = new TemperatureInfo();
        private bool _isConnected = true;
        public TemperatureInfo CurrentTemperature => _currentTemperature;

        public bool IsConnected => _isConnected;

        public MethodResult<bool> Close()=>MethodResult<bool>.Success(true);

        public MethodResult<TemperatureInfo> GetTemperatureAsync()=>MethodResult<TemperatureInfo>.Success(_currentTemperature);

        public MethodResult<bool> Start()=>MethodResult<bool>.Success(true);

        public MethodResult<bool> StopWork()=>MethodResult<bool>.Success(true);

        public MethodResult<bool> TemperatureControl(TemperatureInfo temperatureInfo, TestMode testMode)=>MethodResult<bool>.Success(true);
    }
}
