// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace IronPlot.Plotting3D
{
    public interface IBoundable3D
    {
        Cuboid Bounds
        {
            get;
        }
    }
    
    public struct Cuboid 
    {
        Point3D minimum;
        Point3D maximum;

        public Point3D Minimum
        {
            get { return minimum; }
        }

        public Point3D Maximum
        {
            get { return maximum; }
        }

        public Cuboid(Point3D minimum, Point3D maximum)
        {
            this.minimum = minimum;
            this.maximum = maximum;
        }

        public Cuboid(double xmin, double ymin, double zmin, double xmax, double ymax, double zmax)
        {
            this.minimum = new Point3D(xmin, ymin, zmin);
            this.maximum = new Point3D(xmax, ymax, zmax);
        }

        public bool IsPhysical
        {
            get
            {
                if (((maximum.X - minimum.X) != 0) && ((maximum.Y - minimum.Y) != 0) && ((maximum.Z - minimum.Z) != 0)) return true;
                else return false;
            }
        }

        public static Cuboid Union(Cuboid bounds3D1, Cuboid bounds3D2)
        {
            if (!bounds3D1.IsPhysical) return bounds3D2;
            if (!bounds3D2.IsPhysical) return bounds3D1;
            Point3D minimum1 = bounds3D1.minimum;
            Point3D minimum2 = bounds3D2.minimum;
            Point3D maximum1 = bounds3D1.maximum;
            Point3D maximum2 = bounds3D2.maximum;
            return new Cuboid(Math.Min(minimum1.X, minimum2.X), Math.Min(minimum1.Y, minimum2.Y), Math.Min(minimum1.Z, minimum2.Z),
                Math.Max(maximum1.X, maximum2.X), Math.Max(maximum1.Y, maximum2.Y), Math.Max(maximum1.Z, maximum2.Z));
        }

        /// <summary>
        /// Provides transform that will map one set of bounds to another
        /// </summary>
        public static MatrixTransform3D BoundsMapping(Cuboid sourceBounds, Cuboid destinationBounds)
        {
            Point3D graphMin = sourceBounds.minimum;
            Point3D graphMax = sourceBounds.maximum;
            Point3D modelMin = destinationBounds.minimum;
            Point3D modelMax = destinationBounds.maximum;
            double scaleX = (modelMax.X - modelMin.X) / (graphMax.X - graphMin.X);
            double scaleY = (modelMax.Y - modelMin.Y) / (graphMax.Y - graphMin.Y);
            double scaleZ = (modelMax.Z - modelMin.Z) / (graphMax.Z - graphMin.Z);
            double offX = -graphMin.X * scaleX + modelMin.X;
            double offY = -graphMin.Y * scaleY + modelMin.Y;
            double offZ = -graphMin.Z * scaleZ + modelMin.Z;
            Matrix3D transform = new Matrix3D(scaleX, 0, 0, 0, 0, scaleY, 0, 0,
                0, 0, scaleZ, 0, offX, offY, offZ, 1);
            MatrixTransform3D matrixTransform = new MatrixTransform3D(transform);
            return matrixTransform;
        }
    }
}
