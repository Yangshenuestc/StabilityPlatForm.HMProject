//using System.ComponentModel;
//using System.Runtime.CompilerServices;
//using System.Windows.Input;

//namespace StabilityPlatForm.HMProject.UI.ViewModels
//{
//    // --- 属性通知基类 ---
//    public class ViewModelBase : INotifyPropertyChanged
//    {
//        public event PropertyChangedEventHandler PropertyChanged;

//        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }

//        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
//        {
//            if (Equals(field, value)) return false;
//            field = value;
//            OnPropertyChanged(propertyName);
//            return true;
//        }
//    }

//    // --- 命令绑定类 ---
//    public class RelayCommand : ICommand
//    {
//        private readonly Action<object> _execute;
//        private readonly Func<object, bool> _canExecute;

//        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
//        {
//            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
//            _canExecute = canExecute;
//        }

//        public event EventHandler CanExecuteChanged
//        {
//            add => CommandManager.RequerySuggested += value;
//            remove => CommandManager.RequerySuggested -= value;
//        }

//        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);

//        public void Execute(object parameter) => _execute(parameter);
//    }
//}
