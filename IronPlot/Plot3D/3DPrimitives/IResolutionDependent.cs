using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Interface to be implemented by a primitive whose rendering depends
    /// on the resolution of the output. 
    /// An example is the rendering of a line which is to be 1.0 pixels wide
    /// in device independent pixels.
    /// The rendering of 3D solids does not depend on resultion, by way of counter 
    /// example.
    /// </summary>
    public interface IResolutionDependent
    {
        /// <summary>
        /// Set the resolution under which the primitive is rendered.
        /// </summary>
        /// <param name="dpi">Resolution in dpi.</param>
        void SetResolution(int dpi);
    }
}
