using StabilityPlatForm.HMProject.DataAccessLayer.FileOperations;
using StabilityPlatForm.HMProject.Models.DataStructure;
using StabilityPlatForm.HMProject.Models.Enumeration;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations;

namespace StabilityPlatForm.HMProject.BusinessLogicLayer.Services
{
    public class StabilityTestService
    {
        private readonly ISourceTable _sourceTable;
        private readonly IChannelSwitcher _channelSwitcher;
        private readonly ISemiconductor _semiconductor;
        private readonly ILightSource _lightSource;
        private readonly IBiasSourceTable _biasSourceTable;

        private readonly IvCurveAnalyzer _analyzer;

        //private readonly ExcelExportService _excelService;
        private readonly CsvExportService _csvService;

        private FileStorageManager _fileManager;
        private DateTime _testStartTime;

        //数据库服务及当前任务标识
        private DatabaseExportService _dbService;
        private string _currentTaskId;

        //记录上一次 Excel 保存的时间
        //private DateTime _lastExcelSaveTime;
        private DateTime _lastCsvSaveTime;

        // 新增：全局 IV 源表互斥锁
        private readonly SemaphoreSlim _ivSourceLock;
        //添加队列服务实例
        private readonly DatabaseWriteQueueService _dbWriteQueue;

        public StabilityTestService(
            ISourceTable sourceTable,
            IChannelSwitcher channelSwitcher,
            ISemiconductor semiconductor,
            ILightSource lightSource,
            IBiasSourceTable biasSourceTable,
            DatabaseWriteQueueService dbWriteQueue,
            IvCurveAnalyzer analyzer,
            CsvExportService csvService,
            SemaphoreSlim ivSourceLock)
        {
            _sourceTable = sourceTable;
            _channelSwitcher = channelSwitcher;
            _semiconductor = semiconductor;
            _lightSource = lightSource;
            _biasSourceTable = biasSourceTable;
            _ivSourceLock = ivSourceLock;
            _dbWriteQueue = dbWriteQueue;
            _analyzer = analyzer;
            _csvService = csvService;
        }

        /// <summary>
        /// 开始测试任务逻辑
        /// </summary>
        /// <param name="config"></param>
        /// <param name="progress"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartTestAsync(TestParameter config, IProgress<TestProgressInfo> progress, CancellationToken cancellationToken)
        {
            // 1. 初始化文件存储
            _fileManager = new FileStorageManager(config.SavePath, config.FileName);
            //生成唯一测试任务 ID（仓体名 + 格式化时间），并初始化数据库服务
            _currentTaskId = $"{config.CavityName}_{DateTime.Now:yyyyMMdd_HHmmss}";
            _dbService = new DatabaseExportService(_currentTaskId);

            // 2. 启动硬件
            _sourceTable.Start();
            _channelSwitcher.Start();
            _semiconductor.Start();
            _biasSourceTable.Start();

            //定义开始测试测试时间
            _testStartTime = DateTime.Now;
            _lastCsvSaveTime = DateTime.Now;
            try
            {
                // 3. 根据器件结构确定正反扫
                ElectricalInfo forwardInfo = new ElectricalInfo();
                ElectricalInfo reverseInfo = new ElectricalInfo();

                if (config.IsFormalType && !config.IsInvertedType)
                {
                    forwardInfo = new ElectricalInfo { MinVoltage = config.InitialVoltage, MaxVoltage = config.TerminalVoltage, VoltageStep = config.VoltageStep };
                    reverseInfo = new ElectricalInfo { MinVoltage = config.InitialVoltage, MaxVoltage = config.TerminalVoltage, VoltageStep = config.VoltageStep };
                }
                else if (!config.IsFormalType && config.IsInvertedType)
                {
                    forwardInfo = new ElectricalInfo { MinVoltage = config.TerminalVoltage, MaxVoltage = config.InitialVoltage, VoltageStep = config.VoltageStep };
                    reverseInfo = new ElectricalInfo { MinVoltage = config.InitialVoltage, MaxVoltage = config.TerminalVoltage, VoltageStep = config.VoltageStep };
                }

                // 4. 根据不同模式执行核心逻辑
                #region ISOS-LC-1/2 逻辑
                if (config.SelectedTestMode == "ISOS-LC-1/2")
                {
                    //设置光暗时间
                    _lightSource.SetLightControl(new LightInfo { LightTime = config.SunTime, DarkTime = config.DarkTime });
                    //开启光源
                    _lightSource.StartWork();
                    //根据施加偏压开启偏压源表
                    _biasSourceTable.TestMode_Vmpp(new BiasInfo { Vmpp = config.AppliedVoltage });

                    int round = 1; //轮次计数器

                    // 大循环：只要用户没点停止，就一直测
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // 报告进度：进入新一轮扫描
                        progress?.Report(new TestProgressInfo
                        {
                            StatusMessage = "ISOS-LC-1/2扫描中...",
                            RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours), // 叠加历史时间
                            CurrentTemperature = 25.0 // 模拟室温或从硬件获取
                        });

                        for (int i = 1; i <= 54; i++) //测54个点（共9个器件）
                        {
                            if (cancellationToken.IsCancellationRequested) break; // 及时响应取消请求

                            _channelSwitcher.ChannelSwitch(new ChannelInfo { ChannelNumber = i });
                            await Task.Delay(100, cancellationToken); // 硬件延时

                            string deviceId = (i % 6 == 0) ? $"{i / 6}-6" : $"{1 + i / 6}-{i % 6}";

                            //准备进行 IV 测量，此时请求全局源表的控制权
                            await _ivSourceLock.WaitAsync(cancellationToken);
                            try
                            {
                                // 正扫
                                IVData forwardResult = _sourceTable.IVMode(forwardInfo);
                                ProcessAndSaveDeviceData(deviceId, true, forwardResult.Voltage, forwardResult.Current, config, progress);

                                await Task.Delay(100, cancellationToken);

                                // 反扫
                                IVData reverseResult = _sourceTable.IVMode(reverseInfo);
                                ProcessAndSaveDeviceData(deviceId, false, reverseResult.Voltage, reverseResult.Current, config, progress);
                            }
                            finally
                            {
                                //测完当前点位，立刻释放源表，让其他仓可以接入测量
                                _ivSourceLock.Release();
                            }
                            //测试效果，可以把 1.0 改成 0.1（即每 6 分钟存一次）
                            if ((DateTime.Now - _lastCsvSaveTime).TotalHours >= 0.01)
                            {
                                progress?.Report(new TestProgressInfo
                                {
                                    StatusMessage = "正在执行 Excel 定时后台保存并覆写原文件...",
                                    RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours)
                                });

                                // 执行覆写硬盘并清空内存，完全保留您现有的表头和格式
                                _csvService.SaveAndCloseAll();

                                // 重置上一次保存时间
                                _lastCsvSaveTime = DateTime.Now;
                            }
                        }

                        //一轮54个点全部跑完后，发送一条带有“轮”字的专属日志消息
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            progress?.Report(new TestProgressInfo
                            {
                                StatusMessage = $"[{config.CavityName}] 第 {round} 轮测试全部完成",
                                RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours)
                            });
                        }
                        round++; //准备进入下一轮
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken); // 扫描间隔
                    }
                }
                #endregion

                #region ISOS-L-1逻辑
                else if (config.SelectedTestMode == "ISOS-L-1")
                {
                    //设置光暗时间
                    _lightSource.SetLightControl(new LightInfo { LightTime = 24, DarkTime = 0 });
                    //开启光源
                    _lightSource.StartWork();
                    //设置半导体制冷温度
                    _semiconductor.TemperatureControl(new TemperatureInfo { TargetT = config.TargetTemperature }, TestMode.Mode_2);
                    //根据施加偏压开启偏压源表
                    _biasSourceTable.TestMode_Vmpp(new BiasInfo { Vmpp = config.AppliedVoltage });

                    int round = 1; //轮次计数器

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        progress?.Report(new TestProgressInfo
                        {
                            StatusMessage = "ISOS-L-1扫描中...",
                            RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours) // 叠加历史时间
                        });

                        for (int i = 1; i <= 54; i++) // 54个点
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            _channelSwitcher.ChannelSwitch(new ChannelInfo { ChannelNumber = i });
                            await Task.Delay(100, cancellationToken);

                            string deviceId = (i % 6 == 0) ? $"{i / 6}-6" : $"{1 + i / 6}-{i % 6}";

                            //准备进行 IV 测量，此时请求全局源表的控制权
                            await _ivSourceLock.WaitAsync(cancellationToken);
                            try
                            {
                                // 正扫
                                IVData forwardResult = _sourceTable.IVMode(forwardInfo);
                                ProcessAndSaveDeviceData(deviceId, true, forwardResult.Voltage, forwardResult.Current, config, progress);

                                await Task.Delay(100, cancellationToken);

                                // 反扫
                                IVData reverseResult = _sourceTable.IVMode(reverseInfo);
                                ProcessAndSaveDeviceData(deviceId, false, reverseResult.Voltage, reverseResult.Current, config, progress);
                            }
                            finally
                            {
                                //测完当前点位，立刻释放源表，让其他仓可以接入测量
                                _ivSourceLock.Release();
                            }
                            //测试效果，可以把 1.0 改成 0.1（即每 6 分钟存一次）
                            if ((DateTime.Now - _lastCsvSaveTime).TotalHours >= 0.01)
                            {
                                progress?.Report(new TestProgressInfo
                                {
                                    StatusMessage = "正在执行 Excel 定时后台保存并覆写原文件...",
                                    RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours)
                                });

                                // 执行覆写硬盘并清空内存，完全保留您现有的表头和格式
                                _csvService.SaveAndCloseAll();

                                // 重置上一次保存时间
                                _lastCsvSaveTime = DateTime.Now;
                            }
                        }

                        //一轮54个点全部跑完后，发送一条带有“轮”字的专属日志消息
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            progress?.Report(new TestProgressInfo
                            {
                                StatusMessage = $"[{config.CavityName}] 第 {round} 轮测试全部完成",
                                RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours)
                            });
                        }
                        round++; //准备进入下一轮
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
                #endregion

                #region ISOS-L-2逻辑
                else
                {
                    //设置光暗时间
                    _lightSource.SetLightControl(new LightInfo { LightTime = 24, DarkTime = 0 });
                    //开启光源
                    _lightSource.StartWork();
                    //设置半导体制冷温度
                    _semiconductor.TemperatureControl(new TemperatureInfo { TargetT = config.TargetTemperature }, TestMode.Mode_2);
                    //根据施加偏压开启偏压源表
                    _biasSourceTable.TestMode_Vmpp(new BiasInfo { Vmpp = config.AppliedVoltage });

                    int round = 1; //轮次计数器

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        progress?.Report(new TestProgressInfo
                        {
                            StatusMessage = "ISOS-L-2扫描中...",
                            RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours) // 叠加历史时间 
                        });

                        for (int i = 1; i <= 54; i++) // 54个点
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            _channelSwitcher.ChannelSwitch(new ChannelInfo { ChannelNumber = i });
                            await Task.Delay(100, cancellationToken);

                            string deviceId = (i % 6 == 0) ? $"{i / 6}-6" : $"{1 + i / 6}-{i % 6}";

                            //准备进行 IV 测量，此时请求全局源表的控制权
                            await _ivSourceLock.WaitAsync(cancellationToken);
                            try
                            {
                                // 正扫
                                IVData forwardResult = _sourceTable.IVMode(forwardInfo);
                                ProcessAndSaveDeviceData(deviceId, true, forwardResult.Voltage, forwardResult.Current, config, progress);

                                await Task.Delay(100, cancellationToken);

                                // 反扫
                                IVData reverseResult = _sourceTable.IVMode(reverseInfo);
                                ProcessAndSaveDeviceData(deviceId, false, reverseResult.Voltage, reverseResult.Current, config, progress);
                            }
                            finally
                            {
                                //测完当前点位，立刻释放源表，让其他仓可以接入测量
                                _ivSourceLock.Release();
                            }
                            //测试效果，可以把 1.0 改成 0.1（即每 6 分钟存一次）
                            if ((DateTime.Now - _lastCsvSaveTime).TotalHours >= 0.01)
                            {
                                progress?.Report(new TestProgressInfo
                                {
                                    StatusMessage = "正在执行 Excel 定时后台保存并覆写原文件...",
                                    RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours)
                                });

                                // 执行覆写硬盘并清空内存，完全保留您现有的表头和格式
                                _csvService.SaveAndCloseAll();

                                // 重置上一次保存时间
                                _lastCsvSaveTime = DateTime.Now;
                            }
                        }

                        //一轮54个点全部跑完后，发送一条带有“轮”字的专属日志消息
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            progress?.Report(new TestProgressInfo
                            {
                                StatusMessage = $"[{config.CavityName}] 第 {round} 轮测试全部完成",
                                RunningTime = (DateTime.Now - _testStartTime) + TimeSpan.FromHours(config.ResumedTimeHours)
                            });
                        }
                        round++; //准备进入下一轮
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                    }
                }
                #endregion
            }
            finally
            {
                // 5. 不管是正常结束、报错，还是被取消，强制复位硬件！
                _sourceTable.StopTest();
                _lightSource.StopWork();
                _semiconductor.StopWork();
                _biasSourceTable.StopTest();

                // 将内存中的 Excel 数据正式写入硬盘文件！
                _csvService.SaveAndCloseAll();

                progress?.Report(new TestProgressInfo { StatusMessage = "测试已安全停止并复位", RunningTime = DateTime.Now - _testStartTime });
            }
        }

        /// <summary>
        ///  处理并保存单次测量的 IV 数据
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="isForwardScan"></param>
        /// <param name="vArray"></param>
        /// <param name="iArray"></param>
        /// <param name="config"></param>
        /// <param name="progress"></param>
        private void ProcessAndSaveDeviceData(string deviceId, bool isForwardScan, double[] vArray, double[] iArray, TestParameter config, IProgress<TestProgressInfo> progress)
        {
            //正反扫
            string scanDirectionStr = isForwardScan ? "Forward" : "Reverse";
            //运行时间
            double currentTime = (DateTime.Now - _testStartTime).TotalHours + config.ResumedTimeHours;
            //实时温度实际中需要替换为温度传感器实时读取到的温度
            double currentTemp = 25.0;
            //IV数据文件地址
            string ivFilePath = _fileManager.GetIvFilePath(scanDirectionStr, deviceId);
            //StabilityResult文件地址
            string resultFilePath = _fileManager.GetResultFilePath(scanDirectionStr, deviceId);


            _csvService.AppendIvDataToCsv(ivFilePath, deviceId, currentTime, vArray, iArray);

            PvMeasurementData resultData = _analyzer.Analyze(vArray, iArray, config.DeviceArea);
            resultData.TimeHours = currentTime;
            resultData.SweepDirection = isForwardScan;
            resultData.Temperature = currentTemp;
            resultData.DelaySeconds = 0.1;

            _csvService.AppendResultDataToCsv(resultFilePath, deviceId, resultData);

            _dbWriteQueue.EnqueueWriteTask(async () =>
            {
                // 这段代码会在后台排队依次执行，彻底保护了 MySQL 和主线程
                //将结果异步推入 MySQL 数据库，使用 Fire-and-Forget 模式不阻塞硬件扫描
                await _dbService.SaveResultDataAsync(deviceId, resultData);
                //将原始 IV 数据数组也异步推入 MySQL 数据库
                await _dbService.SaveIvDataAsync(deviceId, isForwardScan, currentTime, vArray, iArray);
            });
            // 依据 PCE = (Pmax / Pin) * 100% 计算光电转换效率
            // 标准太阳光条件(AM 1.5G)通常设定输入功率为 100 mW/cm2
            double pIn = 100.0;

            // 计算出的 pce 为百分比数值 (例如 22.5 代表 22.5%)
            double pce = (resultData.Pmax / pIn) * 100.0;

            // 这里原有的单次测量通知依然保留，ViewModel会用它更新顶部栏和图表，但不会写入日志
            progress?.Report(new TestProgressInfo
            {
                StatusMessage = $"测量完毕: {deviceId}",
                RunningTime = TimeSpan.FromHours(currentTime),
                DeviceId = deviceId,
                NewPceValue = pce,
                IsForwardScan = isForwardScan
            });
        }
    }
}