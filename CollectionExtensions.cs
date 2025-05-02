using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WSLWpfApp
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this ObservableCollection<T> values, IEnumerable<T> collection)
        {
            foreach (var i in collection) values.Add(i);
        }
    }
}
