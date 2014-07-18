using System;
using System.Collections.Generic;
using System.Linq;

namespace PollingTcp.Shared
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<TItemType> OrderByWithGap<TItemType>(this List<TItemType> list, Func<TItemType, int> orderBy)
        {
            var firstSort = list.OrderBy(orderBy).ToList();

            // Find Gap
            var prefValue = orderBy(firstSort[0]);
            var newRangeAt = 0;

            for (int i = 1; i < firstSort.Count; i++)
            {
                var curValue = orderBy(firstSort[i]);

                if (curValue > prefValue)
                {
                    newRangeAt = i;
                }

                prefValue = curValue;
            }

            if (newRangeAt > 0)
            {
                var newList = new List<TItemType>(firstSort.Count);
                newList.AddRange(firstSort.GetRange(newRangeAt, firstSort.Count - newRangeAt));
                newList.AddRange(firstSort.GetRange(0, newRangeAt));

                return newList;
            }

            return firstSort;
        }
    }
}
