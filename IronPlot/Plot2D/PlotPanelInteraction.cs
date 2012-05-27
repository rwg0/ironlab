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
using System.Windows.Threading;
using System.Windows.Controls.Primitives;

namespace IronPlot
{
    public partial class PlotPanel : PlotPanelBase
    {
        // Mouse interaction
        private Point mouseDragStartPoint;
        List<Axis2D> axesBeingDragged;
        List<Range> axesDragStartRanges;

        // Selection Window
        private bool selectionStarted = false;
        private bool dragging = false;
        private Point selectionStart;
        protected Rectangle selection;

        protected Rect cachedRegion;

        private Point currentPosition;
        DispatcherTimer mousePositionTimer = new DispatcherTimer();

        protected void AddInteractionEvents()
        {
            Canvas.MouseLeftButtonUp += new MouseButtonEventHandler(LeftClickEnd);
            Canvas.MouseLeftButtonDown += new MouseButtonEventHandler(LeftClickStart);
            Canvas.MouseRightButtonUp += new MouseButtonEventHandler(canvas_RightClickEnd);
            Canvas.MouseRightButtonDown += new MouseButtonEventHandler(canvas_RightClickStart);
            Canvas.MouseMove += new MouseEventHandler(element_MouseMove);
            Canvas.MouseWheel += new MouseWheelEventHandler(element_MouseWheel);
            var allAxes = Axes.XAxes.Concat(Axes.YAxes);

            mousePositionTimer.Interval = TimeSpan.FromSeconds(0.05);
            mousePositionTimer.Tick += new EventHandler(mousePositionTimer_Tick);
        }

        internal void AddAxisInteractionEvents(IEnumerable<Axis2D> axes)
        {
            if (this is ColourBarPanel) return;
            foreach (Axis2D axis in axes) axis.MouseLeftButtonDown += new MouseButtonEventHandler(LeftClickStart);
            foreach (Axis2D axis in axes) axis.MouseMove += new MouseEventHandler(element_MouseMove);
            foreach (Axis2D axis in axes) axis.MouseLeftButtonUp += new MouseButtonEventHandler(LeftClickEnd);
            foreach (Axis2D axis in axes) axis.MouseWheel += new MouseWheelEventHandler(element_MouseWheel);
        }

        internal void RemoveAxisInteractionEvents(IEnumerable<Axis2D> axes)
        {
            if (this is ColourBarPanel) return;
            foreach (Axis2D axis in axes) axis.MouseLeftButtonDown -= new MouseButtonEventHandler(LeftClickStart);
            foreach (Axis2D axis in axes) axis.MouseMove -= new MouseEventHandler(element_MouseMove);
            foreach (Axis2D axis in axes) axis.MouseLeftButtonUp -= new MouseButtonEventHandler(LeftClickEnd);
            foreach (Axis2D axis in axes) axis.MouseWheel -= new MouseWheelEventHandler(element_MouseWheel);
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
            Canvas.Children.Add(selection);
            selection.SetValue(Canvas.ZIndexProperty, 1000);
        }

        protected void LeftClickStart(object sender, MouseButtonEventArgs e)
        {
            foreach (Plot2DItem item in plotItems)
            {
                if (item is Plot2DCurve && !Double.IsNaN((item as Plot2DCurve).AnnotationPosition.X))
                {
                    (item as Plot2DCurve).AnnotationPosition = new Point(Double.NaN, Double.NaN);
                }
            }
            
            bool ctrlOrShift = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftShift);
            if (ctrlOrShift)
            {
                canvas_RightClickStart(sender, e);
                return;
            }

            // either whole canvas or single axis
            bool isSingleAxis = (sender is Axis2D);
            if (e.ClickCount > 1)
            {
                dragging = false;
                this.Cursor = Cursors.Arrow;
                List<Axis2D> allAxes;
                if (isSingleAxis) allAxes = new List<Axis2D> { sender as Axis2D };
                else allAxes = Axes.XAxes.Concat(Axes.YAxes).ToList();
                foreach (Axis2D axis in allAxes)
                {
                    Range axisRange = GetRangeFromChildren(axis);
                    if (axisRange.Length != 0) axis.SetValue(Axis2D.RangeProperty, axisRange);
                }
            }
            else
            {
                if (isSingleAxis) axesBeingDragged = new List<Axis2D> { sender as Axis2D };
                else axesBeingDragged = Axes.XAxes.Concat(Axes.YAxes).ToList();
                StartDrag(e);
            }
            Canvas.CaptureMouse();
        }

        protected void StartDrag(MouseButtonEventArgs e)
        {
            mouseDragStartPoint = e.GetPosition(this);
            axesDragStartRanges = new List<Range>();
            foreach (Axis2D axis in axesBeingDragged)
            {
                axesDragStartRanges.Add(new Range(axis.Min, axis.Max));
            }
            this.Cursor = Cursors.ScrollAll;
            dragging = true;
        }

        protected void MoveDrag(MouseEventArgs e)
        {
            // Get the new mouse position. 
            Point mouseDragCurrentPoint = e.GetPosition(this);

            Point delta = new Point(
                (mouseDragCurrentPoint.X - mouseDragStartPoint.X),
                (mouseDragCurrentPoint.Y - mouseDragStartPoint.Y));

            int index = 0;
            foreach (Axis2D axis in axesBeingDragged)
            {
                double offset;
                if (axis is XAxis)
                    offset = -delta.X / axis.Scale;
                else offset = delta.Y / axis.Scale;
                axis.SetValue(Axis2D.RangeProperty, new Range(
                    axis.CanvasTransform(axis.GraphTransform(axesDragStartRanges[index].Min) + offset),
                    axis.CanvasTransform(axis.GraphTransform(axesDragStartRanges[index].Max) + offset)));
                index++;
            }
        }

        protected void EndDrag(object sender)
        {
            UIElement element = (UIElement)sender;
            if (element.IsMouseCaptured)
            {
                this.Cursor = Cursors.Arrow;
                dragging = false;
                marginChangeTimer.Interval = TimeSpan.FromSeconds(0.2);
                marginChangeTimer.Start();
            }
            element.ReleaseMouseCapture();
        }

        protected void canvas_RightClickStart(object sender, MouseButtonEventArgs e)
        {
            selectionStarted = true;
            selectionStart = e.GetPosition(Canvas);
            selection.Width = 0;
            selection.Height = 0;
            Canvas.CaptureMouse();
        }

        protected void LeftClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (selectionStarted)
            {
                canvas_RightClickEnd(sender, e);
                return;
            }

            EndDrag(sender);
        }

        protected void canvas_RightClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (Canvas.IsMouseCaptured)
            {
                if (selectionStarted)
                {
                    selection.Width = 0;
                    selection.Height = 0;
                    selectionStarted = false;
                    Point selectionEnd = e.GetPosition(Canvas);
                    if ((Math.Abs(selectionStart.X - selectionEnd.X) <= 1) ||
                       (Math.Abs(selectionStart.Y - selectionEnd.Y) <= 1))
                    {
                        return;
                    }
                    foreach (Axis2D axis in Axes.XAxes)
                    {
                        axis.SetValue(Axis2D.RangeProperty, new Range(
                            Math.Min(axis.CanvasToGraph(selectionStart.X), axis.CanvasToGraph(selectionEnd.X)),
                            Math.Max(axis.CanvasToGraph(selectionStart.X), axis.CanvasToGraph(selectionEnd.X))));
                    }
                    foreach (Axis2D axis in Axes.YAxes)
                    {
                        axis.SetValue(Axis2D.RangeProperty, new Range(
                            Math.Min(axis.CanvasToGraph(selectionStart.Y), axis.CanvasToGraph(selectionEnd.Y)),
                            Math.Max(axis.CanvasToGraph(selectionStart.Y), axis.CanvasToGraph(selectionEnd.Y))));
                    }
                    selectionStarted = false;
                }
            }
            Canvas.ReleaseMouseCapture();
            e.Handled = true;
        }

        protected void element_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            bool isSingleAxis = (sender is Axis2D);
            double delta = e.Delta / 120;
            Point canvasPosition = e.GetPosition(Canvas);
            double factor = Math.Pow(1.4, delta);
            //
            List<Axis2D> zoomAxes;
            if (isSingleAxis) zoomAxes = new List<Axis2D>() { sender as Axis2D };
            else zoomAxes = Axes.XAxes.Concat(Axes.YAxes).ToList();
            foreach (Axis2D axis in zoomAxes)
            {
                double axisMid;
                if (axis is XAxis) axisMid = axis.GraphTransform(axis.CanvasToGraph(canvasPosition.X));
                else axisMid = axis.GraphTransform(axis.CanvasToGraph(canvasPosition.Y));
                double axisMin = axis.GraphTransform(axis.Min);
                double axisMax = axis.GraphTransform(axis.Max);
                axis.SetValue(Axis2D.RangeProperty, new Range(axis.CanvasTransform(axisMid + (axisMin - axisMid) / factor), axis.CanvasTransform(axisMid + (axisMax - axisMid) / factor)));
            }
        }

        protected void element_MouseMove(object sender, MouseEventArgs e)
        {
            currentPosition = e.GetPosition(Canvas);
            if (Canvas.IsMouseCaptured)
            {
                if (dragging)
                {
                    MoveDrag(e);
                }
                if (selectionStarted)
                {
                    Rect rect = new Rect(selectionStart, currentPosition);
                    selection.RenderTransform = new TranslateTransform(rect.X, rect.Y);
                    selection.Width = rect.Width;
                    selection.Height = rect.Height;
                }
            }
            mousePositionTimer.Stop(); mousePositionTimer.Start();
            foreach (Plot2DItem item in plotItems)
            {
                if ((item is Plot2DCurve) && !Double.IsNaN((item as Plot2DCurve).AnnotationPosition.X)) (item as Plot2DCurve).AnnotationPosition = currentPosition;
            }
        }

        protected void marginChangeTimer_Tick(Object sender, EventArgs e)
        {
            marginChangeTimer.Stop();
            marginChangeTimer.Interval = TimeSpan.FromSeconds(0.0);
            this.InvalidateMeasure();
        }

        void mousePositionTimer_Tick(object sender, EventArgs e)
        {
            foreach (Plot2DItem item in plotItems)
            {
                if (item is Plot2DCurve)
                {
                    if (Double.IsNaN((item as Plot2DCurve).AnnotationPosition.X))
                    {
                        if (((item as Plot2DCurve).Line.IsMouseOver || (item as Plot2DCurve).Markers.IsMouseOver))
                        {
                            (item as Plot2DCurve).AnnotationPosition = currentPosition;
                        }
                    }
                }
            }
        }
    }
}
