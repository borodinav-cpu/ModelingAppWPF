using System;
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
                
                PosX = double.Parse(PosXTextBox.Text);
                PosY = double.Parse(PosYTextBox.Text);
                PosZ = double.Parse(PosZTextBox.Text);

               
                switch (PrimitiveType)
                {
                    case "Cube":
                        CubeSide = double.Parse(CubeSideTextBox.Text);
                        break;
                    case "Sphere":
                        SphereRadius = double.Parse(SphereRadiusTextBox.Text);
                        break;
                    case "Cylinder":
                        CylinderDiameter = double.Parse(CylinderDiameterTextBox.Text);
                        CylinderHeight = double.Parse(CylinderHeightTextBox.Text);
                        break;
                    case "Cone":
                        ConeBaseRadius = double.Parse(ConeBaseRadiusTextBox.Text);
                        ConeTopRadius = double.Parse(ConeTopRadiusTextBox.Text);
                        ConeHeight = double.Parse(ConeHeightTextBox.Text);
                        break;
                    case "Torus":
                        TorusDiameter = double.Parse(TorusDiameterTextBox.Text);
                        TorusTubeDiameter = double.Parse(TorusTubeDiameterTextBox.Text);
                        break;
                    case "Pyramid":
                        PyramidSide = double.Parse(PyramidSideTextBox.Text);
                        PyramidHeight = double.Parse(PyramidHeightTextBox.Text);
                        break;
                    case "Ellipsoid":
                        EllipsoidRadiusX = double.Parse(EllipsoidRadiusXTextBox.Text);
                        EllipsoidRadiusY = double.Parse(EllipsoidRadiusYTextBox.Text);
                        EllipsoidRadiusZ = double.Parse(EllipsoidRadiusZTextBox.Text);
                        break;
                    case "Pipe":
                        PipeOuterDiameter = double.Parse(PipeOuterDiameterTextBox.Text);
                        PipeInnerDiameter = double.Parse(PipeInnerDiameterTextBox.Text);
                        PipeLength = double.Parse(PipeLengthTextBox.Text);
                        break;
                }

                
                if (ColorComboBox.SelectedItem is ComboBoxItem colorItem)
                {
                    string colorTag = colorItem.Tag?.ToString();
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

        private Color GetColorFromTag(string tag)
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