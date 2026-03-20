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
            // 计算步数与电压数组
            double step = electricalInfo.VoltageStep > 0 ? electricalInfo.VoltageStep : 0.01;
            int points = (int)(Math.Abs(electricalInfo.MaxVoltage - electricalInfo.MinVoltage) / step) + 1;
            if (points <= 0) points = 100;

            double[] v = new double[points];
            double[] c = new double[points];

            double startV = electricalInfo.MinVoltage;
            double endV = electricalInfo.MaxVoltage;

            // 确定真实的步长正负号
            step = (endV >= startV) ? Math.Abs(step) : -Math.Abs(step);

            // 判断扫描方向：电压从高到低为反扫(Reverse)，反之为正扫(Forward)
            // 钙钛矿器件通常在反扫时表现出更高的 PCE
            bool isReverseScan = step < 0;

            Random rnd = new Random();

            // =========================================================================
            // 真实单结钙钛矿太阳能电池 单二极管模型参数 
            // (假设器件有效面积约为 0.06 cm2，对应 Jsc ~ 24 mA/cm2，Voc ~ 1.14V)
            // =========================================================================
            double IL = 0.00144;        // 光生电流 (A)
            double n = 1.5;             // 二极管理想因子 (钙钛矿缺陷复合通常在 1.3~2.0 之间)
            double Vt = 0.02585;        // 常温 (300K) 下的热电压 (V)
            double I0 = 2e-16;          // 反向饱和漏电流 (A)
            double Rs = 35.0;           // 串联电阻 (欧姆) - 影响曲线在 Voc 处的弯曲度 (FF)
            double Rsh = 20000.0;       // 并联电阻 (欧姆) - 影响曲线在 Jsc 处的倾斜度

            // 【引入迟滞效应模拟】
            // 模拟离子迁移或界面电荷堆积导致的迟滞：正扫时复合加剧、串阻变大
            if (!isReverseScan)
            {
                Rs += 15.0;             // 正扫时表观串联电阻增大，导致填充因子(FF)明显降低
                I0 *= 2.5;              // 正扫时复合增加，导致开路电压(Voc)略微降低
            }

            for (int j = 0; j < points; j++)
            {
                v[j] = startV + j * step;
                double V_current = v[j];

                // 因为真实的单二极管公式是一个隐函数: 
                // I = I0 * [exp((V + I*Rs) / (n*Vt)) - 1] + (V + I*Rs)/Rsh - IL
                // 无法直接得出 I = ...，所以这里使用牛顿-拉弗森(Newton-Raphson)迭代法求解

                // 1. 构造函数 f(I) = I0 * [exp((V + I*Rs)/(n*Vt)) - 1] + (V + I*Rs)/Rsh - IL - I = 0
                // 2. 猜测一个初始电流 (直接使用光生电流的相反数，即第四象限)
                double I_guess = -IL;

                for (int iter = 0; iter < 6; iter++) // 通常 5-6 次迭代即可高精度收敛
                {
                    double exponent = (V_current + I_guess * Rs) / (n * Vt);

                    // 防御性编程：防止在高电压下 exp(100+) 导致数值溢出 (NaN)
                    if (exponent > 80) exponent = 80;

                    double expTerm = Math.Exp(exponent);

                    // f(I) 原函数
                    double f_I = I0 * (expTerm - 1) + (V_current + I_guess * Rs) / Rsh - IL - I_guess;
                    // f'(I) 导函数
                    double f_prime_I = I0 * (Rs / (n * Vt)) * expTerm + (Rs / Rsh) - 1.0;

                    // 牛顿迭代公式：I_new = I_old - f(I_old) / f'(I_old)
                    I_guess = I_guess - f_I / f_prime_I;
                }

                // 模拟高精度源表 (如 Keithley 2400) 的底噪，约几十纳安级别的随机波动
                double noise = (rnd.NextDouble() - 0.5) * 8e-8;

                // 最终得到的即为高度逼近真实物理特性的电流点
                c[j] = I_guess + noise;
            }

            return new IVData { Voltage = v, Current = c };
        }
    }
}
