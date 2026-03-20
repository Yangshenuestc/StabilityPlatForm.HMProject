using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.Models.Interfaces
{
    public interface IChannelSwitcher
    {
        /// <summary>
        /// 获取当前通道号
        /// </summary>
        public ChannelInfo CurrentChannel { get; }
        /// <summary>
        /// 建立连接，启动切换器
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> Start();
        /// <summary>
        /// 断开连接，清理缓存
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> Close();
        /// <summary>
        /// 切换为指定通道
        /// </summary>
        /// <returns></returns>
        public MethodResult<bool> ChannelSwitch(ChannelInfo channelInfo);
        /// <summary>
        /// 获取当前通道信息
        /// </summary>
        /// <returns></returns>
        public MethodResult<ChannelInfo> GetChannelAsync();
    }
}
