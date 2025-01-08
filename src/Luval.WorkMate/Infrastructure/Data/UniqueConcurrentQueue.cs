using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Luval.WorkMate.Infrastructure.Data
{
    /// <summary>
    /// A thread-safe queue that ensures each item is unique based on a specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the key used to ensure uniqueness.</typeparam>
    /// <typeparam name="TEntity">The type of the items in the queue.</typeparam>
    public class UniqueConcurrentQueue<TKey, TEntity>
    {
        private readonly ConcurrentQueue<TEntity> _queue = new ConcurrentQueue<TEntity>();
        private readonly HashSet<TKey> _set = new HashSet<TKey>();
        private readonly object _lock = new object();

        /// <summary>
        /// Tries to enqueue an item if it is not already in the queue.
        /// </summary>
        /// <param name="key">The key used to check for uniqueness.</param>
        /// <param name="item">The item to enqueue.</param>
        /// <returns>True if the item was added, false if it was a duplicate.</returns>
        public bool TryEnqueue(TKey key, TEntity item)
        {
            lock (_lock)
            {
                if (_set.Contains(key))
                {
                    return false; // Item is a duplicate
                }

                _queue.Enqueue(item);
                _set.Add(key);
                return true; // Item was added successfully
            }
        }

        /// <summary>
        /// Tries to dequeue an item.
        /// </summary>
        /// <param name="item">The dequeued item.</param>
        /// <returns>True if an item was dequeued, false if the queue was empty.</returns>
        public bool TryDequeue(out TEntity item)
        {
            if (_queue.TryDequeue(out item))
            {
                return true;
            }

            item = default;
            return false;
        }

        /// <summary>
        /// Gets the count of items in the queue.
        /// </summary>
        public int Count => _queue.Count;

        public int KeyCount => _set.Count;
    }
}
