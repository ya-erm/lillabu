﻿using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = LilaApp.Models.Point;
using WinPoint = System.Windows.Point;

namespace Lilabu.Views
{
    using LilaApp.Algorithm;
    using ViewModels;

    public partial class TraceMap : UserControl
    {
        public MainViewModel MainVM;
        public TraceMapViewModel VM;

        public TraceMap()
        {
            InitializeComponent();

            InitializeScrollView();
        }
        private void TraceMap_OnLoaded(object sender, RoutedEventArgs e)
        {
            MainVM = DataContext as MainViewModel;
            VM = MainVM?.TraceMapVm;

            if (VM != null)
            {
                VM.PropertyChanged += VM_PropertyChanged;
            }
        }

        private void VM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TraceMapViewModel.Points))
            {
                var points = VM.Points;

                // Поиск минимумов и максимумов
                var maxPoint = points.Length > 1 ? points[0] : new Point(0, 0);
                var minPoint = points.Length > 1 ? points[0] : new Point(0, 0);
                foreach (var point in points)
                {
                    if (point.X < minPoint.X) minPoint.X = point.X;
                    if (point.Y < minPoint.Y) minPoint.Y = point.Y;
                    if (point.X > maxPoint.X) maxPoint.X = point.X;
                    if (point.Y > maxPoint.Y) maxPoint.Y = point.Y;
                }
                foreach (var point in MainVM.Model.Points)
                {
                    if (point.X < minPoint.X) minPoint.X = point.X;
                    if (point.Y < minPoint.Y) minPoint.Y = point.Y;
                    if (point.X > maxPoint.X) maxPoint.X = point.X;
                    if (point.Y > maxPoint.Y) maxPoint.Y = point.Y;
                }

                const int padding = 10;
                const int multiplier = 10;

                VM.Width = (maxPoint.X - minPoint.X) * multiplier + 2 * padding + 2;
                VM.Height = (maxPoint.Y - minPoint.Y) * multiplier + 2 * padding + 2;

                grid_MapOuter.Width = VM.Width * 1.2;
                grid_MapOuter.Height = VM.Height * 1.2;

                grid_Map.Children.Clear();

                // Отрисовка линии
                void AddLine(Point p1, Point p2, Brush color, double thickness = 1)
                {
                    grid_Map.Children.Add(new Line
                    {
                        X1 = padding + multiplier * (p1.X - minPoint.X),
                        X2 = padding + multiplier * (p2.X - minPoint.X),
                        Y1 = padding + multiplier * (p1.Y - minPoint.Y),
                        Y2 = padding + multiplier * (p2.Y - minPoint.Y),
                        StrokeThickness = thickness,
                        Stroke = color,
                    });
                }

                // Отрисовка дуги
                void AddArc(Point p1, Point p2, int direction)
                {
                    const double radius = 3;

                    var x1 = padding + multiplier * (p1.X - minPoint.X);
                    var x2 = padding + multiplier * (p2.X - minPoint.X);
                    var y1 = padding + multiplier * (p1.Y - minPoint.Y);
                    var y2 = padding + multiplier * (p2.Y - minPoint.Y);

                    var arc = new ArcSegment()
                    {
                        Point = new WinPoint(x2, y2),
                        Size = new Size(radius * multiplier, radius * multiplier),
                        SweepDirection = direction == 1 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                    };
                    var figure = new PathFigure() { StartPoint = new WinPoint(x1, y1) };
                    figure.Segments.Add(arc);
                    var geometry = new PathGeometry();
                    geometry.Figures.Add(figure);
                    var path = new Path() { Data = geometry, Stroke = Brushes.Red, StrokeThickness = 1 };
                    grid_Map.Children.Add(path);
                }

                // Открисовка круга
                void AddEllipse(Point p1, Brush color, double diameter = 0.3)
                {
                    var x1 = padding + multiplier * (p1.X - minPoint.X);
                    var y1 = padding + multiplier * (p1.Y - minPoint.Y);

                    var size = diameter * multiplier;

                    grid_Map.Children.Add(new Ellipse()
                    {
                        Width = size,
                        Height = size,
                        Fill = color,
                        Margin = new Thickness(x1 - size / 2, y1 - size / 2, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                    });
                }

                // Отрисовка треугольника
                void AddTriangle(Point p1, Brush fill, Brush border = null, double radius = 0.5)
                {
                    AddEllipse(p1, border);
                    var x1 = padding + multiplier * (p1.X - minPoint.X);
                    var y1 = padding + multiplier * (p1.Y - minPoint.Y);

                    var size = radius * multiplier;
                    var xc = size; var yc = size;

                    var triangle = new[]
                    {
                        new WinPoint(xc, yc + size),
                        new WinPoint(xc + size * 0.866, yc - size * 0.5),
                        new WinPoint(xc - size * 0.866, yc - size * 0.5),
                    };

                    var dxy = Convert.ToInt32(p1.Degrees) % 90 == 45 ? size / 0.707 : size;
                    var polygon = new Polygon()
                    {
                        Width = 2 * size,
                        Height = 2 * size,
                        Points = new PointCollection(triangle),
                        Stroke = border ?? fill,
                        Fill = fill,
                    };
                    var polygonGrid = new Grid()
                    {
                        Width = 2 * size,
                        Height = 2 * size,
                        Margin = new Thickness(x1 - dxy, y1 - dxy, 0, 0),
                        LayoutTransform = new RotateTransform(p1.Degrees),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                    };
                    polygonGrid.Children.Add(polygon);
                    grid_Map.Children.Add(polygonGrid);
                }

                // Сетка
                if (VM.ShouldDrawGrid)
                {
                    for (var i = (int)minPoint.X - 1; i <= Math.Ceiling(maxPoint.X) + 1; i++)
                        AddLine(new Point(i, minPoint.Y - 1), new Point(i, maxPoint.Y + 1), Brushes.DarkGray, 0.25);
                    for (var i = (int)minPoint.Y - 1; i <= Math.Ceiling(maxPoint.Y) + 1; i++)
                        AddLine(new Point(minPoint.X - 1, i), new Point(maxPoint.X + 1, i), Brushes.DarkGray, 0.25);
                }

                // Ось OY
                AddLine(new Point(0, 0), new Point(0, 1), Brushes.Black, 0.5);
                AddLine(new Point(0.2, 0.8), new Point(0, 1), Brushes.Black, 0.5);
                AddLine(new Point(-0.2, 0.8), new Point(0, 1), Brushes.Black, 0.5);

                // Трасса
                for (var i = 0; i < points.Length - 1 && i < MainVM.Model.Order.Count; i++)
                {
                    var block = MainVM.Model.Order[i];

                    if (block.StartsWith("T"))
                    {
                        // Рисуем дугу
                        var direction = MainVM.Model.Topology[i].Direction;
                        AddArc(points[i], points[i + 1], direction);
                    }
                    else
                    {
                        // Рисуем линию
                        AddLine(points[i], points[i + 1], Brushes.Red);

                        if (block.StartsWith("B"))
                        {
                            var rotatedPoint = MathFunctions.RotateCoordinates(points[i].Angle, points[i]);
                            rotatedPoint.Y += 1.5;
                            rotatedPoint.X += 0.2;
                            var p1 = MathFunctions.RotateCoordinates(-points[i].Angle, rotatedPoint);
                            rotatedPoint.Y += 1;
                            var p2 = MathFunctions.RotateCoordinates(-points[i].Angle, rotatedPoint);
                            rotatedPoint.X -= 0.2 * 2;
                            var p4 = MathFunctions.RotateCoordinates(-points[i].Angle, rotatedPoint);
                            rotatedPoint.Y -= 1;
                            var p3 = MathFunctions.RotateCoordinates(-points[i].Angle, rotatedPoint);

                            // Рисуем линии моста
                            AddLine(p1, p2, Brushes.Black, thickness: 0.5);
                            AddLine(p3, p4, Brushes.Black, thickness: 0.5);
                        }
                    }

                    // Рисуем точку стыка
                    AddEllipse(points[i], Brushes.Red, 0.2);
                }

                void AddText(string text, Point p, double fontSize = 7, Brush color = null)
                {

                    var x1 = padding + multiplier * (p.X - minPoint.X);
                    var y1 = padding + multiplier * (p.Y - minPoint.Y);

                    var textBox = new TextBlock()
                    {
                        Text = text,
                        FontSize = fontSize,
                        Foreground = color ?? Brushes.Black,
                        LayoutTransform = new ScaleTransform(1, -1),
                        Margin = new Thickness(x1, y1, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top,
                    };
                    grid_Map.Children.Add(textBox);
                }

                // Точки маршрута

                foreach (var route in MainVM.Model.Points)
                {
                    AddEllipse(route, Brushes.Blue);

                    // Стоимость точек
                    AddText($"{route.Price:F0}", route);
                }

                // Второй курсор
                if (VM.Cursor2Point is Point cursorPoint2)
                {
                    var fill = cursorPoint2.Price > 0.5
                        ? new SolidColorBrush(Color.FromArgb(100, 100, 0, 255))
                        : Brushes.Transparent;

                    AddTriangle(cursorPoint2, fill, Brushes.BlueViolet);

                    AddText($"x:{cursorPoint2.X:F0} y:{cursorPoint2.Y:F0}\n", cursorPoint2, 5);
                }
                // Основной курсор алгоритма WASD
                if (VM.Cursor1Point is Point cursorPoint)
                {
                    var fill = cursorPoint.Price > 0.5
                        ? new SolidColorBrush(Color.FromArgb(100, 16, 255, 16))
                        : Brushes.Transparent;

                    AddTriangle(cursorPoint, fill, Brushes.Green);
                    AddText($"x:{cursorPoint.X:F0} y:{cursorPoint.Y:F0}\n", cursorPoint, 5);
                }

                // Центройды
                for (var i = 0; i < VM?.Context?.Centroids?.Count; i++)
                {
                    if (VM?.Context?.Centroids[i] is Point centroid)
                    {
                        AddEllipse(centroid, new SolidColorBrush(Colors.Aqua) { Opacity = 0.5 }, 0.75);

                        AddText($"{centroid.Price:F0}", new Point(centroid.X, centroid.Y - 1), 5, Brushes.DarkGray);
                    }
                }
            }
        }

        #region Zoom and move scrollview

        WinPoint? lastCenterPositionOnTarget;
        WinPoint? lastMousePositionOnTarget;
        WinPoint? lastDragPoint;

        void InitializeScrollView()
        {
            scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
            scrollViewer.MouseLeftButtonUp += OnMouseLeftButtonUp;
            scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;
            scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;

            scrollViewer.PreviewMouseLeftButtonDown += OnMouseLeftButtonDown;
            scrollViewer.MouseMove += OnMouseMove;

            slider.ValueChanged += OnSliderValueChanged;
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (lastDragPoint.HasValue)
            {
                var posNow = e.GetPosition(scrollViewer);

                var dX = posNow.X - lastDragPoint.Value.X;
                var dY = posNow.Y - lastDragPoint.Value.Y;

                lastDragPoint = posNow;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - dX);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - dY);
            }
        }

        void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mousePos = e.GetPosition(scrollViewer);
            // make sure we still can use the scrollbars
            if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y < scrollViewer.ViewportHeight)
            {
                scrollViewer.Cursor = Cursors.SizeAll;
                lastDragPoint = mousePos;
                Mouse.Capture(scrollViewer);
            }
        }

        void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            lastMousePositionOnTarget = Mouse.GetPosition(grid);

            if (e.Delta > 0)
            {
                slider.Value += 1;
            }
            if (e.Delta < 0)
            {
                slider.Value -= 1;
            }

            e.Handled = true;
        }

        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            scrollViewer.Cursor = Cursors.Hand;
            scrollViewer.ReleaseMouseCapture();
            lastDragPoint = null;
        }

        void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            scaleTransform.ScaleX = e.NewValue / 20.0;
            scaleTransform.ScaleY = e.NewValue / 20.0;

            var centerOfViewport = new WinPoint(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
            lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, grid);
        }

        void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange != 0 || e.ExtentWidthChange != 0)
            {
                WinPoint? targetBefore = null;
                WinPoint? targetNow = null;

                if (!lastMousePositionOnTarget.HasValue)
                {
                    if (lastCenterPositionOnTarget.HasValue)
                    {
                        var centerOfViewport = new WinPoint(scrollViewer.ViewportWidth / 2, scrollViewer.ViewportHeight / 2);
                        var centerOfTargetNow = scrollViewer.TranslatePoint(centerOfViewport, grid);

                        targetBefore = lastCenterPositionOnTarget;
                        targetNow = centerOfTargetNow;
                    }
                }
                else
                {
                    targetBefore = lastMousePositionOnTarget;
                    targetNow = Mouse.GetPosition(grid);

                    lastMousePositionOnTarget = null;
                }

                if (targetBefore.HasValue)
                {
                    var dXInTargetPixels = targetNow.Value.X - targetBefore.Value.X;
                    var dYInTargetPixels = targetNow.Value.Y - targetBefore.Value.Y;

                    var multiplicatorX = e.ExtentWidth / grid.Width;
                    var multiplicatorY = e.ExtentHeight / grid.Height;

                    var newOffsetX = scrollViewer.HorizontalOffset - dXInTargetPixels * multiplicatorX;
                    var newOffsetY = scrollViewer.VerticalOffset - dYInTargetPixels * multiplicatorY;

                    if (double.IsNaN(newOffsetX) || double.IsNaN(newOffsetY))
                    {
                        return;
                    }

                    scrollViewer.ScrollToHorizontalOffset(newOffsetX);
                    scrollViewer.ScrollToVerticalOffset(newOffsetY);
                }
            }
        }

        #endregion

    }
}
