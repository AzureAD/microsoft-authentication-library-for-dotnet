// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.


using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client.Utils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Identity.Test.Unit.UtilTests
{
    [TestClass]
    public class ConcurrentHashSetTests : TestBase
    {
        [TestMethod]
        public void Add_SingleItem_ReturnsTrue()
        {
            var set = new ConcurrentHashSet<int>();
            Assert.IsTrue(set.Add(1));
            Assert.HasCount(1, set);
        }

        [TestMethod]
        public void Add_DuplicateItem_ReturnsFalse()
        {
            var set = new ConcurrentHashSet<int>();
            Assert.IsTrue(set.Add(42));
            Assert.IsFalse(set.Add(42));
            Assert.HasCount(1, set);
        }

        [TestMethod]
        public void Add_MultipleItems_AllAdded()
        {
            var set = new ConcurrentHashSet<string>();
            Assert.IsTrue(set.Add("a"));
            Assert.IsTrue(set.Add("b"));
            Assert.IsTrue(set.Add("c"));
            Assert.HasCount(3, set);
        }

        [TestMethod]
        public void Contains_ExistingItem_ReturnsTrue()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(10);
            Assert.IsTrue(set.Contains(10));
        }

        [TestMethod]
        public void Contains_NonExistingItem_ReturnsFalse()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(10);
            Assert.IsFalse(set.Contains(20));
        }

        [TestMethod]
        public void TryRemove_ExistingItem_ReturnsTrue()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(5);
            Assert.IsTrue(set.TryRemove(5));
            Assert.IsEmpty(set);
            Assert.IsFalse(set.Contains(5));
        }

        [TestMethod]
        public void TryRemove_NonExistingItem_ReturnsFalse()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(5);
            Assert.IsFalse(set.TryRemove(99));
            Assert.HasCount(1, set);
        }

        [TestMethod]
        public void TryRemove_MiddleOfChain_WorksCorrectly()
        {
            // Add enough items to likely cause hash collisions in the same bucket
            var set = new ConcurrentHashSet<int>(1, 1);
            set.Add(1);
            set.Add(2);
            set.Add(3);

            Assert.IsTrue(set.TryRemove(2));
            Assert.HasCount(2, set);
            Assert.IsTrue(set.Contains(1));
            Assert.IsFalse(set.Contains(2));
            Assert.IsTrue(set.Contains(3));
        }

        [TestMethod]
        public void Clear_RemovesAllItems()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            set.Clear();

            Assert.IsEmpty(set);
            Assert.IsTrue(set.IsEmpty);
        }

        [TestMethod]
        public void IsEmpty_EmptySet_ReturnsTrue()
        {
            var set = new ConcurrentHashSet<int>();
            Assert.IsTrue(set.IsEmpty);
        }

        [TestMethod]
        public void IsEmpty_NonEmptySet_ReturnsFalse()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);
            Assert.IsFalse(set.IsEmpty);
        }

        [TestMethod]
        public void Constructor_WithCollection()
        {
            var items = new[] { 1, 2, 3, 4, 5 };
            var set = new ConcurrentHashSet<int>(items);

            Assert.HasCount(5, set);
            foreach (var item in items)
            {
                Assert.IsTrue(set.Contains(item));
            }
        }

        [TestMethod]
        public void Constructor_WithCollectionAndComparer()
        {
            var items = new[] { "Hello", "HELLO", "World" };
            var set = new ConcurrentHashSet<string>(items, StringComparer.OrdinalIgnoreCase);

            Assert.HasCount(2, set);
            Assert.IsTrue(set.Contains("hello"));
            Assert.IsTrue(set.Contains("WORLD"));
        }

        [TestMethod]
        public void Constructor_WithConcurrencyAndCapacity()
        {
            var set = new ConcurrentHashSet<int>(4, 16);
            set.Add(1);
            set.Add(2);
            Assert.HasCount(2, set);
        }

        [TestMethod]
        public void Constructor_WithConcurrencyCapacityAndComparer()
        {
            var set = new ConcurrentHashSet<string>(4, 16, StringComparer.OrdinalIgnoreCase);
            set.Add("Test");
            Assert.IsFalse(set.Add("test"));
            Assert.HasCount(1, set);
        }

        [TestMethod]
        public void Constructor_WithComparer()
        {
            var set = new ConcurrentHashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add("Test");
            Assert.IsTrue(set.Contains("TEST"));
            Assert.IsFalse(set.Add("test"));
        }

        [TestMethod]
        public void Constructor_WithConcurrencyCollectionAndComparer()
        {
            var items = new[] { "a", "A", "b" };
            var set = new ConcurrentHashSet<string>(2, items, StringComparer.OrdinalIgnoreCase);

            Assert.HasCount(2, set);
            Assert.IsTrue(set.Contains("A"));
            Assert.IsTrue(set.Contains("B"));
        }

        [TestMethod]
        public void Constructor_InvalidConcurrency_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentHashSet<int>(0, 10));
        }

        [TestMethod]
        public void Constructor_NegativeCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConcurrentHashSet<int>(1, -1));
        }

        [TestMethod]
        public void Constructor_NullCollection_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConcurrentHashSet<int>((IEnumerable<int>)null));
        }

        [TestMethod]
        public void Constructor_NullCollectionWithComparer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ConcurrentHashSet<int>(2, null, EqualityComparer<int>.Default));
        }

        [TestMethod]
        public void GetEnumerator_ReturnsAllItems()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(10);
            set.Add(20);
            set.Add(30);

            var items = new List<int>();
            foreach (var item in set)
            {
                items.Add(item);
            }

            Assert.HasCount(3, items);
            CollectionAssert.Contains(items, 10);
            CollectionAssert.Contains(items, 20);
            CollectionAssert.Contains(items, 30);
        }

        [TestMethod]
        public void ICollection_IsReadOnly_ReturnsFalse()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            Assert.IsFalse(set.IsReadOnly);
        }

        [TestMethod]
        public void ICollection_Add_Works()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            set.Add(1);
            Assert.HasCount(1, set);
        }

        [TestMethod]
        public void ICollection_Remove_Works()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            set.Add(1);
            Assert.IsTrue(set.Remove(1));
            Assert.IsEmpty(set);
        }

        [TestMethod]
        public void ICollection_CopyTo_Works()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);

            int[] array = new int[5];
            set.CopyTo(array, 1);

            // Items should be somewhere in array[1..3]
            var copied = array.Skip(1).Take(3).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, copied);
        }

        [TestMethod]
        public void ICollection_CopyTo_NullArray_Throws()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            Assert.Throws<ArgumentNullException>(() => set.CopyTo(null, 0));
        }

        [TestMethod]
        public void ICollection_CopyTo_NegativeIndex_Throws()
        {
            ICollection<int> set = new ConcurrentHashSet<int>();
            Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(new int[1], -1));
        }

        [TestMethod]
        public void GrowTable_TriggeredByManyInserts()
        {
            // Use a small initial capacity to force table growth
            var set = new ConcurrentHashSet<int>(1, 2);
            for (int i = 0; i < 100; i++)
            {
                set.Add(i);
            }

            Assert.HasCount(100, set);
            for (int i = 0; i < 100; i++)
            {
                Assert.IsTrue(set.Contains(i), $"Missing {i}");
            }
        }

        [TestMethod]
        public void ConcurrentOperations_AreThreadSafe()
        {
            var set = new ConcurrentHashSet<int>();
            var tasks = new List<Task>();

            // Add items from multiple threads
            for (int t = 0; t < 4; t++)
            {
                int start = t * 100;
                tasks.Add(Task.Run(() =>
                {
                    for (int i = start; i < start + 100; i++)
                    {
                        set.Add(i);
                    }
                }));
            }

            Task.WaitAll(tasks.ToArray());

            Assert.HasCount(400, set);
        }

        [TestMethod]
        public void ConcurrentAddAndRemove_AreThreadSafe()
        {
            var set = new ConcurrentHashSet<int>();

            // Pre-populate
            for (int i = 0; i < 100; i++)
            {
                set.Add(i);
            }

            var tasks = new List<Task>
            {
                Task.Run(() =>
                {
                    for (int i = 100; i < 200; i++) set.Add(i);
                }),
                Task.Run(() =>
                {
                    for (int i = 0; i < 50; i++) set.TryRemove(i);
                })
            };

            Task.WaitAll(tasks.ToArray());

            // At least the new items and some remaining old items should exist
            Assert.IsGreaterThanOrEqualTo(100, set.Count);
            for (int i = 100; i < 200; i++)
            {
                Assert.IsTrue(set.Contains(i));
            }
        }

        [TestMethod]
        public void Count_ReturnsCorrectValue_AfterMixedOperations()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Add(3);
            set.TryRemove(2);

            Assert.HasCount(2, set);
        }

        [TestMethod]
        public void AddAfterClear_WorksCorrectly()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);
            set.Add(2);
            set.Clear();
            set.Add(3);

            Assert.HasCount(1, set);
            Assert.IsTrue(set.Contains(3));
            Assert.IsFalse(set.Contains(1));
        }

        [TestMethod]
        public void Enumeration_EmptySet_ReturnsNoItems()
        {
            var set = new ConcurrentHashSet<int>();
            var list = set.ToList();
            Assert.IsEmpty(list);
        }

        [TestMethod]
        public void IEnumerable_NonGeneric_Works()
        {
            var set = new ConcurrentHashSet<int>();
            set.Add(1);
            set.Add(2);

            var items = new List<object>();
            foreach (var item in (System.Collections.IEnumerable)set)
            {
                items.Add(item);
            }

            Assert.HasCount(2, items);
        }
    }
}
