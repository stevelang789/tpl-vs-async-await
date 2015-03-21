using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SteveLang.TplVsAsyncAwait.Domain;

namespace SteveLang.TplVsAsyncAwait.WpfApp
{
    class MainViewModel : INotifyPropertyChanged
    {
        private readonly Window _view;
        private readonly Calculator _calculator = new Calculator();
        private const int IncrementFrom = 1;
        private const int IncrementTo = int.MaxValue / 4;
        private CancellationTokenSource _cancellationTs;

        public MainViewModel(Window view)
        {
            _view = view;
            IsOnUiThread = true;
            Multiplier = 1;
            Divisor = 1;
            IsRunEnabled = true;
            Status = "Ready";
        }

        public bool IsOnUiThread { get; set; }
        public bool IsUsingTpl { get; set; }
        public bool IsUsingAsyncAwait { get; set; }
        public int Multiplier { get; set; }
        public int Divisor { get; set; }

        private bool _isRunEnabled;
        public bool IsRunEnabled
        {
            get { return _isRunEnabled; }
            set
            {
                if (value == _isRunEnabled) return;
                _isRunEnabled = value;
                OnPropertyChanged("IsRunEnabled");
            }
        }

        private bool _isCancelEnabled;
        public bool IsCancelEnabled
        {
            get { return _isCancelEnabled; }
            set
            {
                if (value == _isCancelEnabled) return;
                _isCancelEnabled = value;
                OnPropertyChanged("IsCancelEnabled");
            }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                if (value == _status) return;
                _status = value;
                OnPropertyChanged("Status");
            }
        }

        private string _uiThreadId;
        public string UiThreadId
        {
            get { return _uiThreadId; }
            set
            {
                if (value == _uiThreadId) return;
                _uiThreadId = value;
                OnPropertyChanged("UiThreadId");
            }
        }

        private string _calculationThreadId;
        public string CalculationThreadId
        {
            get { return _calculationThreadId; }
            set
            {
                if (value == _calculationThreadId) return;
                _calculationThreadId = value;
                OnPropertyChanged("CalculationThreadId");
            }
        }

        private string _continueWithOrPostAwaitThreadId;
        public string ContinueWithOrPostAwaitThreadId
        {
            get { return _continueWithOrPostAwaitThreadId; }
            set
            {
                if (value == _continueWithOrPostAwaitThreadId) return;
                _continueWithOrPostAwaitThreadId = value;
                OnPropertyChanged("ContinueWithOrPostAwaitThreadId");
            }
        }

        public void Run()
        {
            SetUiStateRunning();

            if (IsOnUiThread) PerformLongRunningCalculationOnUiThreadAndUpdateUi();
            else if (IsUsingTpl) PerformLongRunningCalculationUsingTplAndUpdateUi();
            else if (IsUsingAsyncAwait) PerformLongRunningCalculationUsingAsyncAwaitAndUpdateUi();
            else throw new NotImplementedException();
        }

        public void Cancel()
        {
            if (_cancellationTs == null) return;
            _cancellationTs.Cancel();
        }

        private void PerformLongRunningCalculationOnUiThreadAndUpdateUi()
        {
            try
            {
                var result = _calculator.Increment(IncrementFrom, IncrementTo, Multiplier, Divisor);
                var calculationThreadId = result.ThreadId.ToString();
                SetUiStateDone(calculationThreadId, "n/a");
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void PerformLongRunningCalculationUsingTplAndUpdateUi()
        {
            _cancellationTs = new CancellationTokenSource();
            var ct = _cancellationTs.Token;
            _calculator.IncrementAsync(IncrementFrom, IncrementTo, Multiplier, Divisor, ct)
                .ContinueWith(task =>
                {
                    if (task.IsCanceled)
                    {
                        SetUiStateCancelled();
                    }
                    else if (task.IsFaulted)
                    {
                        var innerException = task.Exception.Flatten().InnerExceptions.FirstOrDefault();
                        HandleException(innerException ?? task.Exception);
                    }
                    else
                    {
                        var calculationThreadId = task.Result.ThreadId.ToString();
                        var continueWithThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
                        SetUiStateDone(calculationThreadId, continueWithThreadId);
                    }
                    _cancellationTs.Dispose();
                    _cancellationTs = null;
                });
        }

        private async Task PerformLongRunningCalculationUsingAsyncAwaitAndUpdateUi()
        {
            try
            {
                _cancellationTs = new CancellationTokenSource();
                var ct = _cancellationTs.Token;
                var result = await _calculator.IncrementAsync(
                    IncrementFrom, IncrementTo, Multiplier, Divisor, ct);
                _cancellationTs.Dispose();
                _cancellationTs = null;
                var calculationThreadId = result.ThreadId.ToString();
                var postAwaitThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
                SetUiStateDone(calculationThreadId, postAwaitThreadId);
            }
            catch (OperationCanceledException)
            {
                SetUiStateCancelled();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void SetUiStateRunning()
        {
            IsRunEnabled = false;
            IsCancelEnabled = true;
            Status = "Running ...";
            UiThreadId = Thread.CurrentThread.ManagedThreadId.ToString();
            CalculationThreadId = string.Empty;
            ContinueWithOrPostAwaitThreadId = string.Empty;

            // The following is to ensure that the UI is updated
            // before performing the long-running calculation on the UI thread
            // http://www.jonathanantoine.com/2011/08/29/update-my-ui-now-how-to-wait-for-the-rendering-to-finish
            Application.Current.Dispatcher.Invoke(() => { }, DispatcherPriority.ContextIdle);
        }

        private void SetUiStateDone(string computationThreadId, string continueWithOrPostAwaitThreadId)
        {
            IsRunEnabled = true;
            IsCancelEnabled = false;
            Status = "Done";
            CalculationThreadId = computationThreadId;
            ContinueWithOrPostAwaitThreadId = continueWithOrPostAwaitThreadId;
        }

        private void SetUiStateCancelled()
        {
            IsRunEnabled = true;
            IsCancelEnabled = false;
            Status = "Cancelled";
            CalculationThreadId = "n/a";
            ContinueWithOrPostAwaitThreadId = "n/a";
        }

        private void HandleException(Exception ex)
        {
            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => MessageBox.Show(_view, ex.Message, "Error")));
            SetUiStateDone("n/a", "n/a");
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler == null) return;
            handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

}
