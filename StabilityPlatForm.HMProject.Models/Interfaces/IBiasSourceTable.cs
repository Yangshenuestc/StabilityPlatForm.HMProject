using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface IBiasSourceTable
    {
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
        /// 切换为Vmpp测试通道:电压源，电流测量（V_target = V_mpp）
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> TestMode_Vmpp(BiasInfo biasInfo);
        /// <summary>
        /// 停止测试，并且复位相应源表
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StopTest();
    }
}
