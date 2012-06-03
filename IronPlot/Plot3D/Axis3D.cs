// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using IronPlot;

namespace IronPlot.Plotting3D
{
    public interface I2DLayer
    {
        Point CanvasPointFrom3DPoint(Point3D point3D);
        Canvas Canvas { get; }
    }

    public enum XAxisType { MinusY, PlusY };
    public enum YAxisType { MinusX, PlusX };
    public enum ZAxisType { MinusXMinusY, MinusXPlusY, PlusXPlusY, PlusXMinusY };
    
    /// <summary>
    /// Axis for 3D plots. The Axis3D comprises ticks and labels (but not the line itself). 
    /// </summary>
    public abstract class Axis3D : Axis
    {
        // All 3D axis objects are associated with an Axes3D object.
        protected Axes3D axes;

        // The axes will have multiple x, y and z axes. These are administered by Axis3DCollection objects.
        protected Axis3DCollection axisCollection;

        //protected string axisLabelText;

        protected LinesModel3D model3D;

        protected List<TextBlock> axisLabels;

        protected TextBlock axisLabel = null;

        static Axis3D()
        {
            Axis.LabelsVisibleProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(true, Axis3D.OnLabelsVisibleChanged));
            Axis.TicksVisibleProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(true, Axis3D.OnTicksVisibleChanged));
            Axis.NumberOfTicksProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(10, Axis3D.OnNumberOfTicksChanged));
            Axis.TickLengthProperty.OverrideMetadata(typeof(Axis3D), new PropertyMetadata(0.05, Axis3D.OnTickLengthChanged));
        }

        protected override void UpdateTicksAndLabels()
        {
            UpdateLabels();
            UpdateLabelPositions(true);
        }

        internal static void OnLabelsVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            TextBlock label = ((Axis3D)obj).axisLabel;
            if ((bool)e.NewValue == false)
            {
                foreach (TextBlock textBlock in ((Axis3D)obj).axisLabels)
                {
                    textBlock.Visibility = Visibility.Collapsed;
                }
                if (label != null) label.Visibility = Visibility.Collapsed;
            }
            if ((bool)e.NewValue == true)
            {
                foreach (TextBlock textBlock in ((Axis3D)obj).axisLabels)
                {
                    textBlock.Visibility = Visibility.Visible;
                }
                if (label != null) label.Visibility = Visibility.Visible;
            }
        }

        internal static void OnTicksVisibleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == (bool)e.OldValue) return;
            ((Axis3D)obj).model3D.IsVisible = (bool)e.NewValue;
            ((Axis3D)obj).model3D.RequestRender(EventArgs.Empty);
        }

        internal new static void OnNumberOfTicksChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis3D)obj).DeriveTicks();
            ((Axis3D)obj).UpdateLabels();
            ((Axis3D)obj).UpdateLabelPositions(true);
            ((Axis3D)obj).model3D.RequestRender(EventArgs.Empty);
        }

        internal static void OnTickLengthChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axis3D)obj).DeriveTicks();
            ((Axis3D)obj).UpdateLabels();
            ((Axis3D)obj).UpdateLabelPositions(true);
            ((Axis3D)obj).model3D.RequestRender(EventArgs.Empty);
        }

        public Axis3D(Axes3D axes, Axis3DCollection axisCollection)
        {
            axisLabels = new List<TextBlock>();
            labelProperties = new LabelProperties(axisCollection.TickLabels);
            model3D = new LinesModel3D();
            this.axes = axes;
            this.axisCollection = axisCollection;
            this.axes.Children.Add(model3D);
        }

        public TextBlock AxisLabel
        {
            get { return axisLabel; }
        }

        private LabelProperties labelProperties;

        public LabelProperties Labels
        {
            get { return labelProperties; }
        }

        public abstract Point3D TickStartPoint(int i);
        public abstract Point3D TickEndPoint(int i);
        Point3D start, end, centre, offsetCentre;
        public abstract void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre);

        internal LinesModel3D Model3D
        {
            get { return model3D; }
        }

        internal void UpdateLabels()
        {
            if (axes.Layer2D == null) return;
            if (axisLabel == null)
            {
                axisLabel = new TextBlock();
                axisCollection.AxisLabels.BindTextBlock(axisLabel);
                axes.Layer2D.Canvas.Children.Add(axisLabel);
            }
            TextBlock currentTextBlock;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                if (i > (axisLabels.Count - 1))
                {
                    currentTextBlock = new TextBlock();
                    labelProperties.BindTextBlock(currentTextBlock);
                    if ((bool)GetValue(LabelsVisibleProperty) == false) currentTextBlock.Visibility = Visibility.Collapsed;
                    axes.Layer2D.Canvas.Children.Add(currentTextBlock);
                    axisLabels.Add(currentTextBlock);
                }
                else currentTextBlock = axisLabels[i];
                UpdateLabelText(i);
                AddTextToBlock(currentTextBlock, i);
                currentTextBlock.TextAlignment = TextAlignment.Center;
                //currentTextBlock.;
            }
            UpdateLabelPositions(false);
            // Cycle through any now redundant TextBlocks and make invisible
            for (int i = Ticks.Length; i < axisLabels.Count; ++i)
            {
                axisLabels[i].Text = "";
            }
        }

        internal void UpdateLabelPositions(bool allowHide)
        {
            if (((bool)GetValue(LabelsVisibleProperty) == false) || (axes.Layer2D == null)) return;
            Point3D tickStartPoint3D;
            Point3D tickEndPoint3D;
            Point tickStartPoint2D;
            Point tickEndPoint2D;
            double toBottom = 0, toRight = 0;
            TextBlock currentTextBlock;
            int missOutMax = 0;
            int missOut = 0;
            Rect lastRect = new Rect(new Point(Double.MaxValue, Double.MaxValue), new Point(Double.MaxValue, Double.MaxValue));
            Rect currentRect;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                currentTextBlock = axisLabels[i];
                currentTextBlock.Visibility = Visibility.Visible;
                tickEndPoint3D = TickEndPoint(i);
                tickEndPoint2D = axes.Layer2D.CanvasPointFrom3DPoint(tickEndPoint3D);
                if (Double.IsInfinity(tickEndPoint2D.X) || Double.IsInfinity(tickEndPoint2D.Y))
                {
                    continue;
                }
                if (i == 0)
                {
                    tickStartPoint3D = TickStartPoint(i);
                    tickStartPoint2D = axes.Layer2D.CanvasPointFrom3DPoint(tickStartPoint3D);
                    toBottom = (tickEndPoint2D.Y > tickStartPoint2D.Y) ? 0.0 : 1.0; // decide whether end of tick hits top or bottom of label
                    if (Math.Abs((tickEndPoint2D.Y - tickStartPoint2D.Y) / (tickEndPoint2D.X - tickStartPoint2D.X)) < 0.4)
                    {
                        toBottom = 0.5; // end of tick hits the label half way up
                    }
                    toRight = (tickEndPoint2D.X > tickStartPoint2D.X) ? 0.0 : 1.0; // end of tick hits the left of label if 0.0; otherwise the right
                }
                Point Offset = new Point(toRight * currentTextBlock.ActualWidth,
                    toBottom * currentTextBlock.ActualHeight);
                currentRect = new Rect(new Point(tickEndPoint2D.X - Offset.X, tickEndPoint2D.Y - Offset.Y),
                    new Size(currentTextBlock.ActualWidth, currentTextBlock.ActualHeight));
                currentTextBlock.SetValue(Canvas.LeftProperty, currentRect.Left);
                currentTextBlock.SetValue(Canvas.TopProperty, currentRect.Top);
                if (Intersect(currentRect, lastRect))
                {
                    missOut++;
                }
                else
                {
                    lastRect = currentRect;
                    missOutMax = Math.Max(missOut, missOutMax);
                    missOut = 0;
                }
            }
            missOutMax = Math.Max(missOut, missOutMax);
            missOut = 0;
            for (int i = 0; i < Ticks.Length; ++i)
            {
                if ((missOut < missOutMax) && (i > 0))
                {
                    missOut += 1;
                    if (allowHide) axisLabels[i].Visibility = Visibility.Collapsed;
                }
                else
                {
                    missOut = 0;
                }
            }
            UpdateAxisLabelPosition();
        }

        internal override void DeriveTicks()
        {
            base.DeriveTicks();
            // update axis information
            AxisProperties(ref start, ref end, ref centre, ref offsetCentre);
            UpdateModel3D();
        }
        
        /// <summary>
        /// Add ticks to the Model3D
        /// </summary>
        internal void UpdateModel3D()
        {
            model3D.Points.Clear();
            for (int i = 0; i < Ticks.Length; ++i)
            {
                model3D.Points.Add(new Point3DColor(TickStartPoint(i)));
                model3D.Points.Add(new Point3DColor(TickEndPoint(i)));
            }
            model3D.UpdateFromPoints();
        }

        private bool Intersect(Rect rect1, Rect rect2)
        {
            if ((rect2.Left > rect1.Right) || (rect2.Right < rect1.Left)) return false;
            if ((rect2.Top > rect1.Bottom) || (rect2.Bottom < rect1.Top)) return false;
            return true;
        }

        protected void UpdateAxisLabelPosition()
        {
            if (axisLabel == null) return;
            double labelWidth = axisLabel.ActualWidth;
            double labelHeight = axisLabel.ActualHeight;

            //Point3D start, end, centre, offsetCentre;
            // Find perpendicular distance of all labels from axis
            Point start2D = axes.Layer2D.CanvasPointFrom3DPoint(start);
            Point end2D = axes.Layer2D.CanvasPointFrom3DPoint(end);
            Point centre2D = axes.Layer2D.CanvasPointFrom3DPoint(centre);
            Point offsetCentre2D = axes.Layer2D.CanvasPointFrom3DPoint(offsetCentre);
            double distance;
            Line axis = new Line(start2D.ToVector(), end2D.ToVector() - start2D.ToVector());
            Vector centreTick = (offsetCentre2D.ToVector() - centre2D.ToVector());
            centreTick.Normalize();
            // Calculate maximum perpendicular distance of furthest point on any label from line
            distance = 0;
            Vector Offset;
            Vector axisNormal = axis.GetNormalDirection(); axisNormal.Normalize();
            if (Math.Abs(Vector.AngleBetween(axisNormal, centreTick)) > 90.0) axisNormal = axisNormal * -1.0;
            if (axisNormal.X < 0 && axisNormal.Y > 0) // labels in bottom left quadrant of axis
            {
                foreach (TextBlock label in axisLabels)
                {
                    double newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(Canvas.LeftProperty), 
                        (double)label.GetValue(Canvas.TopProperty) + label.ActualHeight)).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Top right corner of axis label
                Offset = new Vector(labelWidth / 2.0, -labelHeight / 2.0);
            }
            else if (axisNormal.X >= 0 && axisNormal.Y > 0) // bottom right quadrant
            {
                foreach (TextBlock label in axisLabels)
                {
                    double newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(Canvas.LeftProperty) + label.ActualWidth, 
                        (double)label.GetValue(Canvas.TopProperty) + label.ActualHeight)).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Top left corner of axis label
                Offset = new Vector(-labelWidth / 2.0, -labelHeight / 2.0);
            }
            else if (axisNormal.X < 0 && axisNormal.Y <= 0) // top left quadrant
            {
                foreach (TextBlock label in axisLabels)
                {
                    double newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(Canvas.LeftProperty),
                        (double)label.GetValue(Canvas.TopProperty))).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Bottom right corner of axis label
                Offset = new Vector(labelWidth / 2.0, labelHeight / 2.0);
            }
            else 
            {
                foreach (TextBlock label in axisLabels) // top right quadrant
                {
                    double newDistance = Math.Abs(Geomery.PointToLineDistance((new Point((double)label.GetValue(Canvas.LeftProperty) + label.ActualWidth,
                        (double)label.GetValue(Canvas.TopProperty))).ToVector(), axis));
                    if (newDistance > distance) distance = newDistance;
                }
                // Bottom left corner of axis label
                Offset = new Vector(-labelWidth / 2.0, labelHeight / 2.0);
            }
            // Our new label is on this line:
            Line offsetAxis = new Line(axis.PointOnLine + axisNormal * distance, axis.Direction);
            // And also on this line:
            //Line offsetCentreTick = new Line(centre2D.ToVector() + Offset, centreTick);
            Line offsetCentreTick = new Line(centre2D.ToVector() + Offset, axisNormal);
            Vector topLeft = offsetAxis.IntersectionWithLine(offsetCentreTick) - Offset - (new Vector(labelWidth / 2.0, labelHeight / 2.0));
            axisLabel.SetValue(Canvas.LeftProperty, topLeft.X);
            axisLabel.SetValue(Canvas.TopProperty, topLeft.Y);
        }

    }

    public class XAxis3D : Axis3D
    {
        XAxisType axisType = XAxisType.MinusY;
        
        public override double Min
        {
            set
            {
                Point3D oldPoint = axes.GraphMin;
                oldPoint.X = value;
                axes.GraphMin = oldPoint;
            }
            get { return axes.GraphMin.X; }
        }

        public override double Max
        {
            set
            {
                Point3D oldPoint = axes.GraphMax;
                oldPoint.X = value;
                axes.GraphMax = oldPoint;
            }
            get { return axes.GraphMax.X; }
        }

        private double Offset;

        public XAxis3D(Axes3D axes, Axis3DCollection axisCollection)
            : base(axes, axisCollection)
        { 
        }

        public XAxis3D(Axes3D axes, Axis3DCollection axisCollection, XAxisType axisType)
            : base(axes, axisCollection)
        {
            this.axisType = axisType;
        }

        public override Point3D TickStartPoint(int i)
        {
            Point3D tickStartPoint3D;
            if (axisType == XAxisType.MinusY)
                tickStartPoint3D = new Point3D(Ticks[i], axes.GraphMin.Y, axes.GraphMin.Z);
            else 
                tickStartPoint3D = new Point3D(Ticks[i], axes.GraphMax.Y, axes.GraphMin.Z);
            return tickStartPoint3D;
        }

        public override Point3D TickEndPoint(int i)
        {
            Offset = TickLength * (axes.GraphMax.Y - axes.GraphMin.Y);
            Point3D tickEndPoint3D;
            if (axisType == XAxisType.MinusY)
                tickEndPoint3D = new Point3D(Ticks[i], axes.GraphMin.Y - Offset, axes.GraphMin.Z);
            else
                tickEndPoint3D = new Point3D(Ticks[i], axes.GraphMax.Y + Offset, axes.GraphMin.Z);
            return tickEndPoint3D;
        }

        public override void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre)
        {
            Offset = TickLength * (axes.GraphMax.Y - axes.GraphMin.Y);
            if (axisType == XAxisType.MinusY)
            {
                start = new Point3D(Min, axes.GraphMin.Y, axes.GraphMin.Z);
                end = new Point3D(Max, start.Y, start.Z);
                centre = new Point3D((Max + Min) / 2.0, start.Y, start.Z);
                offsetCentre = new Point3D(centre.X, centre.Y - Offset, centre.Z);
            }
            else
            {
                start = new Point3D(Min, axes.GraphMax.Y, axes.GraphMin.Z);
                end = new Point3D(Max, start.Y, start.Z);
                centre = new Point3D((Max + Min) / 2.0, start.Y, start.Z);
                offsetCentre = new Point3D(centre.X, centre.Y + Offset, centre.Z);
            }
        }
    }

    public class YAxis3D : Axis3D
    {
        YAxisType axisType = YAxisType.MinusX;
        
        public override double Min
        {
            set
            {
                Point3D oldPoint = axes.GraphMin;
                oldPoint.Y = value;
                axes.GraphMin = oldPoint;
            }
            get { return axes.GraphMin.Y; }
        }

        public override double Max
        {
            set
            {
                Point3D oldPoint = axes.GraphMax;
                oldPoint.Y = value;
                axes.GraphMax = oldPoint;
            }
            get { return axes.GraphMax.Y; }
        }

        private double Offset;

        public YAxis3D(Axes3D axes, Axis3DCollection axisCollection)
            : base(axes, axisCollection)
        { }

        public YAxis3D(Axes3D axes, Axis3DCollection axisCollection, YAxisType axisType)
            : base(axes, axisCollection)
        {
            this.axisType = axisType;
        }

        public override Point3D TickStartPoint(int i)
        {
            Point3D tickStartPoint3D;
            if (axisType == YAxisType.MinusX)
                tickStartPoint3D = new Point3D(axes.GraphMin.X, Ticks[i], axes.GraphMin.Z);
            else
                tickStartPoint3D = new Point3D(axes.GraphMax.X, Ticks[i], axes.GraphMin.Z);
            return tickStartPoint3D;
        }

        public override Point3D TickEndPoint(int i)
        {
            Offset = TickLength * (axes.GraphMax.X - axes.GraphMin.X);
            Point3D tickEndPoint3D;
            if (axisType == YAxisType.MinusX)
                tickEndPoint3D = new Point3D(axes.GraphMin.X - Offset, Ticks[i], axes.GraphMin.Z);
            else
                tickEndPoint3D = new Point3D(axes.GraphMax.X + Offset, Ticks[i], axes.GraphMin.Z);
            return tickEndPoint3D;
        }

        public override void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre)
        {
            Offset = TickLength * (axes.GraphMax.X - axes.GraphMin.X);
            if (axisType == YAxisType.MinusX)
            {
                start = new Point3D(axes.GraphMin.X, Min, axes.GraphMin.Z);
                end = new Point3D(start.X, Max, start.Z);
                centre = new Point3D(start.X, (Max + Min) / 2.0, start.Z);
                offsetCentre = new Point3D(centre.X - Offset, centre.Y, centre.Z);
            }
            else
            {
                start = new Point3D(axes.GraphMax.X, Min, axes.GraphMin.Z);
                end = new Point3D(start.X, Max, start.Z);
                centre = new Point3D(start.X, (Max + Min) / 2.0, start.Z);
                offsetCentre = new Point3D(centre.X + Offset, centre.Y, centre.Z);
            }
        }
    }

    public class ZAxis3D : Axis3D
    {
        ZAxisType axisType = ZAxisType.MinusXPlusY;
        
        public override double Min
        {
            set
            {
                Point3D oldPoint = axes.GraphMin;
                oldPoint.Z = value;
                axes.GraphMin = oldPoint;
            }
            get { return axes.GraphMin.Z; }
        }

        public override double Max
        {
            set
            {
                Point3D oldPoint = axes.GraphMax;
                oldPoint.Z = value;
                axes.GraphMax = oldPoint;
            }
            get { return axes.GraphMax.Z; }
        }

        private double offsetX, offsetY;

        public ZAxis3D(Axes3D axes, Axis3DCollection axisCollection)
            : base(axes, axisCollection)
        { }

        public ZAxis3D(Axes3D axes, Axis3DCollection axisCollection, ZAxisType axisType)
            : base(axes, axisCollection)
        {
            this.axisType = axisType;
        }

        public override Point3D TickStartPoint(int i)
        {
            Point3D tickStartPoint3D;
            switch (axisType)
            {
                case ZAxisType.MinusXMinusY:
                    tickStartPoint3D = new Point3D(axes.GraphMin.X, axes.GraphMin.Y, Ticks[i]);
                    break;
                case ZAxisType.MinusXPlusY:
                    tickStartPoint3D = new Point3D(axes.GraphMin.X, axes.GraphMax.Y, Ticks[i]);
                    break;
                case ZAxisType.PlusXMinusY:
                    tickStartPoint3D = new Point3D(axes.GraphMax.X, axes.GraphMin.Y, Ticks[i]);
                    break;
                case ZAxisType.PlusXPlusY:
                    tickStartPoint3D = new Point3D(axes.GraphMax.X, axes.GraphMax.Y, Ticks[i]);
                    break;
                default: 
                    tickStartPoint3D = new Point3D(axes.GraphMin.X, axes.GraphMin.Y, Ticks[i]);
                    break;
            }
            return tickStartPoint3D;
        }

        public override Point3D TickEndPoint(int i)
        {
            offsetY = TickLength * (axes.GraphMax.Y - axes.GraphMin.Y);
            offsetX = TickLength * (axes.GraphMax.X - axes.GraphMin.X);
            Point3D tickEndPoint3D;
            switch (axisType)
            {
                case ZAxisType.MinusXMinusY:
                    tickEndPoint3D = new Point3D(axes.GraphMin.X - offsetX, axes.GraphMin.Y, Ticks[i]);
                    break;
                case ZAxisType.MinusXPlusY:
                    tickEndPoint3D = new Point3D(axes.GraphMin.X, axes.GraphMax.Y + offsetY, Ticks[i]);
                    break;
                case ZAxisType.PlusXMinusY:
                    tickEndPoint3D = new Point3D(axes.GraphMax.X, axes.GraphMin.Y - offsetY, Ticks[i]);
                    break;
                case ZAxisType.PlusXPlusY:
                    tickEndPoint3D = new Point3D(axes.GraphMax.X + offsetX, axes.GraphMax.Y, Ticks[i]);
                    break;
                default:
                    tickEndPoint3D = new Point3D(axes.GraphMin.X, axes.GraphMax.Y + offsetY, Ticks[i]);
                    break;
            }
            return tickEndPoint3D;
        }

        public override void AxisProperties(ref Point3D start, ref Point3D end, ref Point3D centre, ref Point3D offsetCentre)
        {
            offsetY = TickLength * (axes.GraphMax.Y - axes.GraphMin.Y);
            offsetX = TickLength * (axes.GraphMax.X - axes.GraphMin.X);
            switch (axisType)
            {
                case ZAxisType.MinusXMinusY:
                    start = new Point3D(axes.GraphMin.X, axes.GraphMin.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X - offsetX, centre.Y, centre.Z);
                    break;
                case ZAxisType.MinusXPlusY:
                    start = new Point3D(axes.GraphMin.X, axes.GraphMax.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X, centre.Y + offsetY, centre.Z);
                    break;
                case ZAxisType.PlusXMinusY:
                    start = new Point3D(axes.GraphMax.X, axes.GraphMin.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X, centre.Y - offsetY, centre.Z);
                    break;
                case ZAxisType.PlusXPlusY:
                    start = new Point3D(axes.GraphMax.X, axes.GraphMax.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X + offsetX, centre.Y, centre.Z);
                    break;
                default:
                    start = new Point3D(axes.GraphMin.X, axes.GraphMin.Y, Min);
                    end = new Point3D(start.X, start.Y, Max);
                    centre = new Point3D(start.X, start.Y, (Max + Min) / 2.0);
                    offsetCentre = new Point3D(centre.X - offsetX, centre.Y, centre.Z);
                    break;
            }
        }
    }
}

