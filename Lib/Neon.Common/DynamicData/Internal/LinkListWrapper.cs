﻿//-----------------------------------------------------------------------------
// FILE:	    LinkListWrapper.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:	Copyright (c) 2016-2018 by neonFORGE, LLC.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

using Neon.Common;
using Neon.DynamicData;
using System.Collections;

namespace Neon.DynamicData.Internal
{
    /// <summary>
    /// <b>Platform use only:</b> Used by <see cref="IDynamicEntity"/> implementations 
    /// to wrap an <see cref="IList"/> of entity links around a <see cref="JArray"/>.
    /// </summary>
    /// <typeparam name="TEntity">The list's entity item type (implementing <see cref="IDynamicEntity"/>).</typeparam>
    /// <remarks>
    /// <note>
    /// This class is intended for use only by classes generated by the 
    /// <b>entity-gen</b> build tool.
    /// </note>
    /// </remarks>
    /// <threadsafety instance="false"/>
    public class LinkListWrapper<TEntity> : IList<TEntity>, ICollection<TEntity>, INotifyCollectionChanged
        where TEntity : class, IDynamicEntity, new()
    {
        //---------------------------------------------------------------------
        // Private types

        /// <summary>
        /// Holds the link state.
        /// </summary>
        private struct LinkState
        {
            /// <summary>
            /// The entity link.
            /// </summary>
            public string Link;

            /// <summary>
            /// The cached entity.
            /// </summary>
            public TEntity Entity;

            /// <summary>
            /// The entity deletion detection function.
            /// </summary>
            public Func<bool> IsDeletedFunc;
        } 

        //---------------------------------------------------------------------
        // Implementation

        private const string DetachedError = "The underlying [JArray] has been detached.";

        private IDynamicEntity                         parentEntity;
        private IDynamicEntityContext                  context;
        private JArray                          jArray;
        private ObservableCollection<LinkState> list;
        private bool                            notifyDisabled;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parentEntity">The <see cref="IDynamicEntity"/> that owns this list.</param>
        /// <param name="context">The <see cref="IDynamicEntityContext"/> or <c>null</c>.</param>
        /// <param name="jArray">The underlying <see cref="jArray"/>.</param>
        /// <param name="items">The initial items or <c>null</c> to initialize from <paramref name="jArray"/>.</param>
        public LinkListWrapper(IDynamicEntity parentEntity, IDynamicEntityContext context, JArray jArray, IEnumerable<TEntity> items)
        {
            Covenant.Requires<ArgumentNullException>(parentEntity != null);
            Covenant.Requires<ArgumentNullException>(jArray != null);

            this.parentEntity = parentEntity;
            this.context      = context;
            this.jArray       = jArray;
            this.list         = new ObservableCollection<LinkState>();

            if (items != null)
            {
                Covenant.Assert(jArray.Count == 0);

                foreach (var item in items)
                {
                    Add(item);
                }
            }
            else
            {
                foreach (var token in jArray)
                {
                    list.Add(new LinkState() { Link = GetLink(token) });
                }
            }

            // We're going to listen to our own collection changed event to
            // bubble them up.

            list.CollectionChanged +=
                (s, a) =>
                {
                    if (!notifyDisabled)
                    {
                        CollectionChanged?.Invoke(this, a);
                        parentEntity._OnChanged();
                    }
                };
        }

        /// <summary>
        /// Returns <c>true</c> if the list is currently attached to a <see cref="JArray"/>.
        /// </summary>
        internal bool IsAttached
        {
            get { return jArray != null; }
        }

        /// <summary>
        /// Detaches any event listeners from the underlying items and then
        /// disassociates the array.
        /// </summary>
        internal void Detach()
        {
            list.Clear();
            jArray = null;
        }

        /// <summary>
        /// Converts a <see cref="JToken"/> into an entity link.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>The entity link or <c>null</c>.</returns>
        private static string GetLink(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.String:

                    // This is the preferred property value type.

                    return (string)token.ToString();

                case JTokenType.Bytes:
                case JTokenType.Float:
                case JTokenType.Guid:
                case JTokenType.Integer:
                case JTokenType.Uri:

                    // These will work too.

                    return token.ToString();

                default:

                    // The remaining types indicate null or don't really
                    // make sense, so we'll treat them as null.

                    return null;
            }
        }

        //---------------------------------------------------------------------
        // INotifyCollectionChanged implementation.

        /// <summary>
        /// Raised when the list changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        //---------------------------------------------------------------------
        // IList and ICollection implementations

        /// <summary>
        /// Searches the list for a specific item.
        /// </summary>
        /// <param name="item">The item to be located.</param>
        /// <returns>The index of the first item that matches the index, if found; or -1 otherwise.</returns>">
        /// <exception cref="ArgumentException">Thrown if the entity passed cannot be linked.</exception>
        public int IndexOf(TEntity item)
        {
            if (item == null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Link == null)
                    {
                        return i;
                    }
                }
            }
            else
            {
                var link = GetLink(item);

                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].Link == link)
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns the number of items in the list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        public int Count
        {
            get
            {
                Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);
                return list.Count;
            }
        }

        /// <summary>
        /// Indicates whether the list is read-only.  This always returns <c>false.</c>
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        public bool IsReadOnly
        {
            get
            {
                Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);
                return false;
            }
        }

        /// <summary>
        /// Returns the link string for an entity or <c>null</c>.
        /// </summary>
        /// <param name="entity">The entity or <c>null</c>.</param>
        /// <returns>The entity link or <c>null</c>.</returns>
        /// <exception cref="ArgumentException">Thrown if the entity cannot be linked.</exception>
        /// <remarks>
        /// This method returns <c>null</c> when <paramref name="entity"/>=<c>null</c>, otherwise
        /// it returns the entity's link.  A non-<c>null</c> entity must be linkable.
        /// </remarks>
        private static string GetLink(TEntity entity)
        {
            if (entity == null)
            {
                return null;
            }

            var link = entity._GetLink();

            if (link == null)
            {
                throw new ArgumentException($"The [{nameof(TEntity)}] instance cannot be linked.  For Couchbase scenarios, be sure the entity is hosted within a document.");
            }

            return link;
        }

        /// <summary>
        /// Accesses the item at an index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The element at the index.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        /// <exception cref="ArgumentException">Thrown if the value being saved cannot be linked.</exception>
        public TEntity this[int index]
        {
            get
            {
                Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

                if (context == null)
                {
                    return null;
                }

                if (list[index].Link == null)
                {
                    return null;
                }
                else if (list[index].Entity != null)
                {
                    if (list[index].IsDeletedFunc())
                    {
                        list[index] = new LinkState();  // Clears any cached data
                    }
                    else
                    {
                        return list[index].Entity;
                    }
                }

                // If we get to this point, the list element has a link but no entity.
                // We'll try to load it from the context.

                Func<bool> isDeletedFunc;

                var entity = context.LoadEntity<TEntity>(list[index].Link, out isDeletedFunc);

                if (entity == null)
                {
                    return null;
                }

                // $note(jeff.lill):
                //
                // We need to disable notification here because logically, we're not
                // modifying the list.  Rather, we're just loading and caching the
                // linked entity.

                notifyDisabled = true;

                try
                {
                    list[index] = new LinkState()
                    {
                        Link          = list[index].Link,
                        Entity        = entity,
                        IsDeletedFunc = isDeletedFunc
                    };
                }
                finally
                {
                    notifyDisabled = false;
                }

                return entity;
            }

            set
            {
                Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

                var link = GetLink(value);

                jArray[index] = link;
                list[index]   = new LinkState() { Link = link };
            }
        }

        /// <summary>
        /// Inserts an item at a specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item</param>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        /// <exception cref="ArgumentException">Thrown if the value being saved cannot be linked.</exception>
        public void Insert(int index, TEntity item)
        {
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            var link = GetLink(item);

            jArray.Insert(index, link);
            list.Insert(index, new LinkState() { Link = link });
        }

        /// <summary>
        /// Removes the item at a specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        public void RemoveAt(int index)
        {
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            jArray.RemoveAt(index);
            list.RemoveAt(index);
        }

        /// <summary>
        /// Appends an item to the list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        /// <exception cref="ArgumentException">Thrown if the value being saved cannot be linked.</exception>
        public void Add(TEntity item)
        {
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            var link = GetLink(item);

            jArray.Add(link);
            list.Add(new LinkState() { Link = link });
        }

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        public void Clear()
        {
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            jArray.Clear();
            list.Clear();
        }

        /// <summary>
        /// Determines whether the list contains a specific item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if the item exists.</returns>
        /// <exception cref="ArgumentException">Thrown if the value passed cannot be linked.</exception>
        public bool Contains(TEntity item)
        {
            var link = GetLink(item);

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Link == link)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Copies the list items to an array. 
        /// </summary>
        /// <param name="array">The destination array.</param>
        /// <param name="arrayIndex">The destination starting index.</param>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        public void CopyTo(TEntity[] array, int arrayIndex)
        {
            Covenant.Requires<ArgumentNullException>(array != null);
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            for (int i = 0; i < list.Count; i++)
            {
                array[arrayIndex + i] = this[i];
            }
        }

        /// <summary>
        /// Removes the first occurance of a specific item from the list.
        /// </summary>
        /// <param name="item">The item to be removed.</param>
        /// <returns><c>true</c> if the item was present and was removed.</returns>
        /// <exception cref="ArgumentException">Thrown if the value passed cannot be linked.</exception>
        public bool Remove(TEntity item)
        {
            var index = IndexOf(item);

            if (index == -1)
            {
                return false;
            }

            RemoveAt(index);
            return true;
        }

        /// <summary>
        /// Returns a generic enumerator over the list items.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        public IEnumerator<TEntity> GetEnumerator()
        {
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            for (int i = 0; i < list.Count; i++)
            {
                yield return this[i];
            }
        }

        /// <summary>
        /// Returns an enumerator over the list items.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if the array has been detached.</exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            Covenant.Requires<InvalidOperationException>(jArray != null, DetachedError);

            for (int i = 0; i < list.Count; i++)
            {
                yield return this[i];
            }
        }
    }
}