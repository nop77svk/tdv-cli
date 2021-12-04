namespace NoP77svk.TibcoDV.API
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class EnumerableUnionAll
    {
        public static IEnumerable<TElement> UnionAll<TElement>(this IEnumerable<TElement> self, IEnumerable<TElement> toBeUnioned)
        {
            foreach (TElement element in self)
                yield return element;

            foreach (TElement element in toBeUnioned)
                yield return element;
        }

        public static async IAsyncEnumerable<TElement> UnionAll<TElement>(this IAsyncEnumerable<TElement> self, IAsyncEnumerable<TElement> toBeUnioned)
        {
            await foreach (TElement element in self)
                yield return element;

            await foreach (TElement element in toBeUnioned)
                yield return element;
        }

        public static async Task<List<TElement>> ToListAwait<TElement>(this IAsyncEnumerable<TElement> self)
        {
            List<TElement> result = new List<TElement>();

            await foreach (TElement element in self)
                result.Add(element);

            return result;
        }
    }
}
