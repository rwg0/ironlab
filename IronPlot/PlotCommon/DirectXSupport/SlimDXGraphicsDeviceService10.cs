// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
//using SlimDX;
using SlimDX.DXGI;
using SlimDX.Direct3D10;
using System.Windows.Interop;
using System.Windows.Forms;

namespace IronPlot
{
    public class SlimDXGraphicsDeviceService10
    {
        // Singleton device service instance.
        static SlimDXGraphicsDeviceService10 singletonInstance;

        public event EventHandler DeviceResized;

        // Keep track of how many controls are sharing the singletonInstance.
        static int referenceCount;
        Factory factoryDXGI;
        SlimDX.Direct2D.Factory factory2D;
        Texture2D texture;
        Texture2D shareableTexture;
        //RenderTargetView renderTargetView;
        SlimDX.Direct2D.RenderTarget renderTarget;
        int width;
        int height;

        public Texture2D Texture { get { return shareableTexture; } }
        public SlimDX.Direct2D.RenderTarget RenderTarget { get { return renderTarget; } }
        public SlimDX.Direct3D10_1.Device1 Device { get { return device; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        //SlimDX.Direct3D10.Device device;
        SlimDX.Direct3D10_1.Device1 device;

        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static SlimDXGraphicsDeviceService10 AddRef()
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                singletonInstance = new SlimDXGraphicsDeviceService10();
            }

            return singletonInstance;
        }

        /// <summary>
        /// Create a new GraphicsDeviceService and return a reference to it.
        /// </summary>
        public static SlimDXGraphicsDeviceService10 RefToNew()
        {
            return new SlimDXGraphicsDeviceService10();
        }

        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        SlimDXGraphicsDeviceService10()
        {
            factoryDXGI = new Factory();
            factory2D = new SlimDX.Direct2D.Factory();
            // Try to create a hardware device first and fall back to a
            // software (WARP doens't let us share resources)
            var device1 = TryCreateDevice1(SlimDX.Direct3D10.DriverType.Hardware);
            if (device1 == null)
            {
                device1 = TryCreateDevice1(SlimDX.Direct3D10.DriverType.Software);
                if (device1 == null)
                {
                    throw new SlimDX.Direct3D10.Direct3D10Exception("Unable to create a DirectX 10 device.");
                }
            }
            //this.device = device1.QueryInterface<D3D10.D3DDevice>();
            //device1.Dispose();
            this.device = device1;
        }

        private SlimDX.Direct3D10_1.Device1 TryCreateDevice1(SlimDX.Direct3D10.DriverType type)
        {
            // We'll try to create the device that supports any of these feature levels

            SlimDX.Direct3D10_1.FeatureLevel[] levels =
            {
                SlimDX.Direct3D10_1.FeatureLevel.Level_10_1,
                SlimDX.Direct3D10_1.FeatureLevel.Level_10_0,
                SlimDX.Direct3D10_1.FeatureLevel.Level_9_3,
                SlimDX.Direct3D10_1.FeatureLevel.Level_9_2,
                SlimDX.Direct3D10_1.FeatureLevel.Level_9_1
            };

            foreach (var level in levels)
            {
                try
                {
                    return new SlimDX.Direct3D10_1.Device1(factoryDXGI.GetAdapter(0), type, DeviceCreationFlags.BgraSupport, level);
                        //D3D10.D3DDevice1.CreateDevice1(null, type, null, D3D10.CreateDeviceOptions.SupportBgra, level);
                }
                catch (ArgumentException) // E_INVALIDARG
                {
                    continue; // Try the next feature level
                }
                catch (OutOfMemoryException) // E_OUTOFMEMORY
                {
                    continue; // Try the next feature level
                }
                catch (SlimDX.Direct3D10.Direct3D10Exception) // D3DERR_INVALIDCALL or E_FAIL
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
                if ((width <= this.width) && (height <= this.height)) return;

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

                SlimDX.DXGI.Surface surface = texture.AsSurface();

                CreateRenderTarget(surface);

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
            var description = new SlimDX.Direct3D10.Texture2DDescription();
            description.ArraySize = 1;
            description.BindFlags = SlimDX.Direct3D10.BindFlags.RenderTarget | SlimDX.Direct3D10.BindFlags.ShaderResource;
            description.CpuAccessFlags = CpuAccessFlags.None;
            description.Format = Format.B8G8R8A8_UNorm;
            description.MipLevels = 1;
            //description.MiscellaneousResourceOptions = D3D10.MiscellaneousResourceOptions.Shared;
 
            // Multi-sample anti-aliasing
            //description.MiscellaneousResourceOptions = D3D10.MiscellaneousResourceOptions.Shared;
            int count;
            if (multiSampling) count = 8; else count = 1;
            int quality = device.CheckMultisampleQualityLevels(description.Format, count);
            if (count == 1) quality = 1;
            // Multi-sample anti-aliasing
            SampleDescription sampleDesc = new SampleDescription(count, 0);
            description.SampleDescription = sampleDesc;

            RasterizerStateDescription rastDesc = new RasterizerStateDescription();
            rastDesc.CullMode = CullMode.Back;
            rastDesc.FillMode = FillMode.Solid;
            rastDesc.IsMultisampleEnabled = false;
            rastDesc.IsAntialiasedLineEnabled = false;
            //rastDesc.DepthBias = 0;
            //rastDesc.DepthBiasClamp = 0;
            //rastDesc.IsDepthClipEnabled = true;
            //rastDesc.IsFrontCounterclockwise = false;
            //rastDesc.IsScissorEnabled = true;
            //rastDesc.SlopeScaledDepthBias = 0;

            device.Rasterizer.State = RasterizerState.FromDescription(device, rastDesc);

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
            //Texture2D.LoadTextureFromTexture(texture, shareableTexture, info);
            //device.OutputMerger.SetTargets(renderTargetView);
            //ShaderResourceView view = new ShaderResourceView(device, texture);
            //using (Sprite spriteobject = new Sprite(device, 1))
            //{
            //    SpriteInstance instance = new SpriteInstance(view, SlimDX.Vector2.Zero, new SlimDX.Vector2(width, height)); 
            //    spriteobject.Begin(SpriteFlags.None);
            //    spriteobject.DrawImmediate(new SpriteInstance[] { instance });
            //    spriteobject.Flush();
            //   spriteobject.End();
            //}
        }

        private void CreateRenderTarget(SlimDX.DXGI.Surface surface)
        {
            // Create a D2D render target which can draw into our offscreen D3D
            // surface. D2D uses device independant units, like WPF, at 96/inch
            var properties = new SlimDX.Direct2D.RenderTargetProperties();
            properties.HorizontalDpi = 96;
            properties.VerticalDpi = 96;
            properties.MinimumFeatureLevel = SlimDX.Direct2D.FeatureLevel.Default;
            //properties.PixelFormat = new SlimDX.Direct2D.PixelFormat(Format.Unknown, SlimDX.Direct2D.AlphaMode.Premultiplied);
            properties.PixelFormat = new SlimDX.Direct2D.PixelFormat(Format.Unknown, SlimDX.Direct2D.AlphaMode.Premultiplied);
            //properties.RenderTargetType = D2D.RenderTargetType.Default;
            properties.Usage = SlimDX.Direct2D.RenderTargetUsage.None;

            if (this.renderTarget != null)
            {
                this.renderTarget.Dispose();
            }
            renderTarget = SlimDX.Direct2D.RenderTarget.FromDXGI(factory2D, surface, properties);
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
                if (disposing)
                {

                }

                //graphicsDevice = null;
            }
        }

    }
}
