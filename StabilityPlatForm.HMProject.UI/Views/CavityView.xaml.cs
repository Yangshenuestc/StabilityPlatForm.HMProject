using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StabilityPlatForm.HMProject.UI.Views
{
    /// <summary>
    /// CavityView.xaml 的交互逻辑
    /// </summary>
    public partial class CavityView : UserControl
    {
        public CavityView()
        {
            InitializeComponent();
            // 监听 ListBox 内部集合的变化
            ((INotifyCollectionChanged)LogListBox.Items).CollectionChanged += (s, e) =>
            {
                // 当有新项（新日志）被添加时
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    // 自动让 ListBox 滚动到当前的最后一项
                    LogListBox.ScrollIntoView(LogListBox.Items[LogListBox.Items.Count - 1]);
                }
            };
        }
    }
}
