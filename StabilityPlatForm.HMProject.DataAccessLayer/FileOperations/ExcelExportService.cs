//using ClosedXML.Excel;
//using StabilityPlatForm.HMProject.Models.DataStructure;

//namespace StabilityPlatForm.HMProject.DataAccessLayer.FileOperations
//{
//    /// <summary>
//    /// 原始IV数据和稳定性分析结果导出为标准的Excel文件
//    /// </summary>
//    public class ExcelExportService : IDisposable
//    {
//        //在内存中建了一个“暂存区”：键为文件绝对路径，值为Excel工作簿实例。数据直接写进内存，提升速度，防止卡顿
//        private readonly Dictionary<string, XLWorkbook> _workbookCache = new Dictionary<string, XLWorkbook>();
//        // 线程锁，确保了同一时刻只能有一个线程执行写入操作
//        private readonly object _lock = new object();

//        /// <summary>
//        /// 智能获取工作簿：从缓存获取Workbook，如果缓存没有则去硬盘读取或新建
//        /// </summary>
//        private XLWorkbook GetOrCreateWorkbook(string filePath)
//        {
//            if (_workbookCache.TryGetValue(filePath, out var workbook))
//            {
//                return workbook;
//            }

//            bool fileExists = File.Exists(filePath);
//            var newWorkbook = fileExists ? new XLWorkbook(filePath) : new XLWorkbook();
//            _workbookCache[filePath] = newWorkbook;
//            return newWorkbook;
//        }

//        /// <summary>
//        /// Excel表中写入原始IV数据
//        /// </summary>
//        /// <param name="filePath"></param>
//        /// <param name="deviceId"></param>
//        /// <param name="timeHours"></param>
//        /// <param name="voltage"></param>
//        /// <param name="current"></param>
//        public void AppendIvDataToExcel(string filePath, string deviceId, double timeHours, double[] voltage, double[] current)
//        {
//            lock (_lock)
//            {
//                var workbook = GetOrCreateWorkbook(filePath);
//                //如果是一个全新的文件，deviceId 写在第一列，然后把传入的voltage横向铺开作为表头
//                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("IV_Data");
//                if (worksheet.LastRowUsed() == null)
//                {
//                    worksheet.Cell(1, 1).Value = deviceId;
//                    for (int i = 0; i < voltage.Length; i++)
//                    {
//                        worksheet.Cell(1, i + 2).Value = voltage[i];
//                    }
//                    worksheet.Row(1).Style.Font.Bold = true;
//                    worksheet.SheetView.FreezeRows(1);
//                }

//                //找到当前表格的最后一行空白行，将timeHours写在行首，然后将对应的current横向填入
//                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
//                worksheet.Cell(nextRow, 1).Value = timeHours;
//                for (int i = 0; i < current.Length; i++)
//                {
//                    worksheet.Cell(nextRow, i + 2).Value = current[i];
//                }
//            }
//        }

//        /// <summary>
//        /// Excel表中写入分析后的稳定性数据
//        /// </summary>
//        /// <param name="filePath"></param>
//        /// <param name="deviceId"></param>
//        /// <param name="data"></param>
//        public void AppendResultDataToExcel(string filePath, string deviceId, PvMeasurementData data)
//        {
//            lock (_lock)
//            {
//                var workbook = GetOrCreateWorkbook(filePath);

//                var worksheet = workbook.Worksheets.FirstOrDefault() ?? workbook.Worksheets.Add("Stability Result");
//                if (worksheet.LastRowUsed() == null)
//                {
//                    worksheet.Cell(1, 1).Value = "Time(h)";
//                    worksheet.Cell(1, 2).Value = "Jsc(mA/cm2)";
//                    worksheet.Cell(1, 3).Value = "Voc(V)";
//                    worksheet.Cell(1, 4).Value = "FF";
//                    worksheet.Cell(1, 5).Value = "Pmax";
//                    worksheet.Cell(1, 6).Value = "Vmpp";
//                    worksheet.Cell(1, 7).Value = "Rse (Ohm/cm2)";
//                    worksheet.Cell(1, 8).Value = "Rsh (Ohm/cm2)";
//                    worksheet.Cell(1, 9).Value = "Direction";
//                    worksheet.Cell(1, 10).Value = "Delay(s)";
//                    worksheet.Cell(1, 11).Value = "Tem(℃)";

//                    worksheet.Row(1).Style.Font.Bold = true;
//                    worksheet.SheetView.FreezeRows(1);
//                }



//                int nextRow = (worksheet.LastRowUsed()?.RowNumber() ?? 1) + 1;
//                worksheet.Cell(nextRow, 1).Value = data.TimeHours;
//                worksheet.Cell(nextRow, 2).Value = Math.Round(data.Jsc, 4);
//                worksheet.Cell(nextRow, 3).Value = Math.Round(data.Voc, 4);
//                worksheet.Cell(nextRow, 4).Value = Math.Round(data.FF, 4);
//                worksheet.Cell(nextRow, 5).Value = Math.Round(data.Pmax, 4);
//                worksheet.Cell(nextRow, 6).Value = Math.Round(data.Vmpp, 4);
//                worksheet.Cell(nextRow, 7).Value = Math.Round(data.Rseries, 2);
//                worksheet.Cell(nextRow, 8).Value = Math.Round(data.Rshunt, 2);
//                worksheet.Cell(nextRow, 9).Value = data.SweepDirection ? "Forward" : "Reverse";
//                worksheet.Cell(nextRow, 10).Value = data.DelaySeconds;
//                worksheet.Cell(nextRow, 11).Value = Math.Round(data.Temperature, 1);
//            }
//        }

//        /// <summary>
//        /// 将所有在内存中的工作簿统一保存到硬盘，并清理内存供新一轮的数据进入
//        /// </summary>
//        public void SaveAndCloseAll()
//        {
//            lock (_lock)
//            {
//                foreach (var kvp in _workbookCache)
//                {
//                    try
//                    {
//                        kvp.Value.SaveAs(kvp.Key);
//                    }
//                    catch (Exception ex)
//                    {
//                        // 记录无法保存的异常，比如文件正被用户用Excel打开
//                        System.Diagnostics.Debug.WriteLine($"批量保存失败 [{kvp.Key}]: {ex.Message}");
//                    }
//                    finally
//                    {
//                        kvp.Value.Dispose(); // 释放 ClosedXML 占用的巨量内存
//                    }
//                }
//                _workbookCache.Clear(); // 清空字典，下一轮扫描会重新读取
//            }
//        }
//        /// <summary>
//        /// 服务类被销毁时，自动调用方法，触发 SaveAndCloseAll()，确保所有在内存中的数据都不会丢失，安全落地到硬盘
//        /// </summary>
//        public void Dispose()
//        {
//            SaveAndCloseAll();
//        }
//    }
//}
