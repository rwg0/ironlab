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
        private Rect mouseDragStartRect;
        private string displayMode = "normal";

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
        }

        protected void AddSelectionRectangle()
        {
            mouseDragStartRect = new Rect();
            //
            selection = new Rectangle(); selection.Visibility = Visibility.Visible;
            SolidColorBrush selectionBrush = new SolidColorBrush();
            selectionBrush.Color = Brushes.Aquamarine.Color;
            selectionBrush.Opacity = 0.5;
            selection.Fill = selectionBrush;
            selection.StrokeDashOffset = 5; selection.StrokeThickness = 0.99;
            SolidColorBrush selectionBrush2 = new SolidColorBrush();
            selectionBrush2.Color = Color.FromArgb(255, 0, 0, 0);
            selection.Stroke = selectionBrush2;
            selection.HorizontalAlignment = HorizontalAlignment.Left;
            DoubleCollection strokeDashArray1 = new DoubleCollection(2);
            strokeDashArray1.Add(3); strokeDashArray1.Add(3);
            selection.VerticalAlignment = VerticalAlignment.Top;
            selection.StrokeDashArray = strokeDashArray1;
            selection.ClipToBounds = true;
            selection.Width = 0; selection.Height = 0;
            canvas.Children.Add(selection);
            selection.SetValue(Canvas.ZIndexProperty, 1000);
        }

        protected void canvas_LeftClickStart(object sender, MouseButtonEventArgs e)
        {
            switch (displayMode)
            {
                case "normal":
                    if (e.ClickCount > 1)
                    {
                        dragging = false;
                        this.Cursor = Cursors.Arrow;
                        ViewedRegion = GetBoundsFromChildren();
                        break;
                    }
                    else
                    {
                        mouseDragStartPoint = e.GetPosition(this);
                        mouseDragStartRect = ViewedRegion;
                        this.Cursor = Cursors.ScrollAll;
                        dragging = true;
                        break;
                    }
            }
            canvas.CaptureMouse();
        }

        protected void canvas_RightClickStart(object sender, MouseButtonEventArgs e)
        {
            switch (displayMode)
            {
                case "normal":
                    selectionStarted = true;
                    selectionStart = e.GetPosition(canvas);
                    selection.Width = 0;
                    selection.Height = 0;
                    break;
            }
            canvas.CaptureMouse();
        }

        protected void canvas_LeftClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                switch (displayMode)
                {
                    case "normal":
                        this.Cursor = Cursors.Arrow;
                        dragging = false;
                        marginChangeTimer.Interval = TimeSpan.FromSeconds(0.2);
                        marginChangeTimer.Start();
                        break;
                }
            }
            canvas.ReleaseMouseCapture();
        }

        protected void canvas_RightClickEnd(object sender, MouseButtonEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                switch (displayMode)
                {
                    case "normal":
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
                            Point start = canvasToGraph.Transform(selectionStart);
                            Point end = canvasToGraph.Transform(selectionEnd);
                            Rect newViewedRegion = new Rect(start, end);
                            ViewedRegion = newViewedRegion;
                            selectionStarted = false;
                        }
                        break;
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
            Rect viewedRegion = ViewedRegion;
            double factor = Math.Pow(1.4, delta);
            MatrixTransform newGraphToCanvas = new MatrixTransform(graphToCanvas.Matrix.M11 * factor, 0, 0,
                graphToCanvas.Matrix.M22 * factor,
                canvasPosition.X - graphPosition.X * graphToCanvas.Matrix.M11 * factor,
                canvasPosition.Y - graphPosition.Y * graphToCanvas.Matrix.M22 * factor);
            MatrixTransform newCanvasToGraph = (MatrixTransform)newGraphToCanvas.Inverse;
            Point topLeft = new Point(0, 0);
            Point bottomRight = new Point(canvas.ActualWidth, canvas.ActualHeight);
            viewedRegion = new Rect(newCanvasToGraph.Transform(topLeft), newCanvasToGraph.Transform(bottomRight));
            ViewedRegion = viewedRegion;
        }

        protected void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (canvas.IsMouseCaptured)
            {
                switch (displayMode)
                {
                    case "normal":
                        if (dragging)
                        {
                            // Get the new mouse position. 
                            Point mouseDragCurrentPoint = e.GetPosition(this);

                            Point delta = new Point(
                                (mouseDragCurrentPoint.X - mouseDragStartPoint.X),
                                (mouseDragCurrentPoint.Y - mouseDragStartPoint.Y));

                            Rect newViewedRegion = mouseDragStartRect;
                            Point offset = new Point(-canvasToGraph.Matrix.M11 * delta.X, -canvasToGraph.Matrix.M22 * delta.Y);
                            newViewedRegion.Offset(offset.X, offset.Y);
                            ViewedRegion = newViewedRegion;
                        }
                        if (selectionStarted)
                        {
                            Rect rect = new Rect(selectionStart, e.GetPosition(canvas));
                            selection.RenderTransform = new TranslateTransform(rect.X, rect.Y);
                            selection.Width = rect.Width;
                            selection.Height = rect.Height;
                        }
                        break;
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
