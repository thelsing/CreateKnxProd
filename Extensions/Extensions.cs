using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateKnxProd.Extensions
{
    //split up if it gets crowded here
    static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
        {
            if (enumeration == null)
                return;

            foreach (T item in enumeration)
            {
                action(item);
            }
        }

        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            items.ForEach(item => collection.Add(item));
        }
    }
}
