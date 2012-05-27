// Copyright (c) 2010 Joe Moorhouse

//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
using SharpDX;
using SharpDX.Direct3D9;
using System.Runtime.InteropServices;


namespace IronPlot.Plotting3D
{
    /// <summary>
    /// Custom vertex type for vertices that have a
    /// position, normal and colour.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct VertexPositionColor
    {
        public Vector3 Position;
        public int Color;

        /// <summary>
        /// Constructor.
        /// </summary>
        public VertexPositionColor(Vector3 position, int color)
        {
            Position = position;
            Color = color;
        }


        ///// <summary>
        ///// Vertex format information, used to create a VertexDeclaration.
        ///// </summary>
        //public static readonly VertexElement[] VertexElements =
        //{
        //    new VertexElement(0, 0, VertexElementFormat.Vector3,
        //                            VertexElementMethod.Default,
        //                            VertexElementUsage.Position, 0),

        //    new VertexElement(0, 12, VertexElementFormat.Vector3,
        //                             VertexElementMethod.Default,
        //                             VertexElementUsage.Normal, 0),

        //    new VertexElement(0, 24, VertexElementFormat.Color,
        //                             VertexElementMethod.Default,
        //                             VertexElementUsage.Color, 0),
        //};


        /// <summary>
        /// Size of this vertex type.
        /// </summary>
        public const int SizeInBytes = 12 + 4;
    }
}
