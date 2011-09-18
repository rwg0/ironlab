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
        public double MinMargin;
        public double MaxMargin;

        public AxisMargin(double minMargin, double maxMargin)
        {
            this.MinMargin = minMargin;
            this.MaxMargin = maxMargin;
        }
    }
    
    // An Axis2D objects (as well as an Axes2D) are bound to a particular PlotPanel.
    public class Axis2D : Axis
    {
        // Canvas object which contains annotation.
        protected Canvas canvas;
        //
        // PlotPanel object that contains canvas
        internal PlotPanel plotPanel;
        //
        internal AxisMargin axisMargin;
        internal double scale;
        internal double offset;
        internal double axisMax; // The upper canvas point corresponding to axis and labels combined.
        // Note there is no lower point since this is always 0
        // canvasCoord = graphCoord * scale - offset
        // There is deliberately redundant information here: offset may be inferred from scale and startPoint.
        // Object that supplies the Max and Min of the Axis.

        // Assume that any change to the axis (number and length of ticks, change to labels) requires
        // another layout pass of the PlotPanel.
        // In addition, certain changes require re-derivtion of ticks and labls:
        // Change of tick number, change of max and min
        
        // Height of a one-line label
        protected double singleLineHeight;

        // The desired length of the axis; -1 indicates a lack of desire.
        internal double desiredLength = -1.0;

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
            plotPanel.InvalidateMeasure();
        }

        public Axis2D(PlotPanel plotPanel) 
        {
            axisLabels = new List<TextBlock>();
            this.plotPanel = plotPanel;
            this.canvas = plotPanel.axesCanvas;
        }
        
        protected List<TextBlock> axisLabels;

        static Axis2D()
        {
            Axis2D.AxisTypeProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(Axis2D.OnAxisTypeChanged));
            Axis2D.LabelsVisibleProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(true, Axis2D.OnLabelsVisibleChanged));
            Axis2D.TickLengthProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(5.0, OnTicksPropertyChanged));
            Axis2D.TicksVisibleProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(true, OnTicksPropertyChanged));
            Axis2D.NumberOfTicksProperty.OverrideMetadata(typeof(Axis2D), new PropertyMetadata(10, OnTicksPropertyChanged));
        }

        internal static void OnLabelsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            if ((bool)e.NewValue == false)
            {
                foreach (TextBlock textBlock in ((Axis2D)obj).axisLabels)
                {
                    textBlock.Visibility = Visibility.Collapsed;
                }
                //((Axis2D)obj).axisLabel.Visibility = Visibility.Collapsed;
            }
            if ((bool)e.NewValue == true)
            {
                foreach (TextBlock textBlock in ((Axis2D)obj).axisLabels)
                {
                    textBlock.Visibility = Visibility.Visible;
                }
                //((Axis2D)obj).axisLabel.Visibility = Visibility.Visible;
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
                if (i > (axisLabels.Count - 1))
                {
                    currentTextBlock = new TextBlock();
                    if (LabelsVisible == false) currentTextBlock.Visibility = Visibility.Collapsed;
                    canvas.Children.Add(currentTextBlock);
                    axisLabels.Add(currentTextBlock);
                }
                else currentTextBlock = axisLabels[i];
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
        internal double AxisThickness()
        {
            double maxThickness = 0;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                if (axisLabels[i].Text != "") maxThickness = Math.Max(maxThickness, LimitingLabelDimensionForThickness(i));
            }
            return maxThickness + Math.Max(TickLength, 0);
        }

        protected virtual double LimitingLabelDimensionForLength(int index)
        {
            return axisLabels[index].DesiredSize.Width;
        }

        protected virtual double LimitingLabelDimensionForThickness(int index)
        {
            return axisLabels[index].DesiredSize.Height;
        }

        /// <summary>
        /// Updates scale and offset by reducing the axis length on the canvas if necessary
        /// to avoid labels extending beyond canvas.
        /// Takes account of any length override that might be present.
        /// </summary>
        /// <param name="maxCanvas">The length of the canvas along the axis.</param>
        /// <param name="axisMargin">The margin required at the low and high (in Canvas space) ends of the axis.</param>
        internal void SetAxisLengthFromLabels(double maxCanvas, AxisMargin axisMargin)
        {
            bool overridden = (desiredLength != -1);
            this.axisMargin = axisMargin;
            double minCanvas = 0;
            double max = this.Max;
            double min = this.Min;
            double range = max - min;
            if (range == 0) range = 1;
            double axisLength = maxCanvas - minCanvas - axisMargin.MinMargin - axisMargin.MaxMargin;
            if (axisLength < 1.0)
            {
                // We will need to grow axisCanvas.
                maxCanvas = maxCanvas + (1.0 - axisLength);
                axisLength = 1.0;
            }
            if (overridden) scale = desiredLength / range; 
                else scale = Math.Max(axisLength, 1.0) / range;
            offset = scale * min - (minCanvas + axisMargin.MinMargin); // 
            double limitingMinGraph = min;
            double limitingMaxGraph = max;
            double limitingMinThickness = axisMargin.MinMargin * 2;
            double limitingMaxThickness = axisMargin.MaxMargin * 2;
            bool newLimit = false;
            double partialAxisLength = 0;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                if (axisLabels[i].Text == "") continue;
                if ((scale * Ticks[i] - offset - LimitingLabelDimensionForLength(i) / 2) < (minCanvas - 0.1))
                {
                    limitingMinGraph = Ticks[i];
                    limitingMinThickness = LimitingLabelDimensionForLength(i);
                    newLimit = true;
                }
                if ((scale * Ticks[i] - offset + LimitingLabelDimensionForLength(i) / 2) > (maxCanvas + 0.1))
                {
                    limitingMaxGraph = Ticks[i];
                    limitingMaxThickness = LimitingLabelDimensionForLength(i);
                    newLimit = true;
                }
                if (newLimit)
                {
                    newLimit = false;
                    i = 0; // Go back to the beginning because changing the scale could cause
                    // another label to be the limiting label.
                    double availableLimitDistance = (maxCanvas - minCanvas - (limitingMinThickness + limitingMaxThickness) / 2);
                    double currentLimitDistance = scale * (limitingMaxGraph - limitingMinGraph);
                    if (!(overridden && (availableLimitDistance < currentLimitDistance)))
                    {
                        // can not retain scale
                        partialAxisLength = maxCanvas - minCanvas - (limitingMinThickness + limitingMaxThickness) / 2;
                        double limitLength = limitingMaxGraph - limitingMinGraph;
                        if (limitLength <= 0) limitLength = range;
                        if (partialAxisLength < 1.0)
                        {
                            maxCanvas = maxCanvas + (1.0 - partialAxisLength);
                            partialAxisLength = 1.0;
                        }
                        scale = partialAxisLength / limitLength;
                    }
                    offset = scale * limitingMinGraph - (minCanvas + limitingMinThickness / 2);
                    //if (partialAxisLength == 1.0) break;
                }
            }
            this.axisMax = scale * limitingMaxGraph - offset + limitingMaxThickness / 2;
            this.axisMargin = new AxisMargin(scale * min - offset - minCanvas, axisMax - scale * max + offset);
        }

        internal void OverrideAxisScaling(double scale, double offset, AxisMargin axisMargin)
        {
            this.scale = scale;
            this.offset = offset;
            this.axisMargin = axisMargin;
        }

        // The axis scale has been reduced to make the axes equal.
        // The axis is reduced, keeping the axis minimum point in the same position.
        internal void ScaleAxis(double newScale, double maxCanvas)
        {
            double axisLength = newScale * (Max - Min);
            scale = newScale;
            offset = scale * Min - axisMargin.MinMargin;
            axisMax = axisLength + axisMargin.MinMargin + axisMargin.MaxMargin;
        }

        internal virtual Point TickStartPosition(int i)
        {
            return new Point();
        }

        // Updates offset from current scale and min position.
        // This is for dragging interations where only offset changes.
        internal void UpdateOffset()
        {
            offset = scale * this.Min - axisMargin.MinMargin;
        }
    }

    public enum XAxisPosition { Top, Bottom };

    public class XAxis : Axis2D
    {
        internal double yPosition = 0;

        internal static DependencyProperty XAxisPositionProperty =
            DependencyProperty.Register("XAxisPositionProperty",
            typeof(XAxisPosition), typeof(XAxis), new PropertyMetadata(XAxisPosition.Bottom));

        public XAxis(PlotPanel plotPanel) : base(plotPanel) { }
        
        public override double Min
        {
            set
            {
                plotPanel.XMin = value;
            }
            get { return plotPanel.XMin; }
        }

        public override double Max
        {
            set
            {
                plotPanel.XMax = value;
            }
            get { return plotPanel.XMax; }
        }

        internal override void PositionLabels(bool cullOverlapping)
        {
            TextBlock currentTextBlock;
            int missOut = 0, missOutMax = 0;
            double currentRight, lastRight = Double.NegativeInfinity;
            // Go through ticks in order of increasing Canvas coordinate.
            for (int i = 0; i < Ticks.Length; ++i)
            {
                // Miss out labels if these would overlap.
                currentTextBlock = axisLabels[i];
                currentRight = scale * Ticks[i] - offset + currentTextBlock.DesiredSize.Width / 2.0;
                currentTextBlock.SetValue(Canvas.LeftProperty, currentRight - currentTextBlock.DesiredSize.Width);
                if ((XAxisPosition)GetValue(XAxisPositionProperty) == XAxisPosition.Bottom)
                {
                    currentTextBlock.SetValue(Canvas.TopProperty, yPosition + Math.Max(TickLength, 0.0));
                }
                else
                {
                    currentTextBlock.SetValue(Canvas.TopProperty, yPosition - Math.Max(TickLength, 0.0) - currentTextBlock.DesiredSize.Height);
                }
                if ((currentRight - currentTextBlock.DesiredSize.Width * 1.25) < lastRight)
                {
                    ++missOut;
                }
                else
                {
                    lastRight = currentRight;
                    missOutMax = Math.Max(missOut, missOutMax);
                    missOut = 0;
                }
            }
            missOutMax = Math.Max(missOutMax, missOut);
            missOut = 0;
            if (cullOverlapping)
            {
                for (int i = 0; i < Ticks.Length; ++i)
                {
                    if ((missOut < missOutMax) && (i > 0))
                    {
                        missOut += 1;
                        axisLabels[i].Text = "";
                    }
                    else missOut = 0;
                }
            }
            // Cycle through any now redundant TextBlocks and make invisible.
            for (int i = Ticks.Length; i < axisLabels.Count; ++i)
            {
                axisLabels[i].Text = "";
            }
        }

        protected override double LimitingLabelDimensionForLength(int index)
        {
            return axisLabels[index].DesiredSize.Width;
        }

        protected override double LimitingLabelDimensionForThickness(int index)
        {
            return axisLabels[index].DesiredSize.Height;
        }

        internal override Point TickStartPosition(int i)
        {
            return new Point(Ticks[i] * scale - offset, yPosition);
        }
    }

    public enum YAxisPosition { Left, Right };

    public class YAxis : Axis2D
    {
        internal double xPosition = 0;

        public static readonly DependencyProperty YAxisPositionProperty =
            DependencyProperty.Register("YAxisPositionProperty",
            typeof(YAxisPosition), typeof(YAxis),
            new PropertyMetadata(YAxisPosition.Left));

        public YAxis(PlotPanel plotPanel) : base(plotPanel) { }
        
        internal override void PositionLabels(bool cullOverlapping)
        {
            TextBlock currentTextBlock;
            int missOut = 0, missOutMax = 0;
            double currentTop, lastTop = Double.PositiveInfinity;
            double verticalOffset;
            // Go through ticks in order of decreasing Canvas coordinate
            for (int i = 0; i < Ticks.Length; ++i)
            {
                // Miss out labels if these would overlap.
                currentTextBlock = axisLabels[i];
                verticalOffset = currentTextBlock.DesiredSize.Height - singleLineHeight / 2; 
                currentTop = axisMax - (scale * Ticks[i] - offset) - verticalOffset;
                currentTextBlock.SetValue(Canvas.TopProperty, currentTop);

                if ((YAxisPosition)GetValue(YAxisPositionProperty) == YAxisPosition.Left)
                {     
                    currentTextBlock.TextAlignment = TextAlignment.Right;
                    currentTextBlock.SetValue(Canvas.LeftProperty, xPosition - currentTextBlock.DesiredSize.Width - Math.Max(TickLength, 0.0) - 3);
                }
                else
                {
                    currentTextBlock.TextAlignment = TextAlignment.Left;
                    currentTextBlock.SetValue(Canvas.LeftProperty, xPosition + Math.Max(TickLength, 0.0) + 3);
                }
                
                if ((currentTop + currentTextBlock.DesiredSize.Height) > lastTop)
                {
                    ++missOut;
                }
                else
                {
                    lastTop = currentTop;
                    missOutMax = Math.Max(missOut, missOutMax);
                    missOut = 0;
                }
            }
            missOutMax = Math.Max(missOutMax, missOut);
            missOut = 0;
            if (cullOverlapping)
            {
                for (int i = 0; i < Ticks.Length; ++i)
                {
                    if ((missOut < missOutMax) && (i > 0))
                    {
                        missOut += 1;
                        axisLabels[i].Text = "";
                    }
                    else missOut = 0;
                }
            }
            // Cycle through any now redundant TextBlocks and make invisible.
            for (int i = Ticks.Length; i < axisLabels.Count; ++i)
            {
                axisLabels[i].Text = "";
            }
        }
        
        protected override double LimitingLabelDimensionForLength(int index)
        {
            return axisLabels[index].DesiredSize.Height;
        }

        protected override double LimitingLabelDimensionForThickness(int index)
        {
            return axisLabels[index].DesiredSize.Width + 3.0;
        }
        
        public override double Min
        {
            set
            {
                plotPanel.YMin = value;
            }
            get { return plotPanel.YMin; }
        }

        public override double Max
        {
            set
            {
                plotPanel.YMax = value;
            }
            get { return plotPanel.YMax; }
        }

        internal override Point TickStartPosition(int i)
        {
            return new Point(xPosition, axisMax - Ticks[i] * scale + offset);
        }
    }
}
