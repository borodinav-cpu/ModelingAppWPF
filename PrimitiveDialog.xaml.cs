using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ModelingAppWPF
{
    public partial class PrimitiveDialog : Window
    {
        public string PrimitiveType { get; private set; } = "Cube";
        public double PosX { get; private set; }
        public double PosY { get; private set; }
        public double PosZ { get; private set; }
        public Color SelectedColor { get; private set; } = Colors.Blue;

       
        public double CubeSide { get; private set; }
        public double SphereRadius { get; private set; }
        public double CylinderDiameter { get; private set; }
        public double CylinderHeight { get; private set; }
        public double ConeBaseRadius { get; private set; }
        public double ConeTopRadius { get; private set; }
        public double ConeHeight { get; private set; }
        public double TorusDiameter { get; private set; }
        public double TorusTubeDiameter { get; private set; }
        public double PyramidSide { get; private set; }
        public double PyramidHeight { get; private set; }
        public double EllipsoidRadiusX { get; private set; }
        public double EllipsoidRadiusY { get; private set; }
        public double EllipsoidRadiusZ { get; private set; }
        public double PipeOuterDiameter { get; private set; }
        public double PipeInnerDiameter { get; private set; }
        public double PipeLength { get; private set; }

        public PrimitiveDialog()
        {
            InitializeComponent();
            PrimitiveListBox.SelectedIndex = 0;
            UpdateParametersPanel();
        }

        private void PrimitiveListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateParametersPanel();
        }

        private void UpdateParametersPanel()
        {
            if (PrimitiveListBox.SelectedItem is ListBoxItem selectedItem)
            {
                PrimitiveType = selectedItem.Tag?.ToString() ?? "Cube";

               
                CubeSizePanel.Visibility = Visibility.Collapsed;
                SphereSizePanel.Visibility = Visibility.Collapsed;
                CylinderSizePanel.Visibility = Visibility.Collapsed;
                ConeSizePanel.Visibility = Visibility.Collapsed;
                TorusSizePanel.Visibility = Visibility.Collapsed;
                PyramidSizePanel.Visibility = Visibility.Collapsed;
                EllipsoidSizePanel.Visibility = Visibility.Collapsed;
                PipeSizePanel.Visibility = Visibility.Collapsed;

                
                switch (PrimitiveType)
                {
                    case "Cube":
                        CubeSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Sphere":
                        SphereSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Cylinder":
                        CylinderSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Cone":
                        ConeSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Torus":
                        TorusSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Pyramid":
                        PyramidSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Ellipsoid":
                        EllipsoidSizePanel.Visibility = Visibility.Visible;
                        break;
                    case "Pipe":
                        PipeSizePanel.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                
                PosX = ParseDouble(PosXTextBox.Text);
                PosY = ParseDouble(PosYTextBox.Text);
                PosZ = ParseDouble(PosZTextBox.Text);

               
                switch (PrimitiveType)
                {
                    case "Cube":
                        CubeSide = ParseDouble(CubeSideTextBox.Text);
                        break;
                    case "Sphere":
                        SphereRadius = ParseDouble(SphereRadiusTextBox.Text);
                        break;
                    case "Cylinder":
                        CylinderDiameter = ParseDouble(CylinderDiameterTextBox.Text);
                        CylinderHeight = ParseDouble(CylinderHeightTextBox.Text);
                        break;
                    case "Cone":
                        ConeBaseRadius = ParseDouble(ConeBaseRadiusTextBox.Text);
                        ConeTopRadius = ParseDouble(ConeTopRadiusTextBox.Text);
                        ConeHeight = ParseDouble(ConeHeightTextBox.Text);
                        break;
                    case "Torus":
                        TorusDiameter = ParseDouble(TorusDiameterTextBox.Text);
                        TorusTubeDiameter = ParseDouble(TorusTubeDiameterTextBox.Text);
                        break;
                    case "Pyramid":
                        PyramidSide = ParseDouble(PyramidSideTextBox.Text);
                        PyramidHeight = ParseDouble(PyramidHeightTextBox.Text);
                        break;
                    case "Ellipsoid":
                        EllipsoidRadiusX = ParseDouble(EllipsoidRadiusXTextBox.Text);
                        EllipsoidRadiusY = ParseDouble(EllipsoidRadiusYTextBox.Text);
                        EllipsoidRadiusZ = ParseDouble(EllipsoidRadiusZTextBox.Text);
                        break;
                    case "Pipe":
                        PipeOuterDiameter = ParseDouble(PipeOuterDiameterTextBox.Text);
                        PipeInnerDiameter = ParseDouble(PipeInnerDiameterTextBox.Text);
                        PipeLength = ParseDouble(PipeLengthTextBox.Text);
                        break;
                }

                
                if (ColorComboBox.SelectedItem is ComboBoxItem colorItem)
                {
                    string? colorTag = colorItem.Tag?.ToString();
                    SelectedColor = GetColorFromTag(colorTag);
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка в параметрах: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static double ParseDouble(string text)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out double value))
                return value;

            return double.Parse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture);
        }
        private Color GetColorFromTag(string? tag)
        {
            return tag switch
            {
                "Blue" => Colors.Blue,
                "Red" => Colors.Red,
                "Green" => Colors.Green,
                "Orange" => Colors.Orange,
                "Yellow" => Colors.Yellow,
                "Purple" => Colors.Purple,
                "White" => Colors.White,
                "Black" => Colors.Black,
                "Gray" => Colors.Gray,
                "Silver" => Colors.Silver,
                _ => Colors.Blue
            };
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

