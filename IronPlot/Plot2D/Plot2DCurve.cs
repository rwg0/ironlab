// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace IronPlot
{   
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
        //private MarkersVisualHost markersVisual = new MarkersVisualHost();
        private LegendItem legendItem;

        // An annotation marker for displaying coordinates of a position.
        PlotPointAnnotation annotation; 
        
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
            new PropertyMetadata(String.Empty));

        public static readonly DependencyProperty AnnotationPositionProperty =
            DependencyProperty.Register("AnnotationPositionProperty",
            typeof(Point), typeof(Plot2DCurve),
            new PropertyMetadata(new Point(Double.NaN, Double.NaN),
                OnAnnotationPositionChanged));

        public static readonly DependencyProperty AnnotationEnabledProperty =
            DependencyProperty.Register("AnnotationEnabledProperty",
            typeof(bool), typeof(Plot2DCurve),
            new PropertyMetadata(true,
                OnAnnotationEnabledChanged));

        public static readonly DependencyProperty UseDirect2DProperty =
            DependencyProperty.Register("UseDirect2DProperty",
            typeof(bool), typeof(Plot2DCurve),
            new PropertyMetadata(false, OnUseDirect2DChanged));

        /// <summary>
        /// Desired annotation mapping goes here.
        /// </summary>
        public Func<Point, string> AnnotationFromPoint = (point => point.ToString());
        //public Func<Point, string> AnnotationFromPoint = (point => String.Format("{0:F},{0:F}", point.X, point.Y));

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

        public Point AnnotationPosition
        {
            set
            {
                SetValue(AnnotationPositionProperty, value);
            }
            get { return (Point)GetValue(AnnotationPositionProperty); }
        }

        public bool AnnotationEnabled
        {
            set
            {
                SetValue(AnnotationEnabledProperty, value);
            }
            get { return (bool)GetValue(AnnotationEnabledProperty); }
        }

        protected static void OnMarkersChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Plot2DCurve)obj).UpdateLegendMarkers();
            if (((Plot2DCurve)obj).Plot != null)
                ((Plot2DCurve)obj).Plot.PlotPanel.InvalidateArrange();
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

        protected static void OnAnnotationPositionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Point canvasPosition = (Point)e.NewValue;
            Plot2DCurve localCurve = (Plot2DCurve)obj;
            if (Double.IsNaN(canvasPosition.X) || !localCurve.AnnotationEnabled)
            {
                localCurve.annotation.Visibility = Visibility.Collapsed;
                return;
            }
            else localCurve.annotation.Visibility = Visibility.Visible;
            int index;
            Point curveCanvas = localCurve.SnappedCanvasPoint(canvasPosition, out index);
            localCurve.annotation.Annotation = localCurve.AnnotationFromPoint(new Point(localCurve.curve.x[index], localCurve.curve.y[index]));
            localCurve.annotation.SetValue(Canvas.LeftProperty, curveCanvas.X);
            localCurve.annotation.SetValue(Canvas.TopProperty, curveCanvas.Y);
            localCurve.annotation.InvalidateVisual();
        }

        protected static void OnAnnotationEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Plot2DCurve localCurve = (Plot2DCurve)obj;
            if ((bool)e.NewValue == false) localCurve.annotation.Visibility = Visibility.Collapsed; 
        }

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
            }
            SetBounds();
        }
        Binding bindingDirect2D;

        private void AddElements()
        {
            if ((bool)GetValue(UseDirect2DProperty) == false)
            {   
                line.Data = LineGeometries.PathGeometryFromCurve(curve, null);
                line.SetValue(Canvas.ZIndexProperty, 200);
                line.Data.Transform = graphToCanvas;
                markers.SetValue(Canvas.ZIndexProperty, 200);
                host.Canvas.Children.Add(line);
                host.Canvas.Children.Add(markers);
            }
            else
            {
                host.direct2DControl.AddPath(lineD2D);
                host.direct2DControl.AddPath(markersD2D);
                markersD2D.GraphToCanvas = graphToCanvas;
            }
            Plot.Legend.Items.Add(legendItem);
            annotation.SetValue(Canvas.ZIndexProperty, 201);
            annotation.Visibility = Visibility.Collapsed;
            host.Canvas.Children.Add(annotation);
            //
            UpdateLegendMarkers();
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
            Plot.Legend.Items.Remove(legendItem);
            host.Canvas.Children.Remove(annotation);
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
            annotation = new PlotPointAnnotation();
            //
            legendItem = CreateLegendItem();
            //
            // Name binding
            Binding titleBinding = new Binding("TitleProperty") { Source = this, Mode = BindingMode.OneWay };
            legendItem.SetBinding(LegendItem.TitleProperty, titleBinding);
            // Other bindings:
            BindToThis(line, false, true);
            BindToThis(legendLine, false, true);
            BindToThis(markers, true, false);
            BindToThis(legendMarker, true, false);
        }

        protected virtual LegendItem CreateLegendItem()
        {
            LegendItem legendItem = new LegendItem();
            Grid legendItemGrid = new Grid();
            legendLine = new PlotPath();
            legendMarker = new PlotPath();
            legendMarker.HorizontalAlignment = HorizontalAlignment.Center; legendMarker.VerticalAlignment = VerticalAlignment.Center;
            legendLine.HorizontalAlignment = HorizontalAlignment.Center; legendLine.VerticalAlignment = VerticalAlignment.Center;
            legendLine.Data = new LineGeometry(new Point(0, 0), new Point(30, 0));
            legendItemGrid.Children.Add(legendLine);
            legendItemGrid.Children.Add(legendMarker);
            legendItem.Content = legendItemGrid;
            return legendItem;
        }

        protected static void OnUseDirect2DChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Plot2DCurve plot2DCurveLocal = ((Plot2DCurve)obj);
            if ((bool)e.NewValue == true && plot2DCurveLocal.lineD2D == null)
            {
                plot2DCurveLocal.lineD2D = new DirectPath();
                plot2DCurveLocal.markersD2D = new DirectPathScatter() { Curve = plot2DCurveLocal.curve };
                plot2DCurveLocal.BindToThis(plot2DCurveLocal.lineD2D, false, true);
                plot2DCurveLocal.BindToThis(plot2DCurveLocal.markersD2D, true, false);
            }
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
            graphToCanvas.Matrix = new Matrix(xAxis.Scale, 0, 0, -yAxis.Scale, -xAxis.Offset - xAxis.AxisPadding.Lower, yAxis.Offset + yAxis.AxisTotalLength - yAxis.AxisPadding.Upper);
            canvasToGraph = (MatrixTransform)(graphToCanvas.Inverse); 
            Curve.FilterMinMax(canvasToGraph, new Rect(new Point(xAxis.Min, yAxis.Min), new Point(xAxis.Max, yAxis.Max)));
            if (host.UseDirect2D == true)
            {
                lineD2D.Geometry = curve.ToDirect2DPathGeometry(lineD2D.Factory, graphToCanvas);
                markersD2D.SetGeometry((MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
                //host.direct2DControl.RequestRender();
            }
            else
            {
                line.Data = LineGeometries.PathGeometryFromCurve(curve, graphToCanvas);
                markers.Data = MarkerGeometries.MarkersAsGeometry(Curve, graphToCanvas, (MarkersType)GetValue(MarkersTypeProperty), (double)GetValue(MarkersSizeProperty));
            }
            Point annotationPoint = graphToCanvas.Transform(new Point(curve.xTransformed[0], curve.yTransformed[0]));
            annotation.SetValue(Canvas.TopProperty, annotationPoint.Y); annotation.SetValue(Canvas.LeftProperty, annotationPoint.X);
        }

        internal Point SnappedCanvasPoint(Point canvasPoint, out int index)
        {
            index = CurveIndexFromCanvasPoint(canvasPoint);
            return graphToCanvas.Transform(new Point(curve.xTransformed[index], curve.yTransformed[index]));
        }

        internal int CurveIndexFromCanvasPoint(Point canvasPoint)
        {
            Point graphPoint = canvasToGraph.Transform(canvasPoint);
            double value = curve.SortedValues == SortedValues.X ? graphPoint.X : graphPoint.Y;
            int index = Curve.GetInterpolatedIndex(curve.TransformedSorted, value);
            if (index == (curve.xTransformed.Length - 1)) return curve.SortedToUnsorted[curve.xTransformed.Length - 1]; 
            // otherwise return nearest:
            if ((curve.TransformedSorted[index + 1] - value) < (value - curve.TransformedSorted[index]))
                return curve.SortedToUnsorted[index + 1];
            else return curve.SortedToUnsorted[index];
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
                case MarkersType.TrianglePointUp:
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
            else if (marker == "^") { SetValue(MarkersTypeProperty, MarkersType.TrianglePointUp); currentIndex++; }
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

        internal void UpdateLegendMarkers()
        {
            double markersSize = (double)GetValue(MarkersSizeProperty);
            legendMarker.Data = MarkerGeometries.LegendMarkerGeometry((MarkersType)GetValue(MarkersTypeProperty), markersSize);
            if (legendMarker.Data != null) legendMarker.Data.Transform = new TranslateTransform(markersSize / 2, markersSize / 2);
        }
    }
}
