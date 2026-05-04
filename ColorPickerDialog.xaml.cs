using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModelingAppWPF
{
    public partial class ColorPickerDialog : Window
    {
        public System.Windows.Media.Color SelectedColor { get; private set; }

        private bool _updating;

        private static readonly System.Windows.Media.Color[] Presets =
        [
            Colors.CornflowerBlue, Colors.SteelBlue, Colors.DodgerBlue,
            Colors.MediumSeaGreen, Colors.LimeGreen, Colors.Goldenrod,
            Colors.OrangeRed, Colors.Crimson, Colors.MediumPurple,
            Colors.SlateGray, Colors.White, Colors.DimGray,
        ];

        public ColorPickerDialog(System.Windows.Media.Color initial)
        {
            InitializeComponent();
            SelectedColor = initial;

            _updating = true;
            SliderR.Value = initial.R;
            SliderG.Value = initial.G;
            SliderB.Value = initial.B;
            _updating = false;

            BuildSwatches();
            UpdatePreview();
        }

        private void BuildSwatches()
        {
            foreach (var c in Presets)
            {
                var b = new Border
                {
                    Width  = 24,
                    Height = 24,
                    Margin = new Thickness(2),
                    CornerRadius = new CornerRadius(3),
                    Background = new SolidColorBrush(c),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x44,0x44,0x44)),
                    BorderThickness = new Thickness(1),
                    Tag = c
                };
                b.MouseLeftButtonDown += (s, _) =>
                {
                    if (((Border)s!).Tag is System.Windows.Media.Color col)
                        ApplyColor(col);
                };
                Swatches.Children.Add(b);
            }
        }

        private void ApplyColor(System.Windows.Media.Color c)
        {
            _updating = true;
            SliderR.Value = c.R;
            SliderG.Value = c.G;
            SliderB.Value = c.B;
            _updating = false;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            byte r = (byte)SliderR.Value;
            byte g = (byte)SliderG.Value;
            byte b = (byte)SliderB.Value;
            var c = System.Windows.Media.Color.FromRgb(r, g, b);
            SelectedColor = c;
            PreviewBorder.Background = new SolidColorBrush(c);

            if (!_updating)
            {
                _updating = true;
                BoxR.Text  = r.ToString();
                BoxG.Text  = g.ToString();
                BoxB.Text  = b.ToString();
                BoxHex.Text = $"{r:X2}{g:X2}{b:X2}";
                _updating = false;
            }
        }

        private void Slider_Changed(object sender,
            System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_updating) UpdatePreview();
        }

        private void Box_Changed(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            if (sender is TextBox tb)
            {
                if (!byte.TryParse(tb.Text, out byte val)) return;
                _updating = true;
                if (tb == BoxR) SliderR.Value = val;
                else if (tb == BoxG) SliderG.Value = val;
                else if (tb == BoxB) SliderB.Value = val;
                _updating = false;
                UpdatePreview();
            }
        }

        private void HexBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (_updating) return;
            var hex = BoxHex.Text.TrimStart('#');
            if (hex.Length != 6) return;
            try
            {
                byte r = Convert.ToByte(hex[0..2], 16);
                byte g = Convert.ToByte(hex[2..4], 16);
                byte b = Convert.ToByte(hex[4..6], 16);
                _updating = true;
                SliderR.Value = r;
                SliderG.Value = g;
                SliderB.Value = b;
                _updating = false;
                UpdatePreview();
            }
            catch { }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
