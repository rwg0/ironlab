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
    public partial class Direct2DControl : DirectControl
    {
        public List<DirectPath> Paths
        {
            get { return (directImage as Direct2DImage).paths; }
        }

        public void AddPath(DirectPath path)
        {
            (directImage as Direct2DImage).paths.Add(path);
            path.DirectImage = directImage;
            path.RecreateDisposables();
        }

        public void RemovePath(DirectPath path)
        {
            (directImage as Direct2DImage).paths.Remove(path);
            path.Dispose();
        }

        protected override void CreateDirectImage()
        {
            directImage = new Direct2DImage();
        }

        protected override void OnVisibleChanged_Visible()
        {
            foreach (DirectPath path in Paths) path.RecreateDisposables();
        }

        protected override void OnVisibleChanged_NotVisible()
        {
            foreach (DirectPath path in Paths) path.DisposeDisposables();
        }
    }
}

