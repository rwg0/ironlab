// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IronPlot
{
    public partial class PlotPanel : Panel
    {
        // Mouse interaction
        private Point mouseDragStartPoint;
        Range[] xAxesDragStartRanges;
        Range[] yAxesDragStartRanges;

        // Selection Window
        private bool selectionStarted = false;
        private bool dragging = false;
        private Point selectionStart;
        protected Rectangle selection;

        protected Rect cachedRegion;

        protected void AddInteractionEvents()
        {
            canvas.MouseLeftButtonUp += new MouseButtonEventHandler(canvas_LeftClickEnd);
            canvas.MouseLeftButtonDown += new MouseButtonEventHandler(canvas_LeftClickStart);
            canvas.MouseRightButtonUp += new MouseButtonEventHandler(canvas_RightClickEnd);
            canvas.MouseRightButtonDown += new MouseButtonEventHandler(canvas_RightClickStart);
            canvas.MouseMove += new MouseEventHandler(canvas_MouseMove);
            canvas.MouseWheel += new MouseWheelEventHandler(canvas_MouseWheel);
            var allAxes = axes.XAxes.Concat(axes.YAxes);
            RefreshAxisInteractionEvents(new List<Axis2D>(), allAxes); 
        }

        protected void RefreshAxisInteractionEvents(IEnumerable<Axis2D> oldAxes, IEnumerable<Axis2D> newAxes)
        {
            foreach (Axis2D axis in oldAxes) axis.MouseLeftButtonDown -= new MouseButtonEventHandler(axis_MouseLeftButtonDown); 
            foreach (Axis2D axis in newAxes) axis.MouseLeftButtonDown += new MouseButtonEventHandler(axis_MouseLeftButtonDown); 
        }

        void axis_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        protected void AddSelectionRectangle()
        {
            selection = new Rectangle() { Visibility = Visibility.Visible, ClipToBounds = true, Width = 0, Height = 0, 
                VerticalAlignment = VerticalAlignment.Top };
            SolidColorBrush selectionBrush = new SolidColorBrush() { Color = Brushes.Aquamarine.Color, Opacity = 0.5 };
            selection.Fill = selectionBrush;
            selection.StrokeDashOffset = 5; selection.StrokeThickness = 0.99;
            SolidColorBrush selectionBrush2 = new SolidColorBrush() { Color = Color.FromArgb(255, 0, 0, 0) };
            selection.Stroke = selectionBrush2;
            selection.HorizontalAlignment = HorizontalAlignment.Left;
            DoubleCollection strokeDashArray1 = new DoubleCollection(2);
            strokeDashArray1.Add(3); strokeDashArray1.Add(3);
            selection.StrokeDashArray = strokeDashArray1;
            canvas.Children.Add(selection);
            selection.SetValue(Canvas.ZIndexProperty, 1000);
        }

        protected void canvas_LeftClickStart(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount > 1)
            {
                dragging = false;
                this.Cursor = Cursors.Arrow;
                var allAxes = axes.XAxes.Concat(axes.YAxes);
                foreach (Axis2D axis in allAxes)
                {
                    Range axisRange = GetRangeFromChildren(axis);
                    axis.SetValue(Axis2D.RangeProperty, axisRange);
                }
            }
            else
            {
                mouseDragStartPoint = e.GetPosition(this);

                xAxesDragStartRanges = new Range[axes.XAxes.Count];
                for (int i = 0; i < xAxesDragStartRanges.Length; ++i)
                    xAxesDragStartRanges[i] = new Range(axes.XAxes[i].Min, axes.XAxes[i].Max);
                yAxesDragStartRanges = new Range[axes.YAxes.Count];
                for (int i = 0; i < yAxesDragStartRanges.Length; ++i)
                    yAxesDragStartRanges[i] = new Range(axes.YAxes[i].Min, axes.YAxes[i].Max);

                this.Cursor = Cursors.ScrollAll;
                dragging = true;
            }
            canvas.CaptureMouse();
        }

        protected void canvas_RightClickStart(object sender, MouseButtonEventArgs e)
        {
            selectionStarted = true;
            selectionStart = e.GetPosition(canvas);
            selection.Width = 0;
            selection.Height = 0;
            canvas.CaptureMouse();
        }

        protected void canvas_LeftClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                this.Cursor = Cursors.Arrow;
                dragging = false;
                marginChangeTimer.Interval = TimeSpan.FromSeconds(0.2);
                marginChangeTimer.Start();
            }
            canvas.ReleaseMouseCapture();
        }

        protected void canvas_RightClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                if (selectionStarted)
                {
                    selection.Width = 0;
                    selection.Height = 0;
                    selectionStarted = false;
                    Point selectionEnd = e.GetPosition(canvas);
                    if ((Math.Abs(selectionStart.X - selectionEnd.X) <= 1) ||
                       (Math.Abs(selectionStart.Y - selectionEnd.Y) <= 1))
                    {
                        return;
                    }
                    foreach (Axis2D axis in axes.XAxes)
                    {
                        axis.Min = Math.Min(axis.CanvasToGraph(selectionStart.X), axis.CanvasToGraph(selectionEnd.X));
                        axis.Max = Math.Max(axis.CanvasToGraph(selectionStart.X), axis.CanvasToGraph(selectionEnd.X));
                    }
                    foreach (Axis2D axis in axes.YAxes)
                    {
                        axis.Min = Math.Min(axis.CanvasToGraph(selectionStart.Y), axis.CanvasToGraph(selectionEnd.Y));
                        axis.Max = Math.Max(axis.CanvasToGraph(selectionStart.Y), axis.CanvasToGraph(selectionEnd.Y));
                    }
                    selectionStarted = false;
                }
            }
            canvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        protected void canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = e.Delta / 120;
            Point canvasPosition = e.GetPosition(canvas);
            Point graphPosition = canvasToGraph.Transform(canvasPosition);
            double factor = Math.Pow(1.4, delta);
            // 
            var allAxes = axes.XAxes.Concat(axes.YAxes);
            foreach (Axis2D axis in allAxes)
            {
                double axisMid;
                if (axis is XAxis) axisMid = axis.GraphTransform(axis.CanvasToGraph(canvasPosition.X));
                else axisMid = axis.GraphTransform(axis.CanvasToGraph(canvasPosition.Y));
                double axisMin = axis.GraphTransform(axis.Min);
                double axisMax = axis.GraphTransform(axis.Max);
                axis.Min = axis.CanvasTransform(axisMid + (axisMin - axisMid) / factor);
                axis.Max = axis.CanvasTransform(axisMid + (axisMax - axisMid) / factor);
            }
        }

        protected void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                if (dragging)
                {
                    // Get the new mouse position. 
                    Point mouseDragCurrentPoint = e.GetPosition(this);

                    Point delta = new Point(
                        (mouseDragCurrentPoint.X - mouseDragStartPoint.X),
                        (mouseDragCurrentPoint.Y - mouseDragStartPoint.Y));

                    for (int i = 0; i < xAxesDragStartRanges.Length; ++i)
                    {
                        Axis2D axis = axes.XAxes[i];
                        double offset = -delta.X / axis.Scale;
                        axis.Min = axis.CanvasTransform(axis.GraphTransform(xAxesDragStartRanges[i].Min) + offset);
                        axis.Max = axis.CanvasTransform(axis.GraphTransform(xAxesDragStartRanges[i].Max) + offset);
                    }

                    for (int i = 0; i < yAxesDragStartRanges.Length; ++i)
                    {
                        Axis2D axis = axes.YAxes[i];
                        double offset = delta.Y / axis.Scale;
                        axis.Min = axis.CanvasTransform(axis.GraphTransform(yAxesDragStartRanges[i].Min) + offset);
                        axis.Max = axis.CanvasTransform(axis.GraphTransform(yAxesDragStartRanges[i].Max) + offset);
                    }
                }
                if (selectionStarted)
                {
                    Rect rect = new Rect(selectionStart, e.GetPosition(canvas));
                    selection.RenderTransform = new TranslateTransform(rect.X, rect.Y);
                    selection.Width = rect.Width;
                    selection.Height = rect.Height;
                }
            }
        }

        protected void marginChangeTimer_Tick(Object sender, EventArgs e)
        {
            marginChangeTimer.Stop();
            marginChangeTimer.Interval = TimeSpan.FromSeconds(0.0);
            this.InvalidateMeasure();
        }
    }
}
