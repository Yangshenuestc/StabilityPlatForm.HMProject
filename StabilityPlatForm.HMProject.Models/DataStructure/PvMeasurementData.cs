namespace StabilityPlatForm.HMProject.Models.DataStructure
{
    /// <summary>
    /// 稳定性测试分析结果结构
    /// </summary>
    public class PvMeasurementData
    {
        public double TimeHours { get; set; }        // time (h)
        public int DeviceNumber { get; set; }        // 测量位置 (如 Left-Device 1=1，Right-Device 9=18)
        public double Jsc { get; set; }              // Jsc (mA/cm2)
        public double Voc { get; set; }              // Voc (V)
        public double FF { get; set; }               // FF
        public double Pmax { get; set; }             // Pmax (mW/cm2)
        public double Vmpp { get; set; }             // Vmpp (V)
        public double Rseries { get; set; }          // Rseries (Ohm/cm2)
        public double Rshunt { get; set; }           // Rshunt (Ohm/cm2)
        public bool SweepDirection { get; set; }     // sweep direction(true表示正扫，false表示反扫)
        public double DelaySeconds { get; set; }     // delay (s)
        public double Temperature { get; set; }      // temperature (度)
    }
}
