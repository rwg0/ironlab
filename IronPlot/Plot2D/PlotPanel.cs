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

namespace IronPlot
{
    /// <summary>
    /// Position of annotation elements applied to PlotPanel.
    /// </summary>
    public enum Position { Left, Right, Top, Bottom }

    public partial class PlotPanel : Panel
    {
        // Children are two canvases: one for plot content and one for the axes:
        internal Canvas canvas;
        internal Canvas axesCanvas;
        // Also a background Canvas
        internal Canvas backgroundCanvas;
        // This is present because a Direct2D surface can also be added and it is desirable to make the
        // canvas above transparent in this case. 

        // Also a Direct2DControl: a control which can use Direct2D for fast plotting.
        internal Direct2DControl direct2DControl = null;

        // The items are stacked

        // Axes object (child of axesCanvas) 
        internal Axes2D axes;

        // Annotation regions
        internal StackPanel annotationsLeft;
        internal StackPanel annotationsRight;
        internal StackPanel annotationsTop;
        internal StackPanel annotationsBottom;

        internal Thickness minimumAxesMargin = new Thickness(0);

        // Transforms:
        internal MatrixTransform graphToCanvas;
        internal MatrixTransform graphToAxesCanvas;
        internal MatrixTransform canvasToGraph;

        protected DispatcherTimer marginChangeTimer;

        // Arrangement
        // whether or not legend is shown:
        bool showAnnotationsLeft = false;
        bool showAnnotationsRight = false;
        bool showAnnotationsTop = false;
        bool showAnnotationsBottom = false;
        // The location of canvas and axes; axes canvas always starts at point (0,0):
        protected Rect axesCanvasLocation, canvasLocation;
        // Width and height of legends, axes and canvas combined: 
        double entireWidth = 0, entireHeight = 0;
        // Offset of combination of legends, axes and canvas in available area:
        double offsetX = 0, offsetY = 0;

        public static readonly DependencyProperty EqualAxesProperty =
            DependencyProperty.Register("EqualAxesProperty",
            typeof(bool), typeof(PlotPanel),
            new PropertyMetadata(false, OnEqualAxesChanged));

        public static readonly DependencyProperty UseDirect2DProperty =
            DependencyProperty.Register("UseDirect2DProperty",
            typeof(bool), typeof(PlotPanel),
            new PropertyMetadata(false, OnUseDirect2DChanged));

        internal bool UseDirect2D
        {
            set
            {
                SetValue(UseDirect2DProperty, value);
            }
            get { return (bool)GetValue(UseDirect2DProperty); }
        }

        protected static void OnEqualAxesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PlotPanel plotPanelLocal = ((PlotPanel)obj);
            plotPanelLocal.InvalidateMeasure();
        }

        protected static void OnUseDirect2DChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            PlotPanel plotPanelLocal = ((PlotPanel)obj);
            if (plotPanelLocal.direct2DControl == null && plotPanelLocal.UseDirect2D)
            {
                // Create Direct2DControl:
                try
                {
                    plotPanelLocal.direct2DControl = new Direct2DControl();
                    plotPanelLocal.Children.Add(plotPanelLocal.direct2DControl);
                    plotPanelLocal.direct2DControl.SetValue(Grid.ZIndexProperty, 75);
                }
                catch (Exception)
                {
                    plotPanelLocal.direct2DControl = null;
                    plotPanelLocal.UseDirect2D = false;
                }
                return;
            }
            if (plotPanelLocal.UseDirect2D) plotPanelLocal.direct2DControl.Visibility = Visibility.Visible;
            else plotPanelLocal.direct2DControl.Visibility = Visibility.Collapsed;
            plotPanelLocal.InvalidateMeasure();
        }

        public PlotPanel()
        {
            // Add Canvas objects
            this.Background = Brushes.White;
            canvas = new Canvas();
            axesCanvas = new Canvas();
            backgroundCanvas = new Canvas();
            this.HorizontalAlignment = HorizontalAlignment.Center;
            this.VerticalAlignment = VerticalAlignment.Center;
            this.Children.Add(canvas);
            this.Children.Add(backgroundCanvas);
            canvas.ClipToBounds = true;
            axesCanvas.ClipToBounds = false;
            backgroundCanvas.SetValue(Grid.ZIndexProperty, 50);
            canvas.SetValue(Grid.ZIndexProperty, 100);
            // Create transform objects; these are updated during Arrange
            graphToCanvas = new MatrixTransform();
            canvasToGraph = new MatrixTransform();
            graphToAxesCanvas = new MatrixTransform();
            // Add Axes to axesCanvas
            LinearGradientBrush background = new LinearGradientBrush();
            background.StartPoint = new Point(0, 0); background.EndPoint = new Point(1, 1);
            background.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            background.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));
            canvas.Background = Brushes.Transparent;
            backgroundCanvas.Background = background;
            direct2DControl = null;

            axes = new Axes2D(this);
            axesCanvas.Children.Add(axes);
            //backgroundCanvas.Children.Add(axes.XAxes.GridLines);
            //backgroundCanvas.Children.Add(axes.YAxes.GridLines);
            //axes.XAxes.GridLines.SetValue(Canvas.ZIndexProperty, 50);
            //axes.YAxes.GridLines.SetValue(Canvas.ZIndexProperty, 50);
            //
            AddAxes();
            //
            this.CreateLegends();
            if (!(this is ColourBarPanel)) this.AddInteractionEvents();
            this.AddSelectionRectangle();
            this.InitialiseChildenCollection();
            marginChangeTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.0), DispatcherPriority.Normal, marginChangeTimer_Tick, this.Dispatcher);
        }

        /// <summary>
        /// Visually, the axes comprise an Axes2D and the indiviual Axis2D objects.
        /// There is one of the former, many of the latter. This method must be called
        /// every time a new Axis2D is added or removed.
        /// </summary>
        internal void AddAxes()
        {
            if (!Children.Contains(axesCanvas))
            {
                Children.Add(axesCanvas);
                axesCanvas.Background = null;
                axesCanvas.SetValue(Grid.ZIndexProperty, 200);
            }
            IEnumerable<Axis2D> allAxis2D = axes.XAxes.Concat(axes.YAxes);
            foreach (Axis2D axis2D in allAxis2D)
            {
                if (!Children.Contains(axis2D))
                {
                    Children.Add(axis2D);
                    axis2D.SetValue(Grid.ZIndexProperty, 201);
                }
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var allAxes = axes.XAxes.Concat(axes.YAxes);
            foreach (Axis2D axis in allAxes) axis.UpdateAndMeasureLabels();
            annotationsLeft.Measure(availableSize);
            annotationsRight.Measure(availableSize);
            annotationsTop.Measure(availableSize);
            annotationsBottom.Measure(availableSize);
            //
            availableSize.Height = Math.Min(availableSize.Height, 10000);
            availableSize.Width = Math.Min(availableSize.Width, 10000);
            MeasureAxes(availableSize);
            //
            canvas.Measure(new Size(canvasLocation.Width, canvasLocation.Height));
            backgroundCanvas.Measure(new Size(canvasLocation.Width, canvasLocation.Height));
            availableSize.Height = axesCanvasLocation.Height + annotationsTop.DesiredSize.Height + annotationsBottom.DesiredSize.Height;
            availableSize.Width = axesCanvasLocation.Width + annotationsLeft.DesiredSize.Width + annotationsRight.DesiredSize.Width;

            return availableSize;
        }

        protected void ArrangeAxes(Size availableSize)
        {

        }

        /// <summary>
        /// Render the Axes according to the room available.
        /// </summary>
        /// <param name="availableSize"></param>
        protected void MeasureAxes(Size availableSize)
        {
            // Allow legends their widths.
            showAnnotationsLeft = false;
            showAnnotationsRight = false;
            showAnnotationsTop = false;
            showAnnotationsBottom = false;
            double startX = 0; double startY = 0;
            double endX = availableSize.Width; double endY = availableSize.Height;
            entireWidth = 0; entireHeight = 0;
            offsetX = 0; offsetY = 0;
            if ((endX - startX) > (annotationsLeft.DesiredSize.Width + 1))
            {
                showAnnotationsLeft = true;
                startX += annotationsLeft.DesiredSize.Width;
                entireWidth += annotationsLeft.DesiredSize.Width;
                offsetX += annotationsLeft.DesiredSize.Width;
            }
            if ((endX - startX) > (annotationsRight.DesiredSize.Width + 1))
            {
                showAnnotationsRight = true;
                endX -= annotationsRight.DesiredSize.Width;
                entireWidth += annotationsRight.DesiredSize.Width;
            }
            if ((endY - startY) > (annotationsTop.DesiredSize.Height + 1))
            {
                showAnnotationsTop = true;
                startY += annotationsTop.DesiredSize.Height;
                entireHeight += annotationsTop.DesiredSize.Height;
                offsetY += annotationsTop.DesiredSize.Height;
            }
            if ((endY - startY) > (annotationsBottom.DesiredSize.Height + 1))
            {
                showAnnotationsBottom = true;
                endY -= annotationsBottom.DesiredSize.Height;
                entireHeight += annotationsBottom.DesiredSize.Height;
            }
            Rect available = new Rect(startX, 0, endX - startX, endY - startY);
            bool axesEqual = (bool)this.GetValue(EqualAxesProperty);
            // Calculates the axes positions, positions labels
            // and updates the graphToAxesCanvas transform.
            Size requiredSize;
            Rect canvasLocationWithinAxes;
            if (dragging)
                axes.UpdateAxisPositionsOffsetOnly(available, out canvasLocation, out axesCanvasLocation);
            else
            {
                axes.MeasureAxesFull(new Size(available.Width, available.Height), out canvasLocationWithinAxes, out requiredSize);
                canvasLocation = canvasLocationWithinAxes;
                axesCanvasLocation = new Rect(new Point(0, 0), requiredSize);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //MeasureAxes(finalSize);
            graphToCanvas.Matrix = new Matrix(graphToAxesCanvas.Matrix.M11, 0, 0, graphToAxesCanvas.Matrix.M22,
                graphToAxesCanvas.Matrix.OffsetX - canvasLocation.Left, graphToAxesCanvas.Matrix.OffsetY - canvasLocation.Top);
            MatrixTransform inverse = (MatrixTransform)(graphToCanvas.Inverse);
            if (inverse == null) return finalSize;
            canvasToGraph.Matrix = inverse.Matrix;        
            canvasLocation = new Rect(canvasLocation.X, canvasLocation.Y, canvasLocation.Width, canvasLocation.Height);
            axesCanvasLocation = new Rect(axesCanvasLocation.X, axesCanvasLocation.Y, axesCanvasLocation.Width, axesCanvasLocation.Height);
            double entireWidth = this.entireWidth;
            double entireHeight = this.entireHeight;
            double offsetX = this.offsetX;
            double offsetY = this.offsetY;
            entireWidth += axesCanvasLocation.Width;
            entireHeight += axesCanvasLocation.Height;
            offsetX += (finalSize.Width - entireWidth) / 2;
            offsetY += (finalSize.Height - entireHeight) / 2;
            axesCanvasLocation.X += offsetX;
            canvasLocation.X += offsetX;
            axesCanvasLocation.Y += offsetY;
            canvasLocation.Y += offsetY;
            //canvas.InvalidateVisual();
            BeforeArrange();
            axesCanvas.Arrange(axesCanvasLocation);
            //
            // We also arrange each Axis in the same location.
            foreach (Axis2D axis in axes.XAxes) axis.Arrange(axesCanvasLocation);
            foreach (Axis2D axis in axes.YAxes) axis.Arrange(axesCanvasLocation);

            backgroundCanvas.Arrange(canvasLocation);
            canvas.Arrange(canvasLocation);
            if (direct2DControl != null) direct2DControl.Arrange(canvasLocation);
            backgroundCanvas.InvalidateVisual();
            canvas.InvalidateVisual();
            // Now arrange canvases
            if (showAnnotationsLeft)
            {
                Rect annotationsLeftRect = new Rect(new Point(axesCanvasLocation.Left - annotationsLeft.DesiredSize.Width, axesCanvasLocation.Top),
                    new Point(axesCanvasLocation.Left, axesCanvasLocation.Bottom));
                annotationsLeft.Arrange(annotationsLeftRect);
            }
            if (showAnnotationsRight)
            {
                Rect annotationsRightRect = new Rect(new Point(axesCanvasLocation.Right, axesCanvasLocation.Top),
                    new Point(axesCanvasLocation.Right + annotationsRight.DesiredSize.Width, axesCanvasLocation.Bottom));
                annotationsRight.Arrange(annotationsRightRect);
            }
            else annotationsRight.Arrange(new Rect());
            if (showAnnotationsTop)
            {
                Rect annotationsTopRect = new Rect(new Point(axesCanvasLocation.Left, axesCanvasLocation.Top - annotationsTop.DesiredSize.Height),
                    new Point(axesCanvasLocation.Right, axesCanvasLocation.Top));
                annotationsTop.Arrange(annotationsTopRect);
            }
            if (showAnnotationsBottom)
            {
                Rect annotationsBottomRect = new Rect(new Point(axesCanvasLocation.Left, axesCanvasLocation.Bottom),
                    new Point(axesCanvasLocation.Right, axesCanvasLocation.Bottom + annotationsBottom.DesiredSize.Height));
                annotationsBottom.Arrange(annotationsBottomRect);
            }
            // Finally redraw axes lines
            axes.InvalidateMeasure();
            //axes.XAxes.GridLines.InvalidateMeasure();
            //axes.YAxes.GridLines.InvalidateMeasure();
            //canvasToGraph.axes.XAxes.GridLines.InvalidateMeasure(); 
            return finalSize;
        }

        // Called just before arrange. Uses include giving children a chance to
        // rearrange their geometry in the light of the updated transforms.
        protected virtual void BeforeArrange()
        {
            foreach (Plot2DItem child in plotItems)
            {
                child.BeforeArrange();
            }
        }
    }
}
