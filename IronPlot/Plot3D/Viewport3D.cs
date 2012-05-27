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
        private Grid grid;
        private ImageBrush sceneImage;
        private Trackball trackball;
        private ViewportImage d3dImageViewport;
        private Axes3D axes;

        public Axes3D Axes
        {
            get { return axes; }
        }

        #region DependencyProperties
        
        public static readonly DependencyProperty ModelsProperty =
            DependencyProperty.Register("ModelsProperty",
            typeof(Model3DCollection), typeof(Viewport3D),
            new PropertyMetadata(null));
        
        public static readonly DependencyProperty GraphToWorldProperty =
            DependencyProperty.Register("GraphToWorldProperty",
            typeof(MatrixTransform3D), typeof(Viewport3D),
            new PropertyMetadata((MatrixTransform3D)MatrixTransform3D.Identity, OnGraphToWorldChanged));

        public static readonly DependencyProperty GraphMinProperty =
            DependencyProperty.Register("GraphMinProperty",
            typeof(Point3D), typeof(Viewport3D),
            new PropertyMetadata(new Point3D(-10, -10, -10), OnUpdateGraphMinMax));

        public static readonly DependencyProperty GraphMaxProperty =
            DependencyProperty.Register("GraphMaxProperty",
            typeof(Point3D), typeof(Viewport3D),
            new PropertyMetadata(new Point3D(10, 10, 10), OnUpdateGraphMinMax));

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
            grid.Measure(new Size(available.Width, available.Height));
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Rect final = PlaceAnnotations(finalSize);
            AxesRegion = final;
            grid.Arrange(final);
            ArrangeAnnotations(finalSize);
            d3dImageViewport.RequestRender();
            return finalSize;
        }
        
        public void Initialize()
        {
            grid = new Grid();
            this.Children.Add(grid);
            try
            {
                d3dImageViewport = new ViewportImage() { ViewPort3D = this };
            }
            catch (Exception e)
            {
                // In case of error, just display message on Control
                TextBlock messageBlock = new TextBlock();
                messageBlock.Text = e.Message;
                grid.Children.Add(messageBlock);
                SetValue(ModelsProperty, new Model3DCollection(null));
                return;
            }
            SetValue(ModelsProperty, d3dImageViewport.Models);
            // Set the owner of the Model3DCollection to be the D3DImageViewport
            // This ensures that the Model3D objects are rendered by the D3DImageViewport
            sceneImage = d3dImageViewport.ImageBrush;
            sceneImage.TileMode = TileMode.None;
            grid.Background = sceneImage;

            Canvas canvas = new Canvas() { ClipToBounds = true, Background = Brushes.Transparent };
            d3dImageViewport.SetLayer2D(canvas, GraphToWorld);

            d3dImageViewport.Models.Changed += new Model3DCollection.ItemEventHandler(Models_Changed);

            grid.Children.Add(canvas);
            grid.SizeChanged += new SizeChangedEventHandler(d3dImageViewport.OnSizeChanged);
            trackball = new Trackball();
            trackball.EventSource = canvas;
            trackball.OnTrackBallMoved += new TrackballEventHandler(trackball_TrackBallMoved);
            trackball.OnTrackBallZoom += new TrackballEventHandler(trackball_OnTrackBallZoom);
            trackball.OnTrackBallTranslate += new TrackballEventHandler(trackball_OnTrackBallTranslate);

            axes = new Axes3D();
            d3dImageViewport.Models.Add(axes);

            d3dImageViewport.CameraPosition = new Vector3(-3f, -3f, 2f);
            d3dImageViewport.CameraTarget = new Vector3(0f, 0f, 0f);
            //
            Binding bindingGraphMin = new Binding("GraphMinProperty");
            bindingGraphMin.Source = this;
            bindingGraphMin.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(axes, Axes3D.GraphMinProperty, bindingGraphMin);
            Binding bindingGraphMax = new Binding("GraphMaxProperty");
            bindingGraphMax.Source = this;
            bindingGraphMax.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(axes, Axes3D.GraphMaxProperty, bindingGraphMax);
            Binding bindingGraphToWorld = new Binding("GraphToWorldProperty");
            bindingGraphToWorld.Source = this;
            bindingGraphToWorld.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(d3dImageViewport, ViewportImage.ModelToWorldProperty, bindingGraphToWorld);
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
            d3dImageViewport.Visible = (bool)e.NewValue;
        }

        /// <summary>
        /// Handle request to re-render the scene from the 3D models.
        /// Tell the ViewPort3D to re-render.
        /// </summary>
        protected void OnRequestRender(Object sender, EventArgs e)
        {
            d3dImageViewport.RequestRender();
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
            Vector3 cameraLookDirection = d3dImageViewport.CameraTarget - d3dImageViewport.CameraPosition;
            Vector3 cameraUpDirection = d3dImageViewport.CameraUpVector;
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
            d3dImageViewport.CameraTarget = Vector3.Transform(d3dImageViewport.CameraTarget, cameraTransform).ToVector3();
            d3dImageViewport.CameraPosition = Vector3.Transform(d3dImageViewport.CameraPosition, cameraTransform).ToVector3();
            d3dImageViewport.CameraUpVector = Vector3.Transform(d3dImageViewport.CameraUpVector, cameraTransform).ToVector3();
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
            d3dImageViewport.CameraPosition = Vector3.Multiply(d3dImageViewport.CameraPosition, (float)Scale);
        }

        protected void trackball_OnTrackBallTranslate(Object sender, EventArgs e)
        {
            Point translation = ((Trackball)sender).Translation;
            Vector3 cameraLookDirection = d3dImageViewport.CameraTarget - d3dImageViewport.CameraPosition;
            float distance = cameraLookDirection.Length();
            Vector3 cameraUpDirection = d3dImageViewport.CameraUpVector;
            cameraLookDirection.Normalize();
            // Subtract any component of cameraUpDirection along cameraLookDirection
            cameraUpDirection = cameraUpDirection - Vector3.Multiply(cameraLookDirection, Vector3.Dot(cameraUpDirection, cameraLookDirection));
            cameraUpDirection.Normalize();
            Vector3 cameraX = Vector3.Cross(cameraLookDirection, cameraUpDirection);
            float scalingFactor = d3dImageViewport.TanSemiFOV * distance * 2;
            Vector3 pan = -cameraX * (float)translation.X * scalingFactor + cameraUpDirection * (float)translation.Y * scalingFactor;
            d3dImageViewport.CameraPosition = d3dImageViewport.CameraPosition + pan;
            d3dImageViewport.CameraTarget = d3dImageViewport.CameraTarget + pan;
        }

        private double lastPhi = -10;
        /// <summary>
        /// Calculate azimuthal angle
        /// </summary>
        /// <returns></returns>
        protected double FindPhi()
        {
            Vector3 vector = d3dImageViewport.CameraPosition - d3dImageViewport.CameraTarget;
            return Math.Atan2(vector.Y, vector.X);
        }

        /// <summary>
        /// Set the resolution of the 3D components.
        /// This is used for printing and copying to clipboard etc.
        /// </summary>
        /// <param name="dpi">Resolution in dpi</param>
        internal void SetResolution(int dpi)
        {
            int width = (int)(grid.ActualWidth * dpi / 96.0);
            int height = (int)(grid.ActualHeight * dpi / 96.0);
            Models.SetModelResolution(dpi);
            d3dImageViewport.SetImageSize((int)grid.ActualWidth, (int)grid.ActualHeight, dpi);
            d3dImageViewport.RenderScene();
        }
    }
}
