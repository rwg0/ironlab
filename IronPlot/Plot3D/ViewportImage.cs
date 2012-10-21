// Copyright (c) 2010 Joe Moorhouse

using System.Collections.Generic;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Matrix = SharpDX.Matrix;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Supplies 3D graphics device to draw into and Canvas (and associated transform methods) for
    /// vector overlay.
    /// </summary>
    /// <remarks>
    /// Unlikely that there will be an implementer of IViewportImage that is not a ViewportImage;
    /// mainly used to keep track of the methods that the children Model3D objects need.
    /// </remarks>
    public interface IViewportImage
    {
        I2DLayer Layer2D { get; }
        Device GraphicsDevice { get; }
        MatrixTransform3D ModelToWorld { get; set; }
    }
    
    /// <summary>
    /// Class uses SharpDX to render onto an ImageBrush using D3DImage,
    /// and optionally onto a Canvas that can be overlaid for 2D vector annotation.
    /// ViewportImage renders a collection of Model3D objects.
    /// </summary>
    public class ViewportImage : DirectImage, IViewportImage
    {
        #region Fields

        Matrix world;
        Matrix view;
        Matrix projection;
        Matrix cameraTransform;
        float fov, tanSemiFOV;

        I2DLayer layer2D;

        public Matrix World
        {
            get { return world; }
        }

        public Matrix View
        {
            get { return view; }
        }

        public Matrix Projection
        {
            get { return projection; }
        }

        public float Scale { get; internal set; }

        public float FOV
        {
            get { return fov; }

            set
            {
                fov = value;
                tanSemiFOV = (float)Math.Tan((double)fov / 2);
            }
        }

         public float TanSemiFOV
         {
            get { return tanSemiFOV; }
         }

        public I2DLayer Layer2D
        {
            get { return layer2D; }
        }

        public void SetLayer2D(Canvas canvas, MatrixTransform3D modelToWorld) 
        {
            layer2D = new SharpDXLayer2D(canvas, this, modelToWorld);
        }

        internal Viewport3D ViewPort3D { get; set; }

        #endregion

        #region DependencyProperties

        public static readonly DependencyProperty ModelToWorldProperty =
            DependencyProperty.Register("ModelToWorld",
            typeof(MatrixTransform3D), typeof(ViewportImage),
            new PropertyMetadata((MatrixTransform3D)MatrixTransform3D.Identity, OnModelToWorldChanged));

        public static readonly DependencyProperty ModelsProperty =
            DependencyProperty.Register("Models",
            typeof(Model3DCollection), typeof(ViewportImage),
            new PropertyMetadata(null));

        public Model3DCollection Models
        {
            get { return (Model3DCollection)GetValue(ModelsProperty); }
            set { SetValue(ModelsProperty, value); }
        }

        public static readonly DependencyProperty CameraPositionProperty =
            DependencyProperty.Register("CameraPosition",
            typeof(Vector3), typeof(ViewportImage),
            new FrameworkPropertyMetadata(new Vector3(10f, 0, 0),
            OnCameraChanged));

        public static readonly DependencyProperty CameraTargetProperty =
            DependencyProperty.Register("CameraTarget",
            typeof(Vector3), typeof(ViewportImage),
            new FrameworkPropertyMetadata(Vector3.Zero,
            OnCameraChanged));

        public static readonly DependencyProperty CameraUpVectorProperty =
            DependencyProperty.Register("CameraUpVector",
            typeof(Vector3), typeof(ViewportImage),
            new FrameworkPropertyMetadata(new Vector3(0, 0, 1),
            OnCameraChanged));

         
        public MatrixTransform3D ModelToWorld
        {
            get { return (MatrixTransform3D)GetValue(ModelToWorldProperty); }
            set { SetValue(ModelToWorldProperty, value); }
        }

        public Vector3 CameraPosition
        {
            set { SetValue(CameraPositionProperty, value); }
            get { return (Vector3)GetValue(CameraPositionProperty); }
        }

        public Vector3 CameraTarget
        {
            set { SetValue(CameraTargetProperty, value); }
            get { return (Vector3)GetValue(CameraTargetProperty); }
        }

        public Vector3 CameraUpVector
        {
            set { SetValue(CameraUpVectorProperty, value); }
            get { return (Vector3)GetValue(CameraUpVectorProperty); }
        }

        protected static void OnCameraChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ViewportImage viewportImage = ((ViewportImage)obj);
            viewportImage.RequestRender();
            viewportImage.view = Matrix.LookAtRH(viewportImage.CameraPosition, viewportImage.CameraTarget, viewportImage.CameraUpVector);
        }

        protected override void OnVisiblePropertyChanged(bool isVisible)
        {
            if (isVisible == true) foreach (Model3D model in Models) model.RecursiveRecreateDisposables();
            else foreach (Model3D model in Models) model.RecursiveDisposeDisposables();
        }

        protected static void OnModelToWorldChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((ViewportImage)obj).RequestRender();
        }

        #endregion

        public ViewportImage()
            : base()
        {
            layer2D = null;
            CreateDevice(SurfaceType.DirectX9);
            FOV = 0.75f;
            this.Scale = 1;
        }

        protected override void Initialize()
        {
            SetValue(ModelsProperty, new Model3DCollection(this));
            cameraTransform = Matrix.Identity;
            world = Matrix.Identity;
        }

        protected override void Draw()
        {
            // Ensure transforms are updated and clear; otherwise leave to Model3D tree
            GraphicsDevice.Clear(ClearFlags.Target | ClearFlags.ZBuffer, new Color4(1.0f, 1.0f, 1.0f, 1.0f), 1.0f, 0);
            GraphicsDevice.BeginScene();
            float aspect = (float)GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height;

            switch (this.ViewPort3D.ProjectionType)
            {
                case ProjectionType.Perspective:
                    projection = Matrix.PerspectiveFovRH(fov, aspect, 0.01f, 10000f);
                    break;
                case ProjectionType.Orthogonal:
                    float width = (float)this.Models.Select(m => m.Bounds.Maximum.X).Max() * aspect;
                    float height = (float)this.Models.Select(m => m.Bounds.Maximum.Y).Max();
                    projection = Matrix.OrthoRH(width / this.Scale, height / this.Scale, 1, 100);
                    break;
            }
            
            view = Matrix.LookAtRH(CameraPosition, CameraTarget, CameraUpVector);
            world = Matrix.Identity;
            // ENDTODO
            GraphicsDevice.SetTransform(TransformState.Projection, ref projection);
            GraphicsDevice.SetTransform(TransformState.View, ref view);
            GraphicsDevice.SetTransform(TransformState.World, ref world);
            
            foreach (Model3D model in this.Models)
            {
                model.Draw();
            }
            GraphicsDevice.EndScene();
            GraphicsDevice.Present();
        }
    }

    public enum ProjectionType
    {
        Perspective,
        Orthogonal
    }
}
