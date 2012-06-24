// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Threading;
using System.Threading;
#if ILNumerics
using ILNumerics;
using ILNumerics.Storage;
using ILNumerics.BuiltInFunctions;
using ILNumerics.Exceptions;
#endif

namespace IronPlot.Plotting3D
{
    public enum SurfaceShading { Faceted, Smooth, None };
    public enum MeshLines { None, Triangles }; // quads
    
    /// <summary>
    /// Geometric primitive class for surfaces from ILArrays.
    /// </summary>
    public partial class SurfaceModel3D : Model3D
    {
        VertexPositionNormalColor[] vertices;
        Point3D[] modelVertices;
        int lengthU, lengthV;
        int[] indices;
        protected VertexDeclaration vertexDeclaration;
        protected VertexBuffer vertexBuffer;
        protected int vertexBufferLength = -1;
        protected IndexBuffer indexBuffer;
        protected int indexBufferLength = -1;
        protected ColourMap colourMap;
        
        // This is only used if there is somewhere to display the bar (e.g. this is within a Plot3D) 
        protected ColourBar colourBar;
        public ColourBar ColourBar { get { return colourBar; } }
        DispatcherTimer colourMapUpdateTimer = new DispatcherTimer();

        protected UInt16[] colourMapIndices;
        protected List<SharpDX.Direct3D9.Light> lights;

        private static readonly DependencyProperty SurfaceShadingProperty =
            DependencyProperty.Register("SurfaceShadingProperty",
            typeof(SurfaceShading),
            typeof(SurfaceModel3D),
            new PropertyMetadata(SurfaceShading.Smooth, OnSurfaceShadingChanged));

        private static readonly DependencyProperty MeshLinesProperty =
            DependencyProperty.Register("MeshLinesProperty",
            typeof(MeshLines),
            typeof(SurfaceModel3D),
            new PropertyMetadata(MeshLines.None, OnMeshLinesChanged));

        private static readonly DependencyProperty TransparencyProperty =
            DependencyProperty.Register("TransparencyProperty",
            typeof(byte),
            typeof(SurfaceModel3D),
            new PropertyMetadata((byte)0, OnTransparencyChanged));

        public List<SharpDX.Direct3D9.Light> Lights
        {
            get 
            {
                RequestRender(EventArgs.Empty);
                return lights; 
            }
        }

        SharpDX.Direct3D9.Material material;
        public SharpDX.Direct3D9.Material Material { get { return material; } set { material = value; } }

        public SurfaceShading SurfaceShading
        {
            set { SetValue(SurfaceShadingProperty, value); }
            get { return (SurfaceShading)GetValue(SurfaceShadingProperty); }
        }

        public MeshLines MeshLines
        {
            set { SetValue(MeshLinesProperty, value); }
            get { return (MeshLines)GetValue(MeshLinesProperty); }
        }

        public byte Transparency
        {
            set { SetValue(TransparencyProperty, value); }
            get { return (byte)GetValue(TransparencyProperty); }
        }
        
        static void OnSurfaceShadingChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SurfaceModel3D surface = obj as SurfaceModel3D;
            surface.CreateVertsAndInds();
            surface.SetColorFromIndices();
            surface.RecreateBuffers();
            surface.RequestRender(EventArgs.Empty);
        }

        static void OnMeshLinesChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SurfaceModel3D surface = obj as SurfaceModel3D;
            surface.RequestRender(EventArgs.Empty);
        }

        static void OnTransparencyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            SurfaceModel3D surface = obj as SurfaceModel3D;
            surface.SetColorFromIndices();
            surface.RecreateBuffers();
            surface.RequestRender(EventArgs.Empty);
        }

        protected override void OnModelToWorldChanged()
        {
            TransformVertsAndInds();
            RecreateBuffers();
            base.OnModelToWorldChanged();
        }

        /// <summary>
        /// Constructs a surface primitive.
        /// </summary>
        public SurfaceModel3D(double[,] x, double[,] y, double[,] z)
            : base()
        {
            InitializeSurface(x, y, z);
        }

        public SurfaceModel3D(double[] x, double[] y, double[,] z)
            : base()
        {
            if (x.Length != z.GetLength(1)) throw new ArgumentException("Length of x vector must be equal to columns of z.");
            if (y.Length != z.GetLength(0)) throw new ArgumentException("Length of y vector must be equal to rows of z.");
            InitializeSurface(x, y, z);
        }

        public SurfaceModel3D(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> z, int xLength, int yLength)
        {
            CreateMesh(x, y, z, xLength, yLength);
        }

        public SurfaceModel3D(IEnumerable<object> x, IEnumerable<object> y, IEnumerable<object> z)
        {
            int[] xLengths = new int[3]; int[] yLengths = new int[3]; 
            IEnumerable<double>[] imageEnumerators = new IEnumerable<double>[3];
            IEnumerable<double>[] adjustedImageEnumerators = new IEnumerable<double>[3];

            imageEnumerators[0] = GeneralArray.ToImageEnumerator(x, out xLengths[0], out yLengths[0]);
            imageEnumerators[1] = GeneralArray.ToImageEnumerator(y, out xLengths[1], out yLengths[1]);
            imageEnumerators[2] = GeneralArray.ToImageEnumerator(z, out xLengths[2], out yLengths[2]);
            
            int xLength = -1; int yLength = -1;
            if (yLengths[0] == 0) xLength = xLengths[0];
            if (yLengths[1] == 0) yLength = xLengths[1];
            for (int i = 0; i < 3; ++i)
            {
                adjustedImageEnumerators[i] = imageEnumerators[i];
                if (yLengths[i] != 0)
                {
                    if (xLength == -1) xLength = xLengths[i];
                    else if (xLength != xLengths[i]) throw new ArgumentException("x dimensions are not consistent.");
                    if (yLength == -1) yLength = yLengths[i];
                    else if (yLength != yLengths[i]) throw new ArgumentException("y dimensions are not consistent.");
                }
            }
            if (yLengths[0] == 0) adjustedImageEnumerators[0] = MathHelper.MeshGridX(imageEnumerators[0], yLength);
            if (yLengths[1] == 0) adjustedImageEnumerators[1] = MathHelper.MeshGridY(imageEnumerators[1], xLength);
            if (yLengths[2] == 0 && xLengths[2] != xLength * yLength) throw new ArgumentException("Wrong number of elements in z.");
            
            CreateMesh(adjustedImageEnumerators[0], adjustedImageEnumerators[1], adjustedImageEnumerators[2], xLength, yLength);
        }

        protected void InitializeSurface(double[] x, double[] y, double[,] z)
        {
            CreateMesh(x, y, z);
        }

        protected void InitializeSurface(double[,] x, double[,] y, double[,] z)
        {
            CreateMesh(x, y, z);
        }
#if ILNumerics
        /// <summary>
        /// Constructs a surface primitive.
        /// </summary>
        public SurfaceModel3D(ILArray<double> x, ILArray<double> y, ILArray<double> z)
            : base()
        {
            InitializeSurfaceILArray(x, y, z);
        }
        
        public void InitializeSurfaceILArray(ILArray<double> x, ILArray<double> y, ILArray<double> z)
        {
            CreateMeshILArray(x, y, z);
        }

        protected void CreateMeshILArray(ILArray<double> x, ILArray<double> y, ILArray<double> z)
        {
            bounds = new Cuboid(x.MinValue, y.MinValue, z.MinValue, x.MaxValue, y.MaxValue, z.MaxValue);
            lengthU = x.Dimensions[0];
            lengthV = x.Dimensions[1];
            ILArray<double> xs, ys, zs;
            if (x.IsReference)
                xs = x.Clone() as ILArray<double>;
            else xs = x;
            if (y.IsReference)
                ys = y.Clone() as ILArray<double>;
            else ys = y;
            if (z.IsReference)
                zs = z.Clone() as ILArray<double>;
            else zs = z;
            //if (x.IsReference || y.IsReference || z.IsReference) throw new Exception("x, y and z must be solid arrays");
            double[] xa = xs.InternalArray4Experts;
            double[] ya = ys.InternalArray4Experts;
            double[] za = zs.InternalArray4Experts;
            Cuboid modelBounds = new Cuboid(new System.Windows.Media.Media3D.Point3D(-10, -10, -10), new System.Windows.Media.Media3D.Point3D(10, 10, 10));
            UpdateModelVertices(xa, ya, za, lengthU, lengthV);
            CreateVertsAndInds();
            colourMap = new ColourMap(ColourMapType.Jet, 256);
            colourMapIndices = FalseColourImage.IEnumerableToIndexArray(za, lengthU, lengthV, 256);
            SetColorFromIndices();
        } 
#endif
        protected void CreateMesh(double[] x, double[] y, double[,] z)
        {
            CreateMesh(MathHelper.MeshGridX(x, y.Length), MathHelper.MeshGridY(y, x.Length), z.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), x.GetLength(0), x.GetLength(1));
        }

        protected void CreateMesh(double[,] x, double[,] y, double[,] z)
        {
            CreateMesh(x.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), y.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), z.ArrayEnumerator(EnumerationOrder2D.ColumnMajor), x.GetLength(0), x.GetLength(1));
        }

        protected void CreateMesh(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> z, int xLength, int yLength)
        {
            lengthU = xLength;
            lengthV = yLength;
            bounds = new Cuboid(x.Min(), y.Min(), z.Min(), x.Max(), y.Max(), z.Max());
            Cuboid modelBounds = new Cuboid(new System.Windows.Media.Media3D.Point3D(-10, -10, -10), new System.Windows.Media.Media3D.Point3D(10, 10, 10));
            UpdateModelVertices(x, y, z, xLength, yLength);
            CreateVertsAndInds();
            colourMap = new ColourMap(ColourMapType.HSV, 256);
            colourMapIndices = FalseColourImage.IEnumerableToIndexArray(z, xLength, yLength, 256);
            SetColorFromIndices();
            colourMapUpdateTimer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(0.2) };
            colourMapUpdateTimer.Tick += new EventHandler(colourMapUpdateTimer_Tick);

            lights = new List<SharpDX.Direct3D9.Light>();
            SharpDX.Direct3D9.Light light = new SharpDX.Direct3D9.Light() { Type = LightType.Directional };
            light.Diffuse = new Color4(0.4f, 0.4f, 0.4f, 1.0f);
            light.Direction = new Vector3(0.3f, 0.3f, -0.7f);
            light.Specular = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
            lights.Add(light);

            light = new SharpDX.Direct3D9.Light() { Type = LightType.Directional };
            light.Diffuse = new Color4(0.4f, 0.4f, 0.4f, 1.0f);
            light.Direction = new Vector3(-0.3f, -0.3f, -0.7f);
            light.Specular = new Color4(0.05f, 0.05f, 0.05f, 1.0f);
            lights.Add(light);

            material = new SharpDX.Direct3D9.Material();
            material.Specular = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
            material.Diffuse = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
            material.Ambient = new Color4(0.0f, 0.0f, 0.0f, 1.0f);
            material.Power = 10;
        }

        internal override void OnViewportImageChanged(ViewportImage newViewportImage)
        {
            // if the ViewportImage is owned by a Plot3D, we can add a ColourBar.
            if (viewportImage != null && viewportImage.ViewPort3D != null && colourBar != null)
            {
                viewportImage.ViewPort3D.Annotations.Remove(colourBar);
                colourBar.ColourMapChanged -= new RoutedEventHandler(colourBar_ColourMapChanged);
            }
            base.OnViewportImageChanged(newViewportImage);
            if (viewportImage.ViewPort3D != null)
            {
                if (colourBar == null)
                {
                    colourBar = new ColourBar(colourMap);
                    colourBar.Min = bounds.Minimum.Z; colourBar.Max = bounds.Maximum.Z; 
                    colourBar.ColourMapChanged += new RoutedEventHandler(colourBar_ColourMapChanged);
                }
                viewportImage.ViewPort3D.Annotations.Add(colourBar);
            }
        }
       
        void colourBar_ColourMapChanged(object sender, RoutedEventArgs e)
        {
            colourMapUpdateTimer.Start();
        }

        bool updateInProgress = false;
        object updateLocker = new object();

        void colourMapUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (updateInProgress)
            {
                colourMapUpdateTimer.Start();
                return;
            }
            updateInProgress = true;
            colourMapUpdateTimer.Stop(); 
            ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateColours), new object());
        }

        private void UpdateColours(object state)
        {
            lock (updateLocker)
            {
                SetColorFromIndices();
                RecreateBuffers();
            }
            Dispatcher.BeginInvoke(new Action(delegate()
            {
                RequestRender(EventArgs.Empty);
                updateInProgress = false;
            }));
        }

        protected void CreateVertsAndInds()
        {
            if (SurfaceShading == SurfaceShading.Smooth)
            {
                int newVerticesLength = lengthU * lengthV * 2; // assume two-sided
                int newIndicesLength = 2 * 6 * (lengthU - 1) * (lengthV - 1);
                if (vertices == null || (vertices.Length != newVerticesLength)) vertices = new VertexPositionNormalColor[newVerticesLength];
                if (indices == null || (indices.Length != newIndicesLength)) indices = new int[newIndicesLength];
                UpdateVertsAndIndsSmooth(false, false);
            }
            else
            {
                int newVerticesLength = 6 * (lengthU - 1) * (lengthV - 1);
                int newIndicesLength = 2 * 6 * (lengthU - 1) * (lengthV - 1);
                if (vertices == null || (vertices.Length != newVerticesLength)) vertices = new VertexPositionNormalColor[newVerticesLength];
                if (indices == null || (indices.Length != newIndicesLength)) indices = new int[newIndicesLength];
                UpdateVertsAndIndsGeneral(false, false);
            }
        }

        protected void RecreateBuffers()
        {
            if (viewportImage == null) return;
            lock (updateLocker)
            {
                Pool pool;
                if (viewportImage.GraphicsDeviceService.UseDeviceEx == true) pool = Pool.Default;
                else pool = Pool.Managed;
                if ((vertexBufferLength != vertices.Length) || (vertexBuffer == null))
                {
                    if (vertexBuffer != null) vertexBuffer.Dispose();
                    vertexBuffer = new VertexBuffer(graphicsDevice, vertices.Length * VertexPositionNormalColor.SizeInBytes,
                    Usage.WriteOnly, VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse, pool);
                    vertexBufferLength = vertices.Length;
                }

                using (DataStream stream = vertexBuffer.Lock(0, 0, LockFlags.None))
                {
                    stream.WriteRange(vertices);
                    vertexBuffer.Unlock();
                }

                graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse;

                if ((indexBufferLength != indices.Length) || (indexBuffer == null))
                {
                    if (indexBuffer != null) indexBuffer.Dispose();
                    indexBuffer = new IndexBuffer(graphicsDevice, indices.Length * Marshal.SizeOf(typeof(int)),
                        Usage.WriteOnly, pool, false);
                    indexBufferLength = indices.Length;
                }

                using (DataStream streamIndex = indexBuffer.Lock(0, 0, LockFlags.None))
                {
                    streamIndex.WriteRange(indices);
                    indexBuffer.Unlock();
                }
            }
        }

        protected void TransformVertsAndInds()
        {
            if (SurfaceShading == SurfaceShading.Smooth)
            {
                UpdateVertsAndIndsSmooth(true, false);
            }
            else
            {
                UpdateVertsAndIndsGeneral(true, false);
            }
        }

        /// <summary>
        /// Store off the vertices in model space; these will then be transformed into world space.
        /// Overload to update model vertices with just changes x, y or z (useful for animation).
        /// </summary>
        protected void UpdateModelVertices(IEnumerable<double> x, IEnumerable<double> y, IEnumerable<double> z, int lengthU, int lengthV)
        {
            // Changing everything: just recreate array: 
            modelVertices = new Point3D[lengthU * lengthV];
            int index = 0;
            IEnumerator<double> xi, yi, zi;
            xi = x.GetEnumerator(); yi = y.GetEnumerator(); zi = z.GetEnumerator(); 
            for (int v = 0; v < lengthV; v++)
            {
                for (int u = 0; u < lengthU; u++)
                {
                    xi.MoveNext(); yi.MoveNext(); zi.MoveNext(); 
                    modelVertices[index] = new Point3D(xi.Current, yi.Current, zi.Current);
                    index++;
                }
            }
        }

        protected override void UpdateGeometry()
        {
        }

        /// <summary>
        /// Draws the primitive model, using the specified effect. Unlike the other
        /// Draw overload where you just specify the world/view/projection matrices
        /// and color, this method does not set any renderstates, so you must make
        /// sure all states are set to sensible values before you call it.
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            if (vertexBuffer == null || indexBuffer == null) return;

            graphicsDevice.SetRenderState(RenderState.SpecularEnable, true);

            graphicsDevice.Material = material;
            graphicsDevice.SetRenderState(RenderState.Ambient, Color.DarkGray.ToArgb());
            graphicsDevice.SetRenderState(RenderState.SpecularEnable, true);

            graphicsDevice.SetRenderState(RenderState.ZEnable, ZBufferType.UseZBuffer);
            graphicsDevice.SetRenderState(RenderState.ZWriteEnable, true);
            graphicsDevice.SetRenderState(RenderState.ZFunc, Compare.LessEqual); 
            graphicsDevice.SetRenderState(RenderState.NormalizeNormals, true);

            for (int i = 0; i < lights.Count; ++i)
            {
                SharpDX.Direct3D9.Light light = lights[i];
                graphicsDevice.SetLight(i, ref light);
                graphicsDevice.EnableLight(i, true);
            }

            graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse;
            graphicsDevice.SetStreamSource(0, vertexBuffer, 0, Marshal.SizeOf(typeof(VertexPositionNormalColor)));
            graphicsDevice.Indices = indexBuffer;

            graphicsDevice.SetRenderState(RenderState.AlphaBlendEnable, true);
            graphicsDevice.SetRenderState(RenderState.BlendOperationAlpha, BlendOperation.Add);
            graphicsDevice.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
            graphicsDevice.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
            graphicsDevice.SetRenderState(RenderState.SeparateAlphaBlendEnable, false);
            graphicsDevice.SetRenderState(RenderState.CullMode, Cull.Counterclockwise);
            int primitiveCount = indices.Length / 3;

            graphicsDevice.SetRenderState(RenderState.Lighting, true);

            if (MeshLines != MeshLines.None)
            {
                graphicsDevice.SetRenderState(RenderState.DepthBias, -0.0001f);
                graphicsDevice.SetRenderState(RenderState.AmbientMaterialSource, ColorSource.Material);
                graphicsDevice.SetRenderState(RenderState.DiffuseMaterialSource, ColorSource.Material);
                graphicsDevice.SetRenderState(RenderState.SpecularMaterialSource, ColorSource.Material);
                graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Wireframe);
                if (MeshLines == MeshLines.Triangles)
                {
                    graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, primitiveCount);
                }
                //else graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineStrip, 0, 0, vertices.Length, 0, 1);
            }
            if (SurfaceShading != SurfaceShading.None)
            {
                graphicsDevice.SetRenderState(RenderState.DepthBias, 0);
                graphicsDevice.SetRenderState(RenderState.AmbientMaterialSource, ColorSource.Color1);
                graphicsDevice.SetRenderState(RenderState.DiffuseMaterialSource, ColorSource.Color1);
                graphicsDevice.SetRenderState(RenderState.SpecularMaterialSource, ColorSource.Color1);
                graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);
                graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, primitiveCount);
            }
        }

        protected override void DisposeDisposables()
        {
            base.DisposeDisposables();
            if (vertexBuffer != null) vertexBuffer.Dispose();
            if (indexBuffer != null) indexBuffer.Dispose();
            vertexBuffer = null; indexBuffer = null;
        }

        protected override void RecreateDisposables()
        {
            base.RecreateDisposables();
            RecreateBuffers();
        }
    }
}
