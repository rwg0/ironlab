// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Media;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D9;
using System.Windows.Media.Media3D;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using Color = System.Drawing.Color;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Custom vertex type for vertices that have a
    /// position, normal and colour.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ThickLinesVertex
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public Vector2 Texture;
        public int Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ThickLinesVertex(Vector3 startPosition, Vector3 endPosition, Vector2 texture, int color)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Texture = texture;
            Color = color;
        }

        /// <summary>
        /// Size of this vertex type.
        /// </summary>
        public const int SizeInBytes = 12 + 12 + 8 + 4;
    }

    public class LinesModel3D : Model3D, IResolutionDependent
    {
        // Fields associated with rendering of lines
        private VertexPositionColor[] vertices;
        private ThickLinesVertex[] thickVertices;
        private VertexDeclaration vertexDeclaration;
        //Vector3[] verticesVectors;
        private short[] indices;
        protected VertexBuffer vertexBuffer = null;
        protected IndexBuffer indexBuffer = null;
        private static Effect effect;
        private bool pointsChanged = true;
        // Denotes where single pixel lines or thick lines should be drawn
        private bool thickLines = true;
        private int dpi = 96;

        bool effectUnavailable = false;

        /// <summary>
        /// Update geometry from point collection (rather than have event on collection itself, this must
        /// be called explicitly).
        /// </summary>
        public void UpdateFromPoints()
        {
            pointsChanged = true;
            geometryChanged = true;
        }

        private static readonly DependencyProperty PointCollectionProperty =
            DependencyProperty.Register("PointCollection",
            typeof(List<Point3DColor>),
            typeof(LinesModel3D),
            new PropertyMetadata(null));

        private static readonly DependencyProperty LineThicknessProperty =
            DependencyProperty.Register("LineThickness",
            typeof(double),
            typeof(LinesModel3D),
            new PropertyMetadata(1.5, LineThicknessChanged));

        internal float DepthBias = 0f;

        public List<Point3DColor> Points
        {
            private set { SetValue(PointCollectionProperty, value); }
            get { return (List<Point3DColor>)GetValue(PointCollectionProperty); }
        }

        static void LineThicknessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            LinesModel3D lines = obj as LinesModel3D;
            bool previousThickLines = lines.thickLines;
            bool thickLines = true;
            if (lines.effectUnavailable || (((double)args.NewValue == 1.0) && (lines.dpi == 96))) thickLines = false;
            if (previousThickLines != thickLines)
            {
                lines.pointsChanged = true;
                lines.geometryChanged = true;
                lines.thickVertices = null;
                lines.indices = null;
                lines.vertices = null;
            }
            lines.thickLines = thickLines;
            lines.viewportImage.RequestRender();
        }

        public double LineThickness
        {
            set { SetValue(LineThicknessProperty, value); }
            get { return (double)GetValue(LineThicknessProperty); }
        }

        public LinesModel3D() : base() 
        {
            Points = new List<Point3DColor>();
            pointsChanged = true;
        }

        internal override void OnViewportImageChanged(ViewportImage newViewportImage)
        {
            ViewportImage oldViewportImage = viewportImage;
            if (oldViewportImage != null)
            {
                viewportImage.GraphicsDeviceService.DeviceReset -= new EventHandler(GraphicsDeviceService_DeviceReset);
                viewportImage.GraphicsDeviceService.DeviceResetting -= new EventHandler(GraphicsDeviceService_DeviceResetting);
            }
            base.OnViewportImageChanged(newViewportImage);
            if (!viewportImage.GraphicsDeviceService.IsAntialiased) LineThickness = 1.0;
            viewportImage.GraphicsDeviceService.DeviceReset += new EventHandler(GraphicsDeviceService_DeviceReset);
            viewportImage.GraphicsDeviceService.DeviceResetting += new EventHandler(GraphicsDeviceService_DeviceResetting);
            TryCreateEffects();
            UpdateGeometry();
        }

        private void TryCreateEffects()
        {
            if (!effectUnavailable && effect == null)
            {
                try
                {
                    System.IO.Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("IronPlot.Plot3D._3DPrimitives.Line.fxo");
                    effect = Effect.FromStream(graphicsDevice, stream, ShaderFlags.None);             
                }
                catch (Exception) 
                {
                    effectUnavailable = true;
                    thickLines = false;
                }
            }
        }

        protected override void UpdateGeometry()
        {
            if (Points.Count == 0) return;
            if (pointsChanged)
            {
                if (this.IsVisible) RecreateBuffers();
                pointsChanged = false;
            }
            short vertexIndex = 0;
            short index = 0;
            if (!thickLines)
            {
                Point3D modelPoint;
                foreach (Point3DColor p in Points)
                {
                    modelPoint = ModelToWorld.Transform(p.Point3D);
                    vertices[index] = new VertexPositionColor(new Vector3((float)modelPoint.X, (float)modelPoint.Y, (float)modelPoint.Z), Point3DColor.ColorToInt(p.Color));
                    indices[index] = index;
                    index++;
                }
            }
            else
            {
                Point3D start, end;
                int color;
                for (int i = 0; i < Points.Count - 1; i += 2)
                {
                    start = ModelToWorld.Transform(Points[i].Point3D);
                    end = ModelToWorld.Transform(Points[i + 1].Point3D);
                    color = Point3DColor.ColorToInt(Points[i].Color);
                    thickVertices[vertexIndex] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(0, -0.5f), color);
                    thickVertices[vertexIndex + 1] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(1, -0.5f), color);
                    thickVertices[vertexIndex + 2] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(1, 0.5f), color);
                    thickVertices[vertexIndex + 3] = new ThickLinesVertex(new Vector3((float)start.X, (float)start.Y, (float)start.Z),
                        new Vector3((float)end.X, (float)end.Y, (float)end.Z),
                        new Vector2(0, 0.5f), color);
                    indices[index] = vertexIndex; indices[index + 1] = (short)(vertexIndex + 1); indices[index + 2] = (short)(vertexIndex + 2);
                    indices[index + 3] = vertexIndex; indices[index + 4] = (short)(vertexIndex + 2); indices[index + 5] = (short)(vertexIndex + 3);
                    vertexIndex += 4;
                    index += 6;
                }
            }
            if (this.IsVisible) FillBuffers();
        }

        protected void RecreateBuffers()
        {
            Pool pool;
            if (viewportImage.GraphicsDeviceService.UseDeviceEx == true) pool = Pool.Default;
            else pool = Pool.Managed;
            if (!thickLines)
            {
                // Prepare for using single-pixel lines
                if ((vertices == null) || (vertices.Length != Points.Count) || (vertexBuffer == null))
                {
                    if (vertexBuffer != null) vertexBuffer.Dispose();
                    if ((vertices == null) || (vertices.Length != Points.Count)) vertices = new VertexPositionColor[Points.Count];
                    vertexBuffer = new VertexBuffer(graphicsDevice, vertices.Length * VertexPositionColor.SizeInBytes,
                        Usage.WriteOnly, VertexFormat.Position | VertexFormat.Diffuse, pool);
                }
                if ((indices == null) || (indices.Length != Points.Count) || (indexBuffer == null))
                {
                    if (indexBuffer != null) indexBuffer.Dispose();
                    if ((indices == null) || (indices.Length != Points.Count)) indices = new short[Points.Count];
                    indexBuffer = new IndexBuffer(graphicsDevice, indices.Length * Marshal.SizeOf(typeof(short)),
                        Usage.WriteOnly, pool, true);
                }
            }
            else
            {
                // Prepare for thick lines. 4 vertices and 6 indices per line.
                if ((thickVertices == null) || (thickVertices.Length != Points.Count * 4) || (vertexBuffer == null))
                {
                    if (vertexBuffer != null) vertexBuffer.Dispose();
                    if ((thickVertices == null) || (thickVertices.Length != Points.Count * 4)) thickVertices = new ThickLinesVertex[Points.Count * 4];
                    vertexBuffer = new VertexBuffer(graphicsDevice, thickVertices.Length * ThickLinesVertex.SizeInBytes,
                        Usage.WriteOnly, VertexFormat.Position | VertexFormat.Texture0 | VertexFormat.Texture1 | VertexFormat.Diffuse, pool);
                }
                if ((indices == null) || (indices.Length != Points.Count * 6) || (indexBuffer == null))
                {
                    if (indexBuffer != null) indexBuffer.Dispose();
                    if ((indices == null) || (indices.Length != Points.Count * 6)) indices = new short[6 * Points.Count];
                    indexBuffer = new IndexBuffer(graphicsDevice, indices.Length * Marshal.SizeOf(typeof(short)),
                        Usage.WriteOnly, pool, true);
                }
            }
        }

        protected void FillBuffers()
        {
            DataStream stream, streamIndex;
            if (!thickLines)
            {
                stream = vertexBuffer.Lock(0, 0, LockFlags.None);
                stream.WriteRange(vertices);
                vertexBuffer.Unlock();
                graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Diffuse;
            }
            else
            {
                stream = vertexBuffer.Lock(0, 0, LockFlags.None);
                stream.WriteRange(thickVertices);
                vertexBuffer.Unlock();
                VertexElement[] velements = new VertexElement[]
                {
                     new VertexElement(0, 0, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.Position, 0),
                     new VertexElement(0, 12, DeclarationType.Float3, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 0),
                     new VertexElement(0, 24, DeclarationType.Float2, DeclarationMethod.Default, DeclarationUsage.TextureCoordinate, 1),
                     new VertexElement(0, 32, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
                     VertexElement.VertexDeclarationEnd,
                };
                vertexDeclaration = new VertexDeclaration(graphicsDevice, velements);
            }
            streamIndex = indexBuffer.Lock(0, 0, LockFlags.None);
            streamIndex.WriteRange(indices);
            indexBuffer.Unlock();
        }

        /// <summary>
        /// </summary>
        public override void Draw()
        {
            base.Draw();

            if (vertexBuffer == null || indexBuffer == null) return;

            graphicsDevice.SetRenderState(RenderState.MultisampleAntialias, true);
            graphicsDevice.SetRenderState(RenderState.FillMode, FillMode.Solid);
            graphicsDevice.SetRenderState(RenderState.DepthBias, DepthBias);
            int primitiveCount;
            if (!thickLines)
            {
                graphicsDevice.SetRenderState(RenderState.Lighting, false);
                graphicsDevice.VertexFormat = VertexFormat.Position | VertexFormat.Diffuse;
                graphicsDevice.SetStreamSource(0, vertexBuffer, 0, Marshal.SizeOf(typeof(VertexPositionColor)));
                graphicsDevice.Indices = indexBuffer;
                primitiveCount = indices.Length / 2;
                graphicsDevice.DrawIndexedPrimitive(PrimitiveType.LineList, 0, 0, vertices.Length, 0, primitiveCount);
            }
            else
            {
                graphicsDevice.VertexDeclaration = vertexDeclaration;
                graphicsDevice.SetStreamSource(0, vertexBuffer, 0, Marshal.SizeOf(typeof(ThickLinesVertex)));
                graphicsDevice.Indices = indexBuffer;
                effect.Technique = "Simplest";
                effect.SetValue("XPixels", (float)viewportImage.Width);
                effect.SetValue("YPixels", (float)viewportImage.Height);
                effect.SetValue("LineWidth", (float)LineThickness * (float)dpi / 96.0f);
                effect.SetValue("ViewProjection", viewportImage.View * viewportImage.Projection);
                primitiveCount = indices.Length / 6;
                int numpasses = effect.Begin(0);
                for (int i = 0; i < numpasses; i++)
                {
                    effect.BeginPass(i);
                    graphicsDevice.DrawIndexedPrimitive(PrimitiveType.TriangleList, 0, 0, thickVertices.Length, 0, primitiveCount);
                    effect.EndPass();
                }
                effect.End();
            }
        }

        protected void GraphicsDeviceService_DeviceResetting(object sender, EventArgs e)
        {
            if (effect != null)
            {
                effect.Dispose();
                effect = null;
            }
        }

        protected void GraphicsDeviceService_DeviceReset(object sender, EventArgs e)
        {
            TryCreateEffects();
        }

        public void SetResolution(int dpi)
        {
            if (dpi != this.dpi)
            {
                this.dpi = dpi;
                pointsChanged = true;
                geometryChanged = true;
                thickVertices = null;
                indices = null;
                vertices = null;
            }
            if (effectUnavailable || ((LineThickness == 1.0) && (dpi == 96))) thickLines = false;
            else thickLines = true;
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
            if (Points.Count != 0)
            {
                RecreateBuffers();
                FillBuffers();
            }
        }
    }

    /// <summary>
    /// Custom vertex type for vertices that have a
    /// position, normal and colour.
    /// </summary>
    public struct Point3DColor
    {
        public Point3D Point3D;
        public System.Windows.Media.Color Color;

        public double X
        {
            get { return Point3D.X; }
            set { Point3D.X = value; }
        }

        public double Y
        {
            get { return Point3D.X; }
            set { Point3D.X = value; }
        }

        public double Z
        {
            get { return Point3D.X; }
            set { Point3D.X = value; }
        }

        public Point3DColor(Point3D point3D, System.Windows.Media.Color color)
        {
            Point3D = point3D;
            Color = color;
        }

        public Point3DColor(Point3D point3D)
        {
            Point3D = point3D;
            Color = System.Windows.Media.Colors.Black;
        }

        public Point3DColor(double x, double y, double z, System.Windows.Media.Color color)
        {
            Point3D = new Point3D(x, y, z);
            Color = color;
        }

        public Point3DColor(double x, double y, double z)
        {
            Point3D = new Point3D(x, y, z);
            Color = System.Windows.Media.Colors.Black;
        }

        public static int ColorToInt(System.Windows.Media.Color color)
        {
            return (255 << 24)      // A 
                | (color.R << 16)    // R
                | (color.G << 8)    // G
                | (color.B << 0);   // B
        }
    }

}
