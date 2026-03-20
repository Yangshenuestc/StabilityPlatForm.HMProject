using ClosedXML.Excel;
using StabilityPlatForm.HMProject.Models.DataStructure;

namespace StabilityPlatForm.HMProject.DataAccessLayer.FileOperations
{
    public class ExcelExportService : IDisposable
    {
        #region 1.0代码
        /*
        /// <summary>
        /// 保存原始 IV 数据 (电压步长为表头，时间为行首)
        /// </summary>
        public void AppendIvDataToExcel(string filePath, string deviceId, double timeHours, double[] voltage, double[] current)
        {
            bool fileExists = File.Exists(filePath);

            using (var workbook = fileExists ? new XLWorkbook(filePath) : new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("IV_Data");

                // 如果是新文件，初始化第一行的表头
                if (!fileExists || worksheet.LastRowUsed() == null)
                {
                    worksheet.Cell(1, 1).Value = deviceId; // A1: 1-1

                    // B1, C1, D1... 填入电压步长值
                    for (int i = 0; i < voltage.Length; i++)
                    {
                        worksheet.Cell(1, i + 2).Value = voltage[i];
                    }
                    worksheet.Row(1).Style.Font.Bold = true;
                    worksheet.SheetView.FreezeRows(1); // 冻结首行
                }

                // 找到最后一行空行写入新数据
                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;

                worksheet.Cell(nextRow, 1).Value = timeHours; // A2, A3... 填入时间

                // B2, C2, D2... 填入对应的电流数据 (或者电流密度 J，取决于你传入的 current 数组)
                for (int i = 0; i < current.Length; i++)
                {
                    worksheet.Cell(nextRow, i + 2).Value = current[i];
                }

                workbook.SaveAs(filePath);
            }
        }

        /// <summary>
        /// 保存 Stability Result 综合参数数据 (仅保留核心参数列)
        /// </summary>
        public void AppendResultDataToExcel(string filePath, string deviceId, PvMeasurementData data)
        {
            bool fileExists = File.Exists(filePath);

            using (var workbook = fileExists ? new XLWorkbook(filePath) : new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("Stability Result");

                // 如果是新文件，初始化表头 (严格从 A1 开始)
                if (!fileExists || worksheet.LastRowUsed() == null)
                {
                    worksheet.Cell(1, 1).Value = "Time(h)";                 // A1
                    worksheet.Cell(1, 2).Value = "Jsc(mA/cm2)";             // B1
                    worksheet.Cell(1, 3).Value = "Voc(V)";                  // C1
                    worksheet.Cell(1, 4).Value = "FF";                      // D1
                    worksheet.Cell(1, 5).Value = "Pmax";                    // E1
                    worksheet.Cell(1, 6).Value = "Vmpp";                    // F1
                    worksheet.Cell(1, 7).Value = "Rse (Ohm/cm2)";           // G1
                    worksheet.Cell(1, 8).Value = "Rsh (Ohm/cm2)";           // H1
                    worksheet.Cell(1, 9).Value = "Direction";               // I1
                    worksheet.Cell(1, 10).Value = "Delay(s)";               // J1
                    worksheet.Cell(1, 11).Value = "Tem(℃)";                // K1

                    // 表头加粗并冻结首行
                    worksheet.Row(1).Style.Font.Bold = true;
                    worksheet.SheetView.FreezeRows(1);
                }

                // 获取下一行空白行
                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;

                // 填入数据
                worksheet.Cell(nextRow, 1).Value = data.TimeHours;                               // A列: 时间
                worksheet.Cell(nextRow, 2).Value = System.Math.Round(data.Jsc, 4);               // B列: Jsc
                worksheet.Cell(nextRow, 3).Value = System.Math.Round(data.Voc, 4);               // C列: Voc
                worksheet.Cell(nextRow, 4).Value = System.Math.Round(data.FF, 4);                // D列: FF
                worksheet.Cell(nextRow, 5).Value = System.Math.Round(data.Pmax, 4);              // E列: Pmax
                worksheet.Cell(nextRow, 6).Value = System.Math.Round(data.Vmpp, 4);              // F列: Vmpp
                worksheet.Cell(nextRow, 7).Value = System.Math.Round(data.Rseries, 2);           // G列: Rseries
                worksheet.Cell(nextRow, 8).Value = System.Math.Round(data.Rshunt, 2);            // H列: Rshunt

                // I列: 将 bool 类型的 SweepDirection 转为对应字符串写入
                worksheet.Cell(nextRow, 9).Value = data.SweepDirection ? "Forward" : "Reverse";

                worksheet.Cell(nextRow, 10).Value = data.DelaySeconds;                           // J列: 延迟
                worksheet.Cell(nextRow, 11).Value = System.Math.Round(data.Temperature, 1);      // K列: 温度

                workbook.SaveAs(filePath);
            }
        }
        */
        #endregion

        #region 2.0代码
        // 缓存字典：键为文件绝对路径，值为 Excel 工作簿实例
        private readonly Dictionary<string, XLWorkbook> _workbookCache = new Dictionary<string, XLWorkbook>();

        // 线程锁，确保多线程调用或异步保存时不会发生资源争抢
        private readonly object _lock = new object();

        /// <summary>
        /// 核心：从缓存获取Workbook，如果缓存没有则去硬盘读取或新建
        /// </summary>
        private XLWorkbook GetOrCreateWorkbook(string filePath)
        {
            if (_workbookCache.TryGetValue(filePath, out var workbook))
            {
                return workbook;
            }

            bool fileExists = File.Exists(filePath);
            var newWorkbook = fileExists ? new XLWorkbook(filePath) : new XLWorkbook();
            _workbookCache[filePath] = newWorkbook;
            return newWorkbook;
        }

        public void AppendIvDataToExcel(string filePath, string deviceId, double timeHours, double[] voltage, double[] current)
        {
            lock (_lock)
            {
                // 获取缓存的 workbook，不再使用 using 块
                var workbook = GetOrCreateWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("IV_Data");

                if (worksheet.LastRowUsed() == null)
                {
                    worksheet.Cell(1, 1).Value = deviceId;
                    for (int i = 0; i < voltage.Length; i++)
                    {
                        worksheet.Cell(1, i + 2).Value = voltage[i];
                    }
                    worksheet.Row(1).Style.Font.Bold = true;
                    worksheet.SheetView.FreezeRows(1);
                }

                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
                worksheet.Cell(nextRow, 1).Value = timeHours;

                for (int i = 0; i < current.Length; i++)
                {
                    worksheet.Cell(nextRow, i + 2).Value = current[i];
                }

                // ⚠️ 注意：这里去掉了 workbook.SaveAs(filePath); 数据现已留存在内存中
            }
        }

        public void AppendResultDataToExcel(string filePath, string deviceId, PvMeasurementData data)
        {
            lock (_lock)
            {
                var workbook = GetOrCreateWorkbook(filePath);
                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("Stability Result");

                if (worksheet.LastRowUsed() == null)
                {
                    worksheet.Cell(1, 1).Value = "Time(h)";
                    worksheet.Cell(1, 2).Value = "Jsc(mA/cm2)";
                    worksheet.Cell(1, 3).Value = "Voc(V)";
                    worksheet.Cell(1, 4).Value = "FF";
                    worksheet.Cell(1, 5).Value = "Pmax";
                    worksheet.Cell(1, 6).Value = "Vmpp";
                    worksheet.Cell(1, 7).Value = "Rse (Ohm/cm2)";
                    worksheet.Cell(1, 8).Value = "Rsh (Ohm/cm2)";
                    worksheet.Cell(1, 9).Value = "Direction";
                    worksheet.Cell(1, 10).Value = "Delay(s)";
                    worksheet.Cell(1, 11).Value = "Tem(℃)";

                    worksheet.Row(1).Style.Font.Bold = true;
                    worksheet.SheetView.FreezeRows(1);
                }

                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
                worksheet.Cell(nextRow, 1).Value = data.TimeHours;
                worksheet.Cell(nextRow, 2).Value = System.Math.Round(data.Jsc, 4);
                worksheet.Cell(nextRow, 3).Value = System.Math.Round(data.Voc, 4);
                worksheet.Cell(nextRow, 4).Value = System.Math.Round(data.FF, 4);
                worksheet.Cell(nextRow, 5).Value = System.Math.Round(data.Pmax, 4);
                worksheet.Cell(nextRow, 6).Value = System.Math.Round(data.Vmpp, 4);
                worksheet.Cell(nextRow, 7).Value = System.Math.Round(data.Rseries, 2);
                worksheet.Cell(nextRow, 8).Value = System.Math.Round(data.Rshunt, 2);
                worksheet.Cell(nextRow, 9).Value = data.SweepDirection ? "Forward" : "Reverse";
                worksheet.Cell(nextRow, 10).Value = data.DelaySeconds;
                worksheet.Cell(nextRow, 11).Value = System.Math.Round(data.Temperature, 1);

                // ⚠️ 注意：这里去掉了 workbook.SaveAs(filePath);
            }
        }

        /// <summary>
        /// 将所有在内存中的工作簿统一保存到硬盘，并清理内存
        /// </summary>
        public void SaveAndCloseAll()
        {
            lock (_lock)
            {
                foreach (var kvp in _workbookCache)
                {
                    try
                    {
                        kvp.Value.SaveAs(kvp.Key);
                    }
                    catch (Exception ex)
                    {
                        // 记录无法保存的异常，比如文件正被用户用Excel打开
                        System.Diagnostics.Debug.WriteLine($"批量保存失败 [{kvp.Key}]: {ex.Message}");
                    }
                    finally
                    {
                        kvp.Value.Dispose(); // 释放 ClosedXML 占用的巨量内存
                    }
                }
                _workbookCache.Clear(); // 清空字典，下一轮扫描会重新读取
            }
        }

        public void Dispose()
        {
            SaveAndCloseAll();
        }
        #endregion

    }
}
