using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ModelingAppWPF
{
    public partial class InputDialog : Window
    {
        public double? Angle { get; private set; }

        public InputDialog()
        {
            InitializeComponent();
            AngleTextBox.Focus();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(AngleTextBox.Text, out double angle))
            {
                Angle = angle;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, введите корректное число", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                AngleTextBox.Focus();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Angle = null;
            DialogResult = false;
            Close();
        }
    }
}
