namespace StabilityPlatForm.HMProject.Models.Hardwcare
{
    public class ElectricalInfo
    {
        //扫描最小电压
        public double MinVoltage { get; set; }

        //扫描最大电压
        public double MaxVoltage { get; set; }

        //电压步长
        public double VoltageStep { get; set; }
    }
}
