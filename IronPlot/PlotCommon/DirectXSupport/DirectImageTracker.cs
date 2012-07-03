using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IronPlot
{
    /// <summary>
    /// Class to keep track of visible DirectImage classes
    /// </summary>
    public class DirectImageTracker
    {
        List<DirectImage> imageList = new List<DirectImage>();

        public void Register(DirectImage image)
        {
            if (imageList.Contains(image)) throw new Exception("Multiple registration attempted."); 
            imageList.Add(image);
        }

        public void Unregister(DirectImage image)
        {
            imageList.Remove(image);
        }

        public void GetSizeForMembers(out int width, out int height)
        {
            if (imageList.Count == 0) width = height = 0;
            else
            {
                width = imageList.Max(t => t.ViewportWidth);
                height = imageList.Max(t => t.ViewportHeight);
            }
        }
    }
}
