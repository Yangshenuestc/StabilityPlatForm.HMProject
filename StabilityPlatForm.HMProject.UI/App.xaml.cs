using Microsoft.Extensions.DependencyInjection;
using StabilityPlatForm.HMProject.BusinessLogicLayer.Services;
using StabilityPlatForm.HMProject.Models.Interfaces;
using StabilityPlatForm.HMProject.UI.ViewModels;
using StabilityPlatForm.HMProject.UI.Views;
using System.Windows;

namespace StabilityPlatForm.HMProject.UI
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // 从 DI 容器中获取 MainWindow 并显示
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {

            // 注册全局 IV 源表互斥锁 (初始容量1，最大容量1) 
            services.AddSingleton(new SemaphoreSlim(1, 1));

            // --- 1. 注册 DAL 层硬件驱动与服务
            services.AddSingleton<ISourceTable, DataAccessLayer.HardwareDriverImplementation.SourceTable>();
            services.AddTransient<IChannelSwitcher, DataAccessLayer.HardwareDriverImplementation.ChannelSwitcher>();
            services.AddTransient<ISemiconductor, DataAccessLayer.HardwareDriverImplementation.Semiconductor>();
            services.AddTransient<ILightSource, DataAccessLayer.HardwareDriverImplementation.LightSource>();
            services.AddTransient<IBiasSourceTable, DataAccessLayer.HardwareDriverImplementation.BiasSourceTable>();

            // --- 2. 注册 BLL 层业务服务 ---
            services.AddTransient<StabilityTestService>();

            // --- 3. 注册 UI 层 ViewModels ---
            services.AddTransient<CavityViewModel>();
            services.AddSingleton<Func<string, CavityViewModel>>(provider => name =>
            {
                return ActivatorUtilities.CreateInstance<CavityViewModel>(provider, name);
            });

            services.AddSingleton<MainViewModel>();

            // --- 4. 注册 MainWindow ---
            services.AddSingleton<MainWindow>();
        }
    }

}
