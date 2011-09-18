using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace IronPlot
{
    /// <summary>
    /// An observable collection that does not allow duplicates.
    /// </summary>
    /// <typeparam name="T">The type of items in the collection.</typeparam>
    internal class UniqueObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// Inserts an item at an index. Throws if the item already exists in the collection.
        /// </summary>
        /// <param name="index">The index at which to insert the item.</param>
        /// <param name="item">The item to insert.</param>
        protected override void InsertItem(int index, T item)
        {
            if (!this.Contains(item))
            {
                base.InsertItem(index, item);
            }
            else
            {
                throw new InvalidOperationException("Item already exists within collection.");
            }
        }

        /// <summary>
        /// Sets an item at a given index. Throws if the item already exists at another index.
        /// </summary>
        /// <param name="index">The index at which to insert the item.</param>
        /// <param name="item">The item to be inserted.</param>
        protected override void SetItem(int index, T item)
        {
            int newItemIndex = this.IndexOf(item);
            if (newItemIndex != -1 && newItemIndex != index)
            {
                throw new InvalidOperationException("Item already exists within collection.");
            }
            else
            {
                base.SetItem(index, item);
            }
        }

        /// <summary>
        /// Clears all items in the collection by removing them individually.
        /// </summary>
        protected override void ClearItems()
        {
            IList<T> items = new List<T>(this);
            foreach (T item in items)
            {
                Remove(item);
            }
        }
    }
}
