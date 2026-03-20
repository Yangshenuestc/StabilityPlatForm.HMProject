using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface IBiasSourceTable
    {
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; }
        /// <summary>
        /// 是否正在输出偏压
        /// </summary>
        public bool IsOutputting { get; }
        /// <summary>
        /// 当前所加偏压值
        /// </summary>
        public BiasInfo CurrentBiasInfo { get; }
        /// <summary>
        /// 停止设备并复位
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StopWork();
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
