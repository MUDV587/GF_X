using System;
using System.Collections.Generic;
using UnityEngine;


public static partial class GFUtility
{
    static int m_EntityId = 0;
    public static int GenerateEntityId()
    {
        return ++m_EntityId;
    }
    public static string GetTimeStamp()
    {
        TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return Convert.ToInt64(ts.TotalSeconds).ToString();
    }
    /// <summary>
    /// 获取两个颜色的相似度
    /// </summary>
    /// <param name="c1"></param>
    /// <param name="c2"></param>
    /// <returns></returns>
    public static float Difference(Color c1, Color c2)
    {
        c1 *= 255; c2 *= 255;
        float averageR = (c1.r + c2.r) * 0.5f;
        return Mathf.Sqrt((2 + averageR / 255f) * Mathf.Pow(c1.r - c2.r, 2) + 4 * Mathf.Pow(c1.g - c2.g, 2) + (2 + (255 - averageR) / 255f) * Mathf.Pow(c1.b - c2.b, 2)) / (3 * 255f);
    }
    public static void Disorder<T>(ref List<T> list)
    {
        int halfLen = list.Count / 2;
        for (int i = 0; i < halfLen; i++)
        {
            int idxA = UnityEngine.Random.Range(0, halfLen + 1);
            int idxB = list.Count - idxA - 1;
            T tmpValue = list[idxA];
            list[idxA] = list[idxB];
            list[idxB] = tmpValue;
        }
    }
}