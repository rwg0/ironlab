using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace IronPlot
{
    public class LabelCacheItem
    {
        /// <summary>
        /// The label
        /// </summary>
        public TextBlock Label = new TextBlock();

        public string CacheKey = String.Empty;

        /// <summary>
        /// If AxisType and Value are the same, Label does not require alteration.
        /// </summary>
        public AxisType AxisType = AxisType.Linear;
        /// <summary>
        /// If AxisType and Value are the same, Label does not require alteration.
        /// </summary>
        public double Value = Double.NaN;
        public int RequiredDPs = 0;

        public bool TextRequiresChange(string newKey)
        {
            return newKey != CacheKey;
        }

        /// <summary>
        /// Whether or not the label should be shown. Note that Visibility property is not used as this value may change
        /// several times over layout passes.
        /// </summary>
        public bool IsShown = true;
    }

    public class LabelCache : List<LabelCacheItem> 
    {
        public void Invalidate()
        {
            foreach (LabelCacheItem item in this) item.CacheKey = String.Empty;
        }
    }
}
