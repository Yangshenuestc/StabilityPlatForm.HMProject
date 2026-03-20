using StabilityPlatForm.HMProject.Models.DataStructure;
using System.Text.Json;//微软高性能Json文件处理

namespace StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations
{
    /// <summary>
    /// 数据库数据写入以及保存服务类
    /// </summary>
    public class DatabaseExportService
    {
        private readonly string _currentTaskId;

        //当开始一次新的钙钛矿稳定性测试时，会生成一个唯一的任务 ID（taskId），并用它来创建这个服务类的实例
        //后续保存的任何数据（不管是稳定性结果还是原始IV曲线）都会自动打上这个任务的标签，不用每次调用方法都反复传递任务 ID。
        public DatabaseExportService(string taskId)
        {
            _currentTaskId = taskId;
        }
        /// <summary>
        /// 保存分析之后的StabilityResult到数据库 (支持异步写入，不卡死主线程)
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SaveResultDataAsync(string deviceId, PvMeasurementData data)
        {
            try
            {
                using (var context = new HMDatabaseContext())
                {
                    var entity = new StabilityResultEntity
                    {
                        TaskId = _currentTaskId,
                        DeviceId = deviceId,
                        TimeHours = data.TimeHours,
                        Pmax = data.Pmax,
                        Voc = data.Voc,
                        Jsc = data.Jsc,
                        FF = data.FF,
                        Rseries = data.Rseries,
                        Rshunt = data.Rshunt,
                        SweepDirection = data.SweepDirection,
                        Temperature = data.Temperature
                    };
                    //将数据放入暂存区
                    context.StabilityResults.Add(entity);
                    //正式提交给MySQL
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // 如果写入失败，可以在控制台输出错误，防止程序直接崩溃
                System.Diagnostics.Debug.WriteLine($"MySQL 写入失败 [{deviceId}]: {ex.Message}");
            }
        }

        /// <summary>
        /// 异步保存IV原始数据
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="isForwardScan"></param>
        /// <param name="timeHours"></param>
        /// <param name="voltage"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        public async Task SaveIvDataAsync(string deviceId, bool isForwardScan, double timeHours, double[] voltage, double[] current)
        {
            try
            {
                using (var context = new HMDatabaseContext())
                {
                    var entity = new IvCurveEntity
                    {
                        TaskId = _currentTaskId,
                        DeviceId = deviceId,
                        TimeHours = timeHours,
                        SweepDirection = isForwardScan,

                        //把 double[] 数组压缩成形如 "[0.1, 0.2, 0.3]" 的 JSON 字符串
                        VoltageDataJson = JsonSerializer.Serialize(voltage),
                        CurrentDataJson = JsonSerializer.Serialize(current)
                    };
                    //将数据放入暂存区
                    context.IvCurves.Add(entity);
                    //正式提交给MySQL
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MySQL IV 写入失败 [{deviceId}]: {ex.Message}");
            }
        }
    }
}