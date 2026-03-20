using StabilityPlatForm.HMProject.Models.Enumeration;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface ISemiconductor
    {
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; }
        /// <summary>
        /// 获取当前温度
        /// </summary>
        public TemperatureInfo CurrentTemperature { get; }
        /// <summary>
        /// 获取当前温度信息
        /// </summary>
        /// <returns></returns>
        public MethodResult<TemperatureInfo> GetTemperatureAsync();
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
        /// 按指定的模式执行任务
        /// </summary>
        /// <param name="temperatureInfo"></param>
        /// <returns></returns>
        public MethodResult<bool> TemperatureControl(TemperatureInfo temperatureInfo, TestMode testMode);
        /// <summary>
        /// 停止仪器，并复位
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StopWork();
    }
}
