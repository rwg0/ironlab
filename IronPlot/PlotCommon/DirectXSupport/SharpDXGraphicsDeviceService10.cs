// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SharpDX.DXGI;
using SharpDX.Direct3D10;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Windows.Threading;

namespace IronPlot
{
    public class SharpDXGraphicsDeviceService10
    {
        // Singleton device service instance.
        static SharpDXGraphicsDeviceService10 singletonInstance;

        public event EventHandler DeviceResized;

        // Keep track of how many controls are sharing the singletonInstance.
        static int referenceCount;
        Factory factoryDXGI;
        SharpDX.Direct2D1.Factory factory2D;
        Texture2D texture;
        Texture2D shareableTexture;
        //RenderTargetView renderTargetView;
        SharpDX.Direct2D1.RenderTarget renderTarget;
        int width;
        int height;

        public Texture2D Texture { get { return shareableTexture; } }
        public SharpDX.Direct2D1.RenderTarget RenderTarget { get { return renderTarget; } }
        public SharpDX.Direct3D10.Device1 Device { get { return device; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        //SharpDX.Direct3D10.Device device;
        SharpDX.Direct3D10.Device1 device;

        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static SharpDXGraphicsDeviceService10 AddRef()
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                singletonInstance = new SharpDXGraphicsDeviceService10();
            }
            return singletonInstance;
        }

        /// <summary>
        /// Create a new GraphicsDeviceService and return a reference to it.
        /// </summary>
        public static SharpDXGraphicsDeviceService10 RefToNew()
        {
            return new SharpDXGraphicsDeviceService10();
        }

        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        SharpDXGraphicsDeviceService10()
        {
            factoryDXGI = new Factory();
            factory2D = new SharpDX.Direct2D1.Factory();
            // Try to create a hardware device first and fall back to a
            // software (WARP doens't let us share resources)
            var device1 = TryCreateDevice1(SharpDX.Direct3D10.DriverType.Hardware);
            if (device1 == null)
            {
                device1 = TryCreateDevice1(SharpDX.Direct3D10.DriverType.Software);
                if (device1 == null)
                {
                    throw new Exception("Unable to create a DirectX 10 device.");
                }
            }
            RasterizerStateDescription rastDesc = new RasterizerStateDescription();
            rastDesc.CullMode = CullMode.Back;
            rastDesc.FillMode = FillMode.Solid;
            rastDesc.IsMultisampleEnabled = false;
            rastDesc.IsAntialiasedLineEnabled = false;
            device1.Rasterizer.State = new RasterizerState(device1, rastDesc);
            this.device = device1;
        }

        private SharpDX.Direct3D10.Device1 TryCreateDevice1(SharpDX.Direct3D10.DriverType type)
        {
            // We'll try to create the device that supports any of these feature levels
            SharpDX.Direct3D10.FeatureLevel[] levels =
            {
                SharpDX.Direct3D10.FeatureLevel.Level_10_1,
                SharpDX.Direct3D10.FeatureLevel.Level_10_0,
                SharpDX.Direct3D10.FeatureLevel.Level_9_3,
                SharpDX.Direct3D10.FeatureLevel.Level_9_2,
                SharpDX.Direct3D10.FeatureLevel.Level_9_1
            };

            foreach (var level in levels)
            {
                try
                {
                    //var device = new SharpDX.Direct3D10.Device1(factoryDXGI.GetAdapter(0), DeviceCreationFlags.BgraSupport, level);
                    var device = new SharpDX.Direct3D10.Device1(type, DeviceCreationFlags.BgraSupport, level);
                    return device;
                }
                catch (ArgumentException) // E_INVALIDARG
                {
                    continue; // Try the next feature level
                }
                catch (OutOfMemoryException) // E_OUTOFMEMORY
                {
                    continue; // Try the next feature level
                }
                catch (Exception) // SharpDX.Direct3D10.Direct3D10Exception D3DERR_INVALIDCALL or E_FAIL
                {
                    continue; // Try the next feature level
                }
            }
            return null; // We failed to create a device at any required feature level
        }

        public void ResizeDevice(int width, int height)
        {
            lock (this)
            {
                if (width < 0)
                {
                    throw new ArgumentOutOfRangeException("width", "Value must be positive.");
                }
                if (height < 0)
                {
                    throw new ArgumentOutOfRangeException("height", "Value must be positive.");
                }
                if ((width <= this.width) && (height <= this.height))
                {
                    return;
                }

                // Recreate the render target
                // Assign result to temporary variable in case CreateTexture2D throws an exception.
                var texture = CreateTexture(Math.Max(width, this.width), Math.Max(height, this.height), true);
                if (this.texture != null)
                {
                    this.texture.Dispose();
                }
                this.texture = texture;

                var shareableTexture = CreateTexture(Math.Max(width, this.width), Math.Max(height, this.height), false);
                if (this.shareableTexture != null)
                {
                    this.shareableTexture.Dispose();
                }
                this.shareableTexture = shareableTexture;
                //if (renderTargetView != null) renderTargetView.Dispose();
                //this.renderTargetView = new RenderTargetView(device, shareableTexture);

                this.width = texture.Description.Width;
                this.height = texture.Description.Height;

                using (SharpDX.DXGI.Surface surface = texture.AsSurface())
                {
                    CreateRenderTarget(surface);
                }

                if (DeviceResized != null)
                    DeviceResized(this, EventArgs.Empty);
            }
        }

        public void SetViewport(Viewport viewport)
        {
            this.device.Rasterizer.SetViewports(viewport);
        }

        private Texture2D CreateTexture(int width, int height, bool multiSampling)
        {
            var description = new SharpDX.Direct3D10.Texture2DDescription();
            description.ArraySize = 1;
            description.BindFlags = SharpDX.Direct3D10.BindFlags.RenderTarget | SharpDX.Direct3D10.BindFlags.ShaderResource;
            description.CpuAccessFlags = CpuAccessFlags.None;
            description.Format = Format.B8G8R8A8_UNorm;
            description.MipLevels = 1;
            //description.MiscellaneousResourceOptions = D3D10.MiscellaneousResourceOptions.Shared;
 
            // Multi-sample anti-aliasing
            int count;
            if (multiSampling) count = 8; else count = 1;
            int quality = device.CheckMultisampleQualityLevels(description.Format, count);
            if (count == 1) quality = 1;
            // Multi-sample anti-aliasing
            SampleDescription sampleDesc = new SampleDescription(count, 0);
            description.SampleDescription = sampleDesc;

            description.Usage = ResourceUsage.Default;
            description.OptionFlags = ResourceOptionFlags.Shared;
            description.Height = (int)height;
            description.Width = (int)width;

            return new Texture2D(device, description);
        }

        /// <summary>
        /// Copy texture to shareable texture. 
        /// </summary>
        internal void CopyTextureAcross()
        {
            device.ResolveSubresource(texture, 0, shareableTexture, 0, Format.B8G8R8A8_UNorm);
        }

        private void CreateRenderTarget(SharpDX.DXGI.Surface surface)
        {
            // Create a D2D render target which can draw into our offscreen D3D surface. 
            // D2D uses device independant units, like WPF, at 96/inch.
            var properties = new SharpDX.Direct2D1.RenderTargetProperties();
            properties.DpiX = 96;
            properties.DpiY = 96;
            properties.MinLevel = SharpDX.Direct2D1.FeatureLevel.Level_DEFAULT;
            properties.PixelFormat = new SharpDX.Direct2D1.PixelFormat(Format.Unknown, SharpDX.Direct2D1.AlphaMode.Premultiplied);
            properties.Usage = SharpDX.Direct2D1.RenderTargetUsage.None;

            if (this.renderTarget != null)
            {
                this.renderTarget.Dispose();
            }

            renderTarget = new SharpDX.Direct2D1.RenderTarget(factory2D, surface, properties);
        }

        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                // If this is the last control to finish using the
                // device, we should dispose the singleton instance.
                if (this.texture != null)
                {
                    this.texture.Dispose();
                }
                if (this.shareableTexture != null)
                {
                    this.shareableTexture.Dispose();
                }
            }
        }

    }
}
