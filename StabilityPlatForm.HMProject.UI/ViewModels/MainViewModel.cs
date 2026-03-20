using System.Collections.ObjectModel;

namespace StabilityPlatForm.HMProject.UI.ViewModels
{
    public class MainViewModel : BindableBase
    {
        public ObservableCollection<CavityViewModel> Cavitys { get; set; }

        private CavityViewModel _selectedCavity;
        public CavityViewModel SelectedCavity
        {
            get => _selectedCavity;
            set => SetProperty(ref _selectedCavity, value);
        }

       public MainViewModel(ObservableCollection<CavityViewModel> cavitys)
        {
            Cavitys = cavitys;
            
            // 默认选中第一个仓体
            if (Cavitys != null && Cavitys.Count > 0)
            {
                SelectedCavity = Cavitys[0];
            }
        }
    }
}
