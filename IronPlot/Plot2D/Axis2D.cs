// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Windows.Navigation;
using System.Windows.Data;
using System.Linq;
using System.Text;
using System.Windows.Documents;

namespace IronPlot
{
    /// <summary>
    /// Specifies the space at either end of an axis required to accommodate
    /// annotation. Max is the high end of the axis in Canvas coordinates.
    /// </summary>
    public struct AxisMargin
    {
        public double LowerMargin;
        public double UpperMargin;

        public double Total()
        {
            return LowerMargin + UpperMargin;
        }

        public AxisMargin(double lowerMargin, double upperMargin)
        {
            this.LowerMargin = lowerMargin;
            this.UpperMargin = upperMargin;
        }
    }

    public struct Transform1D
    {
        public double Scale;
        public double Offset;

        public Transform1D(double scale, double offset)
        {
            Scale = scale; Offset = offset;
        }

        public double Transform(double input) { return Scale * input - Offset; }

        public double InverseTransform(double input) { return (input + Offset) / Scale; }

        public Transform1D Inverse() { return new Transform1D(1 / Scale, -Offset / Scale); }
    }

    /// <summary>
    /// An Axis2D is an Axis that contains TextBlock annotation and the axis lines (a Shape).
    /// The ticks can appear both sides of the plot.
    /// </summary>
    public abstract class Axis2D : Axis
    {
        public static readonly DependencyProperty RangeProperty =
            DependencyProperty.Register("RangeProperty",
            typeof(Range), typeof(Axis2D),
            new PropertyMetadata(new Range(0, 10), OnRangeChanged));

        public override double Min
        {
            set {
                Range newRange = (Range)GetValue(RangeProperty);
                newRange.Min = value;
                SetValue(RangeProperty, newRange); 
            }
            get { return ((Range)GetValue(RangeProperty)).Min; }
        }

        public override double Max
        {
            set
            {
                Range newRange = (Range)GetValue(RangeProperty);
                newRange.Max = value;
                SetValue(RangeProperty, newRange);
            }
            get { return ((Range)GetValue(RangeProperty)).Max; }
        }

        internal List<TextBlock> tickLabels;

        protected Path axisLine = new Path() { Stroke = Brushes.Black };
        /// <summary>
        /// Path representing the axis line.
        /// </summary>
        public Path AxisLine { get { return axisLine; } }
        protected StreamGeometry axisLineGeometry = new StreamGeometry();

        protected Label axisLabel = new Label();
        public Label AxisLabel { get { return axisLabel; } }

        protected Path axisTicks = new Path() { Stroke = Brushes.Black };
        /// <summary>
        /// Path representing the axis ticks.
        /// </summary>
        public Path AxisTicks { get { return axisTicks; } }
        protected StreamGeometry axisTicksGeometry = new StreamGeometry();

        protected Rectangle interactionPad = new Rectangle();
        public Rectangle InteractionPad { get { return interactionPad; } }

        protected GridLines gridLines;
        public GridLines GridLines { get { return gridLines; } }

        // PlotPanel object to which the axis belongs.
        internal PlotPanel PlotPanel;

        // Whether this is one of the innermost axes, or an additional axis.
        internal bool IsInnermost = false;
        internal double AxisThickness = 0;
        internal AxisMargin AxisMargin;
        
        internal double Scale;
        internal double Offset;
        internal double AxisTotalLengthConstrained; // The axis length including labels allowed by constraints.
        // The actual AxisTotalLength may exceed this, possibly causing the axis to be clipped.
        internal double AxisTotalLength; // The axis length including labels.
        // canvasCoord = transformedGraphCoord * Scale - Offset
        // There is deliberately redundant information here: Offset may be inferred from Scale and startPoint.
        // Object that supplies the Max and Min of the Axis.

        // Assume that any change to the axis (number and length of ticks, change to labels) requires
        // another layout pass of the PlotPanel.
        // In addition, certain changes require re-derivation of ticks and labels:
        // change of tick number, change of max and min
        
        // Height of a one-line label
        protected double singleLineHeight;

        // The desired length of the axis; -1 indicates a lack of desire.
        internal double desiredLength = -1.0;

        protected static void OnRangeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            Axis2D axis2DLocal = ((Axis2D)obj);
            Range desiredRange = (Range)e.NewValue;
            if (Double.IsNegativeInfinity(desiredRange.Min) || Double.IsNaN(desiredRange.Min)
                || Double.IsPositiveInfinity(desiredRange.Max) || Double.IsNaN(desiredRange.Max))
            {
                axis2DLocal.SetValue(RangeProperty, e.OldValue);
            }
            if (axis2DLocal.AxisType == AxisType.Log)
            {
                if (desiredRange.Min <= 0 || desiredRange.Max <= 0)
                    axis2DLocal.SetValue(RangeProperty, e.OldValue); 
                    //axis2DLocal.SetValue(RangeProperty, new Range(Math.Max(desiredRange.Min, Double.Epsilon), 
                    //    Math.Max(desiredRange.Max, Double.Epsilon * 100)));  
            }
            axis2DLocal.DeriveTicks();
            if (axis2DLocal.PlotPanel != null) axis2DLocal.PlotPanel.InvalidateMeasure();
        }

        Binding axisBinding;
        /// <summary>
        /// Bind the Max and Min of this axis to another axis.
        /// </summary>
        /// <param name="bindingAxis"></param>
        public void BindToAxis(Axis2D bindingAxis)
        {
            axisBinding = new Binding("RangeProperty") { Source = this, Mode = BindingMode.TwoWay };
            bindingAxis.SetBinding(Axis2D.RangeProperty, axisBinding);
        }

        protected static void OnTicksPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis2D)obj).DeriveTicks();
            ((Axis2D)obj).UpdateTicksAndLabels();
        }

        protected override void OnAxisTypeChanged()
        {
            base.OnAxisTypeChanged();
            foreach (Plot2DItem item in PlotPanel.PlotItems)
            {
                if (item.XAxis == this || item.YAxis == this) item.OnAxisTypeChanged();
            }
        }

        protected override void UpdateTicksAndLabels()
        {
            if (PlotPanel != null) PlotPanel.InvalidateMeasure();
        }

        public Axis2D() : base()
        {
            MinTransformed = GraphTransform(Min); MaxTransformed = GraphTransform(Max);
            this.Background = null;
            axisLabel.Visibility = Visibility.Collapsed;
            tickLabels = new List<TextBlock>();
            gridLines = new GridLines(this);
            canvas.Children.Add(axisLine); axisLine.SetValue(Canvas.ZIndexProperty, 100);
            canvas.Children.Add(axisTicks); axisTicks.SetValue(Canvas.ZIndexProperty, 100);
            canvas.Children.Add(axisLabel); axisLabel.SetValue(Canvas.ZIndexProperty, 100);
            canvas.Children.Add(interactionPad); interactionPad.SetValue(Canvas.ZIndexProperty, 50);
            Brush padFill = new SolidColorBrush() { Color = Brushes.Aquamarine.Color, Opacity = 0.0 };
            interactionPad.Fill = padFill;
            axisLine.Data = axisLineGeometry;
            axisTicks.Data = axisTicksGeometry;
            DeriveTicks();
        }
        
        static Axis2D()
        {
            Axis2D.LabelsVisibleProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(true, Axis2D.OnLabelsVisibleChanged));
            Axis2D.TickLengthProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(5.0, OnTicksPropertyChanged));
            Axis2D.TicksVisibleProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(true, OnTicksPropertyChanged));
            Axis2D.NumberOfTicksProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(10, OnTicksPropertyChanged));
        }

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            System.Windows.Size size = base.MeasureOverride(constraint);
            // The parent of an Axis2D is always a PlotPanel. We need to trigger a Measure pass
            // in the parent.
            return size;
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size arrangeSize)
        {
            Size finalSize = base.ArrangeOverride(arrangeSize);
            return finalSize;
        }

        internal static void OnLabelsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            if ((bool)e.NewValue == false)
            {
                foreach (TextBlock textBlock in ((Axis2D)obj).tickLabels)
                {
                    textBlock.Visibility = Visibility.Collapsed;
                }
            }
            if ((bool)e.NewValue == true)
            {
                foreach (TextBlock textBlock in ((Axis2D)obj).tickLabels)
                {
                    textBlock.Visibility = Visibility.Visible;
                }
            }
        }

        internal virtual void PositionLabels(bool cullOverlapping)
        {
            // Do nothing in base Version: no labels.
        }

        internal void UpdateAndMeasureLabels()
        {
            // Make sure the labels are up to date
            TextBlock currentTextBlock;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                // Reuse the text blocks wherever possible (we do not want to keep adding and taking away TextBlocks
                // from the canvas)
                if (i > (tickLabels.Count - 1))
                {
                    currentTextBlock = new TextBlock();
                    if (LabelsVisible == false) currentTextBlock.Visibility = Visibility.Collapsed;
                    currentTextBlock.SetValue(Canvas.ZIndexProperty, 100);
                    canvas.Children.Add(currentTextBlock);
                    tickLabels.Add(currentTextBlock);
                }
                else currentTextBlock = tickLabels[i];
                if (i == 0)
                {
                    currentTextBlock.Text = "0123456789";
                    currentTextBlock.Measure(new Size(Double.PositiveInfinity, double.PositiveInfinity));
                    singleLineHeight = currentTextBlock.DesiredSize.Height;
                }
                AddTextToBlock(currentTextBlock, i);
                currentTextBlock.TextAlignment = TextAlignment.Center;
                currentTextBlock.Measure(new Size(Double.PositiveInfinity, double.PositiveInfinity));
            }
            axisLabel.Measure(new Size(Double.PositiveInfinity, double.PositiveInfinity));
        }

        // Calculate thickness of axis (size in direction penpendicular to axis vector).
        internal double CalculateAxisThickness()
        {
            double maxThickness = 0;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                if (tickLabels[i].Text != "") maxThickness = Math.Max(maxThickness, LimitingTickLabelSizeForThickness(i));
            }
            AxisThickness = maxThickness + Math.Max(TickLength, 0) + LimitingAxisLabelSizeForThickness();
            return AxisThickness;
        }

        internal virtual double LimitingTickLabelSizeForLength(int index)
        {
            return tickLabels[index].DesiredSize.Width;
        }

        protected virtual double LimitingTickLabelSizeForThickness(int index)
        {
            return tickLabels[index].DesiredSize.Height;
        }

        protected virtual double LimitingAxisLabelSizeForLength()
        {
            return axisLabel.DesiredSize.Width;
        }

        protected virtual double LimitingAxisLabelSizeForThickness()
        {
            return axisLabel.DesiredSize.Height;
        }

        internal void OverrideAxisScaling(double Scale, double Offset, AxisMargin AxisMargin)
        {
            this.Scale = Scale;
            this.Offset = Offset;
            this.AxisMargin = AxisMargin;
        }

        /// <summary>
        /// Keep margins the same, but reduce axis length.
        /// </summary>
        /// <param name="newScale"></param>
        internal void RescaleAxis(double newScale)
        {
            double axisLength = newScale * (MaxTransformed - MinTransformed);
            Scale = newScale;
            Offset = Scale * MinTransformed - AxisMargin.LowerMargin;
            AxisTotalLength = axisLength + AxisMargin.Total();
        }

        /// <summary>
        /// Change margins and scale, keeping total length constant.
        /// </summary>
        /// <param name="newScale"></param>
        internal void RescaleAxis(double newScale, AxisMargin newMargin)
        {
            AxisMargin = newMargin;
            Scale = newScale;
            Offset = Scale * MinTransformed - AxisMargin.LowerMargin;
        }

        /// <summary>
        /// Reset AxisMargin, keeping TotalLength the same.
        /// </summary>
        /// <param name="newScale"></param>
        internal void ResetAxisMargin(AxisMargin newMargin)
        {
            AxisMargin = newMargin;
            double axisLength = AxisTotalLength - newMargin.Total();
            Scale = axisLength / (MaxTransformed - MinTransformed);
            Offset = Scale * MinTransformed - AxisMargin.LowerMargin;
        }

        // The axis Scale has been reduced to make the axes equal.
        // The axis is reduced, keeping the axis minimum point in the same position.
        internal void ScaleAxis(double newScale, double maxCanvas)
        {
            double axisLength = newScale * (MaxTransformed - MinTransformed);
            Scale = newScale;
            Offset = Scale * MinTransformed - AxisMargin.LowerMargin;
            AxisTotalLength = axisLength + AxisMargin.LowerMargin + AxisMargin.UpperMargin;
        }

        internal virtual Point TickStartPosition(int i)
        {
            return new Point();
        }

        // Updates Offset from current Scale and min position.
        // This is for dragging interations where only Offset changes.
        internal void UpdateOffset()
        {
            Offset = Scale * this.MinTransformed - AxisMargin.LowerMargin;
        }

        internal virtual void RenderAxis()
        {
            // Derived classes should render axes. Also cause GridLines to be re-rendered.
            gridLines.InvalidateMeasure();
        }

        internal abstract Transform1D GraphToAxesCanvasTransform();
        internal abstract Transform1D GraphToCanvasTransform();

        internal abstract double GraphToCanvas(double canvas);
        internal abstract double CanvasToGraph(double graph);

        public static MatrixTransform GraphToCanvasLinear(XAxis xAxis, YAxis yAxis)
        {
            return new MatrixTransform(xAxis.Scale, 0, 0, -yAxis.Scale, -xAxis.Offset - xAxis.AxisMargin.LowerMargin, yAxis.Offset + yAxis.AxisTotalLength - yAxis.AxisMargin.UpperMargin);
        }
    }
}
