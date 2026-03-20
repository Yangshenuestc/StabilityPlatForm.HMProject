using StabilityPlatForm.HMProject.Models.DataStructure;

namespace StabilityPlatForm.HMProject.BusinessLogicLayer
{
    public class IvCurveAnalyzer
    {
        /// <summary>
        /// 从原始 V 和 I 数组中提取光伏参数
        /// </summary>
        /// <param name="voltage">电压数组 (V)</param>
        /// <param name="current">电流数组 (A)</param>
        /// <param name="areaCm2">器件有效面积 (cm2)</param>
        public PvMeasurementData Analyze(double[] voltage, double[] current, double areaCm2)
        {
            var data = new PvMeasurementData();
            int n = voltage.Length;

            // 1. 将电流(mA)转换为电流密度 J (mA/cm2)
            double[] J = new double[n];
            for (int i = 0; i < n; i++)
            {
                J[i] = (current[i] * 1000.0) / areaCm2;
            }

            // 2. 提取 Jsc (寻找 V 最接近 0 的点)
            int zeroVoltageIndex = 0;
            double minV = double.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (Math.Abs(voltage[i]) < minV)
                {
                    minV = Math.Abs(voltage[i]);
                    zeroVoltageIndex = i;
                }
            }
            data.Jsc = Math.Abs(J[zeroVoltageIndex]);

            // 3. 提取 Voc (线性插值寻找 J 过零点)
            data.Voc = 0;
            for (int i = 0; i < n - 1; i++)
            {
                // 判断符号是否改变（穿过 X 轴）
                if (J[i] * J[i + 1] <= 0)
                {
                    // 线性插值公式求精确过零电压
                    double deltaJ = J[i + 1] - J[i];
                    if (Math.Abs(deltaJ) < 1e-9) deltaJ = 1e-9; // 防止除零
                    data.Voc = voltage[i] - J[i] * ((voltage[i + 1] - voltage[i]) / deltaJ);
                    data.Voc = Math.Abs(data.Voc);
                    break;
                }
            }

            // 4. 计算 Pmax 和 Vmpp (修复：仅在 0 到 Voc 的发电区间内寻找)
            double maxP = 0;
            data.Vmpp = 0; // 赋初始值
            for (int i = 0; i < n; i++)
            {
                // 仅在正电压且小于等于 Voc 的区间内寻找
                if (voltage[i] >= 0 && voltage[i] <= data.Voc)
                {
                    double p = Math.Abs(voltage[i] * J[i]);
                    if (p > maxP)
                    {
                        maxP = p;
                        data.Pmax = maxP;
                        data.Vmpp = voltage[i];
                    }
                }
            }

            // 5. 计算 FF (填充因子)
            if (data.Voc > 0 && data.Jsc > 0)
            {
                data.FF = data.Pmax / (data.Voc * data.Jsc);
            }

            // 6. 估算 Rs 和 Rsh (局部斜率的倒数，这里取简单两点法作演示)
            // 实际工程中建议取 Voc 附近 3-5 个点做最小二乘法线性回归
            try
            {
                // Rs 取靠近 Voc 处的斜率倒数，乘以1000将 mA 转换回 A，单位恢复为 Ohm·cm2
                int vocIndex = Array.FindIndex(voltage, v => v >= data.Voc);
                if (vocIndex > 0 && vocIndex < n)
                    data.Rseries = Math.Abs((voltage[vocIndex] - voltage[vocIndex - 1]) / (J[vocIndex] - J[vocIndex - 1])) * 1000.0;

                // Rsh 取靠近 Jsc (V=0) 处的斜率倒数，乘以1000
                if (zeroVoltageIndex > 0 && zeroVoltageIndex < n - 1)
                    data.Rshunt = Math.Abs((voltage[zeroVoltageIndex + 1] - voltage[zeroVoltageIndex - 1]) / (J[zeroVoltageIndex + 1] - J[zeroVoltageIndex - 1])) * 1000.0;
            }
            catch { /* 忽略除零异常等边界问题，设为默认值0 */ }

            return data;
        }
    }
}
