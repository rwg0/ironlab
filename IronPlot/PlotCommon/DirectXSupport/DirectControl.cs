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
using System.Windows.Threading;

namespace IronPlot
{
    public abstract class DirectControl : Panel
    {
        ImageBrush sceneImage;
        protected DirectImage directImage;
        protected System.Windows.Shapes.Rectangle rectangle;
        TextBlock messageBlock;

        bool initializationFailed = false;
        public bool InitializationFailed { get { return initializationFailed; } }

        // The D3DImage seems, under some cirmstances, not to reaquire its front buffer after
        // loss due to a ctrl-alt-del, screen saver etc. For robustness, a timer checks for a
        // lost front buffer.
        protected DispatcherTimer frontBufferCheckTimer = new DispatcherTimer();

        public DirectControl()
        {
            rectangle = new System.Windows.Shapes.Rectangle();
            try
            {
                CreateDirectImage();
                //throw new Exception("Artificial exception");
            }
            catch (Exception e)
            {
                // In case of error, just display message on Control
                messageBlock = new TextBlock() { Margin = new Thickness(5), Foreground = Brushes.Red };
                messageBlock.Text = "Error initializing DirectX control: " +  e.Message;
                messageBlock.Text += "\rTry installing latest DirectX End-User Runtime";
                this.Children.Add(messageBlock);
                directImage = null;
                initializationFailed = true;
                return;
            }
            directImage.RenderRequested += new EventHandler(directImage_RenderRequested);
            this.Children.Add(rectangle);
            RecreateImage();
            this.IsVisibleChanged += new DependencyPropertyChangedEventHandler(DirectControl_IsVisibleChanged);
            frontBufferCheckTimer.Tick += new EventHandler(frontBufferCheckTimer_Tick);
            frontBufferCheckTimer.Interval = TimeSpan.FromSeconds(0.5);
            frontBufferCheckTimer.Start();
        }

        void directImage_RenderRequested(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        protected abstract void CreateDirectImage();

        void frontBufferCheckTimer_Tick(object sender, EventArgs e)
        {
            CheckImage();
        }

        void CheckImage()
        {
            if (!initializationFailed && (directImage.d3dImage == null || !directImage.d3dImage.IsFrontBufferAvailable))
            {
                RecreateImage();
                directImage.RenderScene();
                this.InvalidateMeasure();
            }
        }

        public void RecreateImage()
        {
            directImage.RecreateD3DImage(true);
            sceneImage = directImage.ImageBrush;
            rectangle.Fill = sceneImage;
            sceneImage.TileMode = TileMode.None;
        }

        public void RequestRender()
        {
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            if (!initializationFailed)
            {
                CheckImage();
                directImage.RenderScene();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            Random random = new Random();
            this.rectangle.Measure(availableSize);
            if (directImage == null) this.messageBlock.Measure(availableSize);
            return availableSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double width = finalSize.Width;
            double height = finalSize.Height;
            rectangle.Arrange(new Rect(0, 0, width, height));
            if (directImage != null)
            {
                if (directImage.d3dImage != null) directImage.SetImageSize((int)width, (int)height, 96);
            }
            else messageBlock.Arrange(new Rect(0, 0, width, height));
            return finalSize;
        }

        void DirectControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            directImage.Visible = (bool)e.NewValue;
            if (directImage.Visible == true)
            {
                OnVisibleChanged_Visible();
                if (Parent is FrameworkElement) (Parent as FrameworkElement).InvalidateMeasure();
                frontBufferCheckTimer.Start();
                directImage.RegisterWithService();
            }
            else
            {
                OnVisibleChanged_NotVisible();
                frontBufferCheckTimer.Stop();
                directImage.UnregisterWithService();
            }
        }

        protected abstract void OnVisibleChanged_Visible();

        protected abstract void OnVisibleChanged_NotVisible();
    }
}

