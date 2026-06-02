using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

using WpfColor = System.Windows.Media.Color;

namespace ModelingAppWPF;

public static class MaterialService
{
    public static DiffuseMaterial MakeDiffuseMaterial(WpfColor color, double opacity)
    {
        var brush = new SolidColorBrush(
            WpfColor.FromArgb((byte)(opacity * 255), color.R, color.G, color.B));
        return new DiffuseMaterial(brush);
    }

    public static DiffuseMaterial MakeMaterial(WpfColor color, double opacity, string? texturePath)
    {
        if (!string.IsNullOrEmpty(texturePath))
        {
            try
            {
                var bmp = new BitmapImage(new Uri(texturePath));
                var brush = new ImageBrush(bmp) { Opacity = opacity };
                return new DiffuseMaterial(brush);
            }
            catch
            {
                // Fallback to color if texture loading fails.
            }
        }

        return MakeDiffuseMaterial(color, opacity);
    }

    public static void ApplyMaterialToModel(Model3D? model, Material mat)
    {
        if (model is GeometryModel3D gm)
        {
            gm.Material = mat;
            gm.BackMaterial = mat;
        }
        else if (model is Model3DGroup grp)
        {
            foreach (var child in grp.Children)
                ApplyMaterialToModel(child, mat);
        }
    }

    public static void ApplyMaterialToObject(SceneObject obj)
    {
        if (obj.Visual == null)
            return;

        var mat = MakeMaterial(obj.DiffuseColor, obj.Opacity, obj.TexturePath);

        if (obj.Visual is ModelVisual3D mv)
        {
            ApplyMaterialToModel(mv.Content, mat);
        }
        else if (obj.Visual is CubeVisual3D cube)
        {
            cube.Fill = mat.Brush;
        }
        else if (obj.Visual is SphereVisual3D sphere)
        {
            sphere.Fill = mat.Brush;
        }
    }
}
