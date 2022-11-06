using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomUtil {
    public static void Shuffle<T>(T[] elems)
    {
        for (int i = elems.Length - 1; i >= 1; --i)
        {
            int j = Random.Range(0, i);
            T tmp = elems[i];
            elems[i] = elems[j];
            elems[j] = tmp;
        }
    }

    public static void Shuffle<T>(List<T> elems)
    {
        for (int i = elems.Count - 1; i >= 1; --i)
        {
            int j = Random.Range(0, i);
            T tmp = elems[i];
            elems[i] = elems[j];
            elems[j] = tmp;
        }
    }
}
