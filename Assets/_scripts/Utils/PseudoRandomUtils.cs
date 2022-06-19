using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public class PseudoRandomUtils : MonoBehaviour
    {
        public static PseudoRandomPair GetPseudoRandomPair(int minInclusive, int maxExclusive, bool allowSameNumbers = false)
        {
            PseudoRandomPair prp = new PseudoRandomPair();
            prp.a = Random.Range(minInclusive, maxExclusive);

            if (allowSameNumbers)
            {
                prp.b = Random.Range(minInclusive, maxExclusive);
            }
            else
            {
                do
                {
                    prp.b = Random.Range(minInclusive, maxExclusive);
                } while (prp.b == prp.a);
            }

            return prp;
        }

        public static int GetPseudoRandomWithProbability(int[] array, int[] frequencies)
        {
            int n = array.Length;
            return myPseudoRand(array, frequencies, n);
        }


        //Downloaded from https://www.geeksforgeeks.org/random-number-generator-in-arbitrary-probability-distribution-fashion/
        static int findCeil(int[] arr, int r, int l, int h)
        {
            int mid;
            while (l < h)
            {
                mid = l + ((h - l) >> 1); // Same as mid = (l+h)/2  
                if (r > arr[mid])
                    l = mid + 1;
                else
                    h = mid;
            }

            if (arr[l] >= r)
                return l;
            else
                return -1;
        }

        // The main function that returns a pseudorandom number 
        // from arr[] according to distribution array  
        // defined by freq[]. n is size of arrays.  
        static int myPseudoRand(int[] arr, int[] freq, int n)
        {
            // Create and fill prefix array  
            int[] prefix = new int[n];
            int i;
            prefix[0] = freq[0];
            for (i = 1; i < n; ++i)
                prefix[i] = prefix[i - 1] + freq[i];

            // prefix[n-1] is sum of all frequencies. 
            // Generate a pseudorandom number with  
            // value from 1 to this sum  
            System.Random pseudoRand = new System.Random();
            int r = (pseudoRand.Next() % prefix[n - 1]) + 1;

            // Find index of ceiling of r in prefix arrat  
            int indexc = findCeil(prefix, r, 0, n - 1);
            return arr[indexc];
        }
    }

    public class PseudoRandomPair
    {
        public int a;
        public int b;
    }


}

