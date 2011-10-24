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

        protected Path axisLine = new Path() { Stroke = Brushes.Black };
        /// <summary>
        /// Path representing the axis line.
        /// </summary>
        public Path AxisLine { get { return axisLine; } }

        // PlotPanel object
        internal PlotPanel PlotPanel;

        internal List<TextBlock> tickLabels;
        /// <summary>
        /// List of the axis labels.
        /// </summary>
        public List<TextBlock> TickLabels { get { return tickLabels; } }

        protected TextBlock axisLabel = new TextBlock();
        public TextBlock AxisLabel { get { return axisLabel; } }
     
        protected StreamGeometry axisLineGeometry = new StreamGeometry();

        internal double AxisThickness = 0;

        protected Path axisTicks = new Path() { Stroke = Brushes.Black };
        protected StreamGeometry axisTicksGeometry = new StreamGeometry();
        /// <summary>
        /// Path representing the axis ticks.
        /// </summary>
        public Path AxisTicks { get { return axisTicks; } }

        // Whether this is one of the innermost axes, or an additional axis.
        internal bool IsInnermost = true; 

        internal AxisMargin AxisMargin;

        // The transform applied to graph coordinates before conversion to canvas coordinates.
        internal Func<double, double> GraphTransform = value => value;

        // The transform applied to canvas coordinates after conversion to graph coordinates, as final step.
        internal Func<double, double> CanvasTransform = value => value;
        
        internal double Scale;
        internal double Offset;
        internal double AxisTotalLengthConstrained; // The axis length including labels allowed by constraints.
        // The actual AxisTotalLength may exceed this, possibly causing the axis to be clipped.
        internal double AxisTotalLength; // The axis length including labels.
        // canvasCoord = transformedGraphCoord * Scale - Offset
        // There is deliberately redundant information here: Offset may be inferred from Scale and startPoint.
        // Object that supplies the Max and Min of the Axis.

        // transformedGraphCoord is the graph coordinate with any transform applied. This is a 
        // log10 transform for log axes and a multiplication be -1 for reversed axes.

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
            axis2DLocal.DeriveTicks();
            if (axis2DLocal.PlotPanel != null) axis2DLocal.PlotPanel.InvalidateMeasure();
        }

        internal static void OnAxisTypeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis2D)obj).UpdateTicksAndLabels();
        }

        protected static void OnTicksPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis2D)obj).DeriveTicks();
            ((Axis2D)obj).UpdateTicksAndLabels();
        }

        protected virtual void UpdateTicksAndLabels()
        {
            if (PlotPanel != null) PlotPanel.InvalidateMeasure();
        }

        public Axis2D() : base()
        {
            this.Background = null;
            axisLabel.Visibility = Visibility.Collapsed;
            tickLabels = new List<TextBlock>();
            canvas.Children.Add(axisLine);
            canvas.Children.Add(axisTicks);
            canvas.Children.Add(axisLabel);
            axisLine.Data = axisLineGeometry;
            axisTicks.Data = axisTicksGeometry;
            DeriveTicks();
        }
        
        static Axis2D()
        {
            Axis2D.AxisTypeProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(Axis2D.OnAxisTypeChanged));
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

        /// <summary>
        /// Expands the AxisMargin if required, keeping AxisTotalLength constant, in order to fit 
        /// labels in.
        /// Also updates Scale and Offset.
        /// </summary>
        internal void ExpandMarginsAsRequired()
        {
            double max = this.Max;
            double min = this.Min;
            double range = max - min;
            if (range == 0) range = 1;
            double axisLength = AxisTotalLength - AxisMargin.Total();
            if (axisLength < 1.0)
            {
                // We will need to grow axisCanvas.
                AxisTotalLength = AxisTotalLength + (1.0 - axisLength);
                axisLength = 1.0;
            }
            else Scale = Math.Max(axisLength, 1.0) / range;
            Offset = Scale * min - (AxisMargin.LowerMargin); // 
            double limitingMinGraph = min;
            double limitingMaxGraph = max;
            double limitingMinThickness = AxisMargin.LowerMargin * 2;
            double limitingMaxThickness = AxisMargin.UpperMargin * 2;
            bool newLimit = false;
            double partialAxisLength = 0;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                if (tickLabels[i].Text == "") continue;
                if ((Scale * Ticks[i] - Offset - LimitingTickLabelSizeForLength(i) / 2) < - 0.1)
                {
                    limitingMinGraph = Ticks[i];
                    limitingMinThickness = LimitingTickLabelSizeForLength(i);
                    newLimit = true;
                }
                if ((Scale * Ticks[i] - Offset + LimitingTickLabelSizeForLength(i) / 2) > (AxisTotalLength + 0.1))
                {
                    limitingMaxGraph = Ticks[i];
                    limitingMaxThickness = LimitingTickLabelSizeForLength(i);
                    newLimit = true;
                }
                if (newLimit)
                {
                    newLimit = false;
                    i = 0; // Go back to the beginning because changing the Scale could cause
                    // another label to be the limiting label.
                    double availableLimitDistance = (AxisTotalLength - (limitingMinThickness + limitingMaxThickness) / 2);
                    double currentLimitDistance = Scale * (limitingMaxGraph - limitingMinGraph);
                    if (availableLimitDistance < currentLimitDistance)
                    {
                        // can not retain Scale
                        partialAxisLength = AxisTotalLength - (limitingMinThickness + limitingMaxThickness) / 2;
                        double limitLength = limitingMaxGraph - limitingMinGraph;
                        if (limitLength <= 0) limitLength = range;
                        if (partialAxisLength < 1.0)
                        {
                            AxisTotalLength = AxisTotalLength + (1.0 - partialAxisLength);
                            partialAxisLength = 1.0;
                        }
                        Scale = partialAxisLength / limitLength;
                    }
                    Offset = Scale * limitingMinGraph - (limitingMinThickness / 2);
                }
            }
            this.AxisTotalLength = Scale * limitingMaxGraph - Offset + limitingMaxThickness / 2;
            this.AxisMargin = new AxisMargin(Scale * min - Offset, AxisTotalLength - Scale * max + Offset);
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
            double axisLength = newScale * (Max - Min);
            Scale = newScale;
            Offset = Scale * Min - AxisMargin.LowerMargin;
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
            Offset = Scale * Min - AxisMargin.LowerMargin;
        }

        /// <summary>
        /// Reset AxisMargin, keeping TotalLength the same.
        /// </summary>
        /// <param name="newScale"></param>
        internal void ResetAxisMargin(AxisMargin newMargin)
        {
            AxisMargin = newMargin;
            double axisLength = AxisTotalLength - newMargin.Total();
            Scale = axisLength / (Max - Min);
            Offset = Scale * Min - AxisMargin.LowerMargin;
        }

        // The axis Scale has been reduced to make the axes equal.
        // The axis is reduced, keeping the axis minimum point in the same position.
        internal void ScaleAxis(double newScale, double maxCanvas)
        {
            double axisLength = newScale * (Max - Min);
            Scale = newScale;
            Offset = Scale * Min - AxisMargin.LowerMargin;
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
            Offset = Scale * this.Min - AxisMargin.LowerMargin;
        }

        internal virtual void RenderAxis()
        {
            // Do nothing at this level.
        }

        internal abstract Transform1D GraphToAxesCanvasTransform();
        internal abstract Transform1D GraphToCanvasTransform();

        internal abstract double GraphToCanvas(double canvas);
        internal abstract double CanvasToGraph(double graph);
    }
}
