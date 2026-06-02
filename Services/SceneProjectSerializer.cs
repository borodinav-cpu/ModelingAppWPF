using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

using WpfColor = System.Windows.Media.Color;

namespace ModelingAppWPF;

public static class SceneProjectSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static SceneObjectData ToData(SceneObject obj)
    {
        var c = obj.DiffuseColor;
        return new SceneObjectData
        {
            Name = obj.Name,
            PrimitiveType = obj.PrimitiveType,
            IsVisible = obj.IsVisible,
            PosX = obj.PosX,
            PosY = obj.PosY,
            PosZ = obj.PosZ,
            RotX = obj.RotX,
            RotY = obj.RotY,
            RotZ = obj.RotZ,
            ScaleX = obj.ScaleX,
            ScaleY = obj.ScaleY,
            ScaleZ = obj.ScaleZ,
            ColorR = c.R,
            ColorG = c.G,
            ColorB = c.B,
            Opacity = obj.Opacity,
            TexturePath = obj.TexturePath,
            SourcePath = obj.SourcePath,
            Parameters = new Dictionary<string, double>(obj.Parameters)
        };
    }

    public static SceneProjectData CreateProject(IEnumerable<SceneObject> objects)
        => new() { Objects = objects.Select(ToData).ToList() };

    public static void Save(string fileName, IEnumerable<SceneObject> objects)
    {
        var json = JsonSerializer.Serialize(CreateProject(objects), JsonOptions);
        System.IO.File.WriteAllText(fileName, json);
    }

    public static SceneProjectData Load(string fileName)
    {
        var json = System.IO.File.ReadAllText(fileName);
        var project = JsonSerializer.Deserialize<SceneProjectData>(json, JsonOptions);
        if (project == null)
            throw new System.InvalidOperationException("Файл проекта пуст или поврежден.");

        return project;
    }
}
