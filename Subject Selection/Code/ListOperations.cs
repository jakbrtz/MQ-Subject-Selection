using System;
using System.Collections.Generic;
using System.Linq;

namespace Subject_Selection
{
    //*
    public static class ListOperators
    {
        public static int Sum<T>(this List<T> list, Func<T, int> function)
        {
            int total = 0;
            for (int i = 0; i < list.Count; i++)
                total += function(list[i]);
            return total;
        }

        public static int SumOfCreditPoints<T>(this List<T> options) where T : Option
        {
            int total = 0;
            for (int i = 0; i < options.Count; i++)
                total += options[i].CreditPoints();
            return total;
        }

        public static bool All<T>(this List<T> list, Func<T, bool> function)
        {
            for (int i = 0; i < list.Count; i++)
                if (!function(list[i]))
                    return false;
            return true;
        }

        public static bool AllCreditPointsAreGreaterThanOrEqualTo(this List<Option> list, int threshold)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i].CreditPoints() < threshold)
                    return false;
            return true;
        }

        public static bool IsSubset<T>(this List<T> listSmaller, List<T> listLarger)
        {
            for (int i = 0; i < listSmaller.Count; i++)
                if (!listLarger.Contains(listSmaller[i]))
                    return false;
            return true;
        }

        public static List<T> Where<T>(this List<T> list, Func<T, bool> function)
        {
            List<T> output = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++)
                if (function(list[i]))
                    output.Add(list[i]);
            return output;
        }

        public static bool Any<T>(this List<T> list)
        {
            return list.Count > 0;
        }

        public static List<T1> OrderBy<T1, T2>(this List<T1> list, Func<T1, T2> function) where T2:IComparable
        {
            List<T1> output = new List<T1>(list.Count);
            T2[] valuesForSorting = new T2[list.Count];
            for (int i = 0; i < list.Count; i++)
                valuesForSorting[i] = function(list[i]);
            bool[] valueHasBeenSorted = new bool[list.Count];
            // TODO: quicksort or something
            for (int n = 0; n < valuesForSorting.Length; n++)
            {
                int nextIndex = -1;
                for (int i = 0; i < valuesForSorting.Length; i++)
                {
                    if (!valueHasBeenSorted[i] && (nextIndex==-1 || valuesForSorting[i].CompareTo(valuesForSorting[nextIndex]) < 0))
                    {
                        nextIndex = i;
                    }
                }
                output.Add(list[nextIndex]);
                valueHasBeenSorted[nextIndex] = true;
            }
            return output;
        }

        public static bool Any<T> (this List<T> list, Func<T, bool> function)
        {
            for (int i = 0; i < list.Count; i++)
                if (function(list[i]))
                    return true;
            return false;
        }

        public static T FirstOrDefault<T>(this List<T> list, Func<T, bool> function)
        {
            for (int i = 0; i < list.Count; i++)
                if (function(list[i]))
                    return list[i];
            return default;
        }

        public static T First<T>(this List<T> list)
        {
            return list[0];
        }

        public static bool SequenceEqual<T>(this List<T> list1, List<T> list2)
        {
            if (list1.Count != list2.Count)
                return false;
            for (int i = 0; i < list1.Count; i++)
                if (!list1[i].Equals(list2[i]))
                    return false;
            return true;
        }

        public static List<T> Concat<T>(this List<T> list1, List<T> list2)
        {
            List<T> output = new List<T>();
            for (int i = 0; i < list1.Count; i++)
                output.Add(list1[i]);
            for (int i = 0; i < list2.Count; i++)
                output.Add(list2[i]);
            return output;
        }

        public static T2 Min<T1, T2>(this List<T1> list, Func<T1, T2> function) where T2 : IComparable
        {
            T2[] list2 = new T2[list.Count];
            for (int i = 0; i < list.Count; i++)
                list2[i] = function(list[i]);
            int bestIndex = 0;
            for (int i = 1; i < list2.Length; i++)
                if (list2[i].CompareTo(list2[bestIndex]) < 0)
                    bestIndex = i;
            return list2[bestIndex];
        }

        public static List<T2> SelectMany<T1, T2>(this List<T1> list, Func<T1, IEnumerable<T2>> function)
        {
            List<T2> output = new List<T2>();
            for (int i = 0; i < list.Count; i++)
                foreach (T2 item in function(list[i]))
                    output.Add(item);
            return output;
        }

        public static List<T> Distinct<T>(this List<T> list)
        {
            HashSet<T> values = new HashSet<T>();
            List<T> output = new List<T>(list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                T item = list[i];
                if (values.Contains(item)) continue;
                output.Add(item);
                values.Add(item);
            }
            return output;
        }

        public static T2 Max<T1, T2>(this List<T1> list, Func<T1, T2> function) where T2:IComparable
        {
            T2[] list2 = new T2[list.Count];
            for (int i = 0; i < list.Count; i++)
                list2[i] = function(list[i]);
            int bestIndex = 0;
            for (int i = 1; i < list2.Length; i++)
                if (list2[i].CompareTo(list2[bestIndex]) > 0)
                    bestIndex = i;
            return list2[bestIndex];
        }

        public static T First<T>(this T[] array)
        {
            return array[0];
        }

        public static T Last<T>(this T[] array)
        {
            return array[array.Length - 1];
        }

        public static List<T> Union<T>(List<T> list1, List<T> list2)
        {
            List<T> output = new List<T>();
            HashSet<T> values = new HashSet<T>();
            for (int i = 0; i < list1.Count; i++)
            {
                T item = list1[i];
                if (values.Contains(item)) continue;
                output.Add(item);
                values.Add(item);
            }
            for (int i = 0; i < list2.Count; i++)
            {
                T item = list2[i];
                if (values.Contains(item)) continue;
                output.Add(item);
                values.Add(item);
            }
            return output;
        }

        public static List<T2> Select<T1, T2>(this List<T1> list, Func<T1, T2> function)
        {
            List<T2> output = new List<T2>(list.Count);
            for (int i = 0; i < list.Count; i++)
                output.Add(function(list[i]));
            return output;
        }
    }
    //*/

    public class UniqueQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly HashSet<T> _set = new HashSet<T>();

        public void Enqueue(T item)
        {
            if (_set.Contains(item)) return;
            _queue.Enqueue(item);
            _set.Add(item);
        }

        public T Dequeue()
        {
            T result = _queue.Dequeue();
            _set.Remove(result);
            return result;
        }

        public bool Any()
        {
            return _queue.Any();
        }
    }
}
