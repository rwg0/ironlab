using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using SharpDX;
using SharpDX.Direct2D1;
using System.Drawing;

namespace IronPlot
{
    public class MarkerGeometriesD2D
    {
        public static Geometry MarkerGeometry(MarkersType markersType, Factory factory, float width, float height)
        {
            Geometry geometry = null;
            switch (markersType)
            {
                case MarkersType.None:
                    break;
                case MarkersType.Square:
                    geometry = new RectangleGeometry(factory, new System.Drawing.RectangleF()
                    {
                        X = 0,
                        Y = 0,
                        Width = width,
                        Height = height
                    });
                    break;
                case MarkersType.Circle:
                    geometry = new EllipseGeometry(factory, new Ellipse()
                    {
                        Point = new System.Drawing.PointF(0, 0),
                        RadiusX = width / 2,
                        RadiusY = height / 2,
                    });
                    break;
                default:
                    GenericMarker markerSpecification = MarkerGeometries.GenericMarkerLookup[markersType];
                    geometry = new PathGeometry(factory);
                    using (GeometrySink sink = (geometry as PathGeometry).Open())
                    {
                        PointF p0 = new PointF((float)markerSpecification.X[0] * width,  (float)markerSpecification.Y[0] * height); 
                        sink.BeginFigure(p0, FigureBegin.Hollow);
                        int n = markerSpecification.X.Length;
                        for (int i = 1; i < n; ++i)
                        {
                            sink.AddLine(new PointF((float)markerSpecification.X[i] * width, (float)markerSpecification.Y[i] * height)); 
                        }
                        sink.EndFigure(FigureEnd.Closed);
                        sink.Close();
                    }
                    break;
            }
            return geometry;
        }
    }
}
