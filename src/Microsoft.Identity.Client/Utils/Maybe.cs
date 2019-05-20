// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Microsoft.Identity.Client.Utils
{
    internal sealed class Maybe<T>
    {
        internal bool HasItem { get; }
        internal T Item { get; }

        public Maybe()
        {
            HasItem = false;
        }

        public static Maybe<T> Empty()
        {
            return new Maybe<T>();
        }

        public Maybe(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            HasItem = true;
            Item = item;
        }

        public Maybe<TResult> Select<TResult>(Func<T, TResult> selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            return HasItem ? new Maybe<TResult>(selector(Item)) : new Maybe<TResult>();
        }

        public T GetValueOrDefault(T defaultValue)
        {
            if (defaultValue == null)
            {
                throw new ArgumentNullException(nameof(defaultValue));
            }

            return HasItem ? Item : defaultValue;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Maybe<T>;
            return other == null ? false : object.Equals(Item, other.Item);
        }

        public override int GetHashCode()
        {
            return HasItem ? Item.GetHashCode() : 0;
        }
    }
}
