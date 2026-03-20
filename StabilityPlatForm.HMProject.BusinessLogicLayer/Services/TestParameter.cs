namespace StabilityPlatForm.HMProject.BusinessLogicLayer.Services
{
    /// <summary>
    /// UI 填写的测试配置参数包
    /// </summary>
    public class TestParameter
    {
        public string CavityName { get; set; }
        public string SavePath { get; set; }
        public string FileName { get; set; }
        public string SelectedTestMode { get; set; }

        // 器件结构
        public bool IsFormalType { get; set; }
        public bool IsInvertedType { get; set; }

        // 核心参数
        public double DeviceArea { get; set; }
        public double InitialVoltage { get; set; }
        public double TerminalVoltage { get; set; }
        public double AppliedVoltage { get; set; }
        public double VoltageStep { get; set; }

        // 环境参数
        public double SunTime { get; set; }
        public double DarkTime { get; set; }
        public double TargetTemperature { get; set; }
        public double CycleLowTemperature { get; set; }
        public double CycleHighTemperature { get; set; }
        public double HeatingTime { get; set; }
        public double CoolingTime { get; set; }

        //断电续跑的起始时间 (小时)
        public double ResumedTimeHours { get; set; }
    }
}
