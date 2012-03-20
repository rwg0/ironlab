// Copyright (c) 2010 Joe Moorhouse (additions only)

//---------------------------------------------------------------------------
//
// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Limited Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/limitedpermissivelicense.mspx
// All other rights reserved.
//
// This file is part of the 3D Tools for Windows Presentation Foundation
// project.  For more information, see:
// 
// http://CodePlex.com/Wiki/View.aspx?ProjectName=3DTools
//
// The following article discusses the mechanics behind this
// trackball implementation: http://viewport3d.com/trackball.htm
//
// Reading the article is not required to use this sample code,
// but skimming it might be useful.
//
//---------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows.Markup;
using Point = System.Windows.Point;
using Mouse = System.Windows.Input.Mouse;
using Quaternion = System.Windows.Media.Media3D.Quaternion;

namespace IronPlot.Plotting3D
{
    public delegate void TrackballEventHandler(object sender, EventArgs e);
    /// <summary>
    ///     Trackball is a utility class which observes the mouse events
    ///     on a specified FrameworkElement and produces a Transform3D
    ///     with the resultant rotation and Scale.
    /// 
    ///     Example Usage:
    /// 
    ///         Trackball trackball = new Trackball();
    ///         trackball.EventSource = myElement;
    ///         myViewport3D.Camera.Transform = trackball.Transform;
    /// 
    ///     Because Viewport3Ds only raise events when the mouse is over the
    ///     rendered 3D geometry (as opposed to not when the mouse is within
    ///     the layout bounds) you usually want to use another element as 
    ///     your EventSource.  For example, a transparent border placed on
    ///     top of your Viewport3D works well:
    ///     
    ///         <Grid>
    ///           <ColumnDefinition />
    ///           <RowDefinition />
    ///           <Viewport3D Name="myViewport" ClipToBounds="True" Grid.Row="0" Grid.Column="0" />
    ///           <Border Name="myElement" Background="Transparent" Grid.Row="0" Grid.Column="0" />
    ///         </Grid>
    ///     
    ///     NOTE: The Transform property may be shared by multiple Cameras
    ///           if you want to have auxilary views following the trackball.
    /// 
    ///           It can also be useful to share the Transform property with
    ///           models in the scene that you want to move with the camera.
    ///           (For example, the Trackport3D's headlight is implemented
    ///           this way.)
    /// 
    ///           You may also use a Transform3DGroup to combine the
    ///           Transform property with additional Transforms.
    /// </summary> 
    public class Trackball : DependencyObject
    {
        protected FrameworkElement _eventSource;
        protected Point _previousPosition2D;
        protected Vector3D _previousPosition3D = new Vector3D(0, 0, 1);

        protected Transform3DGroup _transform;
        protected ScaleTransform3D _scale = new ScaleTransform3D();
        protected AxisAngleRotation3D _rotation = new AxisAngleRotation3D();
        protected TranslateTransform3D _translate = new TranslateTransform3D();

        protected bool _mouseLeftDown, _mouseRightDown;
        
        Quaternion delta;
        double scale;
        Point translation = new Point();

        public event TrackballEventHandler OnTrackBallMoved;
        public event TrackballEventHandler OnTrackBallZoom;
        public event TrackballEventHandler OnTrackBallTranslate;

        protected virtual void RaiseTrackballMovedEvent(EventArgs e)
        {
            if (OnTrackBallMoved != null)
                OnTrackBallMoved(this, e);
        }

        protected virtual void RaiseZoomEvent(EventArgs e)
        {
            if (OnTrackBallZoom != null)
                OnTrackBallZoom(this, e);
        }

        protected virtual void RaiseTranslateEvent(EventArgs e)
        {
            if (OnTrackBallTranslate != null)
                OnTrackBallTranslate(this, e);
        }

        public Quaternion Delta
        {
            get { return delta; }
        }

        public double Scale
        {
            get { return scale; }
        }

        public Point Translation
        {
            get { return translation; }
        }

        public Trackball()
        {
            _transform = new Transform3DGroup();
            _transform.Children.Add(_scale);
            _transform.Children.Add(new RotateTransform3D(_rotation));
        }

        /// <summary>
        ///     A transform to move the camera or scene to the trackball's
        ///     current orientation and Scale.
        /// </summary>
        public Transform3DGroup Transform
        {
            get { return (_transform as Transform3DGroup); }
            //get { return (_matrixTransform as Transform3D); }
        }

        /// <summary>
        ///     A transform to move the camera or scene to the trackball's
        ///     current orientation and Scale.
        /// </summary>
        public AxisAngleRotation3D Rotation
        {
            get { return _rotation; }
            //get { return (_matrixTransform as Transform3D); }
        }

        #region Event Handling

        /// <summary>
        /// The FrameworkElement we listen to for mouse events.
        /// </summary>
        public FrameworkElement EventSource
        {
            get { return _eventSource; }

            set
            {
                if (_eventSource != null)
                {
                    _eventSource.MouseDown -= this.OnMouseDown;
                    _eventSource.MouseUp -= this.OnMouseUp;
                    _eventSource.MouseMove -= this.OnMouseMove;
                    _eventSource.MouseWheel -= this.OnMouseWheel;
                }

                _eventSource = value;

                _eventSource.MouseDown += this.OnMouseDown;
                _eventSource.MouseUp += this.OnMouseUp;
                _eventSource.MouseMove += this.OnMouseMove;
                _eventSource.MouseWheel += this.OnMouseWheel;
            }
        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e)
        {
            Mouse.Capture(EventSource, CaptureMode.Element);
            _previousPosition2D = e.GetPosition(EventSource);
            _previousPosition3D = ProjectToTrackball(
                EventSource.ActualWidth,
                EventSource.ActualHeight,
                _previousPosition2D);
        }

        protected virtual void OnMouseUp(object sender, MouseEventArgs e)
        {
            Mouse.Capture(EventSource, CaptureMode.None);
        }

        protected virtual void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double delta = -e.Delta / 120;
            scale = Math.Pow(1.1, delta);
            Zoom(scale);
        }

        protected virtual void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(EventSource);
            bool ctrlOrShift = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.LeftShift);

            // Prefer tracking to zooming if both buttons are pressed.
            if (e.LeftButton == MouseButtonState.Pressed && !ctrlOrShift)
            {
                if (_mouseLeftDown)
                {
                    Track(currentPosition);
                }
                else
                {
                    _mouseLeftDown = true;
                    Mouse.Capture(EventSource, CaptureMode.Element);
                    _previousPosition2D = e.GetPosition(EventSource);
                    _previousPosition3D = ProjectToTrackball(
                        EventSource.ActualWidth,
                        EventSource.ActualHeight,
                        _previousPosition2D);
                }
            }
            else if (e.RightButton == MouseButtonState.Pressed || (e.LeftButton == MouseButtonState.Pressed && ctrlOrShift))
            {
                if (_mouseRightDown)
                {
                    //if (Zoom(currentPosition)) e.Handled = true;
                    Translate(currentPosition);
                }
                else
                {
                    _mouseRightDown = true;
                    Mouse.Capture(EventSource, CaptureMode.Element);
                    _previousPosition2D = e.GetPosition(EventSource);
                    _previousPosition3D = ProjectToTrackball(
                        EventSource.ActualWidth,
                        EventSource.ActualHeight,
                        _previousPosition2D);
                }
            }
            else
            {
                _mouseLeftDown = false;
                _mouseRightDown = false;
            }

            _previousPosition2D = currentPosition;
        }

        #endregion Event Handling

        protected void Track(Point currentPosition)
        {
            Vector3D currentPosition3D = ProjectToTrackball(
                EventSource.ActualWidth, EventSource.ActualHeight, currentPosition);

            Vector3D axis = Vector3D.CrossProduct(_previousPosition3D, currentPosition3D);
            double angle = Vector3D.AngleBetween(_previousPosition3D, currentPosition3D);
            if (angle == 0.0) return;
            delta = new Quaternion(axis, -angle);

            // Get the current orientantion from the RotateTransform3D
            AxisAngleRotation3D r = _rotation;
            Quaternion q = new Quaternion(_rotation.Axis, _rotation.Angle);

            // Compose the delta with the previous orientation
            q *= delta;

            // Write the new orientation back to the Rotation3D
            _rotation.Axis = q.Axis;
            _rotation.Angle = q.Angle;

            _previousPosition3D = currentPosition3D;

            RaiseTrackballMovedEvent(EventArgs.Empty);
        }

        protected Vector3D ProjectToTrackball(double width, double height, Point point)
        {
            double x = point.X / (width / 2);    // Scale so bounds map to [0,0] - [2,2]
            double y = point.Y / (height / 2);

            x = x - 1;                           // Translate 0,0 to the center
            y = 1 - y;                           // Flip so +Y is up instead of down

            double z2 = 1 - x * x - y * y;       // z^2 = 1 - x^2 - y^2
            double z = z2 > 0 ? Math.Sqrt(z2) : 0;
            return new Vector3D(x, y, z);
        }

        protected bool Zoom(Point currentPosition)
        {
            double yDelta = currentPosition.Y - _previousPosition2D.Y;
            scale = Math.Exp(yDelta / 100);    // e^(yDelta/100) is fairly arbitrary.
            return Zoom(scale);
        }

        protected bool Zoom(double factor)
        {
            _scale.ScaleX *= Scale;
            _scale.ScaleY *= Scale;
            _scale.ScaleZ *= Scale;

            RaiseZoomEvent(EventArgs.Empty);

            return (factor != 1.0);
        }

        protected void Translate(Point currentPosition)
        {
            translation = new Point((currentPosition.X - _previousPosition2D.X) / EventSource.ActualWidth, 
                (currentPosition.Y - _previousPosition2D.Y) / EventSource.ActualHeight);
            RaiseTranslateEvent(EventArgs.Empty);
        }
    }
}

