using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using WpfColor = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace ModelingAppWPF;

public class SceneObject : INotifyPropertyChanged
{
    private string? _name;
    private bool _isVisible = true;
    private string? _primitiveType;
    private Visual3D? _visual;

    public double PosX, PosY, PosZ;
    public double RotX, RotY, RotZ;
    public double ScaleX = 1, ScaleY = 1, ScaleZ = 1;

    public WpfColor DiffuseColor { get; set; } = WpfColors.CornflowerBlue;
    public double Opacity { get; set; } = 1.0;
    public string? TexturePath { get; set; }
    public string? SourcePath { get; set; }
    public Dictionary<string, double> Parameters { get; set; } = new();

    public string? Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(NameColor)); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            _isVisible = value;
            if (_visual != null)
                _visual.SetValue(UIElement.VisibilityProperty,
                    value ? Visibility.Visible : Visibility.Hidden);
            OnPropertyChanged();
            OnPropertyChanged(nameof(EyeIcon));
            OnPropertyChanged(nameof(EyeOpacity));
            OnPropertyChanged(nameof(NameColor));
        }
    }

    public string? PrimitiveType
    {
        get => _primitiveType;
        set { _primitiveType = value; OnPropertyChanged(); OnPropertyChanged(nameof(Icon)); }
    }

    public Visual3D? Visual
    {
        get => _visual;
        set { _visual = value; OnPropertyChanged(); }
    }

    public string Icon => PrimitiveType switch
    {
        "Cube" => "\u25A0",
        "Sphere" => "\u25CB",
        "Cylinder" => "\u2B2D",
        "Cone" => "\u25B3",
        "Torus" => "\u2B55",
        "Pyramid" => "\u25C7",
        "Ellipsoid" => "\u2B2F",
        "Pipe" => "\u25AD",
        _ => "\u25C8"
    };

    public string EyeIcon => IsVisible ? "\U0001F441" : "\U0001F6AB";
    public double EyeOpacity => IsVisible ? 0.6 : 1.0;
    public Brush NameColor => IsVisible
        ? new SolidColorBrush(WpfColor.FromRgb(0xBB, 0xBB, 0xBB))
        : new SolidColorBrush(WpfColor.FromRgb(0x55, 0x55, 0x55));

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? p = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
}
