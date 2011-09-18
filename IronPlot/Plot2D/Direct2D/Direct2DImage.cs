using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SlimDX;
using SlimDX.Direct2D;

namespace IronPlot
{
    public class Direct2DImage : DirectImage
    {
        internal List<DirectPath> paths;
        
        public Direct2DImage()
            : base()
        {
            CreateDevice(SurfaceType.Direct2D);
            paths = new List<DirectPath>();
        }

        protected override void Initialize()
        {
            
        }

        public void RequestRender()
        {
            lock (this)
            {
                renderRequired = true;
            }
        }

        protected override void Draw()
        {
            RenderTarget.BeginDraw();
            //RenderTarget.Transform = Matrix3x2.Identity;
            RenderTarget.Clear(new Color4(0.0f, 0.5f, 0.5f, 0.5f));
            RenderTarget.AntialiasMode = AntialiasMode.Aliased;
            StrokeStyleProperties properties = new StrokeStyleProperties();
            properties.LineJoin = LineJoin.MiterOrBevel;
            StrokeStyle strokeStyle = new StrokeStyle(RenderTarget.Factory, properties);
            foreach (DirectPath path in paths)
            {
                if (path.Geometry != null && path.Brush != null)
                {
                    if (path is DirectPathScatter)
                    {
                        (path as DirectPathScatter).RenderScatterGeometry(RenderTarget);
                    }
                    else
                    {
                        //RenderTarget.FillGeometry(path.Geometry, path.Brush);
                        if (path.QuickStrokeDash != QuickStrokeDash.None)
                        {
                            RenderTarget.DrawGeometry(path.Geometry, path.Brush, (float)path.StrokeThickness, strokeStyle);
                        }
                    }
                }
            }
            RenderTarget.EndDraw();
            graphicsDeviceService10.CopyTextureAcross();
            graphicsDeviceService10.Device.Flush();
        }
    }
}
