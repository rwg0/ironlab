// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D9;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Drawing;
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
        protected UInt16[] colourMapIndices;
        protected List<SlimDX.Direct3D9.Light> lights;

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

        public List<SlimDX.Direct3D9.Light> Lights
        {
            get 
            {
                RequestRender(EventArgs.Empty);
                return lights; 
            }
        }

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
            double[,] xa = GeneralArray.ToDoubleArray(x) as double[,];
            double[,] ya = GeneralArray.ToDoubleArray(y) as double[,];
            double[,] za = GeneralArray.ToDoubleArray(z) as double[,];
            if ((xa == null) && (ya == null) && (za == null))
            {
                throw new Exception("Not all inputs are recongised as two dimensional arrays.");
            }
            InitializeSurface(xa, ya, za);
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
        }


        internal override void Initialize()
        {
            base.Initialize();
            RecreateBuffers();
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

            DataStream stream = vertexBuffer.Lock(0, 0, LockFlags.None);
            stream.WriteRange(vertices);
            vertexBuffer.Unlock();
            stream.Dispose();

            graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Normal | VertexFormat.Diffuse;

            if ((indexBufferLength != indices.Length) || (indexBuffer == null))
            {
                if (indexBuffer != null) indexBuffer.Dispose();
                indexBuffer = new IndexBuffer(graphicsDevice, indices.Length * Marshal.SizeOf(typeof(int)),
                    Usage.WriteOnly, pool, false);
                indexBufferLength = indices.Length;
            }

            DataStream streamIndex = indexBuffer.Lock(0, 0, LockFlags.None);
            streamIndex.WriteRange(indices);
            indexBuffer.Unlock();
            streamIndex.Dispose();
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

        protected void UpdateVertsAndIndsSmooth(bool updateVerticesOnly, bool oneSided)
        {
            //oneSided = true;
            int index = 0;
            int indexOff = lengthU * lengthV;
            Point3D worldPoint;
            MatrixTransform3D modelToWorld = ModelToWorld;
            for (int i = 0; i < indexOff; ++i)
            {
                worldPoint = modelToWorld.Transform(modelVertices[i]);
                vertices[i].Position = new Vector3((float)worldPoint.X, (float)worldPoint.Y, (float)worldPoint.Z);
                vertices[i].Normal = new Vector3(0f, 0f, 0f);
            }
            // Add triangles
            int reverseSideOffset = 6 * (lengthU - 1) * (lengthV - 1);
            if (!updateVerticesOnly)
            {
                index = 0;
                indexOff = 0;
                for (int v = 0; v < lengthV - 1; v++)
                {
                    for (int u = 0; u < lengthU - 1; u++)
                    {
                        indices[index] = indexOff + u;
                        indices[index + 1] = indexOff + u + lengthU + 1;
                        indices[index + 2] = indexOff + u + 1;
                        indices[index + 3] = indexOff + u;
                        indices[index + 4] = indexOff + u + lengthU;
                        indices[index + 5] = indexOff + u + lengthU + 1;
                        index += 6;
                    }
                    indexOff += lengthU;
                }
                if (!oneSided)
                {
                    index = 0;
                    indexOff = lengthU * lengthV;
                    for (int v = 0; v < lengthV - 1; v++)
                    {
                        for (int u = 0; u < lengthU - 1; u++)
                        {
                            indices[index + reverseSideOffset] = indexOff + u + 1;
                            indices[index + 1 + reverseSideOffset] = indexOff + u + lengthU + 1;
                            indices[index + 2 + reverseSideOffset] = indexOff + u;
                            indices[index + 3 + reverseSideOffset] = indexOff + u + lengthU;
                            indices[index + 4 + reverseSideOffset] = indexOff + u;
                            indices[index + 5 + reverseSideOffset] = indexOff + u + lengthU + 1;
                            index += 6;
                        }
                        indexOff += lengthU;
                    }
                }
            }
            // Go through triangles and add normal to all vertices
            Vector3 normal;
            for (int i = 0; i <= reverseSideOffset - 3; i += 3)
            {
                Vector3 vec1, vec2;
                vec1 = vertices[indices[i + 2]].Position - vertices[indices[i + 1]].Position;
                vec2 = vertices[indices[i + 2]].Position - vertices[indices[i]].Position;
                normal = Vector3.Cross(vec1, vec2);
                normal.Normalize();
                vertices[indices[i]].Normal += normal;
                vertices[indices[i + 1]].Normal += normal;
                vertices[indices[i + 2]].Normal += normal;
            }
            if (!oneSided)
            {
                indexOff = lengthU * lengthV;
                for (int i = 0; i < indexOff; ++i)
                {
                    vertices[i + indexOff].Position = vertices[i].Position;
                    vertices[i+ indexOff].Normal = -vertices[i].Normal;
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
            SlimDX.Direct3D9.Material material = new SlimDX.Direct3D9.Material();
            material.Specular = new Color4(1.0f, 1.0f, 1.0f);
            material.Diffuse = new Color4(1.0f, 1.0f, 1.0f);
            material.Ambient = new Color4(1.0f, 1.0f, 1.0f);
            material.Power = 20;
            graphicsDevice.Material = material;
            graphicsDevice.SetRenderState(RenderState.AmbientMaterialSource, ColorSource.Color1);
            graphicsDevice.SetRenderState(RenderState.Ambient, Color.DarkGray.ToArgb());
            graphicsDevice.SetRenderState(RenderState.DiffuseMaterialSource, ColorSource.Color1);
            graphicsDevice.SetRenderState(RenderState.SpecularMaterialSource, ColorSource.Color1);
            graphicsDevice.SetRenderState(RenderState.SpecularEnable, true);
            graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);
            graphicsDevice.SetRenderState(RenderState.Lighting, true);

            graphicsDevice.SetRenderState(RenderState.ZEnable, ZBufferType.UseZBuffer);
            graphicsDevice.SetRenderState(RenderState.ZWriteEnable, true);
            graphicsDevice.SetRenderState(RenderState.ZFunc, Compare.LessEqual); 
            graphicsDevice.SetRenderState(RenderState.NormalizeNormals, true);

            SlimDX.Direct3D9.Light light = new SlimDX.Direct3D9.Light();
            light.Type = LightType.Directional;
            light.Diffuse = new Color4(0.4f, 0.4f, 0.4f);
            //light.Ambient = new Color4(1.0f, 1.0f, 1.0f);
            light.Direction = new Vector3(0.3f, 0.3f, -0.7f);
            //light.Range = 1000f;
            light.Specular = new Color4(0.2f, 0.2f, 0.2f);
            graphicsDevice.SetLight(0, light);
            graphicsDevice.EnableLight(0, true);

            SlimDX.Direct3D9.Light light1 = new SlimDX.Direct3D9.Light();
            light1.Type = LightType.Directional;
            light1.Diffuse = new Color4(0.4f, 0.4f, 0.4f);
            //light1.Ambient = new Color4(1.0f, 1.0f, 1.0f);
            light1.Direction = new Vector3(-0.3f, -0.3f, -0.7f);
            //light1.Range = 1000f;
            light1.Specular = new Color4(0.2f, 0.2f, 0.2f);
            graphicsDevice.SetLight(1, light1);
            graphicsDevice.EnableLight(1, true);

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
            if (SurfaceShading != SurfaceShading.None)
            {
                graphicsDevice.SetRenderState(RenderState.DepthBias, 0);
                graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);
                graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, primitiveCount);
            }
            if (MeshLines != MeshLines.None)
            {
                graphicsDevice.SetRenderState(RenderState.DepthBias, -0.01f);
                graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Wireframe);
                if (MeshLines == MeshLines.Triangles)
                {
                    graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, vertices.Length, 0, primitiveCount);
                }
                //else graphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineStrip, 0, 0, vertices.Length, 0, 1);
            }
        }

        protected void SetColorFromIndices()
        {
            byte opacity = (byte)(255 - (byte)GetValue(TransparencyProperty));
            int[] cmap = colourMap.ToIntArray();
            if (SurfaceShading == SurfaceShading.Smooth)
            {
                int index = 0;
                int indexOff = colourMapIndices.Length;
                foreach (UInt16 magnitude in colourMapIndices)
                {
                    vertices[index].Color = (opacity << 24) | cmap[magnitude];
                    vertices[index + indexOff].Color = (opacity << 24) | cmap[magnitude];
                    index++;
                }
            }
            else
            {
                int currentVertInd = 0;
                UInt16 magnitude;
                int index = 0;
                int colour1, colour2, colour3, colour4;
                for (int v = 0; v < lengthV - 1; v++)
                {
                    for (int u = 0; u < lengthU - 1; u++)
                    {
                        magnitude = colourMapIndices[index];
                        colour1 = (opacity << 24) | cmap[magnitude];
                        magnitude = colourMapIndices[index + 1];
                        colour2 = (opacity << 24) | cmap[magnitude];
                        magnitude = colourMapIndices[index + lengthU + 1];
                        colour3 = (opacity << 24) | cmap[magnitude];
                        magnitude = colourMapIndices[index + lengthU];
                        colour4 = (opacity << 24) | cmap[magnitude];
                        vertices[currentVertInd + 0].Color = colour1;
                        vertices[currentVertInd + 1].Color = colour2;
                        vertices[currentVertInd + 2].Color = colour3;
                        vertices[currentVertInd + 3].Color = colour3;
                        vertices[currentVertInd + 4].Color = colour4;
                        vertices[currentVertInd + 5].Color = colour1;
                        currentVertInd += 6;
                        index++;
                    }
                    index++;
                }
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
