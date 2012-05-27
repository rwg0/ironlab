// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace IronPlot
{
    public class AxisPair
    {
        public XAxis XAxis;
        public YAxis YAxis;

        public AxisPair(XAxis xAxis, YAxis yAxis)
        {
            XAxis = xAxis; YAxis = yAxis;
        }
    }

    public class Axes2D : DependencyObject
    {
        public static readonly DependencyProperty AxisSpacingProperty =
            DependencyProperty.Register("AxisSpacing",
            typeof(double), typeof(Axes2D),
            new PropertyMetadata((double)5, UpdatePanel));

        public static readonly DependencyProperty MinAxisMarginProperty =
           DependencyProperty.Register("MinAxisMargin",
           typeof(Thickness), typeof(Axes2D),
           new PropertyMetadata(new Thickness(0), UpdatePanel));

        public static readonly DependencyProperty WidthProperty =
           DependencyProperty.Register("Width",
           typeof(double), typeof(Axes2D),
           new FrameworkPropertyMetadata(Double.NaN, UpdatePanel));

        public static readonly DependencyProperty HeightProperty =
           DependencyProperty.Register("Height",
           typeof(double), typeof(Axes2D),
           new PropertyMetadata(Double.NaN, UpdatePanel));

        public static readonly DependencyProperty EqualAxesProperty =
            DependencyProperty.Register("EqualAxes",
            typeof(AxisPair), typeof(Axes2D),
            new PropertyMetadata(null, UpdatePanel));

        public double AxisSpacing
        {
            set { SetValue(AxisSpacingProperty, value); }
            get { return (double)GetValue(AxisSpacingProperty); }
        }

        /// <summary>
        /// The minimum Thickness of the region in which axes are rendered. 
        /// </summary>
        public Thickness MinAxisMargin
        {
            set { SetValue(MinAxisMarginProperty, value); }
            get { return (Thickness)GetValue(MinAxisMarginProperty); }
        }

        /// <summary>
        /// The two axes (if any) which should be made to have an equal scale.
        /// </summary>
        public AxisPair EqualAxes
        {
            set { SetValue(EqualAxesProperty, value); }
            get { return (AxisPair)GetValue(EqualAxesProperty); }
        }

        public double Width
        {
            set { SetValue(WidthProperty, value); }
            get { return (double)GetValue(WidthProperty); }
        }
        
        public double Height
        {
            set { SetValue(HeightProperty, value); }
            get { return (double)GetValue(HeightProperty); }
        }

        protected static void UpdatePanel(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axes2D)obj).plotPanel.InvalidateMeasure();
        }

        /// <summary>
        /// Set the innermost bottom X Axis and innermost left Y Axis to have equal scales.
        /// </summary>
        public void SetAxesEqual()
        {
            EqualAxes = new AxisPair(XAxes.Bottom, YAxes.Left);
        }

        internal double WidthForEqualAxes = Double.NaN;
        internal double HeightForEqualAxes = Double.NaN;

        /// <summary>
        /// Set no axes to have equal scales.
        /// </summary>
        public void ResetAxesEqual()
        {
            EqualAxes = null;
        }

        Size maxCanvasSize = new Size(10000, 10000);
        PlotPanel plotPanel;
        
        internal XAxis xAxisBottom, xAxisTop; 
        internal YAxis yAxisLeft, yAxisRight;

        private XAxis xAxis;
        private YAxis yAxis;
        private Axis2DCollection xAxes;
        private Axis2DCollection yAxes;

        public XAxis2DCollection XAxes { get { return xAxes as XAxis2DCollection; } }
        public YAxis2DCollection YAxes { get { return yAxes as YAxis2DCollection; } }

        private AxesFrame frame;

        /// <summary>
        /// This is the basic box on which the axes are presented. 
        /// </summary>
        public Path Frame { get { return frame.Frame; } }

        public Axes2D(PlotPanel plotPanel)
        {
            this.plotPanel = plotPanel;
            frame = new AxesFrame();
            frame.SetValue(Grid.ZIndexProperty, 300);
            // note that individual axes have index of 200
            plotPanel.Children.Add(frame);

            xAxisBottom = new XAxis();
            xAxisBottom.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Bottom);
            xAxisTop = new XAxis();
            xAxisTop.SetValue(XAxis.XAxisPositionProperty, XAxisPosition.Top);
            //
            xAxes = new XAxis2DCollection(plotPanel);
            xAxes.Add(xAxisBottom); xAxes.Add(xAxisTop);
            xAxis = xAxisBottom;
            xAxisTop.LabelsVisible = false;
            xAxisTop.TicksVisible = true;
            xAxisTop.GridLines.Visibility = Visibility.Collapsed;
            xAxisTop.BindToAxis(xAxisBottom);
            //
            yAxisLeft = new YAxis();
            yAxisLeft.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Left);
            yAxisRight = new YAxis();
            yAxisRight.SetValue(YAxis.YAxisPositionProperty, YAxisPosition.Right);
            //
            yAxes = new YAxis2DCollection(plotPanel);
            yAxes.Add(yAxisLeft); yAxes.Add(yAxisRight);
            yAxis = yAxisLeft;
            yAxisRight.LabelsVisible = false;
            yAxisRight.TicksVisible = true;
            yAxisRight.GridLines.Visibility = Visibility.Collapsed;
            yAxisRight.BindToAxis(yAxisLeft);
            //
            UpdateTicks();        
        }

        internal void ArrangeEachAxisAndFrame(Rect axesRegionLocation)
        {
            foreach (Axis2D axis in XAxes) axis.Arrange(axesRegionLocation);
            foreach (Axis2D axis in YAxes) axis.Arrange(axesRegionLocation);
            frame.Arrange(axesRegionLocation);
        }

        internal void RenderEachAxisAndFrame(Rect axesRegionLocation)
        {
            IEnumerable<Axis2D> allAxis = xAxes.Concat(yAxes);
            foreach (Axis2D axis in allAxis) axis.RenderAxis();
            frame.Render(axesRegionLocation);
        }

        internal void UpdateTicks()
        {
            foreach (Axis2D axis in xAxes) axis.DeriveTicks();
            foreach (Axis2D axis in yAxes) axis.DeriveTicks();
        }

        /// <summary>
        /// Increase axis margins by amount necessary to ensure both that there is room
        /// for labels and that the necessary axes are aligned.
        /// </summary>
        /// <param name="alignedAxes">List of axes that need to be aligned.</param>
        private static void ExpandAxisMargins(List<Axis2D> alignedAxes, double plotLength)
        {
            // Calculate margins
            Thickness1D margin = new Thickness1D(alignedAxes.Max(axis => axis.AxisPadding.Lower), alignedAxes.Max(axis => axis.AxisPadding.Upper));

            double minPlotLength = 1.0;
            if (plotLength > minPlotLength)
            {
                double newTotalLength = plotLength + margin.Total();
                foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = newTotalLength;
            }
            else if ((alignedAxes[0].AxisTotalLength - margin.Total()) < minPlotLength)
            {
                foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = margin.Total() + minPlotLength;
            }

            // Set the margin and update Scale and Offset.
            foreach (Axis2D axis in alignedAxes) axis.ResetAxisMargin(margin);
            
            int tickIndex = 0;
            int maxTickIndex = alignedAxes.Max(axis => axis.Ticks.Length) / 2;
            int[] tickPair = new int[2];
            Axis2D limitingLowerAxis = null;
            int limitingLowerTickIndex = 0;
            double limitingLowerSemiWidth = margin.Lower;
            Axis2D limitingUpperAxis = null;
            int limitingUpperTickIndex = 0;
            double limitingUpperSemiWidth = margin.Upper;
            double offsetLower = 0;
            double deltaLower = alignedAxes[0].MaxTransformed - alignedAxes[0].MinTransformed;
            double deltaUpper = deltaLower;
            double axisTotalLength = alignedAxes[0].AxisTotalLength;
            double offsetUpper = 0;

            int nRescales = 0; // for diagnosic purposes only

            while ((tickIndex <= maxTickIndex) && (nRescales < 10))
            {
                bool reset = false;
                // if a rescaling is required, start again from the beginning.
                for (int i = 0; i < alignedAxes.Count; ++i)
                {
                    Axis2D currentAxis = alignedAxes[i];
                    tickPair[0] = tickIndex;
                    tickPair[1] = currentAxis.TicksTransformed.Length - 1 - tickIndex;
                    if ((currentAxis.TicksTransformed.Length - 1 - tickIndex) < tickIndex) continue;
                    for (int j = 0; j <= 1; ++j)
                    {
                        int index = tickPair[j];
                        if (!currentAxis.LabelsVisible || currentAxis.TickLabelCache[index].Label.Text == "" || !currentAxis.TickLabelCache[index].IsShown) continue;
                        if ((currentAxis.Scale * currentAxis.TicksTransformed[index] - currentAxis.Offset - currentAxis.LimitingTickLabelSizeForLength(index) / 2) < -0.1)
                        {
                            // need to rescale axes
                            limitingLowerAxis = currentAxis;
                            limitingLowerTickIndex = index;
                            limitingLowerSemiWidth = currentAxis.LimitingTickLabelSizeForLength(index) / 2;
                            offsetLower = currentAxis.TicksTransformed[index] - currentAxis.MinTransformed;
                            deltaLower = currentAxis.MaxTransformed - currentAxis.MinTransformed;
                        }
                        else if ((currentAxis.Scale * currentAxis.TicksTransformed[index] - currentAxis.Offset + currentAxis.LimitingTickLabelSizeForLength(index) / 2) > (currentAxis.AxisTotalLength + 0.1))
                        {
                            // need to rescale axes
                            limitingUpperAxis = currentAxis;
                            limitingUpperTickIndex = index;
                            limitingUpperSemiWidth = currentAxis.LimitingTickLabelSizeForLength(index) / 2;
                            offsetUpper = currentAxis.MaxTransformed - currentAxis.TicksTransformed[index];
                            deltaUpper = currentAxis.MaxTransformed - currentAxis.MinTransformed;
                        }
                        else continue;
                        
                        // Reset required:
                        reset = true; nRescales++;
                        double offsetUpperPrime = offsetUpper * deltaLower / deltaUpper;
                        
                        // scale for lower-limiting axis
                        double newScale = (axisTotalLength - limitingLowerSemiWidth - limitingUpperSemiWidth) /
                            (deltaLower - offsetLower - offsetUpperPrime);
                        if (plotLength > minPlotLength)
                        {
                            // Axis is fixed to plotLength.
                            newScale = plotLength / deltaLower;
                            margin = new Thickness1D(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = plotLength + margin.Total();
                        }
                        if (newScale * deltaLower <= minPlotLength) 
                        {
                            // Axis is fixed to minPlotLength
                            newScale = minPlotLength / deltaLower;
                            margin = new Thickness1D(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = minPlotLength + margin.Total();
                        }
                        // otherwise, axis is unfixed.
                        margin = new Thickness1D(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                        foreach (Axis2D axis in alignedAxes) axis.RescaleAxis(newScale * deltaLower / (axis.MaxTransformed - axis.MinTransformed), margin);
                        break;
                    }
                    if (reset == true) break;
                }
                if (reset == true) tickIndex = 0;
                else tickIndex++;
            }
            if (nRescales == 10)
            {
                Console.WriteLine("Many rescales...");
            }
        }

        IEnumerable<Axis2D> xAxesBottom; 
        IEnumerable<Axis2D> xAxesTop; 
        IEnumerable<Axis2D> yAxesLeft;
        IEnumerable<Axis2D> yAxesRight;
        IEnumerable<Axis2D> allAxes;

        private Thickness margin;
        private Thickness axisSpacings;

        internal void InitializeMargins()
        {
            foreach (Axis2D axis in allAxes) axis.CalculateAxisThickness();
            double axisSpacing = AxisSpacing;
            axisSpacings = new Thickness(Math.Max((yAxesLeft.Count() - 1) * axisSpacing, 0),
                Math.Max((xAxesTop.Count() - 1) * axisSpacing, 0), Math.Max((yAxesRight.Count() - 1) * axisSpacing, 0), Math.Max((xAxesBottom.Count() - 1) * axisSpacing, 0));

            Thickness minAxisMargin = MinAxisMargin;

            margin = new Thickness(
                Math.Max(yAxesLeft.Sum(axis => axis.AxisThickness) + axisSpacings.Left + plotPanel.LegendRegion.Left, minAxisMargin.Left),
                Math.Max(xAxesTop.Sum(axis => axis.AxisThickness) + axisSpacings.Top + plotPanel.LegendRegion.Top, minAxisMargin.Top),
                Math.Max(yAxesRight.Sum(axis => axis.AxisThickness) + axisSpacings.Right + plotPanel.LegendRegion.Right, minAxisMargin.Right),
                Math.Max(xAxesBottom.Sum(axis => axis.AxisThickness) + axisSpacings.Bottom + plotPanel.LegendRegion.Bottom, minAxisMargin.Bottom));

            ResetMarginsXAxes(plotPanel.AvailableSize, margin);
            ResetMarginsYAxes(plotPanel.AvailableSize, margin);
        }
        
        /// <summary>
        /// Given the available size for the plot area and axes, determine the
        /// required size and the position of the plot region within this region.
        /// Also sets the axes scales and positions labels.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <param name="canvasPosition"></param>
        /// <param name="axesCanvasPositions"></param>
        internal void PlaceAxesFull()
        {
            // The arrangement process is
            // 1 - Calculate axes thicknesses
            // 2 - Expand axis margins if required, taking into account alignment requirements
            // 3 - Reduce axis if equal axes is demanded
            // 4 - Cull labels
            // 5 - Repeat 1 - 3 with culled labels
            int iter = 0;
            // Sort axes into top, bottom, right and left
            UpdateAxisPositions();
            while (iter <= 1)
            {
                // Step 1: calculate axis thicknesses (given any removed labels)
                InitializeMargins();

                // Step 2: expand axis margins if necessary,
                // taking account of any specified Width and/or Height
                ExpandAxisMargins(xAxes.ToList(), Width);
                ExpandAxisMargins(yAxes.ToList(), Height);
  
                // Step 3: take account of equal axes
                // This is only done on the second (and final iteration)
                if ((iter == 1) && (EqualAxes != null))
                {
                    HeightForEqualAxes = WidthForEqualAxes = Double.NaN;
                    if (EqualAxes.XAxis.Scale > EqualAxes.YAxis.Scale)
                    {
                        // XAxes will be reduced in size. First reset all margins to the minimum, then expand margins given the fixed scale.
                        WidthForEqualAxes = EqualAxes.YAxis.Scale * (EqualAxes.XAxis.MaxTransformed - EqualAxes.XAxis.MinTransformed); 
                    }
                    else
                    {
                        HeightForEqualAxes = EqualAxes.XAxis.Scale * (EqualAxes.YAxis.MaxTransformed - EqualAxes.YAxis.MinTransformed); 
                    }
                    if (!Double.IsNaN(WidthForEqualAxes))
                    {
                        ResetMarginsXAxes(plotPanel.AvailableSize, margin);
                        foreach (Axis2D axis in xAxes) axis.SetToShowAllLabels(); 
                        ExpandAxisMargins(xAxes.ToList(), WidthForEqualAxes);
                    }
                    if (!Double.IsNaN(WidthForEqualAxes))
                    {
                        ResetMarginsYAxes(plotPanel.AvailableSize, margin);
                        foreach (Axis2D axis in yAxes) axis.SetToShowAllLabels(); 
                        ExpandAxisMargins(yAxes.ToList(), WidthForEqualAxes);
                    }
                }

                PlaceEachAxis(AxisSpacing);
                // Step 4: cull labels
                bool cullOverlapping = (iter == 0) || (EqualAxes != null && iter == 1);
                foreach (Axis2D axis in allAxes) axis.PositionLabels(cullOverlapping);

                iter++;
            }
            foreach (Axis2D axis in allAxes) axis.SetLabelVisibility(); 
            plotPanel.AxesRegion = new Rect(0, 0, xAxes[0].AxisTotalLength, yAxes[0].AxisTotalLength);
            plotPanel.CanvasLocation = new Rect(new Point(xAxes[0].AxisPadding.Lower, yAxes[0].AxisPadding.Upper),
                new Point(plotPanel.AxesRegion.Width - xAxes[0].AxisPadding.Upper, plotPanel.AxesRegion.Height - yAxes[0].AxisPadding.Lower));
        }

        internal void UpdateAxisPositions()
        {
            xAxesBottom = xAxes.Where(axis => (axis as XAxis).Position == XAxisPosition.Bottom);
            xAxesTop = xAxes.Where(axis => (axis as XAxis).Position == XAxisPosition.Top);
            yAxesLeft = yAxes.Where(axis => (axis as YAxis).Position == YAxisPosition.Left);
            yAxesRight = yAxes.Where(axis => (axis as YAxis).Position == YAxisPosition.Right);
            allAxes = xAxes.Concat(yAxes);
            if (xAxesBottom.First() != null) xAxesBottom.First().IsInnermost = true;
            if (xAxesTop.First() != null) xAxesTop.First().IsInnermost = true;
            if (yAxesLeft.First() != null) yAxesLeft.First().IsInnermost = true;
            if (yAxesRight.First() != null) yAxesRight.First().IsInnermost = true;
        }

        internal void PlaceEachAxis(double axisSpacing)
        {
            double yPosition = yAxes[0].AxisTotalLength - yAxes[0].AxisPadding.Lower;
            foreach (XAxis xAxis in xAxesBottom)
            {
                xAxis.yPosition = yPosition;
                yPosition += xAxis.AxisThickness + axisSpacing;
            }
            yPosition = yAxes[0].AxisPadding.Upper;
            foreach (XAxis xAxis in xAxesTop)
            {
                xAxis.yPosition = yPosition;
                yPosition -= xAxis.AxisThickness + axisSpacing;
            }
            double xPosition = xAxes[0].AxisPadding.Lower;
            foreach (YAxis yAxis in yAxesLeft)
            {
                yAxis.xPosition = xPosition;
                xPosition -= yAxis.AxisThickness + axisSpacing;
            }
            xPosition = xAxes[0].AxisTotalLength - xAxes[0].AxisPadding.Upper;
            foreach (YAxis yAxis in yAxesRight)
            {
                yAxis.xPosition = xPosition;
                xPosition += yAxis.AxisThickness + axisSpacing;
            }
        }

        internal Thickness CalculateInitialAxesMargin()
        {
            return new Thickness();
        }

        private void ResetMarginsXAxes(Size availableSize, Thickness margin)
        {
            foreach (Axis2D axis in xAxes)
            {
                axis.AxisTotalLength = Math.Min(availableSize.Width, maxCanvasSize.Width + axis.AxisPadding.Total());
                axis.ResetAxisMargin(new Thickness1D(margin.Left, margin.Right));
            }
        }

        private void ResetMarginsYAxes(Size availableSize, Thickness margin)
        {
            foreach (Axis2D axis in yAxes)
            {
                axis.AxisTotalLength = Math.Min(availableSize.Height, maxCanvasSize.Height + axis.AxisPadding.Total());
                axis.ResetAxisMargin(new Thickness1D(margin.Bottom, margin.Top));
            }
        }

        internal void UpdateAxisPositionsOffsetOnly()
        {
            var allAxes = xAxes.Concat(yAxes);
            foreach (Axis2D axis in allAxes)
            {
                axis.UpdateOffset();
                axis.PositionLabels(true);
                axis.SetLabelVisibility(); 
            }
            plotPanel.AxesRegion = new Rect(0, 0, xAxisBottom.AxisTotalLength, yAxisLeft.AxisTotalLength);
            plotPanel.CanvasLocation = new Rect(xAxisBottom.AxisPadding.Lower, 
                 yAxisLeft.AxisPadding.Upper,
                xAxisBottom.AxisTotalLength - xAxisBottom.AxisPadding.Lower - xAxisBottom.AxisPadding.Upper,
                yAxisLeft.AxisTotalLength - yAxisLeft.AxisPadding.Lower - yAxisLeft.AxisPadding.Upper);
        }
    }
}
