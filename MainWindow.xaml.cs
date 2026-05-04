using HelixToolkit.Wpf;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

// Явные алиасы чтобы избежать конфликта CS0104
using WpfColor       = System.Windows.Media.Color;
using WpfColors      = System.Windows.Media.Colors;
using WpfMessageBox  = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfButton      = System.Windows.Controls.Button;

namespace ModelingAppWPF
{
    // =========================================================
    //  Модель объекта сцены (для дерева)
    // =========================================================
    public class SceneObject : INotifyPropertyChanged
    {
        private string?  _name;
        private bool     _isVisible = true;
        private string?  _primitiveType;
        private Visual3D? _visual;

        // Трансформации
        public double PosX, PosY, PosZ;
        public double RotX, RotY, RotZ;
        public double ScaleX = 1, ScaleY = 1, ScaleZ = 1;

        // Материал
        public WpfColor DiffuseColor { get; set; } = WpfColors.CornflowerBlue;
        public double   Opacity      { get; set; } = 1.0;
        public string?  TexturePath  { get; set; } = null;

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

        // Вычисляемые свойства для биндинга
        public string Icon => PrimitiveType switch
        {
            "Cube"      => "⬛",
            "Sphere"    => "⚪",
            "Cylinder"  => "⬭",
            "Cone"      => "△",
            "Torus"     => "⭕",
            "Pyramid"   => "◇",
            "Ellipsoid" => "⬯",
            "Pipe"      => "▭",
            _           => "◈"
        };

        public string   EyeIcon    => IsVisible ? "👁" : "🚫";
        public double   EyeOpacity => IsVisible ? 0.6 : 1.0;
        public Brush    NameColor  => IsVisible
            ? new SolidColorBrush(WpfColor.FromRgb(0xBB, 0xBB, 0xBB))
            : new SolidColorBrush(WpfColor.FromRgb(0x55, 0x55, 0x55));

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? p = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    // =========================================================
    //  Code-behind главного окна
    // =========================================================
    public partial class MainWindow : Window
    {
        private readonly ObservableCollection<SceneObject> _sceneObjects = new();
        private SceneObject? _selectedObject;
        private readonly System.Collections.Generic.Dictionary<string, int> _nameCounters = new();

        public MainWindow()
        {
            InitializeComponent();
            SceneTreeList.ItemsSource = _sceneObjects;
            UpdateStatus();
        }

        // ── Вспомогательные ────────────────────────────────────

        private string NextName(string type)
        {
            _nameCounters.TryGetValue(type, out int count);
            _nameCounters[type] = count + 1;
            return $"{type}.{(count + 1):D3}";
        }

        private void SelectObject(SceneObject? obj)
        {
            _selectedObject = obj;

            if (obj == null)
            {
                ObjNameBox.Text    = "";
                PosXBox.Text       = "—";
                PosYBox.Text       = "—";
                PosZBox.Text       = "—";
                RotXBox.Text       = "—";
                RotYBox.Text       = "—";
                RotZBox.Text       = "—";
                ScaleXBox.Text     = "—";
                ScaleYBox.Text     = "—";
                ScaleZBox.Text     = "—";
                StatusSelected.Text = "Ничего не выбрано";
                return;
            }

            ObjNameBox.Text    = obj.Name;
            PosXBox.Text       = obj.PosX.ToString("F2");
            PosYBox.Text       = obj.PosY.ToString("F2");
            PosZBox.Text       = obj.PosZ.ToString("F2");
            RotXBox.Text       = obj.RotX.ToString("F0") + "°";
            RotYBox.Text       = obj.RotY.ToString("F0") + "°";
            RotZBox.Text       = obj.RotZ.ToString("F0") + "°";
            ScaleXBox.Text     = obj.ScaleX.ToString("F2");
            ScaleYBox.Text     = obj.ScaleY.ToString("F2");
            ScaleZBox.Text     = obj.ScaleZ.ToString("F2");
            StatusSelected.Text = $"Выбрано: {obj.Name}";

            // Синхронизируем панель материала
            var c = obj.DiffuseColor;
            ColorSwatch.Background = new SolidColorBrush(c);
            ColorHexLabel.Text     = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
            OpacitySlider.Value    = obj.Opacity;
            UpdateTextureLabel(obj.TexturePath);
        }

        private void UpdateTextureLabel(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                TexturePathLabel.Text       = "Текстура не выбрана";
                TexturePathLabel.Foreground = new SolidColorBrush(WpfColor.FromRgb(0x55, 0x55, 0x55));
                TexturePathLabel.ToolTip    = null;
            }
            else
            {
                TexturePathLabel.Text       = System.IO.Path.GetFileName(path);
                TexturePathLabel.Foreground = new SolidColorBrush(WpfColor.FromRgb(0x6B, 0xB3, 0x8A));
                TexturePathLabel.ToolTip    = path;
            }
        }

        private void UpdateStatus()
        {
            StatusObjects.Text   = $"Объектов: {_sceneObjects.Count}";
            EmptyHint.Visibility = _sceneObjects.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private static DiffuseMaterial MakeDiffuseMaterial(WpfColor color, double opacity)
        {
            var brush = new SolidColorBrush(
                WpfColor.FromArgb((byte)(opacity * 255), color.R, color.G, color.B));
            return new DiffuseMaterial(brush);
        }

        private static DiffuseMaterial MakeMaterial(WpfColor color, double opacity, string? texturePath)
        {
            if (!string.IsNullOrEmpty(texturePath))
            {
                try
                {
                    var bmp   = new BitmapImage(new Uri(texturePath));
                    var brush = new ImageBrush(bmp) { Opacity = opacity };
                    return new DiffuseMaterial(brush);
                }
                catch { /* fallback to color */ }
            }
            return MakeDiffuseMaterial(color, opacity);
        }

        private void RegisterObject(Visual3D visual, string type,
                                    double px, double py, double pz, WpfColor color)
        {
            var obj = new SceneObject
            {
                PrimitiveType = type,
                Visual        = visual,
                PosX = px, PosY = py, PosZ = pz,
                DiffuseColor  = color
            };
            obj.Name = NextName(type);  // after PrimitiveType is set
            _sceneObjects.Add(obj);
            Viewport3D.Children.Add(visual);
            SceneTreeList.SelectedItem = obj;
            SelectObject(obj);
            UpdateStatus();
        }

        private static void ApplyMaterialToModel(Model3D? model, Material mat)
        {
            if (model is GeometryModel3D gm)
            {
                gm.Material     = mat;
                gm.BackMaterial = mat;
            }
            else if (model is Model3DGroup grp)
            {
                foreach (var child in grp.Children)
                    ApplyMaterialToModel(child, mat);
            }
        }

        // ── Добавление примитива ────────────────────────────────

        private void AddPrimitive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PrimitiveDialog { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var color    = WpfColor.FromRgb(
                dialog.SelectedColor.R,
                dialog.SelectedColor.G,
                dialog.SelectedColor.B);
            var material = MakeDiffuseMaterial(color, 1.0);

            try
            {
                Visual3D newObject;

                switch (dialog.PrimitiveType)
                {
                    case "Cube":
                        newObject = new CubeVisual3D
                        {
                            Center     = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ),
                            SideLength = dialog.CubeSide,
                            Fill       = new SolidColorBrush(color),
                            BackMaterial = material
                        };
                        break;

                    case "Sphere":
                        newObject = new SphereVisual3D
                        {
                            Center = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ),
                            Radius = dialog.SphereRadius,
                            Fill   = new SolidColorBrush(color),
                            BackMaterial = material
                        };
                        break;

                    case "Cylinder":
                    {
                        var b  = new MeshBuilder();
                        var p1 = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                        var p2 = new Point3D(dialog.PosX, dialog.PosY + dialog.CylinderHeight, dialog.PosZ);
                        b.AddCylinder(p1, p2, dialog.CylinderDiameter / 2, 36, true, true);
                        var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                        newObject = new ModelVisual3D { Content = geo };
                        break;
                    }

                    case "Cone":
                    {
                        var b      = new MeshBuilder();
                        var origin = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                        var dir    = new Vector3D(0, 1, 0);
                        b.AddCone(origin, dir, dialog.ConeBaseRadius,
                                  dialog.ConeTopRadius, dialog.ConeHeight,
                                  true, true, 36);
                        var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                        newObject = new ModelVisual3D { Content = geo };
                        break;
                    }

                    case "Torus":
                    {
                        var b = new MeshBuilder();
                        b.AddTorus(dialog.TorusDiameter / 2, dialog.TorusTubeDiameter / 2, 36, 24);
                        var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                        var mv  = new ModelVisual3D { Content = geo };
                        var tg  = new Transform3DGroup();
                        tg.Children.Add(new TranslateTransform3D(dialog.PosX, dialog.PosY, dialog.PosZ));
                        mv.Transform = tg;
                        newObject    = mv;
                        break;
                    }

                    case "Pyramid":
                    {
                        var b   = new MeshBuilder();
                        double s = dialog.PyramidSide / 2;
                        double h = dialog.PyramidHeight;
                        double x = dialog.PosX, y = dialog.PosY, z = dialog.PosZ;
                        var apex = new Point3D(x, y + h, z);
                        b.AddTriangle(new Point3D(x-s,y,z-s), new Point3D(x+s,y,z-s), apex);
                        b.AddTriangle(new Point3D(x+s,y,z-s), new Point3D(x+s,y,z+s), apex);
                        b.AddTriangle(new Point3D(x+s,y,z+s), new Point3D(x-s,y,z+s), apex);
                        b.AddTriangle(new Point3D(x-s,y,z+s), new Point3D(x-s,y,z-s), apex);
                        b.AddQuad(new Point3D(x-s,y,z-s), new Point3D(x-s,y,z+s),
                                  new Point3D(x+s,y,z+s), new Point3D(x+s,y,z-s));
                        var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                        newObject = new ModelVisual3D { Content = geo };
                        break;
                    }

                    case "Ellipsoid":
                    {
                        var b = new MeshBuilder();
                        b.AddEllipsoid(
                            new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ),
                            dialog.EllipsoidRadiusX,
                            dialog.EllipsoidRadiusY,
                            dialog.EllipsoidRadiusZ, 32, 32);
                        var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                        newObject = new ModelVisual3D { Content = geo };
                        break;
                    }

                    case "Pipe":
                    {
                        var b  = new MeshBuilder();
                        var p1 = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                        var p2 = new Point3D(dialog.PosX, dialog.PosY + dialog.PipeLength, dialog.PosZ);
                        b.AddPipe(p1, p2, dialog.PipeInnerDiameter / 2, dialog.PipeOuterDiameter / 2, 36);
                        var geo = new GeometryModel3D(b.ToMesh(), material) { BackMaterial = material };
                        newObject = new ModelVisual3D { Content = geo };
                        break;
                    }

                    default:
                        WpfMessageBox.Show("Неизвестный тип примитива.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                }

                RegisterObject(newObject, dialog.PrimitiveType,
                               dialog.PosX, dialog.PosY, dialog.PosZ, color);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Ошибка добавления примитива:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Дерево сцены ───────────────────────────────────────

        private void SceneTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SceneTreeList.SelectedItem is SceneObject obj)
                SelectObject(obj);
        }

        private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton btn && btn.Tag is SceneObject obj)
                obj.IsVisible = !obj.IsVisible;
        }

        private void ObjName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_selectedObject != null && ObjNameBox.IsFocused)
                _selectedObject.Name = ObjNameBox.Text;
        }

        // ── Панель материала ───────────────────────────────────

        private void ColorSwatch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Используем стандартный WPF ColorDialog из HelixToolkit или Windows ColorPicker
            // Простой способ без конфликта — открыть кастомный диалог
            var dlg = new ColorPickerDialog(_selectedObject?.DiffuseColor ?? WpfColors.CornflowerBlue)
            {
                Owner = this
            };
            if (dlg.ShowDialog() != true) return;

            var wc = dlg.SelectedColor;
            ColorSwatch.Background = new SolidColorBrush(wc);
            ColorHexLabel.Text     = $"#{wc.R:X2}{wc.G:X2}{wc.B:X2}";
            if (_selectedObject != null)
                _selectedObject.DiffuseColor = wc;
        }

        private void MatSlider_Changed(object sender,
            System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityLabel != null)
                OpacityLabel.Text = OpacitySlider.Value.ToString("F2");
        }

        private void PickTexture_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject == null)
            {
                WpfMessageBox.Show("Сначала выберите объект.", "Текстура",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new WpfOpenFileDialog
            {
                Title  = "Выберите текстуру",
                Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            _selectedObject.TexturePath = dlg.FileName;
            UpdateTextureLabel(dlg.FileName);
        }

        private void ClearTexture_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject == null) return;
            _selectedObject.TexturePath = null;
            UpdateTextureLabel(null);
        }

        private void ApplyMaterial_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject?.Visual == null)
            {
                WpfMessageBox.Show("Выберите объект в дереве сцены.", "Материал",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var opacity = OpacitySlider.Value;
            _selectedObject.Opacity = opacity;

            var mat = MakeMaterial(_selectedObject.DiffuseColor, opacity, _selectedObject.TexturePath);

            if (_selectedObject.Visual is ModelVisual3D mv)
            {
                ApplyMaterialToModel(mv.Content, mat);
            }
            else if (_selectedObject.Visual is CubeVisual3D cube)
            {
                cube.Fill = mat.Brush;
            }
            else if (_selectedObject.Visual is SphereVisual3D sp)
            {
                sp.Fill = mat.Brush;
            }
        }

        // ── Вращение ───────────────────────────────────────────

        private void RotateX_Click(object sender, RoutedEventArgs e) => RotateSelected("X");
        private void RotateY_Click(object sender, RoutedEventArgs e) => RotateSelected("Y");
        private void RotateZ_Click(object sender, RoutedEventArgs e) => RotateSelected("Z");

        private void RotateSelected(string axis)
        {
            if (_selectedObject?.Visual == null)
            {
                WpfMessageBox.Show("Сначала выберите объект.", "Вращение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new InputDialog { Owner = this };
            if (dlg.ShowDialog() != true || dlg.Angle == null) return;

            double angle  = dlg.Angle.Value;
            var center    = new Point3D(_selectedObject.PosX, _selectedObject.PosY, _selectedObject.PosZ);
            var rotAxis   = axis switch
            {
                "X" => new Vector3D(1, 0, 0),
                "Y" => new Vector3D(0, 1, 0),
                _   => new Vector3D(0, 0, 1)
            };
            var rot3D = new AxisAngleRotation3D(rotAxis, angle);

            // Получить или создать Transform3DGroup
            Transform3DGroup tg;
            if (_selectedObject.Visual.Transform is Transform3DGroup existing)
            {
                tg = existing;
            }
            else
            {
                tg = new Transform3DGroup();
                if (_selectedObject.Visual.Transform != null)
                    tg.Children.Add(_selectedObject.Visual.Transform);
                _selectedObject.Visual.Transform = tg;
            }
            tg.Children.Add(new RotateTransform3D(rot3D, center));

            switch (axis)
            {
                case "X": _selectedObject.RotX = (_selectedObject.RotX + angle) % 360; break;
                case "Y": _selectedObject.RotY = (_selectedObject.RotY + angle) % 360; break;
                default:  _selectedObject.RotZ = (_selectedObject.RotZ + angle) % 360; break;
            }
            SelectObject(_selectedObject);
        }

        // ── Масштаб ────────────────────────────────────────────

        private void ScaleUp_Click(object sender, RoutedEventArgs e)   => ScaleSelected(1.25);
        private void ScaleDown_Click(object sender, RoutedEventArgs e) => ScaleSelected(0.8);

        private void ScaleSelected(double factor)
        {
            if (_selectedObject?.Visual == null)
            {
                WpfMessageBox.Show("Сначала выберите объект.", "Масштаб",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Transform3DGroup tg;
            if (_selectedObject.Visual.Transform is Transform3DGroup existing)
            {
                tg = existing;
            }
            else
            {
                tg = new Transform3DGroup();
                if (_selectedObject.Visual.Transform != null)
                    tg.Children.Add(_selectedObject.Visual.Transform);
                _selectedObject.Visual.Transform = tg;
            }
            tg.Children.Add(new ScaleTransform3D(
                factor, factor, factor,
                _selectedObject.PosX, _selectedObject.PosY, _selectedObject.PosZ));

            _selectedObject.ScaleX *= factor;
            _selectedObject.ScaleY *= factor;
            _selectedObject.ScaleZ *= factor;
            SelectObject(_selectedObject);
        }

        // ── Удаление ───────────────────────────────────────────

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedObject == null)
            {
                WpfMessageBox.Show("Выберите объект для удаления.", "Удаление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var res = WpfMessageBox.Show(
                $"Удалить «{_selectedObject.Name}»?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            if (_selectedObject.Visual != null)
                Viewport3D.Children.Remove(_selectedObject.Visual);

            _sceneObjects.Remove(_selectedObject);
            SelectObject(null);
            UpdateStatus();
        }

        // ── Импорт ─────────────────────────────────────────────

        private void ImportModel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new WpfOpenFileDialog
            {
                Title  = "Импорт 3D модели",
                Filter = "3D модели|*.obj;*.stl;*.3ds|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var reader = new ObjReader();
                var model  = reader.Read(dlg.FileName);
                var visual = new ModelVisual3D { Content = model };

                RegisterObject(visual, "Import", 0, 0, 0, WpfColors.Gray);
                // Переименовать в имя файла
                if (_sceneObjects.Count > 0)
                {
                    _sceneObjects[^1].Name =
                        System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
                    SelectObject(_sceneObjects[^1]);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Ошибка импорта:\n{ex.Message}",
                    "Импорт", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Камера ─────────────────────────────────────────────

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            // ZoomExtents — метод расширения из HelixToolkit
            Viewport3D.ZoomExtents(500);
        }

        // ── Вид ────────────────────────────────────────────────

        private void MenuPerspective_Click(object sender, RoutedEventArgs e)
            => ViewportLabel.Text = "Перспектива | Затенение";

        private void MenuWireframe_Click(object sender, RoutedEventArgs e)
        {
            bool isWire = ViewportLabel.Text.Contains("Каркас");
            ViewportLabel.Text = isWire
                ? "Перспектива | Затенение"
                : "Перспектива | Каркас";
        }

        // ── Файловое меню ──────────────────────────────────────

        private void MenuNew_Click(object sender, RoutedEventArgs e)
        {
            if (_sceneObjects.Count > 0)
            {
                var r = WpfMessageBox.Show(
                    "Создать новый проект? Несохранённые изменения будут потеряны.",
                    "Новый проект", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
            }

            var toRemove = new System.Collections.Generic.List<Visual3D>();
            foreach (var o in _sceneObjects)
                if (o.Visual != null) toRemove.Add(o.Visual);
            foreach (var v in toRemove)
                Viewport3D.Children.Remove(v);

            _sceneObjects.Clear();
            _nameCounters.Clear();
            SelectObject(null);
            UpdateStatus();
        }

        private void MenuOpen_Click(object sender, RoutedEventArgs e)
            => WpfMessageBox.Show("Открытие проекта пока не реализовано.", "Открыть",
                MessageBoxButton.OK, MessageBoxImage.Information);

        private void MenuSave_Click(object sender, RoutedEventArgs e)
            => WpfMessageBox.Show("Сохранение проекта пока не реализовано.", "Сохранить",
                MessageBoxButton.OK, MessageBoxImage.Information);

        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();
    }
}
