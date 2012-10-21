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
using System.Diagnostics;

namespace IronPlot
{
    /// <summary>
    /// Position of annotation elements applied to PlotPanel.
    /// </summary>
    public enum Position { Left, Right, Top, Bottom }

    public partial class PlotPanel : PlotPanelBase
    {
        // Canvas for plot content:
        internal Canvas Canvas;

        // Also a background Canvas
        internal Canvas BackgroundCanvas;
        // This is present because a Direct2D surface can also be added and it is desirable to make the
        // canvas above transparent in this case. 

        // Also a Direct2DControl: a control which can use Direct2D for fast plotting.
        internal Direct2DControl direct2DControl = null;

        internal Axes2D Axes;

        protected DispatcherTimer marginChangeTimer;

        internal Size AvailableSize;
        // The location of the Canvas within the AvailableSize.
        internal Rect CanvasLocation;

        public static readonly DependencyProperty EqualAxesProperty =
            DependencyProperty.Register("EqualAxes",
            typeof(bool), typeof(PlotPanel),
            new PropertyMetadata(false, OnEqualAxesChanged));

        public static readonly DependencyProperty UseDirect2DProperty =
            DependencyProperty.Register("UseDirect2D",
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
            if ((bool)e.NewValue == true) plotPanelLocal.Axes.SetAxesEqual();
            else plotPanelLocal.Axes.ResetAxesEqual();
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
            if (plotPanelLocal.UseDirect2D)
                plotPanelLocal.Children.Add(plotPanelLocal.direct2DControl);
            else
                plotPanelLocal.Children.Remove(plotPanelLocal.direct2DControl);
            plotPanelLocal.InvalidateMeasure();
        }

        public PlotPanel() : base()
        {
            ClipToBounds = true;
            // Add Canvas objects
            this.Background = Brushes.White; this.HorizontalAlignment = HorizontalAlignment.Center; this.VerticalAlignment = VerticalAlignment.Center;
            Canvas = new Canvas();
            BackgroundCanvas = new Canvas();
            this.Children.Add(Canvas);
            this.Children.Add(BackgroundCanvas);
            //
            Canvas.ClipToBounds = true;
            Canvas.SetValue(Grid.ZIndexProperty, 100);
            BackgroundCanvas.SetValue(Grid.ZIndexProperty, 50);
            Axes = new Axes2D(this);

            //LinearGradientBrush background = new LinearGradientBrush();
            //background.StartPoint = new Point(0, 0); background.EndPoint = new Point(1, 1);
            //background.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            //background.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));
            Canvas.Background = Brushes.Transparent;
            BackgroundCanvas.Background = Brushes.White;
            direct2DControl = null;
            //
            if (!(this is ColourBarPanel)) this.AddInteractionEvents();
            this.AddSelectionRectangle();
            this.InitialiseChildenCollection();
            marginChangeTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.0), DispatcherPriority.Normal, marginChangeTimer_Tick, this.Dispatcher);
        }

        Size sizeOnMeasure;
        Size sizeAfterMeasure;

        /// <summary>
        /// For each PlotPanel, place the axes.
        /// </summary>
        /// <param name="plotPanels"></param>
        internal static void PlaceAxes(List<PlotPanel> plotPanels)
        {
            //IEnumerable<Axis2D> xAxes
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            sizeOnMeasure = availableSize;
            AvailableSize = availableSize;
            var allAxes = Axes.XAxes.Concat(Axes.YAxes);
            foreach (Axis2D axis in allAxes)
            {
                axis.UpdateAndMeasureLabels();
            }
            if (direct2DControl != null) direct2DControl.Measure(availableSize);
            AvailableSize.Height = Math.Min(AvailableSize.Height, 10000);
            AvailableSize.Width = Math.Min(AvailableSize.Width, 10000);
            
            // Main measurement work:
            MeasureAnnotations(AvailableSize);
            // Set legendRegion:
            PlaceAnnotations(AvailableSize);

            // Place the axes using this region, setting axesRegionSize and canvasLocation:
            PlaceAxes();
            
            Canvas.Measure(new Size(CanvasLocation.Width, CanvasLocation.Height));
            BackgroundCanvas.Measure(new Size(CanvasLocation.Width, CanvasLocation.Height));
            availableSize.Width = AxesRegion.Width;
            availableSize.Height = AxesRegion.Height;
            //availableSize.Height = Math.Max(Math.Max(axesRegion.Height + AnnotationsTop.DesiredSize.Height + AnnotationsBottom.DesiredSize.Height, AnnotationsLeft.DesiredSize.Height), AnnotationsRight.DesiredSize.Height);
            //availableSize.Width = Math.Max(Math.Max(axesRegion.Width + AnnotationsLeft.DesiredSize.Width + AnnotationsRight.DesiredSize.Width, AnnotationsTop.DesiredSize.Width), AnnotationsBottom.DesiredSize.Width);
            sizeAfterMeasure = AvailableSize;
            return availableSize;
        }

        /// <summary>
        /// Place the Axes according to the room available.
        /// </summary>
        /// <param name="availableSize"></param>
        protected void PlaceAxes()
        {
            // Calculates the axes positions, positions labels, updates geometries.
            if (dragging) 
                Axes.UpdateAxisPositionsOffsetOnly();
            else
            {
                Axes.PlaceAxesFull();
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            //Stopwatch watch = new Stopwatch(); watch.Start();
            if (!(finalSize == sizeOnMeasure || finalSize == sizeAfterMeasure))
            {
                // Set legendRegion:
                PlaceAnnotations(finalSize);
                // Place axes using this region, setting axesRegionSize and canvasLocation:
                AvailableSize = finalSize;
                PlaceAxes();
            }
            BeforeArrange();

            BackgroundCanvas.Arrange(CanvasLocation);
            Rect canvasRelativeToAxesRegion = new Rect(CanvasLocation.X - AxesRegion.X,
                CanvasLocation.Y - AxesRegion.Y, CanvasLocation.Width, CanvasLocation.Height);
            Axes.RenderEachAxisAndFrame(canvasRelativeToAxesRegion);
            // 'Rendering' of plot items, i.e. recreating geometries is done in BeforeArrange.

            // Arrange each Axis. Arranged over the whole axes region, although of course the axis will typically
            // only cover a potion of this.
            Axes.ArrangeEachAxisAndFrame(AxesRegion);

            Canvas.Arrange(CanvasLocation);
            if (direct2DControl != null) direct2DControl.Arrange(CanvasLocation);
            BackgroundCanvas.InvalidateVisual();
            Canvas.InvalidateVisual();
            
            ArrangeAnnotations(finalSize);

            if (direct2DControl != null)
            {
                direct2DControl.Arrange(CanvasLocation);
                direct2DControl.RequestRender();
            }
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
