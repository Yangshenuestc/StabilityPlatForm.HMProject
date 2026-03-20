using StabilityPlatForm.HMProject.Models.DataStructure;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.Utility;

namespace StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation
{
    public class SourceTable : ISourceTable
    {
        public MethodResult<bool> Close() => MethodResult<bool>.Success(true);
        public MethodResult<bool> Start() => MethodResult<bool>.Success(true);
        public MethodResult<bool> StopTest() => MethodResult<bool>.Success(true);

        public IVData IVMode(ElectricalInfo electricalInfo)
        {
            // 模拟生成 IV 曲线数据
            double step = electricalInfo.VoltageStep > 0 ? electricalInfo.VoltageStep : 0.01;
            int points = (int)(Math.Abs(electricalInfo.MaxVoltage - electricalInfo.MinVoltage) / step) + 1;
            if (points <= 0) points = 100;

            double[] v = new double[points];
            double[] c = new double[points];

            double startV = electricalInfo.MinVoltage;
            double endV = electricalInfo.MaxVoltage;
            step = (endV >= startV) ? Math.Abs(step) : -Math.Abs(step);

            Random rnd = new Random();

            for (int j = 0; j < points; j++)
            {
                v[j] = startV + j * step;
                // 模拟简单的钙钛矿太阳能电池模型曲线
                double IL = 0.0012; // 模拟光生电流 1.2mA (对于 0.06cm2 面积约 20mA/cm2)
                double I0 = 1e-10;
                double Vt = 0.02585;

                // 加入一点随机噪声使曲线看起来更真实
                double noise = (rnd.NextDouble() - 0.5) * 1e-5;
                c[j] = I0 * (Math.Exp(v[j] / Vt) - 1) - IL + noise;
            }

            return new IVData { Voltage = v, Current = c };
        }
    }
}
