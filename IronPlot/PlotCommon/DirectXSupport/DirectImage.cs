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

        protected SurfaceType surfaceType;
        Texture SharedTexture;

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
        volatile bool isRendering = false;

        private Surface backBufferSurface;
        // The width and height of the backBuffer or Texture
        protected int bufferWidth, bufferHeight;
        // The width and height of the Viewport
        protected int viewportWidth, viewportHeight;
        // The width and height of the image onto which the viewport is mapped
        internal int imageWidth, imageHeight;
        double pixelsPerDIPixel = 1; 
        // and the dpi
        // On render, viewportWidth = pixelsPerDIPixel * imageWidth

        /// <summary>
        /// Indicates whether rendering is required on the next pass.
        /// </summary>
        protected bool renderRequired = false;

        public unsafe DirectImage()
        {
            bufferWidth = 10; bufferHeight = 10;
            imageWidth = 10; imageHeight = 10;
            viewportWidth = bufferWidth; viewportHeight = bufferHeight;
            d3dImage = new D3DImage();
            imageBrush = new ImageBrush(d3dImage);
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
                graphicsDeviceService9 = SharpDXGraphicsDeviceService9.RefToNew(0, 0);
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
                graphicsDeviceService9.DeviceResetting += new EventHandler(graphicsDeviceService_DeviceResetting);
                graphicsDeviceService9.DeviceReset += new EventHandler(graphicsDeviceService_DeviceReset);
                ResetDevice();
            }
            Initialize();
            if (d3dImage.IsFrontBufferAvailable)
            {
                SetBackBuffer();
                //renderRequired = true;
            }
            CompositionTarget.Rendering += OnRendering;
            d3dImage.IsFrontBufferAvailableChanged += new DependencyPropertyChangedEventHandler(OnIsFrontBufferAvailableChanged);
        }

        void graphicsDeviceService10_DeviceResized(object sender, EventArgs e)
        {
            bufferWidth = graphicsDeviceService10.Width;
            bufferHeight = graphicsDeviceService10.Height;
            SetBackBuffer(graphicsDeviceService10.Texture);
            UpdateImageBrush();
        }

        protected unsafe void SetBackBuffer()
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
            else if (surfaceType == SurfaceType.Direct2D) SetBackBuffer(graphicsDeviceService10.Texture); 
        }

        public void SetBackBuffer(SharpDX.Direct3D10.Texture2D Texture)
        {
            if (SharedTexture != null)
            {
                SharedTexture.Dispose();
                SharedTexture = null;
            }

            if (Texture == null)
            {
                if (SharedTexture != null)
                {
                    SharedTexture = null;
                    d3dImage.Lock();
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                    d3dImage.Unlock();
                }
            }
            else if (IsShareable(Texture))
            {
                Format format = TranslateFormat(Texture);
                if (format == Format.Unknown)
                    throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

                IntPtr Handle = GetSharedHandle(Texture);
                if (Handle == IntPtr.Zero)
                    throw new ArgumentNullException("Handle");

                SharedTexture = new Texture(graphicsDeviceService9.GraphicsDevice, Texture.Description.Width, Texture.Description.Height, 1, Usage.RenderTarget, format, Pool.Default, ref Handle);
                using (Surface Surface = SharedTexture.GetSurfaceLevel(0))
                {
                    d3dImage.Lock();
                    d3dImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, Surface.NativePointer);
                    d3dImage.Unlock();
                }
            }
            else
                throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");
        }

        IntPtr GetSharedHandle(SharpDX.Direct3D10.Texture2D Texture)
        {
            SharpDX.DXGI.Resource resource = Texture.QueryInterface<SharpDX.DXGI.Resource>();
            IntPtr result = resource.SharedHandle;
            resource.Dispose();
            return result;
        }

        Format TranslateFormat(SharpDX.Direct3D10.Texture2D Texture)
        {
            switch (Texture.Description.Format)
            {
                case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
                    return SharpDX.Direct3D9.Format.A2B10G10R10;

                case SharpDX.DXGI.Format.R16G16B16A16_Float:
                    return SharpDX.Direct3D9.Format.A16B16G16R16F;

                case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
                    return SharpDX.Direct3D9.Format.A8R8G8B8;

                default:
                    return SharpDX.Direct3D9.Format.Unknown;
            }
        }

        bool IsShareable(SharpDX.Direct3D10.Texture2D Texture)
        {
            return (Texture.Description.OptionFlags & SharpDX.Direct3D10.ResourceOptionFlags.Shared) != 0;
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
            // If the front buffer is available, we need to start rendering our custom scene
            if (d3dImage.IsFrontBufferAvailable)
            {
                SetBackBuffer();
                RenderScene();
                renderRequired = true;
                CompositionTarget.Rendering += OnRendering;
            }
            else
            {
                if (graphicsDeviceService9.UseDeviceEx == false)
                {
                    if (graphicsDeviceService9.GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset)
                    {
                        ResetDevice();
                    }
                    ReleaseBackBuffer();
                }
                CompositionTarget.Rendering -= OnRendering;
            }
        }

        protected virtual void RecreateBuffers()
        {
            // Do nothing in base.
        }

        protected unsafe void OnLoaded(object sender, RoutedEventArgs e)
        {
            //BeginRenderingScene();
            //this.Dispatcher.ShutdownStarted += new EventHandler(Dispatcher_ShutdownStarted);
        }

        //void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        //{
        //    throw new NotImplementedException();
        //}

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

        protected virtual void OnRendering(object sender, EventArgs e)
        {
            if (!isRendering && renderRequired && (Visible == true))
            {
                isRendering = true;
                renderRequired = false;
                RenderScene();
            }
            else return;
            isRendering = false;
        }

        public void RenderScene()
        {
            lock (graphicsDeviceService9)
            {
                d3dImage.Lock();
                string beginDrawError = BeginDraw();
                if (string.IsNullOrEmpty(beginDrawError))
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
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        string BeginDraw()
        {
            // Make sure the graphics device is big enough, and is not lost.

            string deviceResetError = "";
            deviceResetError = HandleDeviceResetAndSizeChanged();

            if (!string.IsNullOrEmpty(deviceResetError))
            {
                return deviceResetError;
            }

            // Multiples GraphicsDeviceControl instances can share the same
            // GraphicsDevice. The device backbuffer will be resized to fit the
            // largest of these controls. But what if we are currently drawing
            // a smaller control? To avoid unwanted stretching, we set the
            // viewport to only use the top left portion of the full backbuffer.
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

            return null;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method, and is responsible for presenting
        /// the finished image onto the ImageBrush.
        /// </summary>
        void EndDraw()
        {
            try
            {
                graphicsDeviceService9.GraphicsDevice.Present();
                //if (result.Name == "S_PRESENT_MODE_CHANGED")
                //{
                //    ResetDevice();
                    //RecreateD3DImage();
                //}
            }
            catch
            {
                // Present might throw if the device became lost while we were
                // drawing. The lost device will be handled by the next BeginDraw,
                // so we just swallow the exception.
            }
        }

        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        string HandleDeviceResetAndSizeChanged()
        {
            bool deviceNeedsReset = false;
            viewportWidth = (int)(Math.Max(imageWidth * 1, 1) * pixelsPerDIPixel);
            viewportHeight = (int)(Math.Max(imageHeight * 1, 1) * pixelsPerDIPixel);

            if (GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceLost)
            {
                return "Graphics device lost";
            }
            else if (GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset)
            {
                deviceNeedsReset = true;
            }
            else
            {
                // If the device state is ok, check whether it is big enough.
                if ((viewportWidth > bufferWidth) || (viewportHeight > bufferHeight))
                {
                    if (surfaceType == SurfaceType.DirectX9) deviceNeedsReset = true;
                    if (surfaceType == SurfaceType.Direct2D)
                    {
                        requestedWidth = (int)(viewportWidth * 1.1);
                        requestedHeight = (int)(viewportHeight * 1.1);
                        graphicsDeviceService10.ResizeDevice(requestedWidth, requestedHeight);
                        if (bufferWidth < requestedWidth)
                        {

                        }
                    }
                }
                if (surfaceType == SurfaceType.Direct2D)
                {
                }
            }
            // Do we need to reset the device?
            if (deviceNeedsReset)
            {
                try
                {
                    ResetDevice();
                }
                catch (Exception e)
                {
                    return "Graphics device reset failed\n\n" + e;
                }
            }
            return null;
        }

        int requestedWidth, requestedHeight;

        protected virtual void ResetDevice()
        {
            if (viewportWidth > bufferWidth) bufferWidth = (int)(viewportWidth * 1.1);
            if (viewportHeight > bufferHeight) bufferHeight = (int)(viewportHeight * 1.1);
            graphicsDeviceService9.ResetDevice(bufferWidth,
                          bufferHeight);
        }

        protected void graphicsDeviceService_DeviceResetting(object sender, EventArgs e)
        {
            ReleaseBackBuffer();
            if (backBufferSurface != null && !backBufferSurface.IsDisposed)
            {
                backBufferSurface.Dispose();
                backBufferSurface = null;
            }
        }

        protected void graphicsDeviceService_DeviceReset(object sender, EventArgs e)
        {
            bufferWidth = graphicsDeviceService9.PresentParameters.BackBufferWidth; 
            bufferHeight = graphicsDeviceService9.PresentParameters.BackBufferHeight;
            SetBackBuffer();
            imageBrush.Viewbox = new Rect(0, 0, (double)viewportWidth / (double)bufferWidth, (double)viewportHeight / (double)bufferHeight);
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
            //renderRequired = true;
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

        [DllImport("user32.dll", SetLastError = false)]
        static extern IntPtr GetDesktopWindow();
    }
}
