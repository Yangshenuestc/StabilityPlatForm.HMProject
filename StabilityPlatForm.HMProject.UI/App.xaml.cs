using System.Windows;
using System.Collections.ObjectModel;
using StabilityPlatForm.HMProject.BusinessLogicLayer;
using StabilityPlatForm.HMProject.BusinessLogicLayer.Services;
using StabilityPlatForm.HMProject.DataAccessLayer.DatabaseOperations;
using StabilityPlatForm.HMProject.DataAccessLayer.FileOperations;
using StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation;
using StabilityPlatForm.HMProject.UI.ViewModels;
using StabilityPlatForm.HMProject.UI.Views;

namespace StabilityPlatForm.HMProject.UI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //  1、全局共享资源 
            var globalDbWriteQueue = new DatabaseWriteQueueService();
            var globalIvSourceLock = new SemaphoreSlim(1, 1);

            // 获取全局唯一的源表实例
            var sharedSourceTable = SourceTable.GetInstance();

            // 2. 遍历创建仓体 (给每个仓体分配独立的硬件)
            var cavityViewModels = new ObservableCollection<CavityViewModel>();
            string[] cavityNames = { "Cavity 1", "Cavity 2", "Cavity 3" };

            foreach (var name in cavityNames)
            {
                // 这些都是每个仓体独享的硬件实体，直接 new 出来
                var dedicatedChannelSwitcher = new ChannelSwitcher();
                var dedicatedSemiconductor = new Semiconductor();
                var dedicatedLightSource = new LightSource();
                var dedicatedBiasSource = new BiasSourceTable();

                var ivAnalyzer = new IvCurveAnalyzer();
                var csvExport = new CsvExportService();

                // 先组装环境硬件
                var envHelper = new TestEnvironmentHelper(
                    sharedSourceTable,           // 共享的源表
                    dedicatedChannelSwitcher,    // 独享的切换器
                    dedicatedSemiconductor,      // 独享的半导体
                    dedicatedLightSource,        // 独享的光源
                    dedicatedBiasSource          // 独享的偏压源
                   );

                // 组装测试服务
                var testService = new StabilityTestService(
                    sharedSourceTable,         // 1. 所有的仓体都传入同一个源表实例
                    dedicatedChannelSwitcher,  // 2. 独享通道切换器
                    envHelper,                 // 3. 传入刚刚组装好的环境
                    globalDbWriteQueue,        // 4. 共享的数据库队列
                    ivAnalyzer,                // 5. 分析器
                    csvExport,                 // 6. CSV服务
                    globalIvSourceLock         // 7. 共享的互斥锁
                    );

                //组装设备状态
                var deviceStatusVm = new DeviceStatusViewModel(
                    dedicatedLightSource,
                    dedicatedChannelSwitcher,
                    dedicatedBiasSource,
                    dedicatedSemiconductor,
                    sharedSourceTable
                    );

                var cavityVm = new CavityViewModel(name, testService,deviceStatusVm);
                cavityViewModels.Add(cavityVm);
            }

            // 3. 启动主窗口
            var mainViewModel = new MainViewModel(cavityViewModels);
            var mainWindow = new MainWindow(mainViewModel);

            mainWindow.Show();
        }
    }
}