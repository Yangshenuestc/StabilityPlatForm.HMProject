using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class LightSource : ILightSource
    {
        public LightSource() { }
        private bool _isConnected = true; // 模拟默认已连接
        private bool _isWorking = false;
        private LightInfo _currentInfo = new LightInfo { LightTime = 0, DarkTime = 0 };

        public bool IsConnected => _isConnected;

        public bool IsWorking => _isWorking;

        public LightInfo CurrentLightInfo => _currentInfo;

        public MethodResult<bool> Close() =>MethodResult<bool>.Success(true);

        public MethodResult<bool> SetLightControl(LightInfo lightInfo)=> MethodResult<bool>.Success(true);

        public MethodResult<bool> Start()=> MethodResult<bool>.Success(true);

        public MethodResult<bool> StartWork()=>MethodResult<bool>.Success(true);

        public MethodResult<bool> StopWork()=>MethodResult<bool>.Success(true);
    }
}
