namespace StabilityPlatForm.HMProject.BusinessLogicLayer.Services
{
    /// <summary>
    /// 业务层向 UI 层汇报进度的信息包
    /// </summary>
    public class TestProgressInfo
    {
        public string StatusMessage { get; set; }
        public TimeSpan RunningTime { get; set; }
        public double CurrentTemperature { get; set; }

        // 如果触发衰减预警，设为 true
        public bool IsT80Warning { get; set; }

        //用于实时图表的数据
        public string DeviceId { get; set; }           // 刚刚测完的器件号 (例如 "1-1")
        public double NewPceValue { get; set; }        // 提取出的 PCE 值
        public bool IsForwardScan { get; set; }        // 区分正反扫
    }
}
