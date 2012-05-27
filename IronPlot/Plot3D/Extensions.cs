// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows;

namespace IronPlot.ManagedD3D
{
    public static class Extensions
    {
        public static SharpDX.Vector3 ToVector3(this SharpDX.Vector4 vector4)
        {
            return new SharpDX.Vector3(vector4.X, vector4.Y, vector4.Z);
        }
    }
}

namespace IronPlot
{
    public static class Extensions
    {
        public static Vector ToVector(this Point point)
        {
            return new Vector(point.X, point.Y);
        }
    }

    public struct Line
    {
        public Vector PointOnLine;
        public Vector Direction;

        public Line(Vector pointOnLine, Vector direction)
        {
            this.PointOnLine = pointOnLine;
            this.Direction = direction;
        }

        public Vector GetNormalDirection()
        {
            return new Vector(Direction.Y, -Direction.X);
        }

        public Vector IntersectionWithLine(Line line)
        {
            Vector a = this.Direction;
            Vector b = line.Direction;
            Vector c = line.PointOnLine - this.PointOnLine;
            double lamdba = Vector.CrossProduct(c, b) / Vector.CrossProduct(a, b);
            return this.PointOnLine + lamdba * this.Direction;
        }
    }

    public static class Geomery
    {
        public static double PointToLineDistance(Vector point, Line line)
        {
            Vector normal = line.GetNormalDirection();
            normal.Normalize();
            return Vector.Multiply(normal, line.PointOnLine - point);
        }
    }
}
