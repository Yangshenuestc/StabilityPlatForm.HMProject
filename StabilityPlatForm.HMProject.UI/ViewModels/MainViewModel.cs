using System.Collections.ObjectModel;

namespace StabilityPlatForm.HMProject.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        public ObservableCollection<CavityViewModel> Cavitys { get; set; }

        private CavityViewModel _selectedCavity;
        public CavityViewModel SelectedCavity
        {
            get => _selectedCavity;
            set => SetProperty(ref _selectedCavity, value);
        }

        // 通过构造函数注入工厂方法
        public MainViewModel(Func<string, CavityViewModel> cavityFactory)
        {
            // 利用 DI 工厂初始化 3 个 Cavity，硬件接口会自动注入进去
            Cavitys = new ObservableCollection<CavityViewModel>
            {
                cavityFactory("Cavity 1"),
                cavityFactory("Cavity 2")
            };

            SelectedCavity = Cavitys[0];
        }
    }
}
