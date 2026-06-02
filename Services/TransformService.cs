using System.Windows.Media.Media3D;

namespace ModelingAppWPF;

public static class TransformService
{
    public static void MoveByVector(SceneObject obj, double dx, double dy, double dz)
    {
        if (obj.Visual == null)
            return;

        obj.PosX += dx;
        obj.PosY += dy;
        obj.PosZ += dz;

        var transforms = EnsureTransformGroup(obj);
        transforms.Children.Add(new TranslateTransform3D(dx, dy, dz));
    }

    public static void MoveByDistance(SceneObject obj, string axis, double distance)
    {
        double dx = 0;
        double dy = 0;
        double dz = 0;

        switch (axis)
        {
            case "X": dx = distance; break;
            case "Y": dy = distance; break;
            default: dz = distance; break;
        }

        MoveByVector(obj, dx, dy, dz);
    }

    public static void RotateByAngle(SceneObject obj, string axis, double angle)
    {
        if (obj.Visual == null)
            return;

        var center = new Point3D(obj.PosX, obj.PosY, obj.PosZ);
        var rotAxis = axis switch
        {
            "X" => new Vector3D(1, 0, 0),
            "Y" => new Vector3D(0, 1, 0),
            _ => new Vector3D(0, 0, 1)
        };
        var rotation = new AxisAngleRotation3D(rotAxis, angle);

        var transforms = EnsureTransformGroup(obj);
        transforms.Children.Add(new RotateTransform3D(rotation, center));

        switch (axis)
        {
            case "X": obj.RotX = (obj.RotX + angle) % 360; break;
            case "Y": obj.RotY = (obj.RotY + angle) % 360; break;
            default: obj.RotZ = (obj.RotZ + angle) % 360; break;
        }
    }

    public static void ScaleByFactor(SceneObject obj, double factor, string? axis)
    {
        if (obj.Visual == null)
            return;

        double scaleX = 1;
        double scaleY = 1;
        double scaleZ = 1;

        switch (axis)
        {
            case "X":
                scaleX = factor;
                obj.ScaleX *= factor;
                break;
            case "Y":
                scaleY = factor;
                obj.ScaleY *= factor;
                break;
            case "Z":
                scaleZ = factor;
                obj.ScaleZ *= factor;
                break;
            default:
                scaleX = factor;
                scaleY = factor;
                scaleZ = factor;
                obj.ScaleX *= factor;
                obj.ScaleY *= factor;
                obj.ScaleZ *= factor;
                break;
        }

        var transforms = EnsureTransformGroup(obj);
        transforms.Children.Add(new ScaleTransform3D(
            scaleX, scaleY, scaleZ,
            obj.PosX, obj.PosY, obj.PosZ));
    }

    private static Transform3DGroup EnsureTransformGroup(SceneObject obj)
    {
        if (obj.Visual == null)
            return new Transform3DGroup();

        if (obj.Visual.Transform is Transform3DGroup existing)
            return existing;

        var transforms = new Transform3DGroup();
        if (obj.Visual.Transform != null)
            transforms.Children.Add(obj.Visual.Transform);

        obj.Visual.Transform = transforms;
        return transforms;
    }
}
