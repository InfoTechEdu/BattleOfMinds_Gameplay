
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;


namespace Utils
{
    public static class CollectionUtils
    {
        public static void ShuffleList<T>(this IList<T> list)
        {
            int n = list.Count;
            Random pseudoRand = new Random();
            while (n > 1)
            {
                int k = (pseudoRand.Next(0, n) % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static void ShuffleArray<T>(this T[] array)
        {
            int n = array.Length;
            Random pseudoRand = new Random();
            while (n > 1)
            {
                int k = (pseudoRand.Next(0, n) % n);
                n--;
                T value = array[k];
                array[k] = array[n];
                array[n] = value;
            }
        }


        // ------------------- TO STRING UTILS -------------------------
        public static string ListToString<T>(List<T> list, bool newLine = false)
        {
            if (list.Count == 0) return "Empty list";
            if (list == null) return "List is null";

            string divider = newLine ? "\r\n--\r\n" : ", ";
            
            string result = "{";
            for (int i = 0; i < list.Count - 1; i++)
            {
                result += list[i].ToString() + divider;
            }
            result += list[list.Count - 1].ToString() + "}";

            return result;
        }

        public static string DictionaryToString<T, U>(Dictionary<T, U> dict)
        {
            string result = "{";
            for (int i = 0; i < dict.Count - 1; i++)
            {
                result += string.Format("{0} : {1}, ", dict.ElementAt(i).Key, dict.ElementAt(i).Value);
            }

            T lastKey = dict.ElementAt(dict.Count - 1).Key;
            U lastValue = dict.ElementAt(dict.Count - 1).Value;
            result += lastKey + " : " + lastValue + "}";

            //result += string.Format("{0} : {1}}", lastKey.ToString(), lastValue.ToString());

            return result;
        }

        public static string DictionaryKeysToString<T, U>(Dictionary<T, U> dict) //Может удалить? refactor
        {
            return ListToString<T>(dict.Keys.ToList());
        }

        public static string ArrayToString(string[] arr)
        {
            if (arr == null || arr.Length == 0)
                return "null";

            string result = "[";
            for (int i = 0; i < arr.Length - 1; i++)
            {
                result += arr[i] + ", ";
            }
            result += arr[arr.Length - 1] + "]";

            return result;
        }

        public static string ArrayIntToString(int[] arr, bool newLine = false)
        {
            string divider = newLine ? "\r\n--\r\n" : ", ";

            string result = "[";
            for (int i = 0; i < arr.Length - 1; i++)
            {
                result += arr[i].ToString() + divider;
            }
            result += arr[arr.Length - 1].ToString() + "]";

            return result;
        }

        public static string ArrayToString(Object[] arr, bool newLine = false)
        {
            if (arr == null || arr.Length == 0)
                return "null";

            string divider = newLine ? "\r\n--\r\n" : ", ";

            string result = "[";
            for (int i = 0; i < arr.Length - 1; i++)
            {
                result += arr[i].ToString() + divider;
            }
            result += arr[arr.Length - 1].ToString() + "]";

            return result;
        }
    }
}

