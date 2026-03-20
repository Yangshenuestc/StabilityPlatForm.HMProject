using StabilityPlatForm.HMProject.Models.Interfaces;
using System.Windows.Threading;
using System.Windows.Media;

namespace StabilityPlatForm.HMProject.UI.ViewModels
{
    public class DeviceStatusViewModel : BindableBase
    {
        private readonly ILightSource _lightSource;
        private readonly IChannelSwitcher _channelSwitcher;
        private readonly IBiasSourceTable _biasSource;
        private readonly ISemiconductor _semiconductor;
        private readonly ISourceTable _sourceTable;
        private DispatcherTimer _timer;

        public DeviceStatusViewModel(
            ILightSource lightSource,
            IChannelSwitcher channelSwitcher,
            IBiasSourceTable biasSource,
            ISemiconductor semiconductor,
            ISourceTable sourceTable)
        {
            _lightSource = lightSource;
            _channelSwitcher = channelSwitcher;
            _biasSource = biasSource;
            _semiconductor = semiconductor;
            _sourceTable = sourceTable;

            // 启动定时器，每秒刷新一次硬件状态
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => RefreshStatus();
            _timer.Start();
        }

        private void RefreshStatus()
        {
            RaisePropertyChanged(nameof(LightStatusText));
            RaisePropertyChanged(nameof(LightStatusColor));
            RaisePropertyChanged(nameof(LightIsWorkingText));
            RaisePropertyChanged(nameof(LightTime));
            RaisePropertyChanged(nameof(DarkTime));

            RaisePropertyChanged(nameof(ChannelStatusText));
            RaisePropertyChanged(nameof(ChannelStatusColor));
            RaisePropertyChanged(nameof(CurrentChannel));

            RaisePropertyChanged(nameof(BiasStatusText));
            RaisePropertyChanged(nameof(BiasStatusColor));
            RaisePropertyChanged(nameof(BiasIsWorkingText));
            RaisePropertyChanged(nameof(CurrentBiasVoltage));

            RaisePropertyChanged(nameof(SemiStatusText));
            RaisePropertyChanged(nameof(SemiStatusColor));
            RaisePropertyChanged(nameof(CurrentTemperature));

            RaisePropertyChanged(nameof(SourceTableStatusText));
            RaisePropertyChanged(nameof(SourceTableStatusColor));
        }

        // --- 光源状态 ---
        public string LightStatusText => _lightSource.IsConnected ? "已连接" : "未连接";
        public Brush LightStatusColor => _lightSource.IsConnected ? Brushes.LimeGreen : Brushes.Red;
        public string LightIsWorkingText => _lightSource.IsWorking ? "打开" : "关闭";
        public double LightTime => _lightSource.CurrentLightInfo?.LightTime ?? 0;
        public double DarkTime => _lightSource.CurrentLightInfo?.DarkTime ?? 0;

        // --- 通道切换器状态 ---
        public string ChannelStatusText => _channelSwitcher.IsConnected ? "已连接" : "未连接";
        public Brush ChannelStatusColor => _channelSwitcher.IsConnected ? Brushes.LimeGreen : Brushes.Red;
        public int CurrentChannel => _channelSwitcher.CurrentChannel?.ChannelNumber ?? 0;

        // --- 偏压源表状态 ---
        public string BiasStatusText => _biasSource.IsConnected ? "已连接" : "未连接";
        public Brush BiasStatusColor => _biasSource.IsConnected ? Brushes.LimeGreen : Brushes.Red;
        public string BiasIsWorkingText => _biasSource.IsOutputting ? "正在加偏压" : "空闲";
        public double CurrentBiasVoltage => _biasSource.CurrentBiasInfo?.Vmpp ?? 0;

        // --- 半导体温控状态 ---
        public string SemiStatusText => _semiconductor.IsConnected ? "已连接" : "未连接";
        public Brush SemiStatusColor => _semiconductor.IsConnected ? Brushes.LimeGreen : Brushes.Red;
        public double CurrentTemperature => _semiconductor.CurrentTemperature?.CurrentT ?? 0;

        // --- 测试用源表状态 ---
        public string SourceTableStatusText => _sourceTable.IsConnected ? "已连接" : "未连接";
        public Brush SourceTableStatusColor => _sourceTable.IsConnected ? Brushes.LimeGreen : Brushes.Red;
    }
}