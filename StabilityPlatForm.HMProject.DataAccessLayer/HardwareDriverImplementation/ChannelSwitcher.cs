using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class ChannelSwitcher : IChannelSwitcher
    {
        private ChannelInfo _currentChannel = new ChannelInfo { ChannelNumber = 1 };
        public ChannelInfo CurrentChannel => _currentChannel;

        public MethodResult<bool> ChannelSwitch(ChannelInfo channelInfo)
        {
            _currentChannel = channelInfo;
            return MethodResult<bool>.Success(true);
        }

        public MethodResult<bool> Close() => MethodResult<bool>.Success(true);
        public MethodResult<ChannelInfo> GetChannelAsync() => MethodResult<ChannelInfo>.Success(_currentChannel);
        public MethodResult<bool> Start() => MethodResult<bool>.Success(true);
    }
}
