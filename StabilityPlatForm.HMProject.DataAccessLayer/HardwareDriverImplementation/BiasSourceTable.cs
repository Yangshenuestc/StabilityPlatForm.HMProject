using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class BiasSourceTable : IBiasSourceTable
    {
        public BiasSourceTable() { }
        private bool _isConnected = true;
        private bool _isOutputting = false;
        private BiasInfo _currentBiasInfo = new BiasInfo();


        public bool IsConnected => _isConnected;
        public bool IsOutputting => _isOutputting;
        public BiasInfo CurrentBiasInfo => _currentBiasInfo;

        public MethodResult<bool> Close() => MethodResult<bool>.Success(true);
        public MethodResult<bool> Start() => MethodResult<bool>.Success(true);
        public MethodResult<bool> StopTest() => MethodResult<bool>.Success(true);
        public MethodResult<bool> StopWork()=> MethodResult<bool>.Success(true);

        public MethodResult<bool> TestMode_Vmpp(BiasInfo biasInfo) => MethodResult<bool>.Success(true);
    }
}
