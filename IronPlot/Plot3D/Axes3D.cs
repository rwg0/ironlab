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

namespace IronPlot.Plotting3D
{
    public enum GraphSides { MinusX, MinusY, MinusZ, PlusX, PlusY, PlusZ };
    
    public class Axes3D : Model3D
    {
        public static readonly DependencyProperty GraphMinProperty =
            DependencyProperty.Register("GraphMin",
                typeof(Point3D), typeof(Axes3D),
                new PropertyMetadata(new Point3D(-10, -10, -10), OnUpdateGraphMaxMin));

        public static readonly DependencyProperty GraphMaxProperty =
            DependencyProperty.Register("GraphMax",
                typeof(Point3D), typeof(Axes3D),
                new PropertyMetadata(new Point3D(10, 10, 10), OnUpdateGraphMaxMin));

        private static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register("LineThickness",
            typeof(double),
            typeof(Axes3D),
                new PropertyMetadata(1.5, LineThicknessChanged));

        private LabelProperties labelProperties;

        public LabelProperties Labels
        {
            get { return labelProperties; }
        }

        public Point3D GraphMin
        {
            get { return (Point3D)GetValue(GraphMinProperty); }
            set { SetValue(GraphMinProperty, value); }
        }

        public Point3D GraphMax
        {
            get { return (Point3D)GetValue(GraphMaxProperty); }
            set { SetValue(GraphMaxProperty, value); }
        }

        public double LineThickness
        {
            set { SetValue(LineThicknessProperty, value); }
            get { return (double)GetValue(LineThicknessProperty); }
        }

        protected static void OnUpdateGraphMaxMin(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Axes3D)obj).Generate();
            ((Axes3D)obj).RedrawAxesLines();
            ((Axes3D)obj).UpdateLabels();
        }

        protected static void LineThicknessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            Axes3D axes = obj as Axes3D;
            foreach (LinesModel3D model in axes.Children)
            {
                model.LineThickness = (double)args.NewValue;
            }
        }

        public enum Face { MinusX, PlusX, MinusY, PlusY, MinusZ, PlusZ };
        public enum Ticks { XTicks, YTicks, ZTicks };
        public enum OpenSides { PlusXPlusY, MinusXPlusY, MinusXMinusY, PlusXMinusY  }

        public LinesModel3D MinusX, PlusX, MinusY, PlusY, MinusZ, PlusZ, Base;

        protected MatrixTransform3D modelToWorld = (MatrixTransform3D)MatrixTransform3D.Identity;

        private Axis3DCollection xAxisCollection = new Axis3DCollection();
        private Axis3DCollection yAxisCollection = new Axis3DCollection();
        private Axis3DCollection zAxisCollection = new Axis3DCollection();

        private XAxis3D[] xAxes;
        private YAxis3D[] yAxes;
        private ZAxis3D[] zAxes;
        private LinesModel3D[] sides;

        public Axis3DCollection XAxes
        {
            get { return xAxisCollection; }
        }

        public Axis3DCollection YAxes
        {
            get { return yAxisCollection; }
        }

        public Axis3DCollection ZAxes
        {
            get { return zAxisCollection; }
        }

        public Axes3D() : base()
        {
            labelProperties = new LabelProperties();
            xAxisCollection.TickLabels.SetParent(labelProperties);
            yAxisCollection.TickLabels.SetParent(labelProperties);
            zAxisCollection.TickLabels.SetParent(labelProperties);
            xAxisCollection.AxisLabels.Text = "X";
            yAxisCollection.AxisLabels.Text = "Y";
            zAxisCollection.AxisLabels.Text = "Z";

            sides = new LinesModel3D[6]; 
            MinusX = new LinesModel3D(); sides[(int)GraphSides.MinusX] = MinusX;
            MinusY = new LinesModel3D(); sides[(int)GraphSides.MinusY] = MinusY;
            MinusZ = new LinesModel3D(); sides[(int)GraphSides.MinusZ] = MinusZ;
            PlusX = new LinesModel3D(); sides[(int)GraphSides.PlusX] = PlusX;
            PlusY = new LinesModel3D(); sides[(int)GraphSides.PlusY] = PlusY;
            PlusZ = new LinesModel3D(); sides[(int)GraphSides.PlusZ] = PlusZ;

            this.Children.Add(MinusX);
            this.Children.Add(PlusX);
            this.Children.Add(MinusY);
            this.Children.Add(PlusY);
            this.Children.Add(MinusZ);
            this.Children.Add(PlusZ);

            xAxes = new XAxis3D[2];
            yAxes = new YAxis3D[2];
            zAxes = new ZAxis3D[4];
            // X Axes
            xAxes[(int)XAxisType.MinusY] = new XAxis3D(this, xAxisCollection, XAxisType.MinusY);
            xAxes[(int)XAxisType.PlusY] = new XAxis3D(this, xAxisCollection, XAxisType.PlusY);
            xAxisCollection.AddAxis(xAxes[(int)XAxisType.MinusY]); xAxisCollection.AddAxis(xAxes[(int)XAxisType.PlusY]); 

            // Y Axes
            yAxes[(int)YAxisType.MinusX] = new YAxis3D(this, yAxisCollection, YAxisType.MinusX);
            yAxes[(int)YAxisType.PlusX] = new YAxis3D(this, yAxisCollection, YAxisType.PlusX);
            yAxisCollection.AddAxis(yAxes[(int)YAxisType.MinusX]); yAxisCollection.AddAxis(yAxes[(int)YAxisType.PlusX]); 

            // Z Axes
            zAxes[(int)ZAxisType.MinusXMinusY] = new ZAxis3D(this, zAxisCollection, ZAxisType.MinusXMinusY);
            zAxes[(int)ZAxisType.MinusXPlusY] = new ZAxis3D(this, zAxisCollection, ZAxisType.MinusXPlusY);
            zAxes[(int)ZAxisType.PlusXMinusY] = new ZAxis3D(this, zAxisCollection, ZAxisType.PlusXMinusY);
            zAxes[(int)ZAxisType.PlusXPlusY] = new ZAxis3D(this, zAxisCollection, ZAxisType.PlusXPlusY);
            zAxisCollection.AddAxis(zAxes[(int)ZAxisType.MinusXMinusY]); zAxisCollection.AddAxis(zAxes[(int)ZAxisType.MinusXPlusY]);
            zAxisCollection.AddAxis(zAxes[(int)ZAxisType.PlusXMinusY]); zAxisCollection.AddAxis(zAxes[(int)ZAxisType.PlusXPlusY]);

            Base = new LinesModel3D(); 
            this.Children.Add(Base); // Add base last so that this can overwrite other lines.
            PlusZ.IsVisible = false;
            // Note axes are already added as Children
            Generate();
        }

        internal override void OnViewportImageChanged(ViewportImage newViewportImage)
        {
 	        base.OnViewportImageChanged(newViewportImage);
            UpdateLabels();
            this.OnDraw -= new OnDrawEventHandler(OnDrawUpdate);
            this.OnDraw += new OnDrawEventHandler(OnDrawUpdate);
        }

        /// <summary>
        /// Called on draw: used to update WPF elements on the 2D layer
        /// </summary>
        protected void OnDrawUpdate(object sender, EventArgs e)
        {
            UpdateLabelPositions();
        }

        double PIBy2 = Math.PI / 2;
        double PI = Math.PI;

        internal void UpdateOpenSides(double phi)
        {
            if (phi > 0 && phi < PIBy2) SetVisibleSidesAndAxes(OpenSides.PlusXPlusY);
            else if (phi >= PIBy2 && phi <= PI) SetVisibleSidesAndAxes(OpenSides.MinusXPlusY);
            else if (phi <= 0 && phi > -PIBy2) SetVisibleSidesAndAxes(OpenSides.PlusXMinusY);
            else SetVisibleSidesAndAxes(OpenSides.MinusXMinusY);
        }

        protected void SetVisibleSidesAndAxes(OpenSides openSides)
        {
            int openSide1;
            int openSide2;
            int visibleXAxis;
            int visibleYAxis;
            int visibleZAxis;
            switch (openSides)
            {
                case OpenSides.MinusXMinusY:
                    openSide1 = (int)GraphSides.MinusX;
                    openSide2 = (int)GraphSides.MinusY;
                    visibleXAxis = (int)XAxisType.MinusY;
                    visibleYAxis = (int)YAxisType.MinusX;
                    visibleZAxis = (int)ZAxisType.MinusXPlusY;
                    break;
                case OpenSides.MinusXPlusY:
                    openSide1 = (int)GraphSides.MinusX;
                    openSide2 = (int)GraphSides.PlusY;
                    visibleXAxis = (int)XAxisType.PlusY;
                    visibleYAxis = (int)YAxisType.MinusX;
                    visibleZAxis = (int)ZAxisType.PlusXPlusY;
                    break;
                case OpenSides.PlusXMinusY:
                    openSide1 = (int)GraphSides.PlusX;
                    openSide2 = (int)GraphSides.MinusY;
                    visibleXAxis = (int)XAxisType.MinusY;
                    visibleYAxis = (int)YAxisType.PlusX;
                    visibleZAxis = (int)ZAxisType.MinusXMinusY;
                    break;
                case OpenSides.PlusXPlusY:
                    openSide1 = (int)GraphSides.PlusX;
                    openSide2 = (int)GraphSides.PlusY;
                    visibleXAxis = (int)XAxisType.PlusY;
                    visibleYAxis = (int)YAxisType.PlusX;
                    visibleZAxis = (int)ZAxisType.PlusXMinusY;
                    break;
                default:
                    openSide1 = (int)GraphSides.MinusX;
                    openSide2 = (int)GraphSides.MinusY;
                    visibleXAxis = (int)XAxisType.MinusY;
                    visibleYAxis = (int)YAxisType.MinusX;
                    visibleZAxis = (int)ZAxisType.PlusXMinusY;
                    break;
            }
            foreach (XAxis3D axis in xAxes) { axis.LabelsVisible = false; axis.TicksVisible = false; }
            foreach (YAxis3D axis in yAxes) { axis.LabelsVisible = false; axis.TicksVisible = false; }
            foreach (ZAxis3D axis in zAxes) { axis.LabelsVisible = false; axis.TicksVisible = false; }
            xAxes[visibleXAxis].LabelsVisible = true; xAxes[visibleXAxis].TicksVisible = true;
            yAxes[visibleYAxis].LabelsVisible = true; yAxes[visibleYAxis].TicksVisible = true;
            zAxes[visibleZAxis].LabelsVisible = true; zAxes[visibleZAxis].TicksVisible = true;
            foreach (LinesModel3D side in sides) { side.IsVisible = true; }
            sides[openSide1].IsVisible = false; sides[openSide2].IsVisible = false;
            PlusZ.IsVisible = false;
        }

        internal void RedrawAxesLines()
        {
            foreach (Model3D model in Children)
            {
                (model as LinesModel3D).UpdateFromPoints();
            }
        }

        internal void AddBase(Point3D graphMin, Point3D graphMax)
        {
            Base.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            Base.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
        }

        internal void AddGridLines(List<Point3DColor> points, Face face, Ticks ticks, System.Windows.Media.Color gridColor)
        {
            List<Axis3D> axisList = new List<Axis3D>(3);
            axisList.Add(xAxes[0]); axisList.Add(yAxes[0]); axisList.Add(zAxes[0]);
            int constIndex; // Point dimension with all constant values
            double constValue;
            int ticksIndex; // Point dimension spanned by ticks
            int lineIndex = 0; // Point dimension spanned by single line 
            constIndex = (int)face / 2;
            constValue = ((int)face % 2) == 0 ? axisList[constIndex].Min : axisList[constIndex].Max;
            ticksIndex = (int)ticks;
            if (ticksIndex == constIndex)
            {
                return;
            }
            for (int i = 0; i < 3; ++i)
            {
                if ((i != constIndex) && (i != ticksIndex)) { lineIndex = i; break; }
            }
            double[] ticksValue = axisList[ticksIndex].Ticks;
            double min = axisList[ticksIndex].Min;
            double max = axisList[ticksIndex].Max;
            double[] lineValue = new double[2];
            lineValue[0] = axisList[lineIndex].Min;
            lineValue[1] = axisList[lineIndex].Max;
            double[] startPoint = new Double[3];
            double[] endPoint = new Double[3];
            startPoint[constIndex] = constValue;
            endPoint[constIndex] = constValue;
            startPoint[lineIndex] = lineValue[0];
            endPoint[lineIndex] = lineValue[1];
            for (int i = 0; i < ticksValue.Length; ++i)
            {
                if (ticksValue[i] == min || ticksValue[i] == max) continue;
                startPoint[ticksIndex] = endPoint[ticksIndex] = ticksValue[i];
                points.Add(new Point3DColor(startPoint[0], startPoint[1], startPoint[2], gridColor));
                points.Add(new Point3DColor(endPoint[0], endPoint[1], endPoint[2], gridColor));
            }
        }

        internal void UpdateLabels()
        {
            if (layer2D != null)
            {
                foreach (XAxis3D axis in xAxes) axis.UpdateLabels();
                foreach (YAxis3D axis in yAxes) axis.UpdateLabels();
                foreach (ZAxis3D axis in zAxes) axis.UpdateLabels();
            }
        }

        internal void UpdateLabelPositions()
        {
            if (layer2D != null)
            {
                foreach (XAxis3D axis in xAxes) axis.UpdateLabelPositions(true);
                foreach (YAxis3D axis in yAxes) axis.UpdateLabelPositions(true);
                foreach (ZAxis3D axis in zAxes) axis.UpdateLabelPositions(true);
            }
        }

        protected void Generate()
        {
            Base.Points.Clear();
            Point3D graphMin = GraphMin;
            Point3D graphMax = GraphMax;
            foreach (XAxis3D axis in xAxes) axis.DeriveTicks();
            foreach (YAxis3D axis in yAxes) axis.DeriveTicks();
            foreach (ZAxis3D axis in zAxes) axis.DeriveTicks();
            UpdateLabels();
            //

            PlusX.Points.Clear();
            AddGridLines(PlusX.Points, Face.PlusX, Ticks.ZTicks, Colors.Gray);
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMax.Z));
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            PlusX.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMax.Z));
            //
            PlusY.Points.Clear();
            AddGridLines(PlusY.Points, Face.PlusY, Ticks.ZTicks, Colors.Gray);
            PlusY.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMin.Z));
            PlusY.Points.Add(new Point3DColor(graphMax.X, graphMax.Y, graphMax.Z));
            PlusY.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            PlusY.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMax.Z));
            //
            MinusX.Points.Clear();
            AddGridLines(MinusX.Points, Face.MinusX, Ticks.ZTicks, Colors.Gray);
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMin.Z));
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMax.Y, graphMax.Z));
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
            MinusX.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMax.Z)); 
            //
            MinusY.Points.Clear();
            AddGridLines(MinusY.Points, Face.MinusY, Ticks.ZTicks, Colors.Gray);
            MinusY.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMin.Z));
            MinusY.Points.Add(new Point3DColor(graphMax.X, graphMin.Y, graphMax.Z));
            MinusY.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMin.Z));
            MinusY.Points.Add(new Point3DColor(graphMin.X, graphMin.Y, graphMax.Z));
            //
            MinusZ.Points.Clear();
            AddGridLines(MinusZ.Points, Face.MinusZ, Ticks.XTicks, Colors.Gray);
            AddGridLines(MinusZ.Points, Face.MinusZ, Ticks.YTicks, Colors.Gray);
            //
            PlusZ.Points.Clear();
            AddBase(graphMin, graphMax);

            RedrawAxesLines();
        }
    }
}
