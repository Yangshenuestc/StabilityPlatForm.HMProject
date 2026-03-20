using System.Text;

namespace StabilityPlatForm.HMProject.DataAccessLayer.FileOperations
{
    public class IvDataExportService
    {
        /// <summary>
        /// 将测得的电压和电流数组保存为 CSV 文件
        /// </summary>
        public void SaveRawIvCurve(string filePath, double[] voltage, double[] current)
        {
            // 使用 StringBuilder 提高大量数据拼接的性能
            StringBuilder sb = new StringBuilder();

            // 写入表头
            sb.AppendLine("Voltage(V),Current(A)");

            // 写入数据
            for (int i = 0; i < voltage.Length; i++)
            {
                // 确保即使数组长度不一致也不会报错（防御性编程）
                double iVal = i < current.Length ? current[i] : 0;
                sb.AppendLine($"{voltage[i]},{iVal}");
            }

            // 写入文件
            //File.WriteAllText(filePath, sb.ToString());
        }
    }
}
