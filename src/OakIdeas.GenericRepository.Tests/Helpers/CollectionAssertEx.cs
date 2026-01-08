using System.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace OakIdeas.GenericRepository.Tests.Helpers
{
    public static class CollectionAssertEx
    {
        public static void HasCount(IEnumerable collection, int expected, string? message = null)
        {
            int count = 0;
            foreach (var _ in collection) count++;
            Assert.AreEqual(expected, count, message);
        }

        public static void IsEmpty(IEnumerable collection, string? message = null)
        {
            foreach (var _ in collection)
            {
                Assert.Fail(message ?? "Collection is not empty.");
            }
        }

        public static void IsNotEmpty(IEnumerable collection, string? message = null)
        {
            bool any = false;
            foreach (var _ in collection)
            {
                any = true;
                break;
            }
            Assert.IsTrue(any, message ?? "Collection is empty.");
        }
    }
}
