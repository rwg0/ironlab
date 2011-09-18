// Copyright (c) 2010 Joe Moorhouse

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace IronPlot
{
    public interface IBoundable
    {
        Rect Bounds
        {
            get;
        }
    }
}
