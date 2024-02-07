using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace fast_timer_load
{
    enum OnePageState
    {
        Main,
        Timer,
    }
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext.PropertyChanged += (sender, e) =>
            {
                switch(e.PropertyName) 
                {
                    case nameof(OnePageState):
                        switch (BindingContext.OnePageState)
                        {
                            case OnePageState.Main:
                                Shell.SetNavBarIsVisible(this, true);
                                break;
                            case OnePageState.Timer:
                                Shell.SetNavBarIsVisible(this, false);
                                break;
                            default:
                                break;
                        }
                        break;
                }
            };
        }
        protected override bool OnBackButtonPressed()
        {
            switch (BindingContext.OnePageState)
            {
                case OnePageState.Timer:
                    BindingContext.OnePageState = OnePageState.Main;
                    return true;
                case OnePageState.Main:
                default:
                    return base.OnBackButtonPressed();
            }
        }
        new MainPageBindingContext BindingContext => (MainPageBindingContext)base.BindingContext;
    }
    class MainPageBindingContext : INotifyPropertyChanged
    {
        public MainPageBindingContext()
        {
            StartTimerCommand = new Command(OnStartTimer);
            ResetTimerCommand = new Command(OnResetTimer);
            SetOnePageStateCommand = new Command<OnePageState>(OnSetOnePageState);
        }
        public ICommand StartTimerCommand { get; private set; }
        public ICommand SetOnePageStateCommand { get; private set; }
        public ICommand ResetTimerCommand { get; private set; }

        Task _pollingTask;
        private Stopwatch _stopwatch = new Stopwatch();
        private async void OnStartTimer(object o)
        {
            if (_stopwatch.IsRunning)
            {
                switch (OnePageState)
                {
                    case OnePageState.Main:
                        OnePageState = OnePageState.Timer;
                        break;
                    case OnePageState.Timer:
                        OnResetTimer(o);
                        break;
                }
            }
            else
            {
                _stopwatch.Restart();
                OnePageState = OnePageState.Timer;
                try
                {
                    if (_cts != null)
                    {
                        _cts.Cancel();
                    }
                    _cts = new CancellationTokenSource();
                    var token = _cts.Token;
                    while (!token.IsCancellationRequested)
                    {
                        var elapsed = _stopwatch.Elapsed;
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            TimerDisplay =
                                elapsed < TimeSpan.FromSeconds(1) ?
                                elapsed.ToString(@"hh\:mm\:ss\.ff") :
                                elapsed.ToString(@"hh\:mm\:ss");
                        });
                        await Task.Delay(TimeSpan.FromSeconds(0.1), token);
                    }
                }
                catch { }
                finally
                {
                    _stopwatch.Stop();
                    _pollingTask?.Dispose();
                }
            }
        }
        private void OnSetOnePageState(OnePageState context)
        {
            OnePageState = context;
        }
        private void OnResetTimer(object o)
        {
            _cts?.Cancel();
            _stopwatch.Stop();
            _stopwatch.Reset();
            TimerDisplay = "Start Timer";
        }

        CancellationTokenSource _cts = null;
        public string TimerDisplay
        {
            get => _TimerDisplay;
            set
            {
                if (!Equals(_TimerDisplay, value))
                {
                    _TimerDisplay = value;
                    OnPropertyChanged(); 
                }
            }
        }
        string _TimerDisplay = "Start Timer";
        public OnePageState OnePageState
        {
            get => _onePageState;
            set
            {
                if (!Equals(_onePageState, value))
                {
                    _onePageState = value;
                    OnPropertyChanged();
                }
            }
        }
        OnePageState _onePageState = default;

        protected virtual void OnPropertyChanged([CallerMemberName]string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler PropertyChanged;
    }
    class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object unk, Type targetType, object parameter, CultureInfo culture)
        {
            if (unk is Enum enum1 && parameter is Enum @enum2)
            {
                return enum1.Equals(@enum2);
            }
            return false;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
