using System.Collections.Generic;

namespace ModelingAppWPF;

public class SceneProjectData
{
    public int Version { get; set; } = 1;
    public List<SceneObjectData> Objects { get; set; } = new();
}

public class SceneObjectData
{
    public string? Name { get; set; }
    public string? PrimitiveType { get; set; }
    public bool IsVisible { get; set; } = true;
    public double PosX { get; set; }
    public double PosY { get; set; }
    public double PosZ { get; set; }
    public double RotX { get; set; }
    public double RotY { get; set; }
    public double RotZ { get; set; }
    public double ScaleX { get; set; } = 1;
    public double ScaleY { get; set; } = 1;
    public double ScaleZ { get; set; } = 1;
    public byte ColorR { get; set; }
    public byte ColorG { get; set; }
    public byte ColorB { get; set; }
    public double Opacity { get; set; } = 1;
    public string? TexturePath { get; set; }
    public string? SourcePath { get; set; }
    public Dictionary<string, double> Parameters { get; set; } = new();
}
