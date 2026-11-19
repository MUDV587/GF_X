using UnityEngine;


public static partial class GFUtility
{
    public class CurvePath
    {
        public static float GetPathLength(Vector3[] pathPoints, bool isLoop = false)
        {
            float result = 0;
            for (int i = 0; i < pathPoints.Length - 1; i++)
            {
                result += Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            }

            if (isLoop)
            {
                result += Vector3.Distance(pathPoints[0], pathPoints[pathPoints.Length - 1]);
            }
            return result;
        }
    }
}