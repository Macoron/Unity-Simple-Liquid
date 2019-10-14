using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySimpleLiquid
{
    public static class GeomUtils
    {
        /// <summary>
        /// Checks interesection of two planes
        /// </summary>
        /// <param name="linePoint">Point on the intersection line</param>
        /// <param name="lineVec">Intersection direction</param>
        /// <param name="plane1"></param>
        /// <param name="plane2"></param>
        /// <returns></returns>
        public static bool PlanePlaneIntersection(out Vector3 linePoint, out Vector3 lineVec, Plane plane1, Plane plane2)
        {
            // https://forum.unity.com/threads/how-to-find-line-of-intersecting-planes.109458/#post-725977

            linePoint = Vector3.zero;
            lineVec = Vector3.zero;

            //Get the normals of the planes.
            Vector3 plane1Normal = plane1.normal;
            Vector3 plane2Normal = plane2.normal;

            lineVec = Vector3.Cross(plane1Normal, plane2Normal);

            Vector3 ldir = Vector3.Cross(plane2Normal, lineVec);
            float numerator = Vector3.Dot(plane1Normal, ldir);

            if (Mathf.Abs(numerator) > 0.000001f)
            {
                var pos1 = plane1.normal * plane1.distance;
                var pos2 = plane2.normal * plane2.distance;

                Vector3 plane1ToPlane2 = pos1 - pos2;
                float t = Vector3.Dot(plane1Normal, plane1ToPlane2) / numerator;
                linePoint = pos2 + t * ldir;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Map function to other scale
        /// </summary>
        /// <param name="x"></param>
        /// <param name="in_min"></param>
        /// <param name="in_max"></param>
        /// <param name="out_min"></param>
        /// <param name="out_max"></param>
        /// <returns></returns>
        public static float Map(float x, float in_min, float in_max, float out_min, float out_max)
        {
            // https://forum.unity.com/threads/mapping-or-scaling-values-to-a-new-range.180090/#post-2241099
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }
    }
}
