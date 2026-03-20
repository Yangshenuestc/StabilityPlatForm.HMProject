namespace StabilityPlatForm.HMProject.Models.Hardwcare
{
    public class TemperatureInfo
    {
        //设定温度
        public double TargetT { get; set; }

        //当前温度
        public double CurrentT { get; set; }

        //低温
        public double LowT { get; set; }

        //高温
        public double HighT { get; set; }

        //升温时间
        public double HeatingTime { get; set; }

        //降温时间
        public double CoolingTime { get; set; }
    }
}
