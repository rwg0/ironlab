// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using SharpDX;
using SharpDX.Direct3D9;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Markup;
using IronPlot.ManagedD3D;

namespace IronPlot.Plotting3D
{
    //[ContentProperty("ModelsProperty")]
    public class Viewport3D : PlotPanelBase
    {     
        internal Trackball trackball;
        private Axes3D axes;
        private Viewport3DControl viewport3DControl;
        private ViewportImage viewport3DImage; 

        public Axes3D Axes
        {
            get { return axes; }
        }

        #region DependencyProperties
        
        public static readonly DependencyProperty ModelsProperty =
            DependencyProperty.Register("Models",
            typeof(Model3DCollection), typeof(Viewport3D),
            new PropertyMetadata(null));
        
        public static readonly DependencyProperty GraphToWorldProperty =
            DependencyProperty.Register("GraphToWorld",
            typeof(MatrixTransform3D), typeof(Viewport3D),
            new PropertyMetadata((MatrixTransform3D)MatrixTransform3D.Identity, OnGraphToWorldChanged));

        public static readonly DependencyProperty GraphMinProperty =
            DependencyProperty.Register("GraphMin",
            typeof(Point3D), typeof(Viewport3D),
            new PropertyMetadata(new Point3D(-10, -10, -10), OnUpdateGraphMinMax));

        public static readonly DependencyProperty GraphMaxProperty =
            DependencyProperty.Register("GraphMax",
            typeof(Point3D), typeof(Viewport3D),
            new PropertyMetadata(new Point3D(10, 10, 10), OnUpdateGraphMinMax));

        public static readonly DependencyProperty ProjectionTypeProperty =
            DependencyProperty.Register("ProjectionType",
            typeof(ProjectionType), typeof(Viewport3D),
            new FrameworkPropertyMetadata(ProjectionType.Perspective));

        public MatrixTransform3D GraphToWorld
        {
            get { return (MatrixTransform3D)GetValue(GraphToWorldProperty); }
            set { SetValue(GraphToWorldProperty, value); }
        }

        public Model3DCollection Models
        {
            get { return (Model3DCollection)GetValue(ModelsProperty); }
            set { SetValue(ModelsProperty, value); }
        }

        public Point3D GraphMin
        {
            get { return (Point3D)GetValue(GraphMinProperty); }
            set { SetValue(GraphMinProperty, value); }
        }

        public Point3D GraphMax
        {
            get { return (Point3D)GetValue(GraphMaxProperty); }
            set { SetValue(GraphMaxProperty, value); }
        }

        public ProjectionType ProjectionType
        {
            set { SetValue(ProjectionTypeProperty, value); }
            get { return (ProjectionType)GetValue(ProjectionTypeProperty); }
        }

        protected Point3D worldMin;
        public Point3D WorldMin
        {
            get
            {
                try
                {
                    worldMin = GraphToWorld.Transform(GraphMin);
                }
                catch
                {
                    worldMin = new Point3D(0, 0, 0);
                }
                return worldMin;
            }
            set
            {
                worldMin = value;
                UpdateGraphToWorld();
            }
        }

        protected Point3D worldMax;
        public Point3D WorldMax
        {
            get
            {
                try
                {
                    worldMax = GraphToWorld.Transform(GraphMax);
                }
                catch
                {
                    worldMax = new Point3D(0, 0, 0);
                }
                return worldMax;
            }
            set
            {
                worldMax = value;
                UpdateGraphToWorld();
            }
        }

        protected enum UpdateType { UpdateWorldMin, UpdateWorldMax, AlreadyUpdated };

        protected static void OnGraphToWorldChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
        }

        protected static void OnUpdateGraphMinMax(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Viewport3D)obj).UpdateGraphToWorld(); 
        }

        protected void UpdateGraphToWorld()
        {
            Point3D graphMin = (Point3D)GetValue(GraphMinProperty);
            Point3D graphMax = (Point3D)GetValue(GraphMaxProperty);
            double scaleX = (worldMax.X - worldMin.X) / (graphMax.X - graphMin.X);
            double scaleY = (worldMax.Y - worldMin.Y) / (graphMax.Y - graphMin.Y);
            double scaleZ = (worldMax.Z - worldMin.Z) / (graphMax.Z - graphMin.Z);
            double offX = -graphMin.X * scaleX + worldMin.X;
            double offY = -graphMin.Y * scaleY + worldMin.Y;
            double offZ = -graphMin.Z * scaleZ + worldMin.Z;

            Matrix3D transform = new Matrix3D(scaleX, 0, 0, 0, 0, scaleY, 0, 0,
                0, 0, scaleZ, 0, offX, offY, offZ, 1);

            MatrixTransform3D matrixTransform = new MatrixTransform3D();
            matrixTransform.Matrix = transform;
            SetValue(GraphToWorldProperty, matrixTransform);
        }

        #endregion

        public Viewport3D() : base()
        {
            Initialize();
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            MeasureAnnotations(availableSize);
            // Return the region available for plotting and set legendRegion:
            Rect available = PlaceAnnotations(availableSize);
            viewport3DControl.Measure(new Size(available.Width, available.Height));
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect final = PlaceAnnotations(finalSize);
            AxesRegion = final;
            axes.UpdateLabels();
            ArrangeAnnotations(finalSize);
            viewport3DControl.Arrange(final);
            return finalSize;
        }
        
        public void Initialize()
        {
            Background = null;
            viewport3DControl = new Viewport3DControl();
            viewport3DControl.SetValue(Grid.ZIndexProperty, 100);
            this.Children.Add(viewport3DControl);

            viewport3DImage = viewport3DControl.Viewport3DImage;
            if (viewport3DImage == null)
            {
                axes = new Axes3D();
                SetValue(ModelsProperty, new Model3DCollection(null));
                return;
            }
            viewport3DImage.ViewPort3D = this;

            SetValue(ModelsProperty, viewport3DImage.Models);
            // Set the owner of the Model3DCollection to be the D3DImageViewport
            // This ensures that the Model3D objects are rendered by the D3DImageViewport
            viewport3DImage.SetLayer2D(viewport3DImage.Canvas, GraphToWorld);

            viewport3DImage.Models.Changed += new Model3DCollection.ItemEventHandler(Models_Changed);

            trackball = new Trackball();
            trackball.EventSource = viewport3DControl; //viewport3DImage.Canvas;
            trackball.OnTrackBallMoved += new TrackballEventHandler(trackball_TrackBallMoved);
            trackball.OnTrackBallZoom += new TrackballEventHandler(trackball_OnTrackBallZoom);
            trackball.OnTrackBallTranslate += new TrackballEventHandler(trackball_OnTrackBallTranslate);

            axes = new Axes3D();
            viewport3DImage.Models.Add(axes);

            viewport3DImage.CameraPosition = new Vector3(-3f, -3f, 2f);
            viewport3DImage.CameraTarget = new Vector3(0f, 0f, 0f);
            //
            Binding bindingGraphMin = new Binding("GraphMin");
            bindingGraphMin.Source = this;
            bindingGraphMin.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(axes, Axes3D.GraphMinProperty, bindingGraphMin);
            Binding bindingGraphMax = new Binding("GraphMax");
            bindingGraphMax.Source = this;
            bindingGraphMax.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(axes, Axes3D.GraphMaxProperty, bindingGraphMax);
            Binding bindingGraphToWorld = new Binding("GraphToWorld");
            bindingGraphToWorld.Source = this;
            bindingGraphToWorld.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(viewport3DImage, ViewportImage.ModelToWorldProperty, bindingGraphToWorld);
            ////
            GraphMax = new Point3D(1, 1, 1);
            GraphMin = new Point3D(-1, -1, -1);
            WorldMin = new Point3D(-1, -1, -1);
            WorldMax = new Point3D(1, 1, 1);
            axes.UpdateOpenSides(FindPhi());

            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(Viewport3D_IsVisibleChanged);
        }

        void Viewport3D_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            viewport3DImage.Visible = (bool)e.NewValue;
        }

        /// <summary>
        /// Handle request to re-render the scene from the 3D models.
        /// Tell the ViewPort3D to re-render.
        /// </summary>
        protected void OnRequestRender(Object sender, EventArgs e)
        {
            viewport3DImage.RequestRender();
        }

        /// <summary>
        /// Fired when model added or removed
        /// </summary>
        protected void Models_Changed(Object sender, ItemEventArgs e)
        {
            // Find new bounds that gives the world max and min
            Cuboid bounds = new Cuboid(new Point3D(0, 0, 0), new Point3D(0, 0, 0));
            foreach (Model3D model in (Model3DCollection)sender)
            {
                bounds = Cuboid.Union(model.Bounds, bounds);
            }
            if (!bounds.IsPhysical)
            {
                bounds = new Cuboid(new Point3D(-1, -1, -1), new Point3D(1, 1, 1));
            }
            GraphMin = bounds.Minimum;
            GraphMax = bounds.Maximum;
        }
        
        protected void Base_OnDraw(Object sender, EventArgs e)
        {
            axes.UpdateLabels();
        }

        protected void trackball_TrackBallMoved(Object sender, EventArgs e)
        {
            System.Windows.Media.Media3D.Quaternion delta = ((Trackball)sender).Delta;
            Vector3 deltaAxis = new Vector3();
            deltaAxis.X = (float)delta.Axis.X; 
            deltaAxis.Y = (float)delta.Axis.Y;
            deltaAxis.Z = (float)delta.Axis.Z;
            deltaAxis.Normalize();
            Vector3 cameraLookDirection = viewport3DImage.CameraTarget - viewport3DImage.CameraPosition;
            Vector3 cameraUpDirection = viewport3DImage.CameraUpVector;
            cameraLookDirection.Normalize();
            // Subtract any component of cameraUpDirection along cameraLookDirection
            cameraUpDirection = cameraUpDirection - Vector3.Multiply(cameraLookDirection, Vector3.Dot(cameraUpDirection, cameraLookDirection));
            cameraUpDirection.Normalize();
            Vector3 cameraX = Vector3.Cross(cameraLookDirection, cameraUpDirection);
            // Get axis of rotation in the camera coordinates
            Vector3 deltaAxisWorld = Vector3.Multiply(cameraX, deltaAxis.X) +
                Vector3.Multiply(cameraUpDirection, deltaAxis.Y) +
                Vector3.Multiply(cameraLookDirection, -deltaAxis.Z);
            SharpDX.Matrix cameraTransform = SharpDX.Matrix.RotationAxis(deltaAxisWorld, (float)(delta.Angle * Math.PI / 180.0));
            viewport3DImage.CameraTarget = Vector3.Transform(viewport3DImage.CameraTarget, cameraTransform).ToVector3();
            viewport3DImage.CameraPosition = Vector3.Transform(viewport3DImage.CameraPosition, cameraTransform).ToVector3();
            viewport3DImage.CameraUpVector = Vector3.Transform(viewport3DImage.CameraUpVector, cameraTransform).ToVector3();
            double newPhi = FindPhi();
            if (newPhi != lastPhi)
            {
                lastPhi = newPhi;
                axes.UpdateOpenSides(newPhi);
            }
        }

        protected void trackball_OnTrackBallZoom(Object sender, EventArgs e)
        {
            double Scale = ((Trackball)sender).Scale;
            switch (this.ProjectionType)
            {
                case ProjectionType.Perspective:
                    viewport3DImage.CameraPosition = Vector3.Multiply(viewport3DImage.CameraPosition, (float)Scale);
                    break;
                case ProjectionType.Orthogonal:
                    viewport3DImage.Scale = Convert.ToSingle(viewport3DImage.Scale / Scale);
                    viewport3DImage.RequestRender();
                    break;
            }
        }

        protected void trackball_OnTrackBallTranslate(Object sender, EventArgs e)
        {
            Point translation = ((Trackball)sender).Translation;
            Vector3 cameraLookDirection = viewport3DImage.CameraTarget - viewport3DImage.CameraPosition;
            float distance = cameraLookDirection.Length();
            Vector3 cameraUpDirection = viewport3DImage.CameraUpVector;
            cameraLookDirection.Normalize();
            // Subtract any component of cameraUpDirection along cameraLookDirection
            cameraUpDirection = cameraUpDirection - Vector3.Multiply(cameraLookDirection, Vector3.Dot(cameraUpDirection, cameraLookDirection));
            cameraUpDirection.Normalize();
            Vector3 cameraX = Vector3.Cross(cameraLookDirection, cameraUpDirection);
            float scalingFactor = viewport3DImage.TanSemiFOV * distance * 2;
            Vector3 pan = -cameraX * (float)translation.X * scalingFactor + cameraUpDirection * (float)translation.Y * scalingFactor;
            viewport3DImage.CameraPosition = viewport3DImage.CameraPosition + pan;
            viewport3DImage.CameraTarget = viewport3DImage.CameraTarget + pan;
        }

        private double lastPhi = -10;
        /// <summary>
        /// Calculate azimuthal angle
        /// </summary>
        /// <returns></returns>
        protected double FindPhi()
        {
            Vector3 vector = viewport3DImage.CameraPosition - viewport3DImage.CameraTarget;
            return Math.Atan2(vector.Y, vector.X);
        }

        /// <summary>
        /// Set the resolution of the 3D components.
        /// This is used for printing and copying to clipboard etc.
        /// </summary>
        /// <param name="dpi">Resolution in dpi</param>
        internal void SetResolution(int dpi)
        {
            int width = (int)(viewport3DControl.ActualWidth * dpi / 96.0);
            int height = (int)(viewport3DControl.ActualHeight * dpi / 96.0);
            Models.SetModelResolution(dpi);
            viewport3DImage.SetImageSize((int)viewport3DControl.ActualWidth, (int)viewport3DControl.ActualHeight, dpi);
            viewport3DImage.RenderScene();
        }
    }
}
