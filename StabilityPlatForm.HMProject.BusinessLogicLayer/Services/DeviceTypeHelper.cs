using StabilityPlatForm.HMProject.Models.Enumeration;
using StabilityPlatForm.HMProject.Models.Hardwcare;

namespace StabilityPlatForm.HMProject.BusinessLogicLayer.Services
{
    public static class DeviceTypeHelper
    {
        public static (ElectricalInfo Forward, ElectricalInfo Reverse) ScanDirection(TestParameter config)
        {
            // 1. 正扫逻辑：正式结构 (FormalType) 正扫是从 Initial 到 Terminal，反式结构则相反
            ElectricalInfo forwardInfo = config.DeviceType == DeviceType.FormalType
                ? new ElectricalInfo { MinVoltage = config.InitialVoltage, MaxVoltage = config.TerminalVoltage, VoltageStep = config.VoltageStep }
                : new ElectricalInfo { MinVoltage = config.TerminalVoltage, MaxVoltage = config.InitialVoltage, VoltageStep = config.VoltageStep };

            // 2. 反扫逻辑：无论是正式还是反式，反扫 (Reverse) 永远是从 Terminal 到 Initial
            ElectricalInfo reverseInfo = new ElectricalInfo
            {
                MinVoltage = config.TerminalVoltage,
                MaxVoltage = config.InitialVoltage,
                VoltageStep = config.VoltageStep
            };

            // 返回元组
            return (forwardInfo, reverseInfo);
        }
    }
}
