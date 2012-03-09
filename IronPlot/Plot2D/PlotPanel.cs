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

        internal Axes2D axes;

        protected DispatcherTimer marginChangeTimer;

        protected Size axesRegionSize;
        // The location of canvas:
        protected Rect canvasLocation;

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
            if ((bool)e.NewValue == true) plotPanelLocal.axes.SetAxesEqual();
            else plotPanelLocal.axes.ResetAxesEqual();
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
            axes = new Axes2D(this);
            this.Children.Add(axes);
            axes.SetValue(Grid.ZIndexProperty, 300);
            // note that individual axes have index of 200

            LinearGradientBrush background = new LinearGradientBrush();
            background.StartPoint = new Point(0, 0); background.EndPoint = new Point(1, 1);
            background.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            background.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));
            Canvas.Background = Brushes.Transparent;
            BackgroundCanvas.Background = background;
            direct2DControl = null;
            //
            //this.CreateLegends();
            if (!(this is ColourBarPanel)) this.AddInteractionEvents();
            this.AddSelectionRectangle();
            this.InitialiseChildenCollection();
            marginChangeTimer = new DispatcherTimer(TimeSpan.FromSeconds(0.0), DispatcherPriority.Normal, marginChangeTimer_Tick, this.Dispatcher);
        }

        Size sizeOnMeasure;
        Size sizeAfterMeasure;

        protected override Size MeasureOverride(Size availableSize)
        {
            sizeOnMeasure = availableSize;
            var allAxes = axes.XAxes.Concat(axes.YAxes);
            axes.Measure(availableSize);
            foreach (Axis2D axis in allAxes)
            {
                axis.UpdateAndMeasureLabels();
            }

            AnnotationsLeft.Measure(availableSize); AnnotationsRight.Measure(availableSize);
            AnnotationsTop.Measure(availableSize); AnnotationsBottom.Measure(availableSize);
            
            availableSize.Height = Math.Min(availableSize.Height, 10000);
            availableSize.Width = Math.Min(availableSize.Width, 10000);
            
            // Main measurement work:
            // Return the region available for plotting and set legendRegion:
            Rect available = PlaceAnnotations(availableSize);
            // Place the axes using this region, setting axesRegionSize and canvasLocation:
            PlaceAxes(available);
            
            Canvas.Measure(new Size(canvasLocation.Width, canvasLocation.Height));
            BackgroundCanvas.Measure(new Size(canvasLocation.Width, canvasLocation.Height));
            availableSize.Height = axesRegionSize.Height + AnnotationsTop.DesiredSize.Height + AnnotationsBottom.DesiredSize.Height;
            availableSize.Width = axesRegionSize.Width + AnnotationsLeft.DesiredSize.Width + AnnotationsRight.DesiredSize.Width;
            sizeAfterMeasure = availableSize;
            return availableSize;
        }

        /// <summary>
        /// Place the Axes according to the room available.
        /// </summary>
        /// <param name="availableSize"></param>
        protected void PlaceAxes(Rect available)
        {
            // Allow legends their widths.
            axes.Measure(new Size(available.Width, available.Height));

            bool axesEqual = (bool)this.GetValue(EqualAxesProperty);
            // Calculates the axes positions, positions labels, updates geometries.
            Rect canvasLocationWithinAxes;
            if (dragging)
                axes.UpdateAxisPositionsOffsetOnly(available, out canvasLocation, out axesRegionSize);
            else
            {
                axes.PlaceAxesFull(new Size(available.Width, available.Height), out canvasLocationWithinAxes, out axesRegionSize);
                canvasLocation = canvasLocationWithinAxes;
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Stopwatch watch = new Stopwatch(); watch.Start();
            if (!(finalSize == sizeOnMeasure || finalSize == sizeAfterMeasure))
            {
                // Return the final region for plotting and set legendRegion:
                Rect final = PlaceAnnotations(finalSize);
                // Place axes using this region, setting axesRegionSize and canvasLocation:
                PlaceAxes(final);
            }
            canvasLocation = new Rect(canvasLocation.X, canvasLocation.Y, canvasLocation.Width, canvasLocation.Height);
            axesRegionLocation = new Rect(0, 0, axesRegionSize.Width, axesRegionSize.Height);
            double entireWidth = legendRegion.Left + legendRegion.Right;
            double entireHeight = legendRegion.Top + legendRegion.Bottom;
            double offsetX = legendRegion.Left;
            double offsetY = legendRegion.Top;
            entireWidth += axesRegionSize.Width;
            entireHeight += axesRegionSize.Height;
            offsetX += (finalSize.Width - entireWidth) / 2;
            offsetY += (finalSize.Height - entireHeight) / 2;
            axesRegionLocation.X += offsetX;
            canvasLocation.X += offsetX;
            axesRegionLocation.Y += offsetY;
            canvasLocation.Y += offsetY;

            BeforeArrange();

            // The axes themselves (i.e. the rectangle around the plot canvas):
            axes.Arrange(axesRegionLocation);
            axes.InvalidateVisual();

            // Arrange each Axis. Arranged over the whole axes region, although of course the axis will typically
            // only cover a potion of this.
            foreach (Axis2D axis in axes.XAxes) axis.Arrange(axesRegionLocation);
            foreach (Axis2D axis in axes.YAxes) axis.Arrange(axesRegionLocation);

            BackgroundCanvas.Arrange(canvasLocation);
            axes.RenderEachAxis();
            // 'Rendering' of plot items, i.e. recreating geometries is done in BeforeArrange.

            Canvas.Arrange(canvasLocation);
            if (direct2DControl != null) direct2DControl.Arrange(canvasLocation);
            BackgroundCanvas.InvalidateVisual();
            Canvas.InvalidateVisual();
            
            ArrangeAnnotations();
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
