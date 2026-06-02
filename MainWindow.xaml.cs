using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;


using WpfColor       = System.Windows.Media.Color;
using WpfColors      = System.Windows.Media.Colors;
using WpfMessageBox  = System.Windows.MessageBox;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfSaveFileDialog = Microsoft.Win32.SaveFileDialog;
using WpfButton      = System.Windows.Controls.Button;

namespace ModelingAppWPF
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel = new();
        private readonly ObservableCollection<SceneObject> _sceneObjects;
        private SceneObject? _selectedObject;
        private readonly List<BoundingBoxVisual3D> _selectionBounds = new();
        private readonly List<Visual3D> _gizmoVisuals = new();
        private readonly Dictionary<Visual3D, string> _gizmoAxes = new();
        private bool _isUpdatingSelection;
        private bool _isDraggingGizmo;
        private string? _activeGizmoAxis;
        private Point? _gizmoDragStartPoint;
        private Point3D _gizmoDragOrigin;
        private double _gizmoDragLastDistance;
        private Point? _viewportMouseDownPoint;
        private readonly System.Collections.Generic.Dictionary<string, int> _nameCounters = new();
        private const double GridHalfSize = 10.0;
        private const double GridStep = 1.0;
        private const double ClickMoveTolerance = 4.0;
        private const double GizmoLength = 2.2;
        private const double GizmoDiameter = 0.08;
        private const double GizmoHeadLength = 0.35;

        public MainWindow()
        {
            _sceneObjects = _viewModel.SceneObjects;
            InitializeComponent();
            DataContext = _viewModel;
            _viewModel.SelectObjectsRequested += SelectObjects;
            _viewModel.DeleteSelectedRequested += DeleteSelectedFromViewModel;
            _viewModel.DuplicateSelectedRequested += DuplicateSelectedFromViewModel;
            _viewModel.NewProjectRequested += NewProjectFromViewModel;
            _viewModel.OpenProjectRequested += OpenProjectFromViewModel;
            _viewModel.SaveProjectRequested += SaveProjectFromViewModel;
            _viewModel.SaveProjectAsRequested += SaveProjectAsFromViewModel;
            _viewModel.ImportModelRequested += ImportModelFromViewModel;
            AddCoordinateGrid();
            SceneTreeList.ItemsSource = _sceneObjects;
            UpdateStatus();
        }

        
        private void AddCoordinateGrid()
        {
            var grid = new GridLinesVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Normal = new Vector3D(0, 0, 1),
                Width = GridHalfSize * 2,
                Length = GridHalfSize * 2,
                MinorDistance = GridStep,
                MajorDistance = GridStep * 5,
                Thickness = 0.01,
                Fill = new SolidColorBrush(WpfColor.FromRgb(0x3A, 0x3A, 0x3A))
            };

            var xAxis = new LinesVisual3D
            {
                Color = WpfColor.FromRgb(0xE0, 0x70, 0x70),
                Thickness = 2
            };
            xAxis.Points.Add(new Point3D(-GridHalfSize, 0, 0.002));
            xAxis.Points.Add(new Point3D(GridHalfSize, 0, 0.002));

            var yAxis = new LinesVisual3D
            {
                Color = WpfColor.FromRgb(0x70, 0xC0, 0x70),
                Thickness = 2
            };
            yAxis.Points.Add(new Point3D(0, -GridHalfSize, 0.002));
            yAxis.Points.Add(new Point3D(0, GridHalfSize, 0.002));

            Viewport3D.Children.Add(grid);
            Viewport3D.Children.Add(xAxis);
            Viewport3D.Children.Add(yAxis);
        }

        

        private string NextName(string type)
        {
            _nameCounters.TryGetValue(type, out int count);
            _nameCounters[type] = count + 1;
            return $"{type}.{(count + 1):D3}";
        }

        private List<SceneObject> GetSelectedObjects()
            => SceneTreeList.SelectedItems.Cast<SceneObject>().ToList();

        private bool IsObjectSelected(SceneObject obj)
            => SceneTreeList.SelectedItems.Contains(obj);

        private void SelectObject(SceneObject? obj)
        {
            if (obj == null)
            {
                SelectObjects(Array.Empty<SceneObject>());
                return;
            }

            SelectObjects(new[] { obj });
        }

        private void SelectObjects(IEnumerable<SceneObject> objects)
        {
            var selected = objects
                .Where(o => _sceneObjects.Contains(o))
                .Distinct()
                .ToList();

            _isUpdatingSelection = true;
            try
            {
                SceneTreeList.SelectedItems.Clear();
                foreach (var obj in selected)
                    SceneTreeList.SelectedItems.Add(obj);
            }
            finally
            {
                _isUpdatingSelection = false;
            }

            UpdateSelection(selected);
        }

        private void UpdateSelection(IReadOnlyList<SceneObject> selected)
        {
            _selectedObject = selected.Count > 0 ? selected[^1] : null;
            _viewModel.SetSelectedObjects(selected);
            UpdateSelectionBounds(selected);
            UpdateGizmo(selected);
            UpdateSelectionDetails(selected);
        }

        private void UpdateSelectionDetails(IReadOnlyList<SceneObject> selected)
        {
            if (selected.Count == 0)
            {
                ObjNameBox.IsReadOnly = false;
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
                ColorSwatch.Background = new SolidColorBrush(WpfColor.FromRgb(0x3B, 0x82, 0xF6));
                ColorHexLabel.Text = "#3B82F6";
                UpdateTextureLabel(null);
                return;
            }

            if (selected.Count > 1)
            {
                ObjNameBox.IsReadOnly = true;
                ObjNameBox.Text    = "Несколько";
                PosXBox.Text       = "—";
                PosYBox.Text       = "—";
                PosZBox.Text       = "—";
                RotXBox.Text       = "—";
                RotYBox.Text       = "—";
                RotZBox.Text       = "—";
                ScaleXBox.Text     = "—";
                ScaleYBox.Text     = "—";
                ScaleZBox.Text     = "—";
                StatusSelected.Text = $"Выбрано: {selected.Count}";

                var c = selected[^1].DiffuseColor;
                ColorSwatch.Background = new SolidColorBrush(c);
                ColorHexLabel.Text     = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
                OpacitySlider.Value    = selected[^1].Opacity;
                TexturePathLabel.Text       = "Несколько объектов";
                TexturePathLabel.Foreground = new SolidColorBrush(WpfColor.FromRgb(0x88, 0x88, 0x88));
                TexturePathLabel.ToolTip    = null;
                return;
            }

            var obj = selected[0];
            ObjNameBox.IsReadOnly = false;
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

            var color = obj.DiffuseColor;
            ColorSwatch.Background = new SolidColorBrush(color);
            ColorHexLabel.Text     = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            OpacitySlider.Value    = obj.Opacity;
            UpdateTextureLabel(obj.TexturePath);
        }

        private void UpdateSelectionBounds(IEnumerable<SceneObject> selected)
        {
            foreach (var bound in _selectionBounds)
                Viewport3D.Children.Remove(bound);
            _selectionBounds.Clear();

            foreach (var obj in selected)
            {
                if (obj.Visual == null || !obj.IsVisible)
                    continue;

                var bounds = Visual3DHelper.FindBounds(obj.Visual, Transform3D.Identity);
                if (bounds.IsEmpty)
                    continue;

                bounds = InflateBounds(bounds);
                var selectionBound = new BoundingBoxVisual3D
                {
                    BoundingBox = bounds,
                    Fill = new SolidColorBrush(WpfColor.FromRgb(0xFF, 0xD4, 0x2A))
                };
                _selectionBounds.Add(selectionBound);
                Viewport3D.Children.Add(selectionBound);
            }
        }
        private static Rect3D InflateBounds(Rect3D bounds)
        {
            double margin = Math.Max(Math.Max(bounds.SizeX, bounds.SizeY), bounds.SizeZ) * 0.04;
            margin = Math.Max(margin, 0.08);
            return new Rect3D(
                bounds.X - margin,
                bounds.Y - margin,
                bounds.Z - margin,
                bounds.SizeX + margin * 2,
                bounds.SizeY + margin * 2,
                bounds.SizeZ + margin * 2);
        }

        private void UpdateGizmo(IEnumerable<SceneObject> selected)
        {
            RemoveGizmo();

            var visibleSelected = selected
                .Where(obj => obj.Visual != null && obj.IsVisible)
                .ToList();
            if (visibleSelected.Count == 0)
                return;

            var center = GetGizmoCenter(visibleSelected);
            AddGizmoArrow("X", center, new Vector3D(1, 0, 0), WpfColor.FromRgb(0xE0, 0x70, 0x70));
            AddGizmoArrow("Y", center, new Vector3D(0, 1, 0), WpfColor.FromRgb(0x70, 0xC0, 0x70));
            AddGizmoArrow("Z", center, new Vector3D(0, 0, 1), WpfColor.FromRgb(0x70, 0x90, 0xE0));

            var origin = new SphereVisual3D
            {
                Center = center,
                Radius = 0.12,
                Fill = new SolidColorBrush(WpfColor.FromRgb(0xF2, 0xD0, 0x55))
            };
            _gizmoVisuals.Add(origin);
            Viewport3D.Children.Add(origin);
        }

        private void RemoveGizmo()
        {
            foreach (var visual in _gizmoVisuals)
                Viewport3D.Children.Remove(visual);

            _gizmoVisuals.Clear();
            _gizmoAxes.Clear();
        }

        private void AddGizmoArrow(string axis, Point3D center, Vector3D direction, WpfColor color)
        {
            var arrow = new ArrowVisual3D
            {
                Point1 = center,
                Point2 = center + direction * GizmoLength,
                Diameter = GizmoDiameter,
                HeadLength = GizmoHeadLength,
                Fill = new SolidColorBrush(color)
            };

            _gizmoVisuals.Add(arrow);
            _gizmoAxes[arrow] = axis;
            Viewport3D.Children.Add(arrow);
        }

        private static Point3D GetGizmoCenter(IReadOnlyList<SceneObject> selected)
        {
            return new Point3D(
                selected.Average(obj => obj.PosX),
                selected.Average(obj => obj.PosY),
                selected.Average(obj => obj.PosZ));
        }

        private string? FindGizmoAxisAt(Point point)
        {
            foreach (var hit in Viewport3D.Viewport.FindHits(point))
            {
                foreach (var pair in _gizmoAxes)
                {
                    if (ReferenceEquals(hit.Visual, pair.Key) || ContainsModel(pair.Key, hit.Model))
                        return pair.Value;
                }
            }

            return null;
        }

        private bool IsGizmoAt(Point point)
        {
            foreach (var hit in Viewport3D.Viewport.FindHits(point))
            {
                if (_gizmoVisuals.Any(visual => ReferenceEquals(hit.Visual, visual) || ContainsModel(visual, hit.Model)))
                    return true;
            }

            return false;
        }
        private static Vector3D AxisVector(string axis)
        {
            return axis switch
            {
                "X" => new Vector3D(1, 0, 0),
                "Y" => new Vector3D(0, 1, 0),
                _ => new Vector3D(0, 0, 1)
            };
        }

        private double GetGizmoDistance(Point currentPoint, Point startPoint, Point3D origin, string axis)
        {
            var axisVector = AxisVector(axis);
            var origin2D = Viewport3DHelper.Point3DtoPoint2D(Viewport3D.Viewport, origin);
            var axis2D = Viewport3DHelper.Point3DtoPoint2D(Viewport3D.Viewport, origin + axisVector);
            var screenAxis = axis2D - origin2D;
            var pixelsPerUnit = screenAxis.Length;
            if (pixelsPerUnit < 0.001)
                return 0;

            screenAxis.Normalize();
            var mouseDelta = currentPoint - startPoint;
            return (mouseDelta.X * screenAxis.X + mouseDelta.Y * screenAxis.Y) / pixelsPerUnit;
        }

        private void MoveSelectedByVector(Vector3D delta)
        {
            var selected = GetSelectedObjects().Where(obj => obj.Visual != null && obj.IsVisible).ToList();
            if (selected.Count == 0)
                return;

            foreach (var obj in selected)
                TransformService.MoveByVector(obj, delta.X, delta.Y, delta.Z);

            UpdateSelection(selected);
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
            _viewModel.NotifySceneChanged();
        }
        private SceneObject RegisterObject(Visual3D visual, string type,
                                    double px, double py, double pz, WpfColor color,
                                    double opacity = 1.0, string? texturePath = null,
                                    Dictionary<string, double>? parameters = null,
                                    string? sourcePath = null, string? name = null,
                                    bool isVisible = true)
        {
            var obj = new SceneObject
            {
                PrimitiveType = type,
                Visual        = visual,
                PosX = px, PosY = py, PosZ = pz,
                DiffuseColor  = color,
                Opacity       = opacity,
                TexturePath   = texturePath,
                SourcePath    = sourcePath,
                Parameters    = parameters != null ? new Dictionary<string, double>(parameters) : new Dictionary<string, double>()
            };
            obj.Name = string.IsNullOrWhiteSpace(name) ? NextName(type) : name;
            obj.IsVisible = isVisible;  
_sceneObjects.Add(obj);
            Viewport3D.Children.Add(visual);
            SceneTreeList.SelectedItem = obj;
            SelectObject(obj);
            UpdateStatus();
            return obj;
        }
        private void AddPrimitive_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PrimitiveDialog { Owner = this };
            if (dialog.ShowDialog() != true) return;

            var color = WpfColor.FromRgb(
                dialog.SelectedColor.R,
                dialog.SelectedColor.G,
                dialog.SelectedColor.B);
            var parameters = PrimitiveFactory.CreateParameters(dialog);

            try
            {
                var newObject = PrimitiveFactory.CreateVisual(dialog.PrimitiveType,
                    dialog.PosX, dialog.PosY, dialog.PosZ, color, 1.0, null, parameters);

                RegisterObject(newObject, dialog.PrimitiveType,
                    dialog.PosX, dialog.PosY, dialog.PosZ, color,
                    parameters: parameters);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Ошибка добавления примитива:\n{ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       

        private void SceneTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingSelection)
                return;

            UpdateSelection(GetSelectedObjects());
        }

        private void Viewport3D_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var point = e.GetPosition(Viewport3D);
            var gizmoAxis = FindGizmoAxisAt(point);
            if (gizmoAxis != null)
            {
                _isDraggingGizmo = true;
                _activeGizmoAxis = gizmoAxis;
                _gizmoDragStartPoint = point;
                _gizmoDragOrigin = GetGizmoCenter(GetSelectedObjects().Where(obj => obj.Visual != null && obj.IsVisible).ToList());
                _gizmoDragLastDistance = 0;
                _viewportMouseDownPoint = null;
                Viewport3D.CaptureMouse();
                Viewport3D.Cursor = System.Windows.Input.Cursors.SizeAll;
                e.Handled = true;
                return;
            }

            _viewportMouseDownPoint = point;
        }

        private void Viewport3D_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDraggingGizmo || _activeGizmoAxis == null || _gizmoDragStartPoint == null)
                return;

            var currentPoint = e.GetPosition(Viewport3D);
            var distance = GetGizmoDistance(currentPoint, _gizmoDragStartPoint.Value, _gizmoDragOrigin, _activeGizmoAxis);
            var deltaDistance = distance - _gizmoDragLastDistance;
            if (Math.Abs(deltaDistance) < 0.001)
                return;

            _gizmoDragLastDistance = distance;
            MoveSelectedByVector(AxisVector(_activeGizmoAxis) * deltaDistance);
            e.Handled = true;
        }

        private void Viewport3D_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isDraggingGizmo)
            {
                _isDraggingGizmo = false;
                _activeGizmoAxis = null;
                _gizmoDragStartPoint = null;
                _gizmoDragLastDistance = 0;
                Viewport3D.ReleaseMouseCapture();
                Viewport3D.Cursor = null;
                e.Handled = true;
                return;
            }

            if (_viewportMouseDownPoint == null)
                return;

            var mouseUpPoint = e.GetPosition(Viewport3D);
            var delta = mouseUpPoint - _viewportMouseDownPoint.Value;
            _viewportMouseDownPoint = null;

            if (Math.Abs(delta.X) > ClickMoveTolerance || Math.Abs(delta.Y) > ClickMoveTolerance)
                return;

            SelectObjectFromViewport(mouseUpPoint);
        }

        private void SelectObjectFromViewport(Point point)
        {
            if (IsGizmoAt(point))
                return;

            var clickedObject = FindSceneObjectAt(point);
            var modifiers = System.Windows.Input.Keyboard.Modifiers;
            var selected = GetSelectedObjects();

            if (clickedObject == null)
            {
                if ((modifiers & System.Windows.Input.ModifierKeys.Control) == 0 &&
                    (modifiers & System.Windows.Input.ModifierKeys.Shift) == 0)
                    SelectObject(null);
                return;
            }

            if ((modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (selected.Contains(clickedObject))
                    selected.Remove(clickedObject);
                else
                    selected.Add(clickedObject);
                SelectObjects(selected);
            }
            else if ((modifiers & System.Windows.Input.ModifierKeys.Shift) != 0)
            {
                if (!selected.Contains(clickedObject))
                    selected.Add(clickedObject);
                SelectObjects(selected);
            }
            else
            {
                SelectObject(clickedObject);
            }

            Viewport3D.Focus();
        }

        private SceneObject? FindSceneObjectAt(Point point)
        {
            foreach (var hit in Viewport3D.Viewport.FindHits(point))
            {
                if (_selectionBounds.Any(bound => ReferenceEquals(hit.Visual, bound)) ||
                    _gizmoVisuals.Any(visual => ReferenceEquals(hit.Visual, visual) || ContainsModel(visual, hit.Model)))
                    continue;

                foreach (var obj in _sceneObjects)
                {
                    if (!obj.IsVisible || obj.Visual == null)
                        continue;

                    if (ReferenceEquals(hit.Visual, obj.Visual) || ContainsModel(obj.Visual, hit.Model))
                        return obj;
                }
            }

            return null;
        }

        private static bool ContainsModel(Visual3D visual, Model3D? model)
        {
            if (model == null)
                return false;

            return visual is ModelVisual3D modelVisual && ContainsModel(modelVisual.Content, model);
        }

        private static bool ContainsModel(Model3D? root, Model3D target)
        {
            if (root == null)
                return false;

            if (ReferenceEquals(root, target))
                return true;

            if (root is Model3DGroup group)
            {
                foreach (var child in group.Children)
                    if (ContainsModel(child, target))
                        return true;
            }

            return false;
        }

        private void ToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton btn && btn.Tag is SceneObject obj)
            {
                obj.IsVisible = !obj.IsVisible;
                if (IsObjectSelected(obj))
                    UpdateSelection(GetSelectedObjects());
            }
        }

        private void ObjName_TextChanged(object sender, TextChangedEventArgs e)
        {
            var selected = GetSelectedObjects();
            if (selected.Count == 1 && ObjNameBox.IsFocused)
                selected[0].Name = ObjNameBox.Text;
        }
        private void TransformField_LostFocus(object sender, RoutedEventArgs e)
        {
            ApplyTransformFields();
        }

        private void TransformField_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                ApplyTransformFields();
                Viewport3D.Focus();
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                UpdateSelection(GetSelectedObjects());
                Viewport3D.Focus();
            }
        }

        private void ApplyTransformFields()
        {
            var selected = GetSelectedObjects();
            if (selected.Count != 1 || selected[0].Visual == null)
                return;

            var obj = selected[0];
            if (!TryParseDouble(PosXBox.Text, out double posX) ||
                !TryParseDouble(PosYBox.Text, out double posY) ||
                !TryParseDouble(PosZBox.Text, out double posZ) ||
                !TryParseAngle(RotXBox.Text, out double rotX) ||
                !TryParseAngle(RotYBox.Text, out double rotY) ||
                !TryParseAngle(RotZBox.Text, out double rotZ) ||
                !TryParseDouble(ScaleXBox.Text, out double scaleX) || scaleX <= 0 ||
                !TryParseDouble(ScaleYBox.Text, out double scaleY) || scaleY <= 0 ||
                !TryParseDouble(ScaleZBox.Text, out double scaleZ) || scaleZ <= 0)
            {
                WpfMessageBox.Show("Введите корректные значения позиции, вращения и масштаба.",
                    "Свойства", MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateSelection(selected);
                return;
            }

            TransformService.MoveByVector(obj, posX - obj.PosX, posY - obj.PosY, posZ - obj.PosZ);
            TransformService.RotateByAngle(obj, "X", rotX - obj.RotX);
            TransformService.RotateByAngle(obj, "Y", rotY - obj.RotY);
            TransformService.RotateByAngle(obj, "Z", rotZ - obj.RotZ);
            TransformService.ScaleByFactor(obj, scaleX / obj.ScaleX, "X");
            TransformService.ScaleByFactor(obj, scaleY / obj.ScaleY, "Y");
            TransformService.ScaleByFactor(obj, scaleZ / obj.ScaleZ, "Z");

            UpdateSelection(selected);
        }

        private static bool TryParseAngle(string text, out double value)
            => TryParseDouble(text.Replace("°", "").Trim(), out value);

       

        private void ColorSwatch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selected = GetSelectedObjects();
            var initialColor = selected.Count > 0 ? selected[^1].DiffuseColor : WpfColors.CornflowerBlue;
            var dlg = new ColorPickerDialog(initialColor)
            {
                Owner = this
            };
            if (dlg.ShowDialog() != true) return;

            var wc = dlg.SelectedColor;
            ColorSwatch.Background = new SolidColorBrush(wc);
            ColorHexLabel.Text     = $"#{wc.R:X2}{wc.G:X2}{wc.B:X2}";

            foreach (var obj in selected)
                obj.DiffuseColor = wc;
        }

        private void MatSlider_Changed(object sender,
            System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityLabel != null)
                OpacityLabel.Text = OpacitySlider.Value.ToString("F2");
        }

        private void PickTexture_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedObjects();
            if (selected.Count == 0)
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

            foreach (var obj in selected)
                obj.TexturePath = dlg.FileName;
            UpdateSelection(selected);
        }

        private void ClearTexture_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedObjects();
            if (selected.Count == 0) return;

            foreach (var obj in selected)
                obj.TexturePath = null;
            UpdateSelection(selected);
        }

        private void ApplyMaterial_Click(object sender, RoutedEventArgs e)
        {
            var selected = GetSelectedObjects().Where(obj => obj.Visual != null).ToList();
            if (selected.Count == 0)
            {
                WpfMessageBox.Show("Выберите объект в дереве сцены.", "Материал",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var opacity = OpacitySlider.Value;
            foreach (var obj in selected)
            {
                obj.Opacity = opacity;
                ApplyMaterialToObject(obj);
            }

            UpdateSelection(selected);
        }
        private static void ApplyMaterialToObject(SceneObject obj)
            => MaterialService.ApplyMaterialToObject(obj);

       

        private void RotateX_Click(object sender, RoutedEventArgs e) => RotateSelected("X");
        private void RotateY_Click(object sender, RoutedEventArgs e) => RotateSelected("Y");
        private void RotateZ_Click(object sender, RoutedEventArgs e) => RotateSelected("Z");

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (MoveMenuPopup.IsOpen && e.OriginalSource is not TextBox && TryGetAxisKey(e.Key, out string moveAxis))
            {
                e.Handled = true;
                ApplyMoveMenu(moveAxis);
                return;
            }

            if (ScaleMenuPopup.IsOpen && e.OriginalSource is not TextBox && TryGetAxisKey(e.Key, out string scaleAxis))
            {
                e.Handled = true;
                ApplyScaleMenu(scaleAxis);
                return;
            }

            if (e.OriginalSource is TextBox)
                return;

            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0)
            {
                if (e.Key == System.Windows.Input.Key.A)
                {
                    e.Handled = true;
                    SelectAll_Click(sender, e);
                    return;
                }
                if (e.Key == System.Windows.Input.Key.D)
                {
                    e.Handled = true;
                    DuplicateSelected_Click(sender, e);
                    return;
                }
            }

            if (e.Key == System.Windows.Input.Key.G)
            {
                e.Handled = true;
                ShowMoveMenu();
            }
            else if (e.Key == System.Windows.Input.Key.R)
            {
                e.Handled = true;
                ShowRotateMenu();
            }
            else if (e.Key == System.Windows.Input.Key.S)
            {
                e.Handled = true;
                ShowScaleMenu();
            }
            else if (e.Key == System.Windows.Input.Key.Escape && (MoveMenuPopup.IsOpen || RotateMenuPopup.IsOpen || ScaleMenuPopup.IsOpen))
            {
                e.Handled = true;
                MoveMenuPopup.IsOpen = false;
                RotateMenuPopup.IsOpen = false;
                ScaleMenuPopup.IsOpen = false;
                Viewport3D.Focus();
            }
        }

        private void ShowMoveMenu_Click(object sender, RoutedEventArgs e) => ShowMoveMenu();

        private void ShowMoveMenu()
        {
            if (!EnsureSelectedVisibleObject("Перемещение"))
                return;

            var point = GetViewportPopupPoint();
            RotateMenuPopup.IsOpen = false;
            ScaleMenuPopup.IsOpen = false;
            MoveMenuPopup.HorizontalOffset = point.X;
            MoveMenuPopup.VerticalOffset = point.Y;
            MoveMenuPopup.IsOpen = true;
            MoveDistanceBox.SelectAll();
            MoveDistanceBox.Focus();
        }

        private void MoveContextAxis_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton button && button.Tag is string axis)
                ApplyMoveMenu(axis);
        }

        private void MoveDistanceBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (TryGetAxisKey(e.Key, out string axis))
            {
                e.Handled = true;
                ApplyMoveMenu(axis);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                MoveMenuPopup.IsOpen = false;
                Viewport3D.Focus();
            }
        }

        private void ApplyMoveMenu(string axis)
        {
            if (!TryParseDouble(MoveDistanceBox.Text, out double distance))
            {
                WpfMessageBox.Show("Введите корректное расстояние перемещения.", "Перемещение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                MoveDistanceBox.SelectAll();
                MoveDistanceBox.Focus();
                return;
            }

            MoveMenuPopup.IsOpen = false;
            MoveSelectedByDistance(axis, distance);
            Viewport3D.Focus();
        }

        private void ShowRotateMenu()
        {
            if (!EnsureSelectedVisibleObject("Вращение"))
                return;

            var point = GetViewportPopupPoint();
            MoveMenuPopup.IsOpen = false;
            ScaleMenuPopup.IsOpen = false;
            RotateMenuPopup.HorizontalOffset = point.X;
            RotateMenuPopup.VerticalOffset = point.Y;
            RotateMenuPopup.IsOpen = true;
            RotateAngleBox.SelectAll();
            RotateAngleBox.Focus();
        }

        private bool EnsureSelectedVisibleObject(string title)
        {
            if (GetSelectedObjects().Any(obj => obj.Visual != null && obj.IsVisible))
                return true;

            WpfMessageBox.Show("Сначала выберите видимый объект.", title,
                MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        private Point GetViewportPopupPoint()
        {
            var point = System.Windows.Input.Mouse.GetPosition(Viewport3D);
            if (point.X < 0 || point.Y < 0 || point.X > Viewport3D.ActualWidth || point.Y > Viewport3D.ActualHeight)
                point = new Point(Math.Max(0, Viewport3D.ActualWidth / 2 - 95), Math.Max(0, Viewport3D.ActualHeight / 2 - 55));

            return new Point(point.X + 10, point.Y + 10);
        }

        private void RotateContextAxis_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton button && button.Tag is string axis)
                ApplyRotateMenu(axis);
        }

        private void RotateAngleBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                ApplyRotateMenu("Z");
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                RotateMenuPopup.IsOpen = false;
                Viewport3D.Focus();
            }
        }

        private static bool TryParseDouble(string text, out double value)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                return true;

            return double.TryParse(text.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
        private void ApplyRotateMenu(string axis)
        {
            if (!TryParseDouble(RotateAngleBox.Text, out double angle))
            {
                WpfMessageBox.Show("Введите корректный угол поворота.", "Вращение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                RotateAngleBox.SelectAll();
                RotateAngleBox.Focus();
                return;
            }

            RotateMenuPopup.IsOpen = false;
            RotateSelectedByAngle(axis, angle);
            Viewport3D.Focus();
        }

        private void ShowScaleMenu_Click(object sender, RoutedEventArgs e) => ShowScaleMenu();

        private void ShowScaleMenu()
        {
            if (!EnsureSelectedVisibleObject("Масштаб"))
                return;

            var point = GetViewportPopupPoint();
            MoveMenuPopup.IsOpen = false;
            RotateMenuPopup.IsOpen = false;
            ScaleMenuPopup.HorizontalOffset = point.X;
            ScaleMenuPopup.VerticalOffset = point.Y;
            ScaleMenuPopup.IsOpen = true;
            ScaleFactorBox.SelectAll();
            ScaleFactorBox.Focus();
        }

        private void ApplyScaleMenu_Click(object sender, RoutedEventArgs e) => ApplyScaleMenu(null);

        private void ScaleContextAxis_Click(object sender, RoutedEventArgs e)
        {
            if (sender is WpfButton button && button.Tag is string axis)
                ApplyScaleMenu(axis);
        }

        private void ScaleFactorBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (TryGetAxisKey(e.Key, out string axis))
            {
                e.Handled = true;
                ApplyScaleMenu(axis);
            }
            else if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true;
                ApplyScaleMenu(null);
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                e.Handled = true;
                ScaleMenuPopup.IsOpen = false;
                Viewport3D.Focus();
            }
        }

        private static bool TryGetAxisKey(System.Windows.Input.Key key, out string axis)
        {
            axis = key switch
            {
                System.Windows.Input.Key.X => "X",
                System.Windows.Input.Key.Y => "Y",
                System.Windows.Input.Key.Z => "Z",
                _ => string.Empty
            };

            return axis.Length > 0;
        }

        private void ApplyScaleMenu(string? axis)
        {
            if (!TryParseDouble(ScaleFactorBox.Text, out double factor) || factor <= 0)
            {
                WpfMessageBox.Show("Введите положительный коэффициент масштаба.", "Масштаб",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ScaleFactorBox.SelectAll();
                ScaleFactorBox.Focus();
                return;
            }

            ScaleMenuPopup.IsOpen = false;
            ScaleSelectedByFactor(factor, axis);
            Viewport3D.Focus();
        }

        private void MoveSelectedByDistance(string axis, double distance)
        {
            var selected = GetSelectedObjects().Where(obj => obj.Visual != null && obj.IsVisible).ToList();
            if (selected.Count == 0)
                return;

            foreach (var obj in selected)
                TransformService.MoveByDistance(obj, axis, distance);

            UpdateSelection(GetSelectedObjects());
        }
        private void RotateSelected(string axis)
        {
            if (GetSelectedObjects().All(obj => obj.Visual == null || !obj.IsVisible))
            {
                WpfMessageBox.Show("Сначала выберите объект.", "Вращение",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new InputDialog { Owner = this };
            if (dlg.ShowDialog() != true || dlg.Angle == null) return;

            RotateSelectedByAngle(axis, dlg.Angle.Value);
        }

        private void RotateSelectedByAngle(string axis, double angle)
        {
            var selected = GetSelectedObjects().Where(obj => obj.Visual != null && obj.IsVisible).ToList();
            if (selected.Count == 0)
                return;

            foreach (var obj in selected)
                TransformService.RotateByAngle(obj, axis, angle);

            UpdateSelection(GetSelectedObjects());
        }
        private void ScaleSelectedByFactor(double factor, string? axis)
        {
            var selected = GetSelectedObjects().Where(obj => obj.Visual != null && obj.IsVisible).ToList();
            if (selected.Count == 0)
                return;

            foreach (var obj in selected)
                TransformService.ScaleByFactor(obj, factor, axis);

            UpdateSelection(GetSelectedObjects());
        }
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.SelectAllCommand.Execute(null);
            Viewport3D.Focus();
        }

        private void DuplicateSelected_Click(object sender, RoutedEventArgs e)
            => _viewModel.DuplicateSelectedCommand.Execute(null);

        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
            => _viewModel.DeleteSelectedCommand.Execute(null);

        private void DuplicateSelectedFromViewModel(IReadOnlyList<SceneObject> selected)
        {
            var source = selected.Where(obj => obj.Visual != null).ToList();
            if (source.Count == 0)
            {
                WpfMessageBox.Show("Выберите объект для дублирования.", "Дублировать",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var created = new List<SceneObject>();
            foreach (var obj in source)
            {
                var data = SceneProjectSerializer.ToData(obj);
                data.Name = string.IsNullOrWhiteSpace(obj.Name) ? null : obj.Name + " Copy";
                data.PosX += 0.5;
                data.PosY += 0.5;
                var duplicate = CreateObjectFromData(data);
                if (duplicate != null)
                    created.Add(duplicate);
            }

            if (created.Count > 0)
                SelectObjects(created);
        }

        private void DeleteSelectedFromViewModel(IReadOnlyList<SceneObject> selected)
        {
            if (selected.Count == 0)
            {
                WpfMessageBox.Show("Выберите объект для удаления.", "Удаление",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var message = selected.Count == 1
                ? $"Удалить «{selected[0].Name}»?"
                : $"Удалить выбранные объекты ({selected.Count})?";
            var res = WpfMessageBox.Show(
                message, "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res != MessageBoxResult.Yes) return;

            foreach (var obj in selected)
            {
                if (obj.Visual != null)
                    Viewport3D.Children.Remove(obj.Visual);
                _sceneObjects.Remove(obj);
            }

            SelectObject(null);
            UpdateStatus();
        }

        

        private void ImportModelFromViewModel()
        {
            var dlg = new WpfOpenFileDialog
            {
                Title  = "Импорт 3D модели",
                Filter = "Wavefront OBJ|*.obj|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var reader = new ObjReader();
                var model  = reader.Read(dlg.FileName);
                var visual = new ModelVisual3D { Content = model };

                RegisterObject(visual, "Import", 0, 0, 0, WpfColors.Gray, sourcePath: dlg.FileName);
              
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
        private void ClearScene()
        {
            RemoveGizmo();

            foreach (var bound in _selectionBounds)
                Viewport3D.Children.Remove(bound);
            _selectionBounds.Clear();

            foreach (var obj in _sceneObjects.ToList())
                if (obj.Visual != null)
                    Viewport3D.Children.Remove(obj.Visual);

            _sceneObjects.Clear();
            _nameCounters.Clear();
            SelectObject(null);
            UpdateStatus();
        }

        private SceneObject? CreateObjectFromData(SceneObjectData data)
        {
            if (string.IsNullOrWhiteSpace(data.PrimitiveType))
                return null;

            var color = WpfColor.FromRgb(data.ColorR, data.ColorG, data.ColorB);
            Visual3D visual;
            double startX = data.PosX;
            double startY = data.PosY;
            double startZ = data.PosZ;

            if (data.PrimitiveType == "Import")
            {
                if (string.IsNullOrWhiteSpace(data.SourcePath) || !System.IO.File.Exists(data.SourcePath))
                    return null;

                var reader = new ObjReader();
                visual = new ModelVisual3D { Content = reader.Read(data.SourcePath) };
                startX = startY = startZ = 0;
            }
            else
            {
                visual = PrimitiveFactory.CreateVisual(data.PrimitiveType, data.PosX, data.PosY, data.PosZ,
                    color, data.Opacity, data.TexturePath, data.Parameters);
            }

            var obj = RegisterObject(visual, data.PrimitiveType, startX, startY, startZ, color,
                data.Opacity, data.TexturePath, data.Parameters, data.SourcePath, data.Name, data.IsVisible);

            if (data.PrimitiveType == "Import")
                TransformService.MoveByVector(obj, data.PosX, data.PosY, data.PosZ);

            if (Math.Abs(data.RotX) > double.Epsilon)
                TransformService.RotateByAngle(obj, "X", data.RotX);
            if (Math.Abs(data.RotY) > double.Epsilon)
                TransformService.RotateByAngle(obj, "Y", data.RotY);
            if (Math.Abs(data.RotZ) > double.Epsilon)
                TransformService.RotateByAngle(obj, "Z", data.RotZ);

            if (Math.Abs(data.ScaleX - 1) > double.Epsilon)
                TransformService.ScaleByFactor(obj, data.ScaleX, "X");
            if (Math.Abs(data.ScaleY - 1) > double.Epsilon)
                TransformService.ScaleByFactor(obj, data.ScaleY, "Y");
            if (Math.Abs(data.ScaleZ - 1) > double.Epsilon)
                TransformService.ScaleByFactor(obj, data.ScaleZ, "Z");

            obj.IsVisible = data.IsVisible;
            ApplyMaterialToObject(obj);
            return obj;
        }
        private void SaveProjectTo(string fileName)
        {
            SceneProjectSerializer.Save(fileName, _sceneObjects);
            _viewModel.CurrentProjectPath = fileName;
            StatusSelected.Text = $"Сохранено: {System.IO.Path.GetFileName(fileName)}";
        }

        private void OpenProjectFrom(string fileName)
        {
            var project = SceneProjectSerializer.Load(fileName);

            ClearScene();
            var restored = new List<SceneObject>();
            var skipped = 0;
            foreach (var data in project.Objects)
            {
                var obj = CreateObjectFromData(data);
                if (obj != null)
                    restored.Add(obj);
                else
                    skipped++;
            }

            _viewModel.CurrentProjectPath = fileName;
            SelectObjects(restored.TakeLast(1));
            UpdateStatus();

            if (skipped > 0)
            {
                WpfMessageBox.Show($"Проект открыт, но не удалось восстановить объектов: {skipped}.",
                    "Открыть", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

       

        private void ResetCamera_Click(object sender, RoutedEventArgs e)
        {
            
            Viewport3D.ZoomExtents(500);
        }

       

        private void MenuPerspective_Click(object sender, RoutedEventArgs e)
            => ViewportLabel.Text = "Перспектива | Затенение";

        private void MenuWireframe_Click(object sender, RoutedEventArgs e)
        {
            bool isWire = ViewportLabel.Text.Contains("Каркас");
            ViewportLabel.Text = isWire
                ? "Перспектива | Затенение"
                : "Перспектива | Каркас";
        }

        

        private void NewProjectFromViewModel()
        {
            if (_sceneObjects.Count > 0)
            {
                var r = WpfMessageBox.Show(
                    "Создать новый проект? Несохранённые изменения будут потеряны.",
                    "Новый проект", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (r != MessageBoxResult.Yes) return;
            }

            ClearScene();
            _viewModel.CurrentProjectPath = null;
        }

        private void OpenProjectFromViewModel()
        {
            var dlg = new WpfOpenFileDialog
            {
                Title = "Открыть проект",
                Filter = "ModelingApp project|*.maproj|JSON|*.json|Все файлы|*.*"
            };
            if (dlg.ShowDialog() != true)
                return;

            try
            {
                OpenProjectFrom(dlg.FileName);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Ошибка открытия проекта:\n{ex.Message}",
                    "Открыть", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProjectFromViewModel()
        {
            if (string.IsNullOrWhiteSpace(_viewModel.CurrentProjectPath))
            {
                SaveProjectAsFromViewModel();
                return;
            }

            try
            {
                SaveProjectTo(_viewModel.CurrentProjectPath);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Ошибка сохранения проекта:\n{ex.Message}",
                    "Сохранить", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProjectAsFromViewModel()
        {
            var dlg = new WpfSaveFileDialog
            {
                Title = "Сохранить проект",
                Filter = "ModelingApp project|*.maproj|JSON|*.json|Все файлы|*.*",
                DefaultExt = ".maproj",
                AddExtension = true
            };
            if (dlg.ShowDialog() != true)
                return;

            try
            {
                SaveProjectTo(dlg.FileName);
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Ошибка сохранения проекта:\n{ex.Message}",
                    "Сохранить", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void MenuExit_Click(object sender, RoutedEventArgs e) => Close();
    }
}

