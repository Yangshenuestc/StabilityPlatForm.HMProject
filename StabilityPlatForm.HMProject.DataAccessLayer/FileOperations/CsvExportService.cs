using StabilityPlatForm.HMProject.Models.DataStructure;
using System.Text;

namespace StabilityPlatForm.HMProject.DataAccessLayer.FileOperations
{
    /// <summary>
    /// 原始IV数据和稳定性分析结果导出为标准的CSV文件
    /// </summary>
    public class CsvExportService : IDisposable
    {
        // 使用 StringBuilder 作为内存暂存区，速度极快且占用内存小
        private readonly Dictionary<string, StringBuilder> _csvCache = new Dictionary<string, StringBuilder>();
        // 线程锁，确保同一时刻只能有一个线程执行写入操作
        private readonly object _lock = new object();

        /// <summary>
        /// CSV表中写入原始IV数据
        /// </summary>
        public void AppendIvDataToCsv(string filePath, string deviceId, double timeHours, double[] voltage, double[] current)
        {
            lock (_lock)
            {
                if (!_csvCache.ContainsKey(filePath))
                {
                    _csvCache[filePath] = new StringBuilder();
                }

                var sb = _csvCache[filePath];
                bool fileExists = File.Exists(filePath);

                // 如果文件不存在且缓存中也没有数据，说明是第一次写入，需要写入表头 (DeviceID 和 电压数组)
                if (!fileExists && sb.Length == 0)
                {
                    // 使用 "Device_1-1" 的格式，强制 Excel 将其作为纯文本读取，防止变成日期
                    sb.Append($"Device_{deviceId}").Append(",");
                    sb.AppendLine(string.Join(",", voltage));
                }

                // 写入当前行的数据 (时间和电流数组)
                sb.Append(timeHours).Append(",");
                sb.AppendLine(string.Join(",", current));
            }
        }

        /// <summary>
        /// CSV表中写入分析后的稳定性数据
        /// </summary>
        public void AppendResultDataToCsv(string filePath, string deviceId, PvMeasurementData data)
        {
            lock (_lock)
            {
                if (!_csvCache.ContainsKey(filePath))
                {
                    _csvCache[filePath] = new StringBuilder();
                }

                var sb = _csvCache[filePath];
                bool fileExists = File.Exists(filePath);

                // 如果文件不存在且缓存中没有数据，写入表头
                if (!fileExists && sb.Length == 0)
                {
                    sb.AppendLine("Time(h),Jsc(mA/cm2),Voc(V),FF,Pmax,Vmpp,Rse (Ohm/cm2),Rsh (Ohm/cm2),Direction,Delay(s),Tem(℃)");
                }

                string directionStr = data.SweepDirection ? "Forward" : "Reverse";

                // 写入具体数据行
                sb.AppendLine($"{data.TimeHours},{Math.Round(data.Jsc, 4)},{Math.Round(data.Voc, 4)},{Math.Round(data.FF, 4)},{Math.Round(data.Pmax, 4)},{Math.Round(data.Vmpp, 4)},{Math.Round(data.Rseries, 2)},{Math.Round(data.Rshunt, 2)},{directionStr},{data.DelaySeconds},{Math.Round(data.Temperature, 1)}");
            }
        }

        /// <summary>
        /// 将所有在内存中的StringBuilder文本统一追加保存到硬盘，并清理内存
        /// </summary>
        public void SaveAndCloseAll()
        {
            lock (_lock)
            {
                foreach (var kvp in _csvCache)
                {
                    if (kvp.Value.Length > 0)
                    {
                        try
                        {
                            // 使用带有 BOM 的 UTF8，确保使用 Excel 打开 CSV 时不会出现中文乱码
                            //File.AppendAllText(kvp.Key, kvp.Value.ToString(), new UTF8Encoding(true));
                            bool appendBOM = !File.Exists(kvp.Key);
                            if (appendBOM)
                            {
                                File.AppendAllText(kvp.Key, kvp.Value.ToString(), new UTF8Encoding(true));
                            }
                            else
                            {
                                File.AppendAllText(kvp.Key, kvp.Value.ToString(), new UTF8Encoding(false));
                            }
                            kvp.Value.Clear(); // 写入硬盘后清空当前文件的内存暂存
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"批量保存CSV失败 [{kvp.Key}]: {ex.Message}");
                            // 防止数据重复追加和内存溢出
                            kvp.Value.Clear();
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            SaveAndCloseAll();
        }
    }
}