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
            AxisMargin margin = new AxisMargin(alignedAxes.Max(axis => axis.AxisMargin.LowerMargin), alignedAxes.Max(axis => axis.AxisMargin.UpperMargin));

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
            double limitingLowerSemiWidth = margin.LowerMargin;
            Axis2D limitingUpperAxis = null;
            int limitingUpperTickIndex = 0;
            double limitingUpperSemiWidth = margin.UpperMargin;
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
                            margin = new AxisMargin(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = plotLength + margin.Total();
                        }
                        if (newScale * deltaLower <= minPlotLength) 
                        {
                            // Axis is fixed to minPlotLength
                            newScale = minPlotLength / deltaLower;
                            margin = new AxisMargin(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
                            foreach (Axis2D axis in alignedAxes) axis.AxisTotalLength = minPlotLength + margin.Total();
                        }
                        // otherwise, axis is unfixed.
                        margin = new AxisMargin(limitingLowerSemiWidth - offsetLower * newScale, limitingUpperSemiWidth - offsetUpperPrime * newScale);
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

        /// <summary>
        /// Given the available size for the plot area and axes, determine the
        /// required size and the position of the plot region within this region.
        /// Also sets the axes scales and positions and positions labels.
        /// </summary>
        /// <param name="availableSize"></param>
        /// <param name="canvasPosition"></param>
        /// <param name="axesCanvasPositions"></param>
        internal void PlaceAxesFull(Size availableSize, out Rect canvasPosition, out Size requiredSize)
        {
            Stopwatch watch = new Stopwatch(); watch.Start();
            // The arrangement process is
            // 1 - Calculate axes thicknesses
            // 2 - Expand axis margins if required, taking into account alignment requirements
            // 3 - Reduce axis if equal axes is demanded
            // 4 - Cull labels
            // 5 - Repeat 1 - 3 with culled labels
            requiredSize = availableSize;
            canvasPosition = new Rect();
            int iter = 0;
            UpdateAxisPositions();
            while (iter <= 1)
            {
                // Step 1: calculate axis thicknesses (given any removed labels)
                foreach (Axis2D axis in allAxes) axis.CalculateAxisThickness();
                double axisSpacing = AxisSpacing;
                Thickness axisSpacings = new Thickness(Math.Max((yAxesLeft.Count() - 1) * axisSpacing, 0),
                    Math.Max((xAxesTop.Count() - 1) * axisSpacing, 0), Math.Max((yAxesRight.Count() - 1) * axisSpacing, 0), Math.Max((xAxesBottom.Count() - 1) * axisSpacing, 0));
                Thickness minAxisMargin = MinAxisMargin;
                Thickness margin = new Thickness(Math.Max(yAxesLeft.Sum(axis => axis.AxisThickness) + axisSpacings.Left, minAxisMargin.Left), 
                    Math.Max(xAxesTop.Sum(axis => axis.AxisThickness) + axisSpacings.Top, minAxisMargin.Top),
                    Math.Max(yAxesRight.Sum(axis => axis.AxisThickness) + axisSpacings.Right, minAxisMargin.Right), 
                    Math.Max(xAxesBottom.Sum(axis => axis.AxisThickness) + axisSpacings.Bottom, minAxisMargin.Bottom));

                ResetMarginsXAxes(availableSize, margin);
                ResetMarginsYAxes(availableSize, margin);

                // Step 2: expand axis margins if necessary,
                // taking account of any specified Width and/or Height
                ExpandAxisMargins(xAxes.ToList(), Width);
                ExpandAxisMargins(yAxes.ToList(), Height);
  

                // Step 3: take account of equal axes
                // This is only done on the second (and final iteration)
                if ((iter == 1) && (EqualAxes != null))
                {
                    if (EqualAxes.XAxis.Scale > EqualAxes.YAxis.Scale)
                    {
                        // XAxes will be reduced in size. First reset all margins to the minimum, then expand margins given the fixed scale.
                        ResetMarginsXAxes(availableSize, margin);
                        foreach (Axis2D axis in xAxes) axis.SetToShowAllLabels(); 
                        ExpandAxisMargins(xAxes.ToList(), EqualAxes.YAxis.Scale * (EqualAxes.XAxis.MaxTransformed - EqualAxes.XAxis.MinTransformed)); 
                    }
                    else
                    {
                        ResetMarginsYAxes(availableSize, margin);
                        foreach (Axis2D axis in yAxes) axis.SetToShowAllLabels(); 
                        ExpandAxisMargins(yAxes.ToList(), EqualAxes.XAxis.Scale * (EqualAxes.YAxis.MaxTransformed - EqualAxes.YAxis.MinTransformed)); 
                    }

                }

                double yPosition = yAxes[0].AxisTotalLength - yAxes[0].AxisMargin.LowerMargin;
                foreach (XAxis xAxis in xAxesBottom)
                {
                    xAxis.yPosition = yPosition;
                    yPosition += xAxis.AxisThickness + axisSpacing;
                }
                yPosition = yAxes[0].AxisMargin.UpperMargin;
                foreach (XAxis xAxis in xAxesTop)
                {
                    xAxis.yPosition = yPosition;
                    yPosition -= xAxis.AxisThickness + axisSpacing;
                }
                double xPosition = xAxes[0].AxisMargin.LowerMargin;
                foreach (YAxis yAxis in yAxesLeft)
                {
                    yAxis.xPosition = xPosition;
                    xPosition -= yAxis.AxisThickness + axisSpacing;
                }
                xPosition = xAxes[0].AxisTotalLength - xAxes[0].AxisMargin.UpperMargin;
                foreach (YAxis yAxis in yAxesRight)
                {
                    yAxis.xPosition = xPosition;
                    xPosition += yAxis.AxisThickness + axisSpacing;
                }

                // Step 4: cull labels
                bool cullOverlapping = (iter == 0) || (EqualAxes != null && iter == 1);
                foreach (Axis2D axis in allAxes) axis.PositionLabels(cullOverlapping);

                iter++;
            }
            foreach (Axis2D axis in allAxes) axis.SetLabelVisibility(); 
            requiredSize = new Size(xAxes[0].AxisTotalLength, yAxes[0].AxisTotalLength);
            canvasPosition = new Rect(new Point(xAxes[0].AxisMargin.LowerMargin, yAxes[0].AxisMargin.UpperMargin),
                new Point(requiredSize.Width - xAxes[0].AxisMargin.UpperMargin, requiredSize.Height - yAxes[0].AxisMargin.LowerMargin));
            watch.Stop();
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

        internal Thickness CalculateInitialAxesMargin()
        {
            return new Thickness();
        }

        private void ResetMarginsXAxes(Size availableSize, Thickness margin)
        {
            foreach (Axis2D axis in xAxes)
            {
                axis.AxisTotalLength = Math.Min(availableSize.Width, maxCanvasSize.Width + axis.AxisMargin.Total());
                axis.AxisTotalLengthConstrained = axis.AxisTotalLength;
                axis.ResetAxisMargin(new AxisMargin(margin.Left, margin.Right));
            }
        }

        private void ResetMarginsYAxes(Size availableSize, Thickness margin)
        {
            foreach (Axis2D axis in yAxes)
            {
                axis.AxisTotalLength = Math.Min(availableSize.Height, maxCanvasSize.Height + axis.AxisMargin.Total());
                axis.AxisTotalLengthConstrained = axis.AxisTotalLength;
                axis.ResetAxisMargin(new AxisMargin(margin.Bottom, margin.Top));
            }
        }

        internal void UpdateAxisPositionsOffsetOnly(Rect availableSize, out Rect canvasPosition, out Size requiredSize)
        {
            var allAxes = xAxes.Concat(yAxes);
            foreach (Axis2D axis in allAxes)
            {
                axis.UpdateOffset();
                axis.PositionLabels(true);
                axis.SetLabelVisibility(); 
            }
            requiredSize = new Size(xAxisBottom.AxisTotalLength, yAxisLeft.AxisTotalLength);
            canvasPosition = new Rect(xAxisBottom.AxisMargin.LowerMargin, 
                 yAxisLeft.AxisMargin.UpperMargin,
                xAxisBottom.AxisTotalLength - xAxisBottom.AxisMargin.LowerMargin - xAxisBottom.AxisMargin.UpperMargin,
                yAxisLeft.AxisTotalLength - yAxisLeft.AxisMargin.LowerMargin - yAxisLeft.AxisMargin.UpperMargin);
        }
    }
}
