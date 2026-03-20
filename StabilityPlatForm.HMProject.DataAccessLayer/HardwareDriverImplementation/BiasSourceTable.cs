using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class BiasSourceTable : IBiasSourceTable
    {
        public MethodResult<bool> Close() => MethodResult<bool>.Success(true);
        public MethodResult<bool> Start() => MethodResult<bool>.Success(true);
        public MethodResult<bool> StopTest() => MethodResult<bool>.Success(true);
        public MethodResult<bool> TestMode_Vmpp(BiasInfo biasInfo) => MethodResult<bool>.Success(true);
    }
}
