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
        double? _stored;
        string? _op;
        bool _isNew = true;

        public string DisplayText { get => _display; set { _display = value; OnProp(nameof(DisplayText)); } }

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
        void ClearAll() { DisplayText = "0"; _stored = null; _op = null; _isNew = true; }

        void ApplyOperator(string op)
        {
            if (!_isNew)
            {
                if (_stored.HasValue && _op != null)
                    _stored = Calculate(_stored.Value, ParseDisp(), _op);
                else
                    _stored = ParseDisp();
            }
            _op = op;
            _isNew = true;
            if (_stored.HasValue) DisplayText = Format(_stored.Value);
        }

        void EqualsOp()
        {
            if (_op != null && _stored.HasValue)
            {
                var result = Calculate(_stored.Value, ParseDisp(), _op);
                DisplayText = Format(result);
                _stored = null; _op = null; _isNew = true;
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
            DisplayText = double.IsNaN(res) || double.IsInfinity(res) ? "Error" : Format(res);
            _isNew = true;
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
