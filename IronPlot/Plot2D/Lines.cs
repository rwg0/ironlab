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

namespace IronPlot
{
    public class LinePoints
    {
        public double X1;
        public double Y1;
        public bool Move;
        public bool Closed;

        public LinePoints(double x1, double y1, bool move, bool closed)
        {
            X1 = x1; Y1 = y1; Move = move; Closed = closed;
        }
    }

    public class Lines : List<LinePoints>
    {
        protected Coordinates coordinates;

        public Lines(Coordinates coordinates)
        {
            this.coordinates = coordinates;
        }

        public Lines()
        {
        }

        public void Add(double x1, double y1, double x2, double y2)
        {
            this.Add(new LinePoints(x1, y1, true, false));
            this.Add(new LinePoints(x2, y2, false, false));
        }

        public void Add(double x1, double y1, double x2, double y2, bool closed)
        {
            this.Add(new LinePoints(x1, y1, true, closed));
            this.Add(new LinePoints(x2, y2, false, closed));
        }

        public void Add(Point p1, Point p2)
        {
            this.Add(new LinePoints(p1.X, p1.Y, false, false));
            this.Add(new LinePoints(p2.X, p2.Y, false, false));
        }

        public void Add(double x2, double y2)
        {
            this.Add(new LinePoints(x2, y2, false, false));
        }

        public void Add(Point p2)
        {
            this.Add(new LinePoints(p2.X, p2.Y, false, false));
        }

        public void AddNormal(double x1n, double y1n, double x2n, double y2n)
        {
            double x1 = x1n * coordinates.CanvasWidth ;
            double y1 = (1 - y1n) * coordinates.CanvasHeight ;
            double x2 = x2n * coordinates.CanvasWidth ;
            double y2 = (1 - y2n) * coordinates.CanvasHeight ;
            this.Add(new LinePoints(x1, y1, true, false));
            this.Add(new LinePoints(x2, y2, false, false));
        }

        public void AddToGridFromGraph(double x1g, double y1g, double x2g, double y2g)
        {
            double x1n = (x1g - coordinates.GraphBottomLeft.X) / coordinates.GraphWidth;
            double x2n = (x2g - coordinates.GraphBottomLeft.X) / coordinates.GraphWidth;
            double y1n = (y2g - coordinates.GraphBottomLeft.Y) / coordinates.GraphHeight;
            double y2n = (y1g - coordinates.GraphBottomLeft.Y) / coordinates.GraphHeight;
            double x1 = x1n * coordinates.CanvasWidth + coordinates.CanvasTopLeft.X;
            double y1 = (1 - y1n) * coordinates.CanvasHeight + coordinates.CanvasTopLeft.Y;
            double x2 = x2n * coordinates.CanvasWidth + coordinates.CanvasTopLeft.X;
            double y2 = (1 - y2n) * coordinates.CanvasHeight + coordinates.CanvasTopLeft.Y;
            this.Add(new LinePoints(x1, y1, true, false));
            this.Add(new LinePoints(x2, y2, false, false));
        }

        public void AddNormal(double x2n, double y2n)
        {
            double x2 = x2n * coordinates.CanvasWidth;
            double y2 = (1 - y2n) * coordinates.CanvasHeight;
            this.Add(new LinePoints(x2, y2, false, false));
        }

        protected Point PointFromNormal(double x, double y)
        {
            x = x * coordinates.CanvasWidth + coordinates.CanvasTopLeft.X;
            y = y * coordinates.CanvasHeight + coordinates.CanvasTopLeft.Y;
            return new Point(x, y);
        }

        protected Point CanvasPointFromGraph(double xg, double yg)
        {
            double xn = (xg - coordinates.GraphBottomLeft.X) / coordinates.GraphWidth;
            double yn = (yg - coordinates.GraphBottomLeft.Y) / coordinates.GraphHeight;
            double x = xn * coordinates.CanvasWidth + coordinates.CanvasTopLeft.X;
            double y = (1 - yn) * coordinates.CanvasHeight + coordinates.CanvasTopLeft.Y;
            return new Point(x, y);
        }

        public void AddToPathGeometry(ref PathGeometry pathGeometry)
        {
            PathFigure pathFigure;
            LineSegment lineSegment;
            pathFigure = new PathFigure();
            pathFigure.StartPoint = new Point(this[0].X1, this[0].Y1);
            lineSegment = new LineSegment();
            for (int i = 0; i < this.Count-1; ++i)
            {
                lineSegment.Point = new Point(this[i+1].X1, this[i+1].Y1);
                pathFigure.Segments.Add(lineSegment);
                pathGeometry.Figures.Add(pathFigure);
                pathFigure.IsClosed = false;
            }
        }

        public void AddToDrawingContext(ref DrawingContext drawingContext, out Pen pen)
        {
            pen = new Pen();
            pen.Thickness = 5;
            pen.Brush = Brushes.Black;
            pen.LineJoin = PenLineJoin.Round;
            //pen.Freeze();
            for (int i = 0; i < this.Count-1; ++i)
            {
                drawingContext.DrawLine(pen, new Point(this[i].X1, this[i].Y1), new Point(this[i+1].X1, this[i+1].Y1));
            }
        }

        public StreamGeometry AsStreamGeometry()
        {
            StreamGeometry streamGeometry = new StreamGeometry();
            StreamGeometryContext context = streamGeometry.Open();
            AddToStreamGeometryContext(context);
            context.Close();
            return streamGeometry;
        }
        
        public void AddToStreamGeometryContext(StreamGeometryContext streamGeometryContext)
        {
            for (int i = 0; i < this.Count; ++i)
            {
                if (this[i].Move)
                {
                    streamGeometryContext.BeginFigure(new Point(this[i].X1, this[i].Y1), false, this[i].Closed);
                }
                else
                {
                    streamGeometryContext.LineTo(new Point(this[i].X1, this[i].Y1), true, false);
                }
            }
        }
    }

    public class Coordinates
    {
        // This is always (0, 0), but added in case needed later
        public Point CanvasTopLeft;
        public Point GraphBottomLeft;
        public Point VisibleGraphBottomLeft;
        public double CanvasWidth, CanvasHeight;
        public double GraphWidth, GraphHeight;
        public double VisibleGraphWidth, VisibleGraphHeight;
        public double CanvasXFromGraphX(double x)
        {
            double xn = (x - GraphBottomLeft.X) / GraphWidth;
            return xn * CanvasWidth + CanvasTopLeft.X;
        }
        public double CanvasYFromGraphY(double y)
        {
            double yn = (y - GraphBottomLeft.Y) / GraphHeight;
            return (1 - yn) * CanvasHeight + CanvasTopLeft.Y;
        }
    }
}
