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
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions; 
using System.Windows.Xps;
//using System.Windows.Xps.Packaging;
//using System.Windows.Xps.Serialization;
using System.Printing;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace ILab.Plot
{
    public enum BarType { Horizontal, Vertical }
    
    public class Bars : UIElement, IBoundable
    {
        protected Plot2D plot2D;

        protected List<Path> rectangles;
        
        public List<Path> Paths
        {
            get { return rectangles; }
        }

        #region DependencyProperties

        public static readonly DependencyProperty BoundsProperty =
            DependencyProperty.Register("BoundsProperty",
            typeof(Rect), typeof(Bars),
            new PropertyMetadata(new Rect(0, 0, 10, 10)));

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register("FillProperty",
            typeof(Brush), typeof(Bars),
            new PropertyMetadata(Brushes.SlateBlue));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("StrokeProperty",
            typeof(Brush), typeof(Bars),
            new PropertyMetadata(Brushes.Black));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThicknessProperty",
            typeof(double), typeof(Bars),
            new PropertyMetadata(1.0));

        public Brush Fill
        {
            set
            {
                SetValue(FillProperty, value);
            }
            get { return (Brush)GetValue(FillProperty); }
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

        public Rect Bounds
        {
            set
            {
                SetValue(BoundsProperty, (Rect)value);
            }
            get { return (Rect)GetValue(BoundsProperty); }
        }

        #endregion

        public Bars(Plot2D plot2D, ILArray<double> barStart, ILArray<double> barEnd, ILArray<double> barPosition, ILArray<double> barThickness, BarType barType)
        {
            this.plot2D = plot2D;
            int n = barStart.Length;
            rectangles = new List<Path>();
            Geometry geometry;
            Path rectangle;
            Rect bounds = new Rect();
            Rect rectangleBounds = new Rect();
            for (int i = 0; i < n; ++i)
            {
                rectangle = new Path();
                rectangles.Add(rectangle);
                if (barType == BarType.Horizontal)
                {
                    rectangleBounds = new Rect(new Point(barStart.GetValue(i), barPosition.GetValue(i) + barThickness.GetValue(i) / 2),
                        new Point(barEnd.GetValue(i), barPosition.GetValue(i) - barThickness.GetValue(i) / 2));
                }
                else
                {
                    rectangleBounds = new Rect(new Point(barPosition.GetValue(i) + barThickness.GetValue(i) / 2, barStart.GetValue(i)),
                        new Point(barPosition.GetValue(i) - barThickness.GetValue(i) / 2, barEnd.GetValue(i)));
                }
                geometry = new RectangleGeometry(rectangleBounds);
                rectangle.Data = geometry;
                geometry.Transform = plot2D.GraphToCanvas;
                rectangle.Fill = (Brush)(this.GetValue(FillProperty));
                rectangle.StrokeThickness = (double)(this.GetValue(StrokeThicknessProperty));
                rectangle.Stroke = (Brush)(this.GetValue(StrokeProperty));
                if (i == 0) bounds = rectangleBounds;
                else
                {
                    bounds.Union(rectangleBounds);
                }
            }
            Bounds = bounds;
            SetBindings();
        }

        protected void SetBindings()
        {
            Binding fillBinding = new Binding("FillProperty");
            Binding strokeBinding = new Binding("StrokeProperty");
            Binding strokeThicknessBinding = new Binding("StrokeThicknessProperty");
            fillBinding.Source = this;
            strokeBinding.Source = this;
            strokeThicknessBinding.Source = this;
            fillBinding.Mode = BindingMode.OneWay;
            strokeBinding.Mode = BindingMode.OneWay;
            strokeThicknessBinding.Mode = BindingMode.OneWay;
            foreach (Path rectangle in rectangles)
            {
                rectangle.SetBinding(Rectangle.FillProperty, fillBinding);
                rectangle.SetBinding(Rectangle.StrokeProperty, strokeBinding);
                rectangle.SetBinding(Rectangle.StrokeThicknessProperty, strokeThicknessBinding);
            }
        }
    }

    public partial class Plot2D : ContentControl
    {
        protected List<Bars> barsList = new List<Bars>();

        public List<Bars> BarsList
        {
            get { return barsList; }
        }

        public Bars AddBars(ILArray<double> barStart, ILArray<double> barEnd, ILArray<double> barPosition, ILArray<double> barThickness, BarType barType)
        {
            int n = barStart.Dimensions[0];
            if (barThickness.IsScalar) barThickness = ILMath.ones(n) * barThickness;
            Bars bars = new Bars(this, barStart, barEnd, barPosition, barThickness, barType);
            barsList.Add(bars);
            foreach (Path rectangle in bars.Paths)
            {
                canvas.Children.Add(rectangle);
                IBoundableFromChild.Add((object)(rectangle), bars);
            }
            if (graphToCanvas.Matrix.M11 != 0) ViewedRegion = GetCanvasChildrenBounds();
            else ViewedRegion = bars.Bounds;
            return bars;
        }

        public Bars AddBars(ILArray<double> barLength)
        {
            if (!barLength.IsVector && !barLength.IsScalar) { throw new ILArgumentException("Argument must be a vector or scalar"); }
            int n = barLength.Dimensions[0];
            return AddBars(ILMath.zeros(n), barLength, ILMath.counter(n) - 1.0, 0.8, BarType.Vertical);
        }
    }
}
