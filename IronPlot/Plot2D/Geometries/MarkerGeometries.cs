using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace IronPlot
{
    public struct GenericMarker
    {
        public double[] X;
        public double[] Y;
    }

    public enum MarkersType { None, Square, Circle, TrianglePointDown, TrianglePointUp, Diamond, Star };

    public class MarkerGeometries
    {
        public static Dictionary<MarkersType, GenericMarker> GenericMarkerLookup = new Dictionary<MarkersType, GenericMarker>();

        static MarkerGeometries()
        {
            double cos30 = Math.Cos(30.0 * Math.PI / 180.0);
            double cos45 = Math.Cos(45.0 * Math.PI / 180.0);
            
            GenericMarkerLookup.Add(MarkersType.TrianglePointDown,
                new GenericMarker() { X = new double[] { 0, 0.5, -0.5 }, Y = new double[] { 0.5 / cos30, -(1 - 0.5 / cos30), -(1 - 0.5 / cos30) } });
            
            GenericMarkerLookup.Add(MarkersType.TrianglePointUp,
                new GenericMarker() { X = new double[] { 0, 0.5, -0.5 }, Y = new double[] { -0.5 / cos30, 1 - 0.5 / cos30, 1 - 0.5 / cos30 } });
            
            GenericMarkerLookup.Add(MarkersType.Diamond,
                new GenericMarker() { X = new double[] { 0, cos45, 0, -cos45 }, Y = new double[] { cos45, 0, -cos45, 0 } });
            
            // Star geometry (5-pointed star):
            double[] x = new double[10]; double[] y = new double[10];
            double bluntness = 0.4; double r = 1 / (1 + Math.Cos(72 * Math.PI / 180.0));
            // 0.1 is sharp, 0.9 is blunt!
            for (int i = 0; i < 10; ++i)
            {
                
                x[i] = Math.Sin(36 * i * Math.PI / 180.0) * r;
                y[i] = -Math.Cos(36 * i * Math.PI / 180.0) * r;
                if (i % 2 == 1)
                {
                    x[i] *= bluntness; y[i] *= bluntness; 
                }
            }
            GenericMarkerLookup.Add(MarkersType.Star,
                new GenericMarker() { X = x, Y = y });
        }
        
        internal static Geometry LegendMarkerGeometry(MarkersType markersType, double markersSize)
        {
            return LegendMarkerGeometry(markersType, markersSize, markersSize);
        }

        internal static Geometry LegendMarkerGeometry(MarkersType markersType, double width, double height)
        {
            Geometry legendMarkerGeometry = null;
            switch (markersType)
            {
                case MarkersType.None:
                    break;
                case MarkersType.Square:
                    legendMarkerGeometry = MarkerGeometries.RectangleMarker(width, height, new Point(0, 0));
                    break;
                case MarkersType.Circle:
                    legendMarkerGeometry = MarkerGeometries.EllipseMarker(width, height, new Point(0, 0));
                    break;
                default:
                    legendMarkerGeometry = GetGenericGeometry(markersType, width, height);
                    break;
            }
            return legendMarkerGeometry;
        }

        internal static Geometry MarkersAsGeometry(Curve curve, MatrixTransform graphToCanvas, MarkersType markersType, double markersSize)
        {
            double xScale = graphToCanvas.Matrix.M11;
            double xOffset = graphToCanvas.Matrix.OffsetX;
            double yScale = graphToCanvas.Matrix.M22;
            double yOffset = graphToCanvas.Matrix.OffsetY;
            GeometryGroup markers = new GeometryGroup();
            double width = Math.Abs(markersSize);
            double height = Math.Abs(markersSize);
            Geometry markerGeometry = LegendMarkerGeometry(markersType, markersSize);
            if (markerGeometry == null) return null;
            markerGeometry.Freeze();
            for (int i = 0; i < curve.xTransformed.Length; ++i)
            {
                if (!curve.includeMarker[i]) continue;
                double xCanvas = curve.xTransformed[i] * xScale + xOffset;
                double yCanvas = curve.yTransformed[i] * yScale + yOffset;
                Geometry newMarker = markerGeometry.Clone();
                newMarker.Transform = new TranslateTransform(xCanvas, yCanvas);
                markers.Children.Add(newMarker);
            }
            markers.Freeze();
            return markers;
        }

        public static Geometry RectangleMarker(double width, double height, Point centre)
        {
            return new RectangleGeometry(new Rect(centre.X - width / 2, centre.Y - height / 2, width, height));
        }

        public static Geometry EllipseMarker(double width, double height, Point centre)
        {
            return new EllipseGeometry(new Rect(centre.X - width / 2, centre.Y - height / 2, width, height));
        }

        public static Geometry StringMarker(string markerString, double points)
        {
            FormattedText text = new FormattedText(markerString,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Tahoma"),
                points * 96.0 / 72.0 * 2,
                Brushes.Black);
            text.TextAlignment = TextAlignment.Center;
            text.Trimming = TextTrimming.CharacterEllipsis;
            Geometry geometry = text.BuildGeometry(new Point(0, 0));
            double height = text.Extent;
            geometry = text.BuildGeometry(new Point(0, -height / 2));
            return geometry;
        }

        public static Geometry GetGenericGeometry(MarkersType markersType, double width, double height)
        {
            StreamGeometry geometry = new StreamGeometry();
            GenericMarker markerSpecification = GenericMarkerLookup[markersType];
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(markerSpecification.X[0] * width, markerSpecification.Y[0] * height), false /* is filled */, true /* is closed */);
                int n = markerSpecification.X.Length;
                for (int i = 1; i < n; ++i)
                {
                    ctx.LineTo(new Point(markerSpecification.X[i] * width, markerSpecification.Y[i] * height), true /* is stroked */, false /* is smooth join */);
                }
            }
            return geometry;
        }
    }
}
