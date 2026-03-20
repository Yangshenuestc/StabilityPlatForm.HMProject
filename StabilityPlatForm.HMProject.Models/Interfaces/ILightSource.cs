using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface ILightSource
    {
        /// <summary>
        /// 是否连接
        /// </summary>
        public bool IsConnected { get; }
        /// <summary>
        /// 是否处于工作状态
        /// </summary>
        public bool IsWorking { get; }
        /// <summary>
        /// 当前光照配置
        /// </summary>
        public LightInfo CurrentLightInfo { get; }
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
        /// 设定光照条件
        /// </summary>
        /// <param name="temperatureInfo"></param>
        /// <returns></returns>
        public MethodResult<bool> SetLightControl(LightInfo lightInfo);
        /// <summary>
        /// 按设定光照条件开始运行
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StartWork();
        /// <summary>
        /// 停止设备，并复位
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StopWork();
    }
}
