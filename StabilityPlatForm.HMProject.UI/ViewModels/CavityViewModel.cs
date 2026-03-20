using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Win32;
using SkiaSharp;
using StabilityPlatForm.HMProject.BusinessLogicLayer.Services;
using StabilityPlatForm.HMProject.Models.Enumeration;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace StabilityPlatForm.HMProject.UI.ViewModels
{
    public class CavityViewModel : BindableBase
    {
        private readonly StabilityTestService _testService;
        private CancellationTokenSource _cancellationTokenSource;

        //存储所有 54 个通道的历史坐标点 (线程安全字典)
        //private ConcurrentDictionary<string, List<ObservablePoint>> _deviceDataBuffer = new();
        private ConcurrentDictionary<string, ObservableCollection<ObservablePoint>> _deviceDataBuffer = new();
        // 设定单通道最大保留点数，50000点大约能记录很长一段时间的数据且不卡顿
        private const int MAX_POINTS = 50000;

        //日志集合，绑定到前台的 ListBox
        public ObservableCollection<string> TestLogs { get; } = new ObservableCollection<string>();
        private string _lastLoggedMessage = string.Empty;

        //设备状态UI绑定
        public DeviceStatusViewModel DeviceStatusVM { get; }

        public CavityViewModel(string name, StabilityTestService testService,DeviceStatusViewModel deviceStatusVM)
        {
            CavityName = name;
            _testService = testService;
            DeviceStatusVM = deviceStatusVM;

            // 初始化默认值
            IsFormalType = true;
            SelectedTestMode = "ISOS-LC-1/2";
            T80WarningColor = Brushes.LimeGreen;
            T80StatusText = "器件效率正常衰减中...";
            CurrentTemperature = 25.0;
            RunningTime = TimeSpan.Zero;

            // ================== Prism 命令绑定优化 ==================
            // 1. StartTestCommand 结合 ObservesProperty 自动控制按钮启用状态
            StartTestCommand = new DelegateCommand(StartTest, CanStartTest)
                .ObservesProperty(() => IsTesting)
                .ObservesProperty(() => SelectedTestMode);

            StopTestCommand = new DelegateCommand(StopTest);
            SelectPathCommand = new DelegateCommand(SelectPath);

            // 2. 强类型的 DelegateCommand<T>，替代繁琐的 param is string 判断
            // 当用户点击左侧/右侧设备按钮时，切换图表数据
            SelectDeviceCommand = new DelegateCommand<string>(deviceName =>
            {
                if (!string.IsNullOrEmpty(deviceName))
                {
                    SelectedDeviceTitle = deviceName;
                    RaisePropertyChanged(nameof(ChartDisplayTitle)); // 使用 Prism 的 RaisePropertyChanged
                    UpdateChartForSelectedDevice();
                }
            });

            // 点击 1~6 点位时的命令
            SelectPointCommand = new DelegateCommand<string>(ptStr =>
            {
                if (int.TryParse(ptStr, out int pt))
                {
                    SelectedPointIndex = pt;
                    UpdateChartForSelectedDevice();
                }
            });
            // ========================================================

            // 初始化空图表
            PceSeries = new ISeries[]
            {
                new LineSeries<ObservablePoint>
                {
                    // 初始绑定一个空的 ObservableCollection
                    Values = new ObservableCollection<ObservablePoint>(),
                    Name = "PCE (%)",
                    GeometryFill = null, // 关闭圆点渲染可极大提升流畅度
                    GeometryStroke = null,
                    LineSmoothness = 0   // 直线渲染性能最高
                }
            };
        }

        #region 基础属性
        /// <summary>
        /// 测试准备界面属性绑定
        /// </summary>
        //断电续跑时间
        private string _resumedTimeHours = "0";
        public string ResumedTimeHours
        {
            get => _resumedTimeHours;
            set => SetProperty(ref _resumedTimeHours, value);
        }
        //仓体名称
        public string CavityName { get; }

        //是否正在测试
        private bool _isTesting;
        public bool IsTesting
        {
            get => _isTesting;
            set => SetProperty(ref _isTesting, value);
        }

        //测试模式
        public List<string> AvailableTestModes { get; } = new List<string>
        {
            "ISOS-LC-1/2",
            "ISOS-L-1",
            "ISOS-L-2"
        };

        //文件保存路径
        private string _savePath = @"Please select path...";
        public string SavePath
        {
            get => _savePath;
            set => SetProperty(ref _savePath, value);
        }

        // 路径显示颜色
        private Brush _pathColor = Brushes.Red;
        public Brush PathColor
        {
            get => _pathColor;
            set => SetProperty(ref _pathColor, value);
        }

        //文件名称
        private string _fileName = DateTime.Now.ToString("yyyy.MM.dd");
        public string FileName
        {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }

        //测试模式选择
        private string _selectedTestMode;
        public string SelectedTestMode
        {
            get => _selectedTestMode;
            set
            {
                if (SetProperty(ref _selectedTestMode, value))
                {
                    // 当测试模式改变时，通知界面重新评估是否解禁后续参数 (Prism 方法)
                    RaisePropertyChanged(nameof(HasTestMode));
                }
            }
        }

        //判断是否已经选择了测试模式
        public bool HasTestMode => !string.IsNullOrEmpty(SelectedTestMode);
        //结构判断
        private DeviceType _currentDeviceType = DeviceType.FormalType;

        // 正式结构 (N-I-P)- 供前台 RadioButton 绑定
        private bool _isFormalType;
        public bool IsFormalType
        {
            get => _currentDeviceType == DeviceType.FormalType;
            set
            {
                if (value)
                {
                    _currentDeviceType = DeviceType.FormalType;
                    RaisePropertyChanged(nameof(IsFormalType));
                    RaisePropertyChanged(nameof(IsInvertedType));
                }
            }
        }

        // 反式结构 (P-I-N)- 供前台 RadioButton 绑定
        private bool _isInvertedType;
        public bool IsInvertedType
        {
            get => _currentDeviceType == DeviceType.InvertedType;
            set
            {
                if (value)
                {
                    _currentDeviceType = DeviceType.InvertedType;
                    RaisePropertyChanged(nameof(IsFormalType));
                    RaisePropertyChanged(nameof(IsInvertedType));
                }
            }
        }

        //电压范围起始值
        private string _initialVoltage = "-0.1";
        public string InitialVoltage
        {
            get => _initialVoltage;
            set => SetProperty(ref _initialVoltage, value);
        }

        //电压范围终止值
        private string _terminalVoltage = "1.2";
        public string TerminalVoltage
        {
            get => _terminalVoltage;
            set => SetProperty(ref _terminalVoltage, value);
        }

        //添加的偏压值设置
        private string _appliedVoltage = "0.8";
        public string AppliedVoltage
        {
            get => _appliedVoltage;
            set => SetProperty(ref _appliedVoltage, value);
        }

        //电压步长
        private string _voltageStep = "0.01";
        public string VoltageStep
        {
            get => _voltageStep;
            set => SetProperty(ref _voltageStep, value);
        }

        //器件面积
        private string _deviceArea = "0.06158";
        public string DeviceArea
        {
            get => _deviceArea;
            set => SetProperty(ref _deviceArea, value);
        }

        //--- ISOS-LC-1/2 专属温度参数 ---
        private string _sunTime = "12.0";//光照时间
        public string SunTime
        {
            get => _sunTime;
            set => SetProperty(ref _sunTime, value);
        }
        private string _darkTime = "12.0";//黑暗时间
        public string DarkTime
        {
            get => _darkTime;
            set => SetProperty(ref _darkTime, value);
        }

        // --- ISOS-L-1 专属温度参数 ---
        private string _targetTemperature = "60.0"; //默认65度
        public string TargetTemperature
        {
            get => _targetTemperature;
            set => SetProperty(ref _targetTemperature, value);
        }

        // --- ISOS-L-2 专属循环温度参数 ---
        private string _cycleLowTemperature = "25.0";//循环低温
        public string CycleLowTemperature
        {
            get => _cycleLowTemperature;
            set => SetProperty(ref _cycleLowTemperature, value);
        }
        private string _cycleHighTemperature = "85.0";//循环高温
        public string CycleHighTemperature
        {
            get => _cycleHighTemperature;
            set => SetProperty(ref _cycleHighTemperature, value);
        }

        // 升温时间
        private string _heatingTime = "6";
        public string HeatingTime
        {
            get => _heatingTime;
            set => SetProperty(ref _heatingTime, value);
        }

        // 降温时间
        private string _coolingTime = "6";
        public string CoolingTime
        {
            get => _coolingTime;
            set => SetProperty(ref _coolingTime, value);
        }
        /// <summary>
        /// 测试过程界面属性绑定
        /// </summary>
        //当前仓体温度
        private double _currentTemperature;
        public double CurrentTemperature
        {
            get => _currentTemperature;
            set => SetProperty(ref _currentTemperature, value);
        }

        // 持续运行的时间
        private TimeSpan _runningTime;
        public TimeSpan RunningTime
        {
            get => _runningTime;
            set => SetProperty(ref _runningTime, value);
        }

        //T80预警
        private Brush _t80WarningColor;
        public Brush T80WarningColor
        {
            get => _t80WarningColor;
            set => SetProperty(ref _t80WarningColor, value);
        }
        private string _t80StatusText;
        public string T80StatusText
        {
            get => _t80StatusText;
            set => SetProperty(ref _t80StatusText, value);
        }

        //器件展示选择
        private string _selectedDeviceTitle = "Left - Device 1"; // 默认显示1号
        public string SelectedDeviceTitle
        {
            get => _selectedDeviceTitle;
            set => SetProperty(ref _selectedDeviceTitle, value);
        }

        //测试点相关属性与命令
        private int _selectedPointIndex = 1; // 默认选中第1个点
        public int SelectedPointIndex
        {
            get => _selectedPointIndex;
            set
            {
                if (SetProperty(ref _selectedPointIndex, value))
                {
                    RaisePropertyChanged(nameof(ChartDisplayTitle)); // Prism 方法
                }
            }
        }
        // 动态拼接图表标题，例如 "Left - Device 1 - Point 1"
        public string ChartDisplayTitle => $"{SelectedDeviceTitle} - Point {_selectedPointIndex}";

        // 绑定到前台 XAML 图表的 Series
        private ISeries[] _pceSeries;
        public ISeries[] PceSeries
        {
            get => _pceSeries;
            set => SetProperty(ref _pceSeries, value);
        }
        public Axis[] XAxes { get; set; } =
        {
            new Axis
            {
                Name = "Time (h)",
                NameTextSize = 16, // 设置标题字体大小
                NamePaint = new SolidColorPaint(SKColors.DimGray) // 设置标题字体颜色为深灰色
            }
        };

        public Axis[] YAxes { get; set; } =
        {
            new Axis
            {
                Name = "PCE (%)",
                NameTextSize = 16,
                NamePaint = new SolidColorPaint(SKColors.DimGray)
            }
        };

        /// <summary>
        /// 定时器和时间记录字段
        /// </summary>
        private DispatcherTimer _testTimer;
        private DateTime _testStartTime;
        private Random _random = new Random(); // 用于模拟温度波动
        #endregion

        #region 命令绑定定义
        public DelegateCommand StartTestCommand { get; }
        public DelegateCommand StopTestCommand { get; }
        public DelegateCommand SelectPathCommand { get; }
        public DelegateCommand<string> SelectDeviceCommand { get; }
        public DelegateCommand<string> SelectPointCommand { get; }
        #endregion

        #region 测试逻辑
        // --- Prism 专用的 CanExecute 验证逻辑 ---
        private bool CanStartTest()
        {
            // 只有当不在测试中，且已经选择了测试模式时，按钮才可用
            return !IsTesting && HasTestMode;
        }

        private async void StartTest()
        {
            IsTesting = true;

            //每次开始测试前可选择清空日志，并输出启动信息
            TestLogs.Clear();

            // 1. 初始化取消令牌 (用于中途停止)
            _cancellationTokenSource = new CancellationTokenSource();

            // 2. 将界面字符串打包为配置对象
            double.TryParse(DeviceArea, out double area);
            double.TryParse(InitialVoltage, out double initV);
            double.TryParse(TerminalVoltage, out double termV);
            double.TryParse(AppliedVoltage, out double appV);
            double.TryParse(VoltageStep, out double stepV);
            double.TryParse(SunTime, out double sunT);
            double.TryParse(DarkTime, out double darkT);
            double.TryParse(TargetTemperature, out double tarTemp);
            double.TryParse(ResumedTimeHours, out double resumedTime);

            var config = new TestParameter
            {
                CavityName = CavityName,
                SavePath = SavePath,
                FileName = FileName,
                SelectedTestMode = SelectedTestMode switch
                {
                    "ISOS-LC-1/2" => TestMode.Mode_1,
                    "ISOS-L-1" => TestMode.Mode_2,
                    "ISOS-L-2" => TestMode.Mode_3,
                    _ => TestMode.Mode_1
                },
                DeviceType = _currentDeviceType,
                DeviceArea = area == 0 ? 0.06158 : area,
                InitialVoltage = initV,
                TerminalVoltage = termV,
                AppliedVoltage = appV,
                VoltageStep = stepV,
                SunTime = sunT,
                DarkTime = darkT,
                TargetTemperature = tarTemp,
                ResumedTimeHours = resumedTime
            };

            // 3. 定义进度回调（BLL层报告进度时，自动回到 UI 线程执行更新）
            var progress = new Progress<TestProgressInfo>(info =>
            {
                RunningTime = info.RunningTime;

                // 2. 底部日志框 (TestLogs) 仅过滤并输出“一轮结束”的消息
                if (!string.IsNullOrEmpty(info.StatusMessage) &&
                   (info.StatusMessage.Contains("轮") || info.StatusMessage.Contains("Round") || info.StatusMessage.Contains("完成测试")))
                {
                    // 防抖处理：避免底层多次 Report 相同的完成消息导致重复打印
                    if (info.StatusMessage != _lastLoggedMessage)
                    {
                        AddTestLog(info.StatusMessage);
                        _lastLoggedMessage = info.StatusMessage; // 记录最新一条日志
                    }
                }

                if (info.CurrentTemperature > 0) CurrentTemperature = info.CurrentTemperature;

                if (info.IsT80Warning) T80WarningColor = Brushes.Red;

                // --- 提取图表新数据 ---
                if (!string.IsNullOrEmpty(info.DeviceId) && info.IsForwardScan) // 仅绘制正扫或反扫
                {
                    // 如果字典中没有该通道，初始化它
                    if (!_deviceDataBuffer.ContainsKey(info.DeviceId))
                    {
                        _deviceDataBuffer[info.DeviceId] = new ObservableCollection<ObservablePoint>();
                    }

                    var points = _deviceDataBuffer[info.DeviceId];

                    // 直接添加新点，如果此时该通道正被 UI 观看，LiveCharts会自动平滑绘制这一个点
                    points.Add(new ObservablePoint(info.RunningTime.TotalHours, info.NewPceValue));

                    // 【内存控制】如果超出最大上限，丢弃最旧的数据 (FIFO)
                    if (points.Count > MAX_POINTS)
                    {
                        points.RemoveAt(0);
                    }
                }
            });

            try
            {
                // 4. 将沉重的硬件测试推入后台线程！防止UI卡死
                await Task.Run(() => _testService.StartTestAsync(config, progress, _cancellationTokenSource.Token));
            }
            catch (TaskCanceledException)
            {
                // 【新增】记录手动中止
                AddTestLog("已接收停止指令，硬件连接已安全断开。");
            }
            catch (Exception ex)
            {
                //记录异常报错
                AddTestLog($"[警告] 发生异常: {ex.Message}");
                T80StatusText = $"发生异常: {ex.Message}";
                T80WarningColor = Brushes.Red;
            }
            finally
            {
                //结束提示
                AddTestLog("===== 测试流程已结束 =====");
                IsTesting = false; // 测试结束，自动恢复配置界面
            }
        }

        // 停止测试
        private void StopTest()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                AddTestLog("用户发起停止请求，正在等待当前测试轮次安全结束..."); //手动停止提示
                _cancellationTokenSource.Cancel(); // 触发取消令牌
            }
        }

        //选择文件保存路径
        private void SelectPath()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "请选择保存地址并输入文件名",
                Filter = "CSV 数据文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                DefaultExt = ".csv",
                AddExtension = true,
                FileName = FileName
            };

            if (Directory.Exists(SavePath))
            {
                saveFileDialog.InitialDirectory = SavePath;
            }

            bool? result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string fullPath = saveFileDialog.FileName;

                SavePath = Path.GetDirectoryName(fullPath) + "\\";
                FileName = Path.GetFileNameWithoutExtension(fullPath);

                // 当用户实质性地选择了路径后，将颜色恢复为正常颜色
                PathColor = Brushes.Black;
            }
        }
        #endregion

        // 解析当前的底层键值 (例如 "Device 1" 和 "Point 2" 将拼接成 "1-2")
        private string GetBufferKey()
        {
            // 利用 LINQ 从 "Left - Device 3" 提取出数字 "3"
            string deviceNumStr = new string(SelectedDeviceTitle.Where(char.IsDigit).ToArray());
            if (string.IsNullOrEmpty(deviceNumStr)) deviceNumStr = "1";

            // 最终返回给缓冲池查询的 Key
            return $"{deviceNumStr}-{SelectedPointIndex}";
        }

        private void UpdateChartForSelectedDevice()
        {
            string currentKey = GetBufferKey();

            // 如果没数据，给个空的防止报错
            if (!_deviceDataBuffer.ContainsKey(currentKey))
            {
                _deviceDataBuffer[currentKey] = new ObservableCollection<ObservablePoint>();
            }

            // 解除图表的缩放/平移锁定，强制恢复全局视角
            if (XAxes != null && XAxes.Length > 0)
            {
                XAxes[0].MinLimit = null;
                XAxes[0].MaxLimit = null;
            }
            if (YAxes != null && YAxes.Length > 0)
            {
                YAxes[0].MinLimit = null;
                YAxes[0].MaxLimit = null;
            }

            // 直接拿到构造函数里 new 好的那个 Series
            var lineSeries = (LineSeries<ObservablePoint>)PceSeries[0];

            // 仅替换数据源的引用
            lineSeries.Values = _deviceDataBuffer[currentKey];
            lineSeries.Name = $"PCE - {currentKey}";
        }

        //封装一个安全的日志输出方法
        public void AddTestLog(string message)
        {
            // 使用 Dispatcher 确保能在后台线程调用更新 UI
            Application.Current.Dispatcher.Invoke(() =>
            {
                // 格式化时间并拼接消息，例如：[14:25:30] Left Block 一轮测试已完成
                TestLogs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
            });
        }

        // 辅助方法：将 "Left - Device 1" 转为底层的 "1-1"
        private string MapUiNameToDeviceId(string uiName)
        {
            return "1-1";
        }
    }
}