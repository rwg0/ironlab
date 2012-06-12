using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using SharpDX;
using SharpDX.Direct2D1;

namespace IronPlot
{
    public class Direct2DImage : DirectImage
    {
        internal List<DirectPath> paths;

        System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer(); 

        public Direct2DImage()
            : base()
        {
            CreateDevice(SurfaceType.Direct2D);
            paths = new List<DirectPath>();
            timer.Interval = TimeSpan.FromSeconds(0.1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            //if (!this.d3dImage.IsFrontBufferAvailable)
            //{
            //
            //}
        }

        protected override void Initialize()
        {
            
        }

        protected override void Draw()
        {
            RenderTarget.BeginDraw();
            RenderTarget.Transform = Matrix3x2.Identity;
            Random random = new Random();
            RenderTarget.Clear(new Color4(0.5f, 0.5f, 0.5f, 0.0f));
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
