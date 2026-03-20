using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class LightSource : ILightSource
    {
        public MethodResult<bool> Close() =>MethodResult<bool>.Success(true);

        public MethodResult<bool> SetLightControl(LightInfo lightInfo)=> MethodResult<bool>.Success(true);

        public MethodResult<bool> Start()=> MethodResult<bool>.Success(true);

        public MethodResult<bool> StartWork()=>MethodResult<bool>.Success(true);

        public MethodResult<bool> StopWork()=>MethodResult<bool>.Success(true);
    }
}
