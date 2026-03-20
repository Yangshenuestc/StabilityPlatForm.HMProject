using StabilityPlatForm.HMProject.Models.DataStructure;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface ISourceTable
    {
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; }
        /// <summary>
        /// 建立连接，启动设备
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> Start();
        /// <summary>
        /// 断开连接，清理缓存
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> Close();
        /// <summary>
        /// IV曲线的测量
        /// </summary>
        /// <returns></returns>
        public IVData IVMode(ElectricalInfo electricalInfo);
        /// <summary>
        /// 停止测试，并且复位相应源表
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StopTest();


    }
}
