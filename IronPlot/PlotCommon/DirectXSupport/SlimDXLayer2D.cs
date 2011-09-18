// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Controls;
using System.Windows;
using System.Text;
using SlimDX;
using SlimDX.Direct3D9;
using IronPlot.ManagedD3D;
namespace IronPlot.Plotting3D
{
    public class SlimDXLayer2D : I2DLayer
    {
        protected Canvas canvas;

        public Canvas Canvas
        {
            get { return canvas; }
            set { value = canvas; }
        }

        public SlimDXLayer2D(Canvas canvas, ViewportImage ViewportImage, MatrixTransform3D ModelToWorld)
        {
            this.canvas = canvas;
            this.ModelToWorld = ModelToWorld;
            this.ViewportImage = ViewportImage;
        }

        internal MatrixTransform3D ModelToWorld;

        internal ViewportImage ViewportImage;

        public System.Windows.Point CanvasPointFrom3DPoint(Point3D modelPoint3D)
        {
            Point3D worldPoint = ViewportImage.ModelToWorld.Transform(modelPoint3D);
            SlimDX.Vector3 worldPointSlimDX = new SlimDX.Vector3((float)worldPoint.X, (float)worldPoint.Y, (float)worldPoint.Z);
            float width = (float)ViewportImage.imageWidth;
            float height = (float)ViewportImage.imageHeight;
            SlimDX.Matrix trans = SlimDX.Matrix.Multiply(ViewportImage.View, ViewportImage.Projection);
            //Vector3 point2DSlimDX = Vector3.Project(worldPointSlimDX, 0, 0, ViewportImage.Width, ViewportImage.Height, 0.0f, 1.0f, trans);
            Vector3 point2DSlimDX = Vector3.Project(worldPointSlimDX, 0, 0, width, height, 0.0f, 1.0f, trans);
            return new System.Windows.Point((double)(point2DSlimDX.X), (double)(point2DSlimDX.Y));
        }
    }
}
