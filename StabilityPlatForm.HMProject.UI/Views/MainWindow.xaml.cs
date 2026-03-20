using StabilityPlatForm.HMProject.UI.ViewModels;
using System.ComponentModel;
using System.Windows;
using StabilityPlatForm.HMProject.DataAccessLayer.HardwareDriverImplementation;

namespace StabilityPlatForm.HMProject.UI.Views
{
    public partial class MainWindow : Window
    {
        // 标记是否已经完成了安全清理工作
        private bool _isSafeToClose = false;

        public MainWindow(MainViewModel mainViewModel)
        {
            InitializeComponent();
            this.DataContext = mainViewModel;
        }

        // 拦截窗口关闭事件
        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            // 如果已经执行过安全清理，允许直接关闭
            if (_isSafeToClose) return;

            var vm = this.DataContext as MainViewModel;
            if (vm == null) return;

            // 检查是否还有任何仓位正在处于测试状态
            bool isAnyTesting = vm.Cavitys.Any(c => c.IsTesting);

            if (isAnyTesting)
            {
                var result = MessageBox.Show(
                    "当前有仓位正在进行测试！\n强制关闭会导致数据丢失且硬件无法复位。\n\n确定要停止测试、保存数据并安全退出吗？",
                    "安全退出警告",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                // 如果用户点否，取消关闭，退回软件界面
                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            // 1. 先拦截并取消原生的瞬间关闭动作
            e.Cancel = true;

            // 2. 将整个 UI 变灰禁用，防止用户在保存期间乱点
            this.IsEnabled = false;
            this.Title = "HM Lab - 正在复位硬件并保存数据，请稍候...";

            // 3. 给所有正在测试的仓位发送停止指令 (触发 CancellationToken)
            foreach (var cavity in vm.Cavitys)
            {
                if (cavity.IsTesting && cavity.StopTestCommand.CanExecute())
                {
                    cavity.StopTestCommand.Execute();
                }
            }

            // 4. 开启异步等待，死循环监听所有仓位的 IsTesting 状态
            // 直到业务层的 finally 块执行完毕 (此时 IsTesting 才会变为 false)
            await Task.Run(async () =>
            {
                while (vm.Cavitys.Any(c => c.IsTesting))
                {
                    await Task.Delay(100); // 每0.1秒检查一次
                }

                // 确保数据落盘和继电器断开，额外给0.5秒的缓冲安全时间
                await Task.Delay(500);
            });
            // 5.所有仓体都已安全停机，此时安全关闭全局共享的源表！
            try
            {
                SourceTable.GetInstance().StopTest();
            }
            catch
            {
                // 可选：吞掉异常或记录日志。因为软件都要关了，即使这里断开连接失败也不影响退出
            }
            // 6. 标记为绝对安全，并重新调用关闭方法，此时程序会真正退出
            _isSafeToClose = true;
            this.Close();
        }
    }
}