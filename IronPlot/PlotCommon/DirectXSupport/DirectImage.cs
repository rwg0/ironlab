// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows.Forms;
using System.Reflection;
using System.Windows.Interop;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace IronPlot
{
    public enum SurfaceType { DirectX9, Direct2D };

    public enum ResizeResetResult { OK, D3D9DeviceLost, D3D9ResetFailed };
    
    /// <summary>
    /// Class uses SharpDX to render onto an ImageBrush using D3DImage,
    /// and optionally onto a Canvas that can be overlaid for 2D vector annotation.
    /// Base contains just basic rendering capability.
    /// Rendering is either Direct3D9 for 3D or Direct2D for 2D, controlled by surfaceType field.
    /// </summary>
    public class DirectImage : DependencyObject
    {
        public static readonly DependencyProperty VisibleProperty =
            DependencyProperty.Register("VisibleProperty",
            typeof(bool), typeof(DirectImage),
            new FrameworkPropertyMetadata(true, OnVisiblePropertyChanged));

        internal bool Visible
        {
            set { SetValue(VisibleProperty, value); }
            get { return (bool)GetValue(VisibleProperty); }
        }

        public event EventHandler RenderRequested;

        protected SurfaceType surfaceType;
        //Texture SharedTexture;

        // All  GraphicsDevices, managed by this helper service.
        protected SharpDXGraphicsDeviceService9 graphicsDeviceService9;
        // And this one for Direct3D10 (needed for Direct2D)
        protected SharpDXGraphicsDeviceService10 graphicsDeviceService10;
        //DeviceEx graphicsDeviceTemp;
        // The D3DImage...
        internal D3DImage d3dImage;
        // and derived ImageBrush
        protected ImageBrush imageBrush;
        // Canvas for optional overlaying of vector graphics
        Canvas canvas = null;

        bool afterResizeReset = false;

        private Surface backBufferSurface;

        // The width and height of the backBuffer or Textures
        protected int bufferWidth;
        public int BufferWidth { get { return bufferWidth; } }
        protected int bufferHeight;
        public int BufferHeight { get { return bufferWidth; } }

        // The width and height of the Viewport
        protected int viewportWidth;
        public int ViewportWidth { get { return viewportWidth; } }
        protected int viewportHeight;
        public int ViewportHeight { get { return viewportHeight; } }
        
        // The width and height of the image onto which the viewport is mapped
        internal int imageWidth, imageHeight;
        double pixelsPerDIPixel = 1; 
        // and the dpi
        // On render, viewportWidth = pixelsPerDIPixel * imageWidth

        public unsafe DirectImage()
        {
            bufferWidth = 10; bufferHeight = 10;
            imageWidth = 10; imageHeight = 10;
            viewportWidth = bufferWidth; viewportHeight = bufferHeight;
            RecreateD3DImage(false);
        }

        public void RecreateD3DImage(bool setBackBuffer)
        {
            if (d3dImage != null) d3dImage.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
            d3dImage = new D3DImage();
            imageBrush = new ImageBrush(d3dImage);
            d3dImage.IsFrontBufferAvailableChanged += new DependencyPropertyChangedEventHandler(OnIsFrontBufferAvailableChanged);
            if (setBackBuffer) SetBackBuffer();
        }

        protected virtual void OnVisiblePropertyChanged(bool isVisible)
        {
        }

        protected static void OnVisiblePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)(e.NewValue)) ((DirectImage)obj).OnVisiblePropertyChanged(true); else ((DirectImage)obj).OnVisiblePropertyChanged(false);
        }

        // Create Device(s) and start rendering.
        protected virtual unsafe void CreateDevice(SurfaceType surfaceType)
        {
            this.surfaceType = surfaceType;
            if (surfaceType == SurfaceType.Direct2D)
            {
                // Use shared devices resources.
                graphicsDeviceService10 = SharpDXGraphicsDeviceService10.AddRef();
                graphicsDeviceService10.DeviceResized += new EventHandler(graphicsDeviceService10_DeviceResized);
                graphicsDeviceService10.ResizeDevice(bufferWidth, bufferHeight);
                bufferWidth = graphicsDeviceService10.Width;
                bufferHeight = graphicsDeviceService10.Height;
            }
            else if (surfaceType == SurfaceType.DirectX9)
            {
                // Use shared devices resources.
                graphicsDeviceService9 = SharpDXGraphicsDeviceService9.AddRef(bufferWidth, bufferHeight);
                graphicsDeviceService9.DeviceReset += new EventHandler(graphicsDeviceService9_DeviceReset);
                afterResizeReset = true;
            }
            Initialize();
            if (d3dImage.IsFrontBufferAvailable)
            {
                SetBackBuffer();
            }
            //CompositionTarget.Rendering += OnRendering;
        }

        internal unsafe void SetBackBuffer()
        {
            if (surfaceType == SurfaceType.DirectX9)
            {
                d3dImage.Lock();
                using (backBufferSurface = GraphicsDevice.GetBackBuffer(0, 0))
                {
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, backBufferSurface.NativePointer);
                }
                d3dImage.Unlock();
            }
            else if (surfaceType == SurfaceType.Direct2D) SetBackBuffer(graphicsDeviceService10.Texture9); 
        }

        public void SetBackBuffer(SharpDX.Direct3D9.Texture texture)
        {
            if (texture == null) ReleaseBackBuffer();
            else
            {
                using (Surface Surface = texture.GetSurfaceLevel(0))
                {
                    d3dImage.Lock();
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, Surface.NativePointer);
                    d3dImage.Unlock();
                }
            }
        }

        protected unsafe void ReleaseBackBuffer()
        {
            d3dImage.Lock();
            d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            d3dImage.Unlock();
        }

        /// <summary>
        /// Gets a GraphicsDevice that can be used to draw into the 3D space.
        /// </summary>
        public Device GraphicsDevice
        {
            get { return graphicsDeviceService9.GraphicsDevice; }
        }

        /// <summary>
        /// Gets a 2D RenderTarget.
        /// </summary>
        public SharpDX.Direct2D1.RenderTarget RenderTarget
        {
            get { return graphicsDeviceService10.RenderTarget; }
        }

        /// <summary>
        /// Gets a GraphicsDeviceService.
        /// </summary>
        public SharpDXGraphicsDeviceService9 GraphicsDeviceService
        {
            get { return graphicsDeviceService9; }
        }

        public int Width
        {
            get { return viewportWidth; }
        }

        public int Height
        {
            get { return viewportHeight; }
        }

        /// <summary>
        /// Gets or sets the Canvas for vector overlays
        /// </summary>
        public Canvas Canvas
        {
            get { return canvas; }
            set { canvas = value; }
        }

        /// <summary>
        /// Gets an ImageBrush of the Viewport 
        /// </summary>
        public ImageBrush ImageBrush
        {
            get { return imageBrush; }
        }

        private unsafe void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // If the front buffer becomes unavailable, the D3DImage is discarded. This is for robustness since in
            // some situations the front buffer seems never to become available again.
            if (d3dImage.IsFrontBufferAvailable)
            {
                ReleaseBackBuffer();
                SetBackBuffer();
                RenderScene();
                if (surfaceType == SurfaceType.DirectX9) ResetDevice();
            }
            else
            {
                imageBrush = null;
                d3dImage.IsFrontBufferAvailableChanged -= OnIsFrontBufferAvailableChanged;
                d3dImage = null;
                if (surfaceType == SurfaceType.DirectX9 && graphicsDeviceService9.UseDeviceEx == false)
                {
                    if (graphicsDeviceService9.GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset)
                    {
                        ResetDevice();
                    }
                    ReleaseBackBuffer();
                }
            }
        }

        /// <summary>
        /// Disposes the control.
        /// </summary>
        protected void Dispose(bool disposing)
        {
            if (graphicsDeviceService9 != null)
            {
                graphicsDeviceService9.Release(disposing);
                graphicsDeviceService9 = null;
            }
            if (graphicsDeviceService10 != null)
            {
                graphicsDeviceService10.Release(disposing);
                graphicsDeviceService10 = null;
            }
        }

        object locker = new object();

        public void RequestRender()
        {
            if (RenderRequested != null) RenderRequested(this, EventArgs.Empty);
        }

        public void RequestRender(object sender, EventArgs e)
        {
            if (RenderRequested != null) RenderRequested(sender, e);
        }

        public void RenderScene()
        {
            if (d3dImage == null) return;
            d3dImage.Lock();
            if (BeginDraw())
            {
                // Draw is overriden in derived classes
                Draw();
                EndDraw();
            }
            if (d3dImage.IsFrontBufferAvailable)
            {
                d3dImage.AddDirtyRect(new Int32Rect(0, 0, viewportWidth, viewportHeight));
                d3dImage.Unlock();
            }
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns false if this was not possible.
        /// </summary>
        private bool BeginDraw()
        {
            // Make sure the graphics device is big enough, and is not lost.
            if (HandleDeviceResetAndSizeChanged() != ResizeResetResult.OK) return false;

            // Multiple DirectImage instances can share the same device. The viewport is adjusted
            // for when a smaller DirectImage is rendering.
            if (surfaceType == SurfaceType.DirectX9)
            {
                Viewport viewport = new Viewport();
                viewport.X = 0;
                viewport.Y = 0;
                viewport.Width = viewportWidth;
                viewport.Height = viewportHeight;
                viewport.MinZ = 0;
                viewport.MaxZ = 1;
                GraphicsDevice.Viewport = viewport;
            }

            if (surfaceType == SurfaceType.Direct2D)
            {
                var viewport2D = new SharpDX.Direct3D10.Viewport();
                viewport2D.Height = viewportHeight;
                viewport2D.MaxDepth = 1;
                viewport2D.MinDepth = 0;
                viewport2D.TopLeftX = 0;
                viewport2D.TopLeftY = 0;
                viewport2D.Width = viewportWidth;
                graphicsDeviceService10.SetViewport(viewport2D);
            }
            return true;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method.
        /// </summary>
        private void EndDraw()
        {
            if (surfaceType == SurfaceType.DirectX9) 
            {
                try
                {
                    graphicsDeviceService9.GraphicsDevice.Present();
                }
                catch
                { 
                    // Present might throw if the device became lost while we were
                    // drawing. The lost device will be handled by the next BeginDraw,
                    // so we just swallow the exception.
                }
            }
        }

        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost.
        /// </summary>
        private ResizeResetResult HandleDeviceResetAndSizeChanged()
        {
            viewportWidth = (int)(Math.Max(imageWidth * 1, 1) * pixelsPerDIPixel);
            viewportHeight = (int)(Math.Max(imageHeight * 1, 1) * pixelsPerDIPixel);
            if (surfaceType == SurfaceType.DirectX9)
            {
                if (GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceLost) return ResizeResetResult.D3D9DeviceLost;
                bool reset = false;
                try
                {
                    reset = graphicsDeviceService9.ResetIfNecessary();
                    bufferWidth = graphicsDeviceService9.PresentParameters.BackBufferWidth;
                    bufferHeight = graphicsDeviceService9.PresentParameters.BackBufferHeight;
                }
                catch
                {
                    return ResizeResetResult.D3D9ResetFailed;
                }
                if (afterResizeReset || reset)
                {
                    AfterResizeReset();
                    afterResizeReset = false;
                }
            }
            else if (surfaceType == SurfaceType.Direct2D)
            {
                if ((viewportWidth > bufferWidth) || (viewportHeight > bufferHeight))
                {
                    requestedWidth = (int)(viewportWidth * 1.1);
                    requestedHeight = (int)(viewportHeight * 1.1);
                    graphicsDeviceService10.ResizeDevice(requestedWidth, requestedHeight);
                    AfterResizeReset();
                }
                else if (afterResizeReset) AfterResizeReset();
            }
            return ResizeResetResult.OK;
        }

        int requestedWidth, requestedHeight;

        protected virtual void ResetDevice()
        {
            //if (viewportWidth > bufferWidth) bufferWidth = (int)(viewportWidth * 1.1);
            //if (viewportHeight > bufferHeight) bufferHeight = (int)(viewportHeight * 1.1);
            //graphicsDeviceService9.RatchetResetDevice(bufferWidth,
            //              bufferHeight);
            //graphicsDeviceService9.ResetIfNecessary();
        }

        protected void graphicsDeviceService9_DeviceReset(object sender, EventArgs e)
        {
            afterResizeReset = true;
        }

        void graphicsDeviceService10_DeviceResized(object sender, EventArgs e)
        {
            afterResizeReset = true;
        }

        private void AfterResizeReset()
        {
            if (surfaceType == SurfaceType.DirectX9)
            {
                bufferWidth = graphicsDeviceService9.PresentParameters.BackBufferWidth;
                bufferHeight = graphicsDeviceService9.PresentParameters.BackBufferHeight;
                SetBackBuffer();
                imageBrush.Viewbox = new Rect(0, 0, (double)viewportWidth / (double)bufferWidth, (double)viewportHeight / (double)bufferHeight);
            }
            else
            {
                bufferWidth = graphicsDeviceService10.Width;
                bufferHeight = graphicsDeviceService10.Height;
                SetBackBuffer(graphicsDeviceService10.Texture9);
                UpdateImageBrush();
            }
        }

        public void OnSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            SetImageSize((int)e.NewSize.Width, (int)e.NewSize.Height, 96);
        }

        internal void SetImageSize(int width, int height, int dpi)
        {
            imageWidth = width;
            imageHeight = height;
            pixelsPerDIPixel = (double)dpi / 96.0;
            UpdateImageBrush();
        }

        private void UpdateImageBrush()
        {
            imageBrush.Viewbox = new Rect(0, 0, (double)imageWidth * pixelsPerDIPixel / (double)bufferWidth, (double)imageHeight * pixelsPerDIPixel / (double)bufferHeight);
            imageBrush.ViewportUnits = BrushMappingMode.RelativeToBoundingBox;
            imageBrush.TileMode = TileMode.None;
            imageBrush.Stretch = Stretch.Fill;
            imageBrush.SetValue(RenderOptions.BitmapScalingModeProperty, BitmapScalingMode.HighQuality);
        }

        protected virtual void Initialize()
        {
            // Do nothing in base 
        }
    
        /// <summary>
        /// Derived classes override this to draw themselves using the GraphicsDevice.
        /// </summary>
        protected virtual void Draw()
        {
            // Do nothing in base
        }

        internal void RegisterWithService()
        {
            if (surfaceType == SurfaceType.Direct2D)
                graphicsDeviceService10.Tracker.Register(this);
            else 
                graphicsDeviceService9.Tracker.Register(this);
        }

        internal void UnregisterWithService()
        {
            if (surfaceType == SurfaceType.Direct2D)
                graphicsDeviceService10.Tracker.Unregister(this);
            else 
                graphicsDeviceService9.Tracker.Unregister(this);
        }

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();
    }
}
