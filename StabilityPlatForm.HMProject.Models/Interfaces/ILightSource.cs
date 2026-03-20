using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface ILightSource
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
        /// 停止设备
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> StopWork();
    }
}
