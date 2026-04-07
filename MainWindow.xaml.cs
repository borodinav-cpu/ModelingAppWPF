
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;


namespace ModelingAppWPF
{
    public partial class MainWindow : Window
    {
        private Model3D _currentModel;
        private double _currentScale = 1.0;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void AddPrimitive_Click(object sender, RoutedEventArgs e)
        {
            
            var dialog = new PrimitiveDialog();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var color = new SolidColorBrush(dialog.SelectedColor);
                    var material = new DiffuseMaterial(color);

                    Visual3D newObject = null;

                    switch (dialog.PrimitiveType)
                    {
                        case "Cube":
                            var cube = new CubeVisual3D
                            {
                                Center = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ),
                                SideLength = dialog.CubeSide,
                                Fill = color,
                                BackMaterial = material
                            };
                            newObject = cube;
                            _currentModel = cube.Content;
                            break;

                        case "Sphere":
                            var sphere = new SphereVisual3D
                            {
                                Center = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ),
                                Radius = dialog.SphereRadius,
                                Fill = color,
                                BackMaterial = material
                            };
                            newObject = sphere;
                            _currentModel = sphere.Content;
                            break;

                        case "Cylinder":
                            {
                                var cylinderBuilder = new MeshBuilder();
                                var p1 = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                                var p2 = new Point3D(dialog.PosX, dialog.PosY + dialog.CylinderHeight, dialog.PosZ);

                                cylinderBuilder.AddCylinder(p1, p2, dialog.CylinderDiameter / 2, 36);

                                cylinderBuilder.AddCylinder(p1, p2, (float)dialog.CylinderDiameter, 36, true, true);
                                var cylinderMesh = cylinderBuilder.ToMesh();
                                var cylinderGeometry = new GeometryModel3D(cylinderMesh, material)
                                {
                                    BackMaterial = material
                                };
                                var cylinderVisual = new ModelVisual3D { Content = cylinderGeometry };
                                view3D.Children.Add(cylinderVisual);
                                _currentModel = cylinderGeometry;
                                break;  
                            }

                        case "Cone":
                            {
                                var coneBuilder = new MeshBuilder();
                                var basePoint = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                                
                                var direction = new Vector3D(0, dialog.ConeHeight, 0);

                                
                                coneBuilder.AddCone(basePoint, direction, dialog.ConeBaseRadius, 0, dialog.ConeHeight, true, true, 36);

                                var coneMesh = coneBuilder.ToMesh();
                                var coneGeometry = new GeometryModel3D(coneMesh, material) { BackMaterial = material };
                                var coneVisual = new ModelVisual3D { Content = coneGeometry };
                                view3D.Children.Add(coneVisual);
                                _currentModel = coneGeometry;
                                break;
                            }

                        case "Torus":
                            {
                                var torusBuilder = new MeshBuilder();
                                torusBuilder.AddTorus((float)dialog.TorusDiameter, (float)dialog.TorusTubeDiameter, 36, 24);
                                var torusMesh = torusBuilder.ToMesh();
                                var torusGeometry = new GeometryModel3D(torusMesh, material)
                                {
                                    BackMaterial = material,
                                    Transform = new TranslateTransform3D(dialog.PosX, dialog.PosY, dialog.PosZ)
                                };
                                var torusVisual = new ModelVisual3D { Content = torusGeometry };
                                view3D.Children.Add(torusVisual);
                                _currentModel = torusGeometry;
                                break;  
                            }

                        case "Pyramid":
                            {
                                var pyramidBuilder = new MeshBuilder();
                               
                                var center = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                                pyramidBuilder.AddPyramid(center, (float)dialog.PyramidSide, (float)dialog.PyramidHeight, true);

                                var pyramidMesh = pyramidBuilder.ToMesh();
                                var pyramidGeometry = new GeometryModel3D(pyramidMesh, material) { BackMaterial = material };
                                var pyramidVisual = new ModelVisual3D { Content = pyramidGeometry };
                                view3D.Children.Add(pyramidVisual);
                                _currentModel = pyramidGeometry;
                                break;
                            }

                        case "Ellipsoid":
                            {
                                var ellipsoidBuilder = new MeshBuilder();
                                
                                var center = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                                ellipsoidBuilder.AddEllipsoid(center, dialog.EllipsoidRadiusX, dialog.EllipsoidRadiusY, dialog.EllipsoidRadiusZ, 32, 16);

                                var ellipsoidMesh = ellipsoidBuilder.ToMesh();
                                var ellipsoidGeometry = new GeometryModel3D(ellipsoidMesh, material) { BackMaterial = material };
                                var ellipsoidVisual = new ModelVisual3D { Content = ellipsoidGeometry };
                                view3D.Children.Add(ellipsoidVisual);
                                _currentModel = ellipsoidGeometry;
                                break;
                            }

                        case "Pipe":
                            {
                                var pipeBuilder = new MeshBuilder();
                                var p1 = new Point3D(dialog.PosX, dialog.PosY, dialog.PosZ);
                                var p2 = new Point3D(dialog.PosX, dialog.PosY + dialog.PipeLength, dialog.PosZ);

                                
                                pipeBuilder.AddPipe(p1, p2, dialog.PipeInnerDiameter / 2, dialog.PipeOuterDiameter / 2, 36);

                                var pipeMesh = pipeBuilder.ToMesh();
                                var pipeGeometry = new GeometryModel3D(pipeMesh, material) { BackMaterial = material };
                                var pipeVisual = new ModelVisual3D { Content = pipeGeometry };
                                view3D.Children.Add(pipeVisual);
                                _currentModel = pipeGeometry;
                                break;
                            }
                    }

                    if (newObject != null)
                    {
                        view3D.Children.Add(newObject);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка создания примитива: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void ImportModel_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "3D Files (*.obj;*.stl)|*.obj;*.stl|All files (*.*)|*.*",
                Title = "Импорт 3D модели"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var importer = new ModelImporter();
                    var model = importer.Load(openFileDialog.FileName);

                    if (model != null)
                    {
                        var modelVisual = new ModelVisual3D { Content = model };
                        view3D.Children.Add(modelVisual);
                        _currentModel = model;
                        MessageBox.Show("Модель успешно импортирована!", "Успех",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка импорта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private void ApplyTransform(Transform3D newTransform)
        {
            if (_currentModel == null) return;

            
            var group = _currentModel.Transform as Transform3DGroup;

            
            if (group == null)
            {
                group = new Transform3DGroup();

               
                if (_currentModel.Transform != null)
                {
                    group.Children.Add(_currentModel.Transform);
                }

                
                _currentModel.Transform = group;
            }

            
            group.Children.Add(newTransform);
        }
        private void RotateX_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) { MessageBox.Show("Выберите объект"); return; }

            var dialog = new InputDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Angle.HasValue)
            {
                
                var center = _currentModel.Bounds.Location;
                center.X += _currentModel.Bounds.SizeX / 2;
                center.Y += _currentModel.Bounds.SizeY / 2;
                center.Z += _currentModel.Bounds.SizeZ / 2;

                var rotate = new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(1, 0, 0), dialog.Angle.Value), center);

                ApplyTransform(rotate);
            }
        }

        private void RotateY_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) { MessageBox.Show("Выберите объект"); return; }

            var dialog = new InputDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Angle.HasValue)
            {
                var center = _currentModel.Bounds.Location;
                center.X += _currentModel.Bounds.SizeX / 2;
                center.Y += _currentModel.Bounds.SizeY / 2;
                center.Z += _currentModel.Bounds.SizeZ / 2;

                var rotate = new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 1, 0), dialog.Angle.Value), center);

                ApplyTransform(rotate);
            }
        }

        private void RotateZ_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) { MessageBox.Show("Выберите объект"); return; }

            var dialog = new InputDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.Angle.HasValue)
            {
                var center = _currentModel.Bounds.Location;
                center.X += _currentModel.Bounds.SizeX / 2;
                center.Y += _currentModel.Bounds.SizeY / 2;
                center.Z += _currentModel.Bounds.SizeZ / 2;

                var rotate = new RotateTransform3D(
                    new AxisAngleRotation3D(new Vector3D(0, 0, 1), dialog.Angle.Value), center);

                ApplyTransform(rotate);
            }
        }

        private void ScaleUp_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;

            
            var center = _currentModel.Bounds.Location;
            center.X += _currentModel.Bounds.SizeX / 2;
            center.Y += _currentModel.Bounds.SizeY / 2;
            center.Z += _currentModel.Bounds.SizeZ / 2;

            ApplyTransform(new ScaleTransform3D(1.2, 1.2, 1.2, center.X, center.Y, center.Z));
        }

        private void ScaleDown_Click(object sender, RoutedEventArgs e)
        {
            if (_currentModel == null) return;

            var center = _currentModel.Bounds.Location;
            center.X += _currentModel.Bounds.SizeX / 2;
            center.Y += _currentModel.Bounds.SizeY / 2;
            center.Z += _currentModel.Bounds.SizeZ / 2;

            ApplyTransform(new ScaleTransform3D(0.8, 0.8, 0.8, center.X, center.Y, center.Z));
        }



        private void ClearScene_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Очистить сцену?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
               
                view3D.Children.Clear();

                
                var lights = new DefaultLights();
                view3D.Children.Add(lights);

              

               
                var grid = new GridLinesVisual3D
                {
                    Width = 400,
                    Length = 400,
                    MinorDistance = 10,
                    MajorDistance = 50,
                    Thickness = 0.5,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#555555"))
                };
                view3D.Children.Add(grid);

                _currentModel = null;
                _currentScale = 1.0;
            }
        }
    }
}