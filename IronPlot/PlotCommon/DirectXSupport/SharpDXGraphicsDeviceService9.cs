// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows.Interop;
using System.Windows.Forms;

namespace IronPlot
{
    // Summary:
    //     Defines a mechanism for retrieving GraphicsDevice objects. Reference page
    //     contains links to related code samples.
    public interface ISharpDXGraphicsDeviceService
    {
        // Summary:
        //     Retrieves a graphcs device.
        Device GraphicsDevice { get; }

        // Summary:
        //     The event that occurs when a graphics device is created.
        event EventHandler DeviceCreated;
        //
        // Summary:
        //     The event that occurs when a graphics device is disposing.
        event EventHandler DeviceDisposing;
        //
        // Summary:
        //     The event that occurs when a graphics device is reset.
        event EventHandler DeviceReset;
        //
        // Summary:
        //     The event that occurs when a graphics device is in the process of resetting.
        event EventHandler DeviceResetting;
    }
    
    public enum DirectXStatus
    {
        Available,
        Unavailable_RemoteSession,
        Unavailable_LowTier,
        Unavailable_MissingDirectX,
        Unavailable_Unknown
    };

    public class Direct3DX9NotFoundException : Exception { }

    // The IGraphicsDeviceService interface requires a DeviceCreated event, but we
    // always just create the device inside our constructor, so we have no place to
    // raise that event. The C# compiler warns us that the event is never used, but
    // we don't care so we just disable this warning.
#pragma warning disable 67

    /// <summary>
    /// Helper class responsible for creating and managing the GraphicsDevice.
    /// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
    /// so even though there can be many controls, there will only ever be a single
    /// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
    /// interface, which provides notification events for when the device is reset
    /// or disposed.
    /// </summary>
    public class SharpDXGraphicsDeviceService9 : ISharpDXGraphicsDeviceService
    {
        #region Fields
        // device settings
        Format adapterFormat = Format.X8R8G8B8;
        Format backbufferFormat = Format.A8R8G8B8; // SurfaceFormat.Color XNA
        Format depthStencilFormat = Format.D16; // DepthFormat.Depth24 XNA
        CreateFlags createFlags = CreateFlags.Multithreaded | CreateFlags.FpuPreserve;
        private PresentParameters presentParameters;

        // Singleton device service instance.
        static SharpDXGraphicsDeviceService9 singletonInstance;

        // Keep track of how many controls are sharing the singletonInstance.
        static int referenceCount;

        private Direct3D direct3D;
        private Direct3DEx direct3DEx;
        private Device device;
        private DeviceEx deviceEx;


        #endregion

        internal DirectImageTracker Tracker = new DirectImageTracker();

        public DirectXStatus DirectXStatus
        {
            get;
            private set;
        }

        public bool UseDeviceEx
        {
            get;
            private set;
        }

        public bool IsAntialiased
        {
            get;
            private set;
        }

        public Direct3D Direct3D
        {
            get
            {
                if (UseDeviceEx)
                    return direct3DEx;
                else
                    return direct3D;
            }
        }

        /// <summary>
        /// Gets the current graphics device.
        /// </summary>
        public Device GraphicsDevice
        {
            get
            {
                if (UseDeviceEx)
                    return deviceEx;
                else
                    return device;
            }
        }

        /// <summary>
        /// Gets the current presentation parameters.
        /// </summary>
        public PresentParameters PresentParameters
        {
            get { return presentParameters; }
        }

        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        SharpDXGraphicsDeviceService9(int width, int height)
        {
            InitializeDirect3D();
            InitializeDevice(width, height);
            if (DirectXStatus != DirectXStatus.Available)
            {
                ReleaseDevice();
                ReleaseDirect3D();
                throw new Exception("Direct3D device unavailable.");
            }
        }

        /// <summary>
        /// Initializes the Direct3D objects and sets the Available flag
        /// </summary>
        private void InitializeDirect3D()
        {
            DirectXStatus = DirectXStatus.Unavailable_Unknown;

            ReleaseDevice();
            ReleaseDirect3D();

            // assume that we can't run at all under terminal services
            //if (GetSystemMetrics(SM_REMOTESESSION) != 0)
            //{
            //    DirectXStatus = DirectXStatus.Unavailable_RemoteSession;
            //    return;
            //}

            //int renderingTier = (RenderCapability.Tier >> 16);
            //if (renderingTier < 2)
            //{
                //DirectXStatus = DirectXStatus.Unavailable_LowTier;
                //return;
            //}

#if USE_XP_MODE
         _direct3D = new Direct3D();
         UseDeviceEx = false;
#else
            try
            {
                direct3DEx = new Direct3DEx();
                UseDeviceEx = true;
            }
            catch
            {
                try
                {
                    direct3D = new Direct3D();
                    UseDeviceEx = false;
                }
                catch (Direct3DX9NotFoundException) 
                {
                    DirectXStatus = DirectXStatus.Unavailable_MissingDirectX;
                    return;
                }
                catch
                {
                    DirectXStatus = DirectXStatus.Unavailable_Unknown;
                    return;
                }
            }
#endif

            bool ok;
            Result result;

            ok = Direct3D.CheckDeviceType(0, DeviceType.Hardware, adapterFormat, backbufferFormat, true, out result);
            if (!ok)
            {
                //const int D3DERR_NOTAVAILABLE = -2005530518;
                //if (result.Code == D3DERR_NOTAVAILABLE)
                //{
                //   ReleaseDirect3D();
                //   Available = Status.Unavailable_NotReady;
                //   return;
                //}
                ReleaseDirect3D();
                return;
            }

            ok = Direct3D.CheckDepthStencilMatch(0, DeviceType.Hardware, adapterFormat, backbufferFormat, depthStencilFormat, out result);
            if (!ok)
            {
                ReleaseDirect3D();
                return;
            }

            Capabilities deviceCaps = Direct3D.GetDeviceCaps(0, DeviceType.Hardware);
            if ((deviceCaps.DeviceCaps & DeviceCaps.HWTransformAndLight) != 0)
                createFlags |= CreateFlags.HardwareVertexProcessing;
            else
                createFlags |= CreateFlags.SoftwareVertexProcessing;

            DirectXStatus = DirectXStatus.Available;

            return;
        }

        /// <summary>
        /// Initializes the Device
        /// </summary>
        private void InitializeDevice(int width, int height)
        {
            if (DirectXStatus != DirectXStatus.Available)
                return;

            Debug.Assert(Direct3D != null);

            ReleaseDevice();

            IntPtr windowHandle = (new Form()).Handle;
            //HwndSource hwnd = new HwndSource(0, 0, 0, 0, 0, width, height, "SharpDXControl", IntPtr.Zero);

            presentParameters = new PresentParameters();
            if (UseDeviceEx) presentParameters.SwapEffect = SwapEffect.Discard;
            else presentParameters.SwapEffect = SwapEffect.Copy;
            
            presentParameters.DeviceWindowHandle = windowHandle;
            presentParameters.Windowed = true;
            presentParameters.BackBufferWidth = Math.Max(width, 1);
            presentParameters.BackBufferHeight = Math.Max(height, 1);
            presentParameters.BackBufferFormat = backbufferFormat;
            presentParameters.AutoDepthStencilFormat = depthStencilFormat;
            presentParameters.EnableAutoDepthStencil = true;
            presentParameters.PresentationInterval = PresentInterval.Immediate;
            presentParameters.MultiSampleType = MultisampleType.None;
            IsAntialiased = false;
            int qualityLevels;
            if (Direct3D.CheckDeviceMultisampleType(0, DeviceType.Hardware, backbufferFormat, true, MultisampleType.EightSamples, out qualityLevels))
            {
                presentParameters.MultiSampleType = MultisampleType.EightSamples;
                presentParameters.MultiSampleQuality = qualityLevels - 1;
                IsAntialiased = true;
            }
            else if (Direct3D.CheckDeviceMultisampleType(0, DeviceType.Hardware, backbufferFormat, true, MultisampleType.FourSamples, out qualityLevels))
            {
                presentParameters.MultiSampleType = MultisampleType.FourSamples;
                presentParameters.MultiSampleQuality = qualityLevels - 1;
                IsAntialiased = false;
            }


            try
            {
                if (UseDeviceEx)
                {
                    deviceEx = new DeviceEx((Direct3DEx)Direct3D, 0,
                       DeviceType.Hardware,
                       windowHandle,
                       createFlags,
                       presentParameters);
                }
                else
                {
                    device = new Device(Direct3D, 0,
                       DeviceType.Hardware,
                       windowHandle,
                       createFlags,
                       presentParameters);
                }
            }
            catch (Exception) //Direct3D9Exception
            {
                DirectXStatus = DirectXStatus.Unavailable_Unknown;
                return;
            }
            return;
        }

        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static SharpDXGraphicsDeviceService9 AddRef(int width, int height)
        {
            // Increment the "how many controls sharing the device" reference count.
            if (Interlocked.Increment(ref referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                singletonInstance = new SharpDXGraphicsDeviceService9(width, height);
            }

            return singletonInstance;
        }

        /// <summary>
        /// Create a new GraphicsDeviceService and return reference to it
        /// </summary>
        public static SharpDXGraphicsDeviceService9 RefToNew(int width, int height)
        {
            return new SharpDXGraphicsDeviceService9(width, height);
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
                    if (DeviceDisposing != null)
                        DeviceDisposing(this, EventArgs.Empty);
                    //graphicsDevice.Dispose();
                    ReleaseDevice();
                }

                //graphicsDevice = null;
            }
        }

        /// <summary>
        /// Resets the graphics device to whichever is bigger out of the specified
        /// resolution or its current size. This behavior means the device will
        /// demand-grow to the largest of all its clients.
        /// </summary>
        public void RatchetResetDevice(int width, int height)
        {
            ResetDevice(Math.Max(presentParameters.BackBufferWidth, width), Math.Max(presentParameters.BackBufferHeight, height));
        }

        public bool ResetIfNecessary()
        {
            int newWidth, newHeight;
            Tracker.GetSizeForMembers(out newWidth, out newHeight);
            int currentWidth = presentParameters.BackBufferWidth;
            int currentHeight = presentParameters.BackBufferHeight;
            bool resetRequired = false;
            if (GraphicsDevice.TestCooperativeLevel() == ResultCode.DeviceNotReset) resetRequired = true;
            if (newWidth > currentWidth)
            {
                newWidth = Math.Max((int)(currentWidth * 1.1), newWidth);
                resetRequired = true;
            }
            else if (newWidth < currentWidth * 0.9)
            {
                resetRequired = true;
            }
            if (newHeight > currentHeight)
            {
                newHeight = Math.Max((int)(currentHeight * 1.1), newHeight);
                resetRequired = true;
            }
            else if (newHeight < currentHeight * 0.9)
            {
                resetRequired = true;
            }
            if (resetRequired) ResetDevice(newWidth, newHeight);
            return resetRequired;
        }

        public void ResetDevice(int width, int height)
        {
            if (DeviceResetting != null)
                DeviceResetting(this, EventArgs.Empty);

            presentParameters.BackBufferWidth = width;
            presentParameters.BackBufferHeight = height;

            if (UseDeviceEx) (GraphicsDevice as DeviceEx).ResetEx(ref presentParameters);
            else GraphicsDevice.Reset(presentParameters);

            if (DeviceReset != null)
                DeviceReset(this, EventArgs.Empty);
        }

        public event EventHandler DeviceCreated;
        public event EventHandler DeviceDisposing;
        public event EventHandler DeviceReset;
        public event EventHandler DeviceResetting;
        public event EventHandler RecreateBuffers;

        private void ReleaseDevice()
        {
            if (device != null)
            {
                if (!device.IsDisposed)
                {
                    device.Dispose();
                    device = null;
                }
            }

            if (deviceEx != null)
            {
                if (!deviceEx.IsDisposed)
                {
                    deviceEx.Dispose();
                    device = null;
                }
            }

            //OnDeviceDisposing(EventArgs.Empty);
        }

        private void ReleaseDirect3D()
        {
            if (direct3D != null)
            {
                if (!direct3D.IsDisposed)
                {
                    direct3D.Dispose();
                    direct3D = null;
                }
            }

            if (direct3DEx != null)
            {
                if (!direct3DEx.IsDisposed)
                {
                    direct3DEx.Dispose();
                    direct3DEx = null;
                }
            }
        }

        #region DLL imports
        // can't figure out how to access remote session status through .NET
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int GetSystemMetrics(int smIndex);
        private const int SM_REMOTESESSION = 0x1000;
        #endregion
    }
}

