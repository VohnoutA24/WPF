using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using WPF_Try_out.Utils;

namespace WPF_Try_out.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        string _display = "0";
        string _operationText = string.Empty;
        string _easterEggImagePath = string.Empty;
        bool _showEasterEgg = false;
        double? _stored;
        string? _op;
        bool _isNew = true;

        public string DisplayText { get => _display; set { _display = value; OnProp(nameof(DisplayText)); } }
        public string OperationText { get => _operationText; private set { _operationText = value; OnProp(nameof(OperationText)); } }
        public string EasterEggImagePath { get => _easterEggImagePath; private set { _easterEggImagePath = value; OnProp(nameof(EasterEggImagePath)); } }
        public bool ShowEasterEgg { get => _showEasterEgg; private set { _showEasterEgg = value; OnProp(nameof(ShowEasterEgg)); } }

        // Commands
        public ICommand DigitCommand { get; }
        public ICommand DecimalCommand { get; }
        public ICommand ToggleSignCommand { get; }
        public ICommand BackspaceCommand { get; }
        public ICommand ClearEntryCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand BinaryCommand { get; }
        public ICommand EqualsCommand { get; }
        public ICommand UnaryCommand { get; }

        public MainViewModel()
        {
            DigitCommand = new RelayCommand(p => PressDigit(p?.ToString() ?? "0"));
            DecimalCommand = new RelayCommand(_ => PressDecimal());
            ToggleSignCommand = new RelayCommand(_ => ToggleSign());
            BackspaceCommand = new RelayCommand(_ => Backspace());
            ClearEntryCommand = new RelayCommand(_ => ClearEntry());
            ClearAllCommand = new RelayCommand(_ => ClearAll());
            BinaryCommand = new RelayCommand(p => ApplyOperator(p?.ToString() ?? "+"));
            EqualsCommand = new RelayCommand(_ => EqualsOp());
            UnaryCommand = new RelayCommand(p => ApplyUnary(p?.ToString() ?? ""));
        }

        void OnProp(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        void UpdateOperationText()
        {
            if (_stored.HasValue && _op != null)
                OperationText = $"{Format(_stored.Value)} {_op}";
            else
                OperationText = string.Empty;
        }

        void PressDigit(string d)
        {
            if (_isNew || DisplayText == "0")
            {
                DisplayText = d;
                _isNew = false;
            }
            else
            {
                DisplayText += d;
            }
        }

        void PressDecimal()
        {
            if (_isNew) { DisplayText = "0."; _isNew = false; return; }
            if (!DisplayText.Contains(".")) DisplayText += ".";
        }

        void ToggleSign()
        {
            if (DisplayText == "0") return;
            if (DisplayText.StartsWith("-")) DisplayText = DisplayText.Substring(1);
            else DisplayText = "-" + DisplayText;
        }

        void Backspace()
        {
            if (_isNew) { DisplayText = "0"; _isNew = true; return; }
            if (DisplayText.Length <= 1) { DisplayText = "0"; _isNew = true; }
            else DisplayText = DisplayText.Substring(0, DisplayText.Length - 1);
        }

        void ClearEntry() { DisplayText = "0"; _isNew = true; }
        void ClearAll() { DisplayText = "0"; _stored = null; _op = null; _isNew = true; OperationText = string.Empty; ShowEasterEgg = false; EasterEggImagePath = string.Empty; }

        void ApplyOperator(string op)
        {
            if (!_isNew)
            {
                if (_stored.HasValue && _op != null)
                {
                    double right = ParseDisp();
                    double left = _stored.Value;
                    var res = Calculate(left, right, _op);
                    _stored = res;
                }
                else
                    _stored = ParseDisp();
            }
            else
            {
                // if user presses an operator after result/equals or repeatedly, use the current display as stored value
                if (!_stored.HasValue)
                    _stored = ParseDisp();

                // If user pressed '-' while waiting for the second operand, treat it as starting a negative entry
                if (op == "-" && _op != null)
                {
                    DisplayText = "-";
                    _isNew = false;
                    return;
                }
            }
            _op = op;
            _isNew = true;
            if (_stored.HasValue)
            {
                // show friendly error when stored value is NaN/Infinity (e.g. division by zero)
                DisplayText = double.IsNaN(_stored.Value) || double.IsInfinity(_stored.Value)
                    ? "What is this diddy blud doing??"
                    : Format(_stored.Value);
            }
            UpdateOperationText();
        }

        void EqualsOp()
        {
            if (_op != null && _stored.HasValue)
            {
                double right = ParseDisp();
                var result = Calculate(_stored.Value, right, _op);

                // Easter egg: 67 * 67 -> special message
                if (_op == "*" && Math.Abs(_stored.Value - 67.0) < 1e-12 && Math.Abs(right - 67.0) < 1e-12)
                {
                    DisplayText = "ESPTEIN IS THAT YOU? (tuff)";
                    // show attached image next to the text
                    EasterEggImagePath = @"C:\Users\Vohnouta24\Documents\GitHub\WPF\WPF\WPF Try-out\Assets\easter.png";
                    ShowEasterEgg = true;
                }
                else
                {
                    DisplayText = double.IsNaN(result) || double.IsInfinity(result)
                        ? "What is this diddy blud doing??"
                        : Format(result);
                    ShowEasterEgg = false;
                    EasterEggImagePath = string.Empty;
                }

                // keep the result as the stored value so subsequent operators can use it
                _stored = double.IsNaN(result) || double.IsInfinity(result) ? (double?)null : result;
                _op = null;
                _isNew = true;
                UpdateOperationText();
            }
        }

        void ApplyUnary(string op)
        {
            double x = ParseDisp();
            double res = op switch
            {
                "Sqrt" => Math.Sqrt(x),
                "Square" => x * x,
                "Reciprocal" => x == 0 ? double.NaN : 1.0 / x,
                "Percent" => x / 100.0,
                _ => x
            };
            DisplayText = double.IsNaN(res) || double.IsInfinity(res) ? "What is this diddy blud doing??" : Format(res);
            _isNew = true;
            // unary results are final for the current entry
            UpdateOperationText();
        }

        double Calculate(double a, double b, string op)
        {
            return op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => b == 0 ? double.NaN : a / b,
                _ => b
            };
        }

        double ParseDisp() => double.TryParse(DisplayText, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0.0;
        string Format(double v) => v.ToString("G15", CultureInfo.InvariantCulture);
    }
}
