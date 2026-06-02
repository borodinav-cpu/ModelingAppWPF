using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

using WpfColor = System.Windows.Media.Color;

namespace ModelingAppWPF;

public static class PrimitiveFactory
{
    public static Dictionary<string, double> CreateParameters(PrimitiveDialog dialog)
    {
        return dialog.PrimitiveType switch
        {
            "Cube" => new() { ["Side"] = dialog.CubeSide },
            "Sphere" => new() { ["Radius"] = dialog.SphereRadius },
            "Cylinder" => new() { ["Diameter"] = dialog.CylinderDiameter, ["Height"] = dialog.CylinderHeight },
            "Cone" => new() { ["BaseRadius"] = dialog.ConeBaseRadius, ["TopRadius"] = dialog.ConeTopRadius, ["Height"] = dialog.ConeHeight },
            "Torus" => new() { ["Diameter"] = dialog.TorusDiameter, ["TubeDiameter"] = dialog.TorusTubeDiameter },
            "Pyramid" => new() { ["Side"] = dialog.PyramidSide, ["Height"] = dialog.PyramidHeight },
            "Ellipsoid" => new() { ["RadiusX"] = dialog.EllipsoidRadiusX, ["RadiusY"] = dialog.EllipsoidRadiusY, ["RadiusZ"] = dialog.EllipsoidRadiusZ },
            "Pipe" => new() { ["OuterDiameter"] = dialog.PipeOuterDiameter, ["InnerDiameter"] = dialog.PipeInnerDiameter, ["Length"] = dialog.PipeLength },
            _ => new()
        };
    }

    public static Visual3D CreateVisual(string type, double px, double py, double pz,
                                        WpfColor color, double opacity,
                                        string? texturePath,
                                        Dictionary<string, double> parameters)
    {
        var material = MaterialService.MakeMaterial(color, opacity, texturePath);

        switch (type)
        {
            case "Cube":
                return new CubeVisual3D
                {
                    Center = new Point3D(px, py, pz),
                    SideLength = Param(parameters, "Side", 2),
                    Fill = material.Brush,
                    BackMaterial = material
                };

            case "Sphere":
                return new SphereVisual3D
                {
                    Center = new Point3D(px, py, pz),
                    Radius = Param(parameters, "Radius", 1),
                    Fill = material.Brush,
                    BackMaterial = material
                };

            case "Cylinder":
            {
                var b = new MeshBuilder();
                var p1 = new Point3D(px, py, pz);
                var p2 = new Point3D(px, py + Param(parameters, "Height", 2), pz);
                b.AddCylinder(p1, p2, Param(parameters, "Diameter", 1.6) / 2, 36, true, true);
                var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                return new ModelVisual3D { Content = geo };
            }

            case "Cone":
            {
                var b = new MeshBuilder();
                b.AddCone(new Point3D(px, py, pz), new Vector3D(0, 1, 0),
                    Param(parameters, "BaseRadius", 1),
                    Param(parameters, "TopRadius", 0),
                    Param(parameters, "Height", 2), true, true, 36);
                var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                return new ModelVisual3D { Content = geo };
            }

            case "Torus":
            {
                var b = new MeshBuilder();
                b.AddTorus(Param(parameters, "Diameter", 3) / 2, Param(parameters, "TubeDiameter", 0.8) / 2, 36, 24);
                var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                return new ModelVisual3D
                {
                    Content = geo,
                    Transform = new TranslateTransform3D(px, py, pz)
                };
            }

            case "Pyramid":
            {
                var b = new MeshBuilder();
                double s = Param(parameters, "Side", 2) / 2;
                double h = Param(parameters, "Height", 2.5);
                var apex = new Point3D(px, py + h, pz);
                b.AddTriangle(new Point3D(px - s, py, pz - s), new Point3D(px + s, py, pz - s), apex);
                b.AddTriangle(new Point3D(px + s, py, pz - s), new Point3D(px + s, py, pz + s), apex);
                b.AddTriangle(new Point3D(px + s, py, pz + s), new Point3D(px - s, py, pz + s), apex);
                b.AddTriangle(new Point3D(px - s, py, pz + s), new Point3D(px - s, py, pz - s), apex);
                b.AddQuad(new Point3D(px - s, py, pz - s), new Point3D(px - s, py, pz + s),
                          new Point3D(px + s, py, pz + s), new Point3D(px + s, py, pz - s));
                var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                return new ModelVisual3D { Content = geo };
            }

            case "Ellipsoid":
            {
                var b = new MeshBuilder();
                b.AddEllipsoid(new Point3D(px, py, pz),
                    Param(parameters, "RadiusX", 1.5),
                    Param(parameters, "RadiusY", 1),
                    Param(parameters, "RadiusZ", 1), 32, 32);
                var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                return new ModelVisual3D { Content = geo };
            }

            case "Pipe":
            {
                var b = new MeshBuilder();
                var p1 = new Point3D(px, py, pz);
                var p2 = new Point3D(px, py + Param(parameters, "Length", 3), pz);
                b.AddPipe(p1, p2, Param(parameters, "InnerDiameter", 1.2) / 2, Param(parameters, "OuterDiameter", 1.6) / 2, 36);
                var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                return new ModelVisual3D { Content = geo };
            }

            default:
                throw new InvalidOperationException("Неизвестный тип примитива.");
        }
    }

    private static double Param(Dictionary<string, double> parameters, string key, double fallback)
        => parameters.TryGetValue(key, out double value) ? value : fallback;
}
