using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using System.Collections.Concurrent;
using Microsoft.Win32;
using StabilityPlatForm.HMProject.BusinessLogicLayer.Services;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace StabilityPlatForm.HMProject.UI.ViewModels
{
    public class CavityViewModel : ViewModelBase
    {
        private readonly StabilityTestService _testService;
        private CancellationTokenSource _cancellationTokenSource;

        //存储所有 54 个通道的历史坐标点 (线程安全字典)
        private ConcurrentDictionary<string, List<ObservablePoint>> _deviceDataBuffer = new();

        public CavityViewModel(string name, StabilityTestService testService)
        {

            CavityName = name;
            _testService = testService;

            // 初始化默认值
            IsFormalType = true;
            SelectedTestMode = "ISOS-LC-1/2";
            T80WarningColor = Brushes.LimeGreen;
            T80StatusText = "正常 (未触发)";
            CurrentTemperature = 25.0;
            RunningTime = TimeSpan.Zero;

            StartTestCommand = new RelayCommand(StartTest);
            StopTestCommand = new RelayCommand(StopTest);
            SelectPathCommand = new RelayCommand(SelectPath);

            //当用户点击左侧/右侧设备按钮时，切换图表数据
            SelectDeviceCommand = new RelayCommand(param =>
            {
                if (param is string deviceName)
                {
                    SelectedDeviceTitle = deviceName;
                    OnPropertyChanged(nameof(ChartDisplayTitle)); // 切换器件时也刷新标题
                    UpdateChartForSelectedDevice();
                }
            });

            //点击 1~6 点位时的命令
            SelectPointCommand = new RelayCommand(param =>
            {
                if (param is string ptStr && int.TryParse(ptStr, out int pt))
                {
                    SelectedPointIndex = pt;
                    UpdateChartForSelectedDevice();
                }
            });

            // 初始化空图表
            PceSeries = new ISeries[]
            {
            new LineSeries<ObservablePoint>
            {
                Values = new List<ObservablePoint>(),
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
        private string _fileName = "TestResult_01";
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
                    // 当测试模式改变时，通知界面重新评估是否解禁后续参数
                    OnPropertyChanged(nameof(HasTestMode));
                }
            }
        }

        //判断是否已经选择了测试模式
        public bool HasTestMode => !string.IsNullOrEmpty(SelectedTestMode);

        //正式结构
        private bool _isFormalType;
        public bool IsFormalType
        {
            get => _isFormalType;
            set => SetProperty(ref _isFormalType, value);
        }

        //反式结构
        private bool _isInvertedType;
        public bool IsInvertedType
        {
            get => _isInvertedType;
            set => SetProperty(ref _isInvertedType, value);
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
                    OnPropertyChanged(nameof(ChartDisplayTitle));
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

        #region 命令绑定
        public ICommand StartTestCommand { get; }
        public ICommand StopTestCommand { get; }
        public ICommand SelectPathCommand { get; }
        public ICommand SelectDeviceCommand { get; }

        public ICommand SelectPointCommand { get; }
        #endregion

        #region 测试
        private async void StartTest(object obj)
        {
            IsTesting = true;
            T80WarningColor = Brushes.LimeGreen;
            T80StatusText = "测试初始化中...";

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
                SelectedTestMode = SelectedTestMode,
                IsFormalType = IsFormalType,
                IsInvertedType = IsInvertedType,
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
                T80StatusText = info.StatusMessage;
                if (info.CurrentTemperature > 0) CurrentTemperature = info.CurrentTemperature;

                if (info.IsT80Warning) T80WarningColor = Brushes.Red;
                // --- 提取图表新数据 ---
                if (!string.IsNullOrEmpty(info.DeviceId) && info.IsForwardScan) // 仅绘制正扫或反扫
                {
                    // 将数据存入对应通道的缓冲池
                    if (!_deviceDataBuffer.ContainsKey(info.DeviceId))
                        _deviceDataBuffer[info.DeviceId] = new List<ObservablePoint>();

                    // X轴为小时，Y轴为PCE
                    _deviceDataBuffer[info.DeviceId].Add(new ObservablePoint(info.RunningTime.TotalHours, info.NewPceValue));

                    // 如果刚刚测完的数据，恰好是用户当前正在查看的【特定点位】，则刷新图表
                    if (info.DeviceId == GetBufferKey())
                    {
                        UpdateChartForSelectedDevice();
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
                // 被用户手动取消，正常捕获即可
            }
            catch (Exception ex)
            {
                T80StatusText = $"发生异常: {ex.Message}";
                T80WarningColor = Brushes.Red;
            }
            finally
            {
                IsTesting = false; // 测试结束，自动恢复配置界面
            }
        }

        // 停止测试
        private void StopTest(object obj)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                T80StatusText = "正在发送停止指令，复位硬件中...";
                _cancellationTokenSource.Cancel(); // 触发取消令牌
            }
        }

        //选择文件保存路径
        private void SelectPath(object obj)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "请选择保存地址并输入文件名",
                Filter = "Excel 文件 (*.xlsx)|*.xlsx|CSV 数据文件 (*.csv)|*.csv|所有文件 (*.*)|*.*",
                DefaultExt = ".xlsx",
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
            string currentKey = GetBufferKey(); // 核心：获取具体的点位Key
            List<ObservablePoint> displayPoints = new List<ObservablePoint>();

            if (_deviceDataBuffer.TryGetValue(currentKey, out var points))
            {
                displayPoints = points.ToList();
            }

            PceSeries = new ISeries[]
            {
        new LineSeries<ObservablePoint>
        {
            Values = displayPoints,
            Name = $"PCE - {currentKey}",
            GeometryFill = null,
            GeometryStroke = null,
            LineSmoothness = 0
        }
            };
        }

        // 辅助方法：将 "Left - Device 1" 转为底层的 "1-1"
        private string MapUiNameToDeviceId(string uiName)
        {
            // 解析您的 UI 逻辑以匹配底层 (这里提供伪代码)
            // 例如 "Left - Device 1" 对应底层 channel 1，即 "1-1"
            return "1-1";
        }
    }
}
