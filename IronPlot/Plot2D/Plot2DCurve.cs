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
using System.Windows.Xps;
//using System.Windows.Xps.Packaging;
//using System.Windows.Xps.Serialization;
using System.Printing;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot
{   
    public enum MarkersType { None, Square, Circle, Triangle };

    public class Plot2DCurve : Plot2DItem
    {
        //VisualLine visualLine;
        protected MatrixTransform graphToCanvas = new MatrixTransform(Matrix.Identity);
        protected MatrixTransform canvasToGraph = new MatrixTransform(Matrix.Identity);
        
        // WPF elements:
        private PlotPath line;
        private PlotPath markers;
        private PlotPath legendLine;
        private PlotPath legendMarker;
        private LegendItem legendItem;
        
        // Direct2D elements:
        private DirectPath lineD2D;
        private DirectPathScatter markersD2D;

        #region DependencyProperties
        public static readonly DependencyProperty MarkersTypeProperty =
            DependencyProperty.Register("MarkersTypeProperty",
            typeof(MarkersType), typeof(Plot2DCurve),
            new PropertyMetadata(MarkersType.None,
                OnMarkersChanged));

        public static readonly DependencyProperty MarkersSizeProperty =
            DependencyProperty.Register("MarkersSizeProperty",
            typeof(double), typeof(Plot2DCurve),
            new PropertyMetadata(10.0,
                OnMarkersChanged));

        public static readonly DependencyProperty MarkersFillProperty =
            DependencyProperty.Register("MarkersFillProperty",
            typeof(Brush), typeof(Plot2DCurve),
            new PropertyMetadata(Brushes.Transparent));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("StrokeProperty",
            typeof(Brush), typeof(Plot2DCurve),
            new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThicknessProperty",
            typeof(double), typeof(Plot2DCurve),
            new PropertyMetadata(1.0));

        public static readonly DependencyProperty QuickLineProperty =
            DependencyProperty.Register("QuickLineProperty",
            typeof(string), typeof(Plot2DCurve),
            new PropertyMetadata("-k",
            OnQuickLinePropertyChanged));

        public static readonly DependencyProperty QuickStrokeDashProperty =
            DependencyProperty.Register("QuickStrokeDashProperty",
            typeof(QuickStrokeDash), typeof(Plot2DCurve),
            new PropertyMetadata(QuickStrokeDash.Solid,
            OnQuickStrokeDashPropertyChanged));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("TitleProperty",
            typeof(string), typeof(Plot2DCurve),
            new PropertyMetadata(""));

        /// <summary>
        /// Get or set line in <line><markers><colour> notation, e.g. --sr for dashed red line with square markers
        /// </summary>
        public string QuickLine
        {
            set
            {
                SetValue(QuickLineProperty, value);
            }
            get
            {
                return GetLinePropertyFromStrokeProperties();
            }
        }

        public QuickStrokeDash QuickStrokeDash
        {
            set
            {
                SetValue(QuickStrokeDashProperty, value);
            }
            get { return (QuickStrokeDash)GetValue(QuickStrokeDashProperty); }
        }

        public MarkersType MarkersType
        {
            set
            {
                SetValue(MarkersTypeProperty, value);
            }
            get { return (MarkersType)GetValue(MarkersTypeProperty); }
        }

        public double MarkersSize
        {
            set
            {
                SetValue(MarkersSizeProperty, value);
            }
            get { return (double)GetValue(MarkersSizeProperty); }
        }

        public Brush MarkersFill
        {
            set
            {
                SetValue(MarkersFillProperty, value);
            }
            get { return (Brush)GetValue(MarkersFillProperty); }
        }

        public Brush Stroke
        {
            set
            {
                SetValue(StrokeProperty, value);
            }
            get { return (Brush)GetValue(StrokeProperty); }
        }

        public double StrokeThickness
        {
            set
            {
                SetValue(StrokeThicknessProperty, value);
            }
            get { return (double)GetValue(StrokeThicknessProperty); }
        }

        public string Title
        {
            set
            {
                SetValue(TitleProperty, value);
            }
            get { return (string)GetValue(TitleProperty); }
        }

        protected static void OnMarkersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((MarkersType)e.NewValue == MarkersType.None)
            {
                ((Plot2DCurve)obj).UpdateMarkers();
            }
            else
            {
                ((Plot2DCurve)obj).UpdateMarkers();
            }
        }

        protected static void OnQuickLinePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            string lineProperty = (string)(e.NewValue);
            ((Plot2DCurve)obj).SetStrokePropertiesFromLineProperty(lineProperty);
        }

        protected static void OnQuickStrokeDashPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Plot2DCurve)obj).line.QuickStrokeDash = (QuickStrokeDash)(e.NewValue);
        }

        public static readonly DependencyProperty UseDirect2DProperty =
            DependencyProperty.Register("UseDirect2DProperty",
            typeof(bool), typeof(Plot2DCurve),
            new PropertyMetadata(false, OnUseDirect2DChanged));
        #endregion

        private Curve curve;
        
        protected override void OnHostChanged(PlotPanel host)
        {
            base.OnHostChanged(host);
            if (this.host != null)
            {
                try
                {
                    RemoveElements((bool)GetValue(UseDirect2DProperty));
                    Plot.Legend.Items.Remove(legendItem);
                    this.host = host;
                    BindingOperations.ClearBinding(this, Plot2DCurve.UseDirect2DProperty);
                }
                catch (Exception) 
                { 
                    // Just swallow any exception 
                }
            }
            else this.host = host;
            if (this.host != null)
            {
                AddElements();
                curve.Transform(xAxis.GraphTransform, yAxis.GraphTransform);
                // Add binding:
                bindingDirect2D = new Binding("UseDirect2DProperty") { Source = host, Mode = BindingMode.OneWay };
                BindingOperations.SetBinding(this, Plot2DCurve.UseDirect2DProperty, bindingDirect2D);
                // Add lines and markers (according to whether we are using Direct2D):
                // Add legend:
                Plot.Legend.Items.Add(legendItem);
            }
            SetBounds();
        }
        Binding bindingDirect2D;

        private void AddElements()
        {
            if ((bool)GetValue(UseDirect2DProperty) == false)
            {   
                line.Data = curve.ToPathGeometry(null);
                line.SetValue(Canvas.ZIndexProperty, 200);
                line.Data.Transform = graphToCanvas;
                markers.SetValue(Canvas.ZIndexProperty, 200);
                host.Canvas.Children.Add(Line);
                host.Canvas.Children.Add(Markers);
            }
            else
            {
                host.direct2DControl.AddPath(lineD2D);
                host.direct2DControl.AddPath(markersD2D);
                markersD2D.GraphToCanvas = graphToCanvas;
            }
            //
            UpdateMarkers();
        }

        private void RemoveElements(bool removeDirect2DComponents)
        {
            if (removeDirect2DComponents)
            {
                host.direct2DControl.RemovePath(lineD2D);
                host.direct2DControl.RemovePath(markersD2D);
            }
            else
            {
                host.Canvas.Children.Remove(line);
                host.Canvas.Children.Remove(markers);
            }
        }

        public Plot2DCurve(Curve curve)
        {
            this.curve = curve;
            Initialize();
        }

        public Plot2DCurve(object x, object y)
        {
            this.curve = new Curve(Plotting.Array(x), Plotting.Array(y));
            Initialize();
        }

        protected void Initialize()
        {
            line = new PlotPath();
            markers = new PlotPath();
            line.StrokeLineJoin = PenLineJoin.Bevel;
            line.Visibility = Visibility.Visible;
            markers.Visibility = Visibility.Visible;
            //
            lineD2D = new DirectPath();
            markersD2D = new DirectPathScatter() { Curve = curve };
            //
            legendLine = new PlotPath();
            legendMarker = new PlotPath();
            legendMarker.HorizontalAlignment = HorizontalAlignment.Center; legendMarker.VerticalAlignment = VerticalAlignment.Center;
            legendLine.HorizontalAlignment = HorizontalAlignment.Center; legendLine.VerticalAlignment = VerticalAlignment.Center;
            legendLine.Data = new LineGeometry(new Point(0, 0), new Point(30, 0));
            legendItem = new LegendItem();
            Grid legendItemGrid = new Grid();
            legendItemGrid.Children.Add(legendLine);
            legendItemGrid.Children.Add(legendMarker);
            legendItem.Content = legendItemGrid;
            //
            // Name binding
            Binding titleBinding = new Binding("TitleProperty") { Source = this, Mode = BindingMode.OneWay };
            legendItem.SetBinding(LegendItem.TitleProperty, titleBinding);
            // Other bindings:
            BindToThis(line, false, true);
            BindToThis(lineD2D, false, true);
            BindToThis(legendLine, false, true);
            BindToThis(markers, true, false);
            BindToThis(markersD2D, true, false);
            BindToThis(legendMarker, true, false);
        }

        protected static void OnUseDirect2DChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Plot2DCurve plot2DCurveLocal = ((Plot2DCurve)obj);
            if (plot2DCurveLocal.host == null) return;
            plot2DCurveLocal.RemoveElements((bool)e.OldValue);
            plot2DCurveLocal.AddElements();
        }

        internal override void OnAxisTypeChanged()
        {
            curve.Transform(xAxis.GraphTransform, yAxis.GraphTransform);
            SetBounds();
        }

        internal override void BeforeArrange()
        {
            graphToCanvas.Matrix = new Matrix(xAxis.Scale, 0, 0, -yAxis.Scale, -xAxis.Offset - xAxis.AxisMargin.LowerMargin, yAxis.Offset + yAxis.AxisTotalLength - yAxis.AxisMargin.UpperMargin);
            canvasToGraph = (MatrixTransform)(graphToCanvas.Inverse); 
            Curve.FilterMinMax(canvasToGraph, new Rect(new Point(xAxis.Min, yAxis.Min), new Point(xAxis.Max, yAxis.Max)));
            if (host.UseDirect2D == true)
            {
                lineD2D.Geometry = curve.ToDirect2DPathGeometry(lineD2D.Factory, graphToCanvas);
                host.direct2DControl.RequestRender();
            }
            else
            {
                line.Data = Curve.ToPathGeometry(graphToCanvas);
            }
            //line.Data.Transform = graphToCanvas;
            //SetBounds();
            //visualLine.InvalidateVisual();
            UpdateMarkers();
        }

        private void SetBounds()
        {
            bounds = curve.Bounds();
        }

        public override Rect TightBounds
        {
            get
            {
                return TransformRect(bounds, xAxis.CanvasTransform, yAxis.CanvasTransform);
            }
        }

        public override Rect PaddedBounds
        {
            get 
            {  
                Rect paddedBounds =  new Rect(bounds.Left - 0.05 * bounds.Width, bounds.Top - 0.05 * bounds.Height, bounds.Width * 1.1, bounds.Height * 1.1);
                return TransformRect(paddedBounds, xAxis.CanvasTransform, yAxis.CanvasTransform); 
            }
        }

        private Rect TransformRect(Rect rect, Func<double, double> transformX, Func<double, double> transformY)
        {
            return new Rect(new Point(transformX(rect.Left), transformY(rect.Top)), new Point(transformX(rect.Right), transformY(rect.Bottom))); 
        }

        public PlotPath Line
        {
            get { return line; }
        }

        public PlotPath Markers
        {
            get { return markers; }
        }

        public Rect Bounds
        {
            get { return bounds; }
        }

        public Curve Curve
        {
            get { return curve; }
        }

        protected string GetLinePropertyFromStrokeProperties()
        {
            string lineProperty = "";
            switch ((QuickStrokeDash)GetValue(QuickStrokeDashProperty))
            {
                case QuickStrokeDash.Solid:
                    lineProperty = "-";
                    break;
                case QuickStrokeDash.Dash:
                    lineProperty = "--";
                    break;
                case QuickStrokeDash.Dot:
                    lineProperty = ":";
                    break;
                case QuickStrokeDash.DashDot:
                    lineProperty = "-.";
                    break;
                case QuickStrokeDash.None:
                    lineProperty = "";
                    break;
            }
            switch ((MarkersType)GetValue(MarkersTypeProperty))
            {
                case MarkersType.Square:
                    lineProperty += "s";
                    break;
                case MarkersType.Circle:
                    lineProperty += "o";
                    break;
                case MarkersType.Triangle:
                    lineProperty += "^";
                    break;
            }
            Brush brush = (Brush)GetValue(StrokeProperty);
            if (brush == Brushes.Red) lineProperty += "r";
            if (brush == Brushes.Green) lineProperty += "g";
            if (brush == Brushes.Blue) lineProperty += "b";
            if (brush == Brushes.Yellow) lineProperty += "y";
            if (brush == Brushes.Cyan) lineProperty += "c";
            if (brush == Brushes.Magenta) lineProperty += "m";
            if (brush == Brushes.Black) lineProperty += "k";
            if (brush == Brushes.White) lineProperty += "w";
            return lineProperty;
        }

        protected void SetStrokePropertiesFromLineProperty(string lineProperty)
        {
            if (lineProperty == "") lineProperty = "-";
            int currentIndex = 0;
            // First check for line type
            string firstTwo = null; string firstOne = null;
            if (lineProperty.Length >= 2) firstTwo = lineProperty.Substring(0, 2);
            if (lineProperty.Length >= 1) firstOne = lineProperty.Substring(0, 1);
            if (firstTwo == "--") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.Dash); currentIndex = 2; }
            else if (firstTwo == "-.") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.DashDot); currentIndex = 2; }
            else if (firstOne == ":") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.Dot); currentIndex = 1; }
            else if (firstOne == "-") { SetValue(QuickStrokeDashProperty, QuickStrokeDash.Solid); currentIndex = 1; }
            else SetValue(QuickStrokeDashProperty, QuickStrokeDash.None);
            // 
            // Next check for markers type
            string marker = null;
            if (lineProperty.Length >= currentIndex + 1) marker = lineProperty.Substring(currentIndex, 1);
            if (marker == "s") { SetValue(MarkersTypeProperty, MarkersType.Square); currentIndex++; }
            else if (marker == "o") { SetValue(MarkersTypeProperty, MarkersType.Circle); currentIndex++; }
            else if (marker == "^") { SetValue(MarkersTypeProperty, MarkersType.Triangle); currentIndex++; }
            else SetValue(MarkersTypeProperty, MarkersType.None);
            //
            // If no line and no marker, assume solid line
            if ((MarkersType == MarkersType.None) && (this.QuickStrokeDash == QuickStrokeDash.None))
            {
                QuickStrokeDash = QuickStrokeDash.Solid;
            }
            // Finally check for colour
            string colour = null;
            if (lineProperty.Length >= currentIndex + 1) colour = lineProperty.Substring(currentIndex, 1);
            if (colour == "r") SetValue(StrokeProperty, Brushes.Red);
            else if (colour == "g") SetValue(StrokeProperty, Brushes.Green);
            else if (colour == "b") SetValue(StrokeProperty, Brushes.Blue);
            else if (colour == "y") SetValue(StrokeProperty, Brushes.Yellow);
            else if (colour == "c") SetValue(StrokeProperty, Brushes.Cyan);
            else if (colour == "m") SetValue(StrokeProperty, Brushes.Magenta);
            else if (colour == "k") SetValue(StrokeProperty, Brushes.Black);
            else if (colour == "w") SetValue(StrokeProperty, Brushes.White);
            else SetValue(StrokeProperty, Brushes.Black);
        }

        protected void BindToThis(PlotPath target, bool includeFill, bool includeDotDash)
        {
            // Set Stroke property to apply to both the Line and Markers
            Binding strokeBinding = new Binding("StrokeProperty") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(PlotPath.StrokeProperty, strokeBinding);
            // Set StrokeThickness property also to apply to both the Line and Markers
            Binding strokeThicknessBinding = new Binding("StrokeThicknessProperty") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(PlotPath.StrokeThicknessProperty, strokeThicknessBinding);
            // Fill binding
            Binding fillBinding = new Binding("MarkersFillProperty") { Source = this, Mode = BindingMode.OneWay };
            if (includeFill) target.SetBinding(PlotPath.FillProperty, fillBinding);
            // Dot-dash of line
            if (includeDotDash)
            {
                Binding dashBinding = new Binding("QuickStrokeDashProperty") { Source = this, Mode = BindingMode.OneWay };
                target.SetBinding(PlotPath.QuickStrokeDashProperty, dashBinding);
            }     
        }

        protected void BindToThis(DirectPath target, bool includeFill, bool includeDotDash)
        {
            // Set Stroke property to apply to both the Line and Markers
            Binding strokeBinding = new Binding("StrokeProperty") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(DirectPath.StrokeProperty, strokeBinding);
            // Set StrokeThickness property also to apply to both the Line and Markers
            Binding strokeThicknessBinding = new Binding("StrokeThicknessProperty") { Source = this, Mode = BindingMode.OneWay };
            target.SetBinding(DirectPath.StrokeThicknessProperty, strokeThicknessBinding);
            // Fill binding
            Binding fillBinding = new Binding("MarkersFillProperty") { Source = this, Mode = BindingMode.OneWay };
            if (includeFill) target.SetBinding(DirectPath.FillProperty, fillBinding);
            // Dot-dash of line
            if (includeDotDash)
            {
                Binding dashBinding = new Binding("QuickStrokeDashProperty") { Source = this, Mode = BindingMode.OneWay };
                target.SetBinding(DirectPath.QuickStrokeDashProperty, dashBinding);
            }
        }

        internal void UpdateMarkers()
        {
            markers.Data = curve.MarkersAsGeometry(graphToCanvas, (MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
            markersD2D.SetGeometry((MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
            //markers.Data.Transform = graphToCanvas;
            legendMarker.Data = curve.LegendMarkerGeometry((MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
        }
    }

    public static class MarkerGeometries
    {
        public static Geometry RectangleMarker(double width, double height, Point centre)
        {
            return new RectangleGeometry(new Rect(centre.X - width / 2, centre.Y - height / 2, width, height));
        }

        public static Geometry EllipseMarker(double width, double height, Point centre)
        {
            return new EllipseGeometry(new Rect(centre.X - width / 2, centre.Y - height / 2, width, height));
        }

        public static Geometry TriangleMarker(double width, double height, Point centre)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(centre.X, centre.Y + height / 2), false /* is filled */, true /* is closed */);
                ctx.LineTo(new Point(centre.X + width / 2, centre.Y - height / 2), true /* is stroked */, false /* is smooth join */);
                ctx.LineTo(new Point(centre.X - width / 2, centre.Y - height / 2), true /* is stroked */, false /* is smooth join */);
            }
            geometry.Freeze();
            return geometry;
        }
    }
}
