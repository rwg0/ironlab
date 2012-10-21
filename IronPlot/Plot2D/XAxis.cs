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
    public enum XAxisPosition { Top, Bottom };

    public class XAxis : Axis2D
    {
        // The y position of the axis in Axis Canvas coordinates. 
        internal double yPosition = 0;

        internal static DependencyProperty XAxisPositionProperty =
            DependencyProperty.Register("XAxisPosition",
            typeof(XAxisPosition), typeof(XAxis), new PropertyMetadata(XAxisPosition.Bottom));

        public XAxisPosition Position { get { return (XAxisPosition)GetValue(XAxisPositionProperty); } set { SetValue(XAxisPositionProperty, value); } }

        public XAxis() : base() 
        {
            //axisLabel.
        }

        internal override void PositionLabels(bool cullOverlapping)
        {
            if (!LabelsVisible) return;
            TextBlock currentTextBlock;
            int missOut = 0, missOutMax = 0;
            double currentRight, lastRight = Double.NegativeInfinity;
            double tickOffset = TicksVisible ? Math.Max(TickLength, 0.0) : 0;
            // Go through ticks in order of increasing Canvas coordinate.
            for (int i = 0; i < TicksTransformed.Length; ++i)
            {
                // Miss out labels if these would overlap.
                currentTextBlock = TickLabelCache[i].Label;
                currentRight = Scale * TicksTransformed[i] - Offset + currentTextBlock.DesiredSize.Width / 2.0;
                currentTextBlock.SetValue(Canvas.LeftProperty, currentRight - currentTextBlock.DesiredSize.Width);
                if ((XAxisPosition)GetValue(XAxisPositionProperty) == XAxisPosition.Bottom)
                {
                    currentTextBlock.SetValue(Canvas.TopProperty, yPosition + tickOffset);
                }
                else
                {
                    currentTextBlock.SetValue(Canvas.TopProperty, yPosition - tickOffset - currentTextBlock.DesiredSize.Height);
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
                for (int i = 0; i < TicksTransformed.Length; ++i)
                {
                    if ((missOut < missOutMax) && (i > 0))
                    {
                        missOut += 1;
                        TickLabelCache[i].IsShown = false;
                    }
                    else missOut = 0;
                }
            }
            // Cycle through any now redundant TextBlocks and make invisible.
            for (int i = TicksTransformed.Length; i < TickLabelCache.Count; ++i)
            {
                TickLabelCache[i].IsShown = false;
            }
            // Finally, position axisLabel.
            if ((XAxisPosition)GetValue(XAxisPositionProperty) == XAxisPosition.Bottom)
                axisLabel.SetValue(Canvas.TopProperty, yPosition + AxisThickness - axisLabel.DesiredSize.Height);
            else axisLabel.SetValue(Canvas.TopProperty, yPosition - AxisThickness + axisLabel.DesiredSize.Height);
            double xPosition = Scale * 0.5 * (MaxTransformed + MinTransformed) - Offset - axisLabel.DesiredSize.Width / 2.0;
            axisLabel.SetValue(Canvas.LeftProperty, xPosition);
        }

        internal override double LimitingTickLabelSizeForLength(int index)
        {
            return TickLabelCache[index].Label.DesiredSize.Width;
        }

        protected override double LimitingTickLabelSizeForThickness(int index)
        {
            return TickLabelCache[index].Label.DesiredSize.Height;
        }

        protected override double LimitingAxisLabelSizeForLength()
        {
            return axisLabel.DesiredSize.Width;
        }

        protected override double LimitingAxisLabelSizeForThickness()
        {
            return axisLabel.DesiredSize.Height;
        }

        internal override Point TickStartPosition(int i)
        {
            return new Point(TicksTransformed[i] * Scale - Offset, yPosition);
        }

        internal override void RenderAxis()
        {
            XAxisPosition position = (XAxisPosition)GetValue(XAxisPositionProperty);
            Point tickPosition;

            StreamGeometryContext lineContext = axisLineGeometry.Open();
            if (!IsInnermost)
            {
                Point axisStart = new Point(MinTransformed * Scale - Offset - axisLine.StrokeThickness / 2, yPosition);
                lineContext.BeginFigure(axisStart, false, false);
                lineContext.LineTo(new Point(MaxTransformed * Scale - Offset + axisLine.StrokeThickness / 2, yPosition), true, false);
            }
            lineContext.Close();
            
            StreamGeometryContext ticksContext = axisTicksGeometry.Open();
            if (TicksVisible)
            {
                for (int i = 0; i < TicksTransformed.Length; ++i)
                {
                    tickPosition = TickStartPosition(i);
                    ticksContext.BeginFigure(tickPosition, false, false);
                    if (position == XAxisPosition.Bottom)
                    {
                        tickPosition.Y = tickPosition.Y + TickLength;
                    }
                    if (position == XAxisPosition.Top)
                    {
                        tickPosition.Y = tickPosition.Y - TickLength;
                    }
                    ticksContext.LineTo(tickPosition, true, false);
                    ticksContext.BeginFigure(tickPosition, false, false);
                    tickPosition.Y = tickPosition.Y;
                    ticksContext.LineTo(tickPosition, true, false);
                }
            }
            ticksContext.Close();
            
            interactionPad.Width = Math.Max(AxisTotalLength - AxisPadding.Total(), 1);
            interactionPad.Height = AxisThickness;
            if (position == XAxisPosition.Bottom) interactionPad.SetValue(Canvas.TopProperty, yPosition);
            else interactionPad.SetValue(Canvas.TopProperty, yPosition - AxisThickness);
            double xPosition = Scale * MinTransformed - Offset;
            interactionPad.SetValue(Canvas.LeftProperty, xPosition);
            base.RenderAxis();
        }

        internal override Transform1D GraphToAxesCanvasTransform()
        {
            return new Transform1D(Scale, Offset);
        }

        internal override Transform1D GraphToCanvasTransform()
        {
            return new Transform1D(Scale, Offset + AxisPadding.Lower);
        }

        internal override double GraphToCanvas(double canvas)
        {
            return GraphTransform(canvas) * Scale - Offset - AxisPadding.Lower;
        }

        internal override double CanvasToGraph(double graph)
        {
            return CanvasTransform(graph / Scale + (Offset + AxisPadding.Lower) / Scale);
        }
    }
}
