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
using System.Windows.Data;

namespace IronPlot.Plotting3D
{
    internal interface I3DDrawable
    {
        void Initialize();
        /// <summary>
        /// Render the primitive using the GraphicsDevice and the Effect (if any).
        /// </summary>
        void Draw();
    }

    public delegate void OnDrawEventHandler(object sender, EventArgs e);
    //public delegate void OnRequestRenderEventHandler(object sender, EventArgs e);

    public class Model3D : DependencyObject, IViewportImage, IBoundable3D
    {
        internal ViewportImage viewportImage;
        protected Device graphicsDevice;
        protected I2DLayer layer2D;
        protected Cuboid bounds;

        #region TreeStructure

        public static readonly DependencyProperty ChildrenProperty =
            DependencyProperty.Register("Children",
            typeof(Model3DCollection), typeof(Model3D),
            new PropertyMetadata(null));

        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.Register("IsVisible",
            typeof(bool), typeof(Model3D),
            new PropertyMetadata(true, OnUpdateIsVisibleProperty));

        protected static void OnUpdateIsVisibleProperty(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Model3D)obj).RequestRender(EventArgs.Empty);
        }

        public Model3DCollection Children
        {
            get { return (Model3DCollection)GetValue(ChildrenProperty); }
            set { SetValue(ChildrenProperty, value); }
        }

        public bool IsVisible
        {
            get { return (bool)GetValue(IsVisibleProperty); }
            set { SetValue(IsVisibleProperty, value); }
        }

        internal void RecursiveSetViewportImage(ViewportImage viewportImage)
        {
            if (viewportImage == null)
            {
                this.DisposeDisposables();
                this.viewportImage = null;
                this.graphicsDevice = null;
                this.layer2D = null;
                Children.viewportImage = null;
            }
            else
            {                
                this.graphicsDevice = viewportImage.GraphicsDevice;
                this.layer2D = viewportImage.Layer2D;
                OnViewportImageChanged(viewportImage);
                BindToViewportImage();
                Children.viewportImage = viewportImage;
                this.RecreateDisposables();
            }
            foreach (Model3D model in this.Children)
            {
                model.RecursiveSetViewportImage(viewportImage);
            }
            // TODO Set target on all children, checking for maximum depth or
            // circular dependencies
        }

        internal void RecursiveSetResolution(int dpi)
        {
            if (this is IResolutionDependent) (this as IResolutionDependent).SetResolution(dpi);
            foreach (Model3D model in this.Children)
            {
                model.RecursiveSetResolution(dpi);
            }
        }

        internal void RecursiveDisposeDisposables()
        {
            DisposeDisposables();
            foreach (Model3D model in this.Children)
            {
                model.RecursiveDisposeDisposables();
            }
        }

        internal void RecursiveRecreateDisposables()
        {
            RecreateDisposables();
            foreach (Model3D model in this.Children)
            {
                model.RecursiveRecreateDisposables();
            }
        }

        internal void BindToViewportImage()
        {
            Binding bindingTransform = new Binding("ModelToWorld");
            bindingTransform.Source = viewportImage;
            bindingTransform.Mode = BindingMode.OneWay;
            BindingOperations.SetBinding(this, Model3D.ModelToWorldProperty, bindingTransform);
            RenderRequested += viewportImage.RequestRender;
        }

        internal void RemoveBindToViewportImage()
        {
            if (viewportImage != null)
            {
                BindingOperations.ClearBinding(this, Model3D.ModelToWorldProperty);
                RenderRequested -= viewportImage.RequestRender;
            }
        }
        #endregion

        public I2DLayer Layer2D
        {
            get { return layer2D; }
        }

        public Cuboid Bounds
        {
            get { return bounds; }
        }

        public Device GraphicsDevice
        {
            get { return graphicsDevice; }
        }

        protected bool geometryChanged = true;

        static object drawLock = new object();

        public event OnDrawEventHandler OnDraw;

        public event EventHandler RenderRequested;

        // Invoke the OnDraw event
        protected virtual void RaiseOnDrawEvent(EventArgs e)
        {
            if (OnDraw != null)
                OnDraw(this, e);
        }

        // Invoke the OnRequestRender event
        public virtual void RequestRender(EventArgs e)
        {
            if (RenderRequested != null)
                RenderRequested(this, e);
        }

        public static readonly DependencyProperty ModelToWorldProperty =
            DependencyProperty.Register("ModelToWorld",
            typeof(MatrixTransform3D), typeof(Model3D),
            new PropertyMetadata((MatrixTransform3D)MatrixTransform3D.Identity,
                OnModelToWorldChanged));

        public MatrixTransform3D ModelToWorld
        {
            get { return (MatrixTransform3D)GetValue(ModelToWorldProperty); }
            set { SetValue(ModelToWorldProperty, value); }
        }

        protected static void OnModelToWorldChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            ((Model3D)obj).OnModelToWorldChanged();
        }

        protected virtual void OnModelToWorldChanged()
        {
            geometryChanged = true;
            RequestRender(EventArgs.Empty);
        }

        public Model3D()
        {
            SetValue(ChildrenProperty, new Model3DCollection(this));
        }

        //public Model3D(Device graphicsDevice)
        //{
        //    SetValue(ChildrenProperty, new Model3DCollection(this));
        //    Initialize();
        //}

        internal virtual void OnViewportImageChanged(ViewportImage newViewportImage)
        {
            RemoveBindToViewportImage();
            viewportImage = newViewportImage;
            this.graphicsDevice = (viewportImage == null) ? null : viewportImage.GraphicsDevice;
            this.layer2D = (viewportImage == null) ? null : viewportImage.Layer2D;
            BindToViewportImage();
            Children.viewportImage = (viewportImage == null) ? null : viewportImage;
            geometryChanged = false;
        }

        /// <summary>
        /// Update geometry (vertices and indices) 
        /// </summary>
        protected virtual void UpdateGeometry()
        {
            // Do nothing in base
        }

        /// <summary>
        /// Draw the model in GraphicsDevice
        /// </summary>
        public virtual void Draw()
        {
            if (geometryChanged)
            {
                geometryChanged = false;
                UpdateGeometry();
            }
            foreach (Model3D child in Children)
            {
                if (child.IsVisible) child.Draw();
            }
            RaiseOnDrawEvent(EventArgs.Empty);
        }

        /// <summary>
        /// Dispose any disposable (wholly owned) members.
        /// </summary>
        protected virtual void DisposeDisposables()
        {
            // No disposables in base.
        }

        /// <summary>
        /// Reinstate any disposable (wholly owned) members.
        /// </summary>
        protected virtual void RecreateDisposables()
        {
            // No disposables in base.
        }
    }
}

