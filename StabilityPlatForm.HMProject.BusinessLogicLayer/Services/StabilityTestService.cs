using StabilityPlatForm.HMProject.DataAccessLayer.FileOperations;
using StabilityPlatForm.HMProject.Models.DataStructure;
using StabilityPlatForm.HMProject.Models.Enumeration;
using StabilityPlatForm.HMProject.Models.Hardwcare;
using StabilityPlatForm.HMProject.Models.Interfaces;
using System.Threading.Tasks.Dataflow;

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
        private readonly ExcelExportService _excelService;

        private FileStorageManager _fileManager;
        private DateTime _testStartTime;

        // 新增：全局 IV 源表互斥锁
        private readonly SemaphoreSlim _ivSourceLock;

        public StabilityTestService(
            ISourceTable sourceTable,
            IChannelSwitcher channelSwitcher,
            ISemiconductor semiconductor,
            ILightSource lightSource,
            IBiasSourceTable biasSourceTable,
            SemaphoreSlim ivSourceLock)
        {
            _sourceTable = sourceTable;
            _channelSwitcher = channelSwitcher;
            _semiconductor = semiconductor;
            _lightSource = lightSource;
            _biasSourceTable = biasSourceTable;
            _ivSourceLock = ivSourceLock;


            _analyzer = new IvCurveAnalyzer();
            _excelService = new ExcelExportService();
        }

        public async Task StartTestAsync(TestParameter config, IProgress<TestProgressInfo> progress, CancellationToken cancellationToken)
        {
            // 1. 初始化文件存储
            _fileManager = new FileStorageManager(config.SavePath, config.FileName);

            // 2. 启动硬件
            _sourceTable.Start();
            _channelSwitcher.Start();
            _semiconductor.Start();
            _biasSourceTable.Start();

            //定义开始测试测试时间
            _testStartTime = DateTime.Now;
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
                #region ISOS-L-1/2 逻辑
                if (config.SelectedTestMode == "ISOS-LC-1/2")
                {
                    //设置光暗时间
                    _lightSource.SetLightControl(new LightInfo { LightTime = config.SunTime, DarkTime = config.DarkTime });
                    //开启光源
                    _lightSource.StartWork();
                    //根据施加偏压开启偏压源表
                    _biasSourceTable.TestMode_Vmpp(new BiasInfo { Vmpp = config.AppliedVoltage });
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
                        }
                        progress?.Report(new TestProgressInfo { StatusMessage = "正在执行后台自动保存..." });
                        //将内存中的 workbook 写进硬盘并 Dispose 掉，释放内存
                        _excelService.SaveAndCloseAll();

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
                        }
                        progress?.Report(new TestProgressInfo { StatusMessage = "正在执行后台自动保存..." });
                        //将内存中的 workbook 写进硬盘并 Dispose 掉，释放内存
                        _excelService.SaveAndCloseAll();

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
                        }
                        progress?.Report(new TestProgressInfo { StatusMessage = "正在执行后台自动保存..." });
                        // 这一步会将内存中的 workbook 写进硬盘并 Dispose 掉，释放内存
                        _excelService.SaveAndCloseAll();

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
                _excelService.SaveAndCloseAll();

                progress?.Report(new TestProgressInfo { StatusMessage = "测试已安全停止并复位", RunningTime = DateTime.Now - _testStartTime });
            }
        }

        // 处理并保存单次测量的 IV 数据
        private void ProcessAndSaveDeviceData(string deviceId, bool isForwardScan, double[] vArray, double[] iArray, TestParameter config, IProgress<TestProgressInfo> progress)
        {
            string scanDirectionStr = isForwardScan ? "Forward" : "Reverse";
            double currentTime = (DateTime.Now - _testStartTime).TotalHours + config.ResumedTimeHours;
            double currentTemp = 25.0; // 实际中可替换为 _semiconductor.CurrentTemperature.CurrentT

            string ivFilePath = _fileManager.GetIvFilePath(scanDirectionStr, deviceId);
            string resultFilePath = _fileManager.GetResultFilePath(scanDirectionStr, deviceId);

            _excelService.AppendIvDataToExcel(ivFilePath, deviceId, currentTime, vArray, iArray);

            PvMeasurementData resultData = _analyzer.Analyze(vArray, iArray, config.DeviceArea);
            resultData.TimeHours = currentTime;
            resultData.SweepDirection = isForwardScan;
            resultData.Temperature = currentTemp;
            resultData.DelaySeconds = 0.1;

            _excelService.AppendResultDataToExcel(resultFilePath, deviceId, resultData);

            // 提取 Pmax (或依据 Pmax/输入功率 计算PCE)
            double pce = resultData.Pmax;

            // 新增：实时通知 UI 层图表更新
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
