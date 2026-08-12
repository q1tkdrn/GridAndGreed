using System;
using System.Collections.Generic;
using System.Linq;

public static class Util
{
    public static void Shuffle<T>(this List<T> list)
    {
        var temp = list.OrderBy(item => Guid.NewGuid()).ToList();
        list.Clear();
        list.AddRange(temp);
    }
}