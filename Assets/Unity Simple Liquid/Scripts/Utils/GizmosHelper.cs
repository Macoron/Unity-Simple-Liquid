using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySimpleLiquid
{
    public static class GizmosHelper
    {
        public static void DrawPlaneGizmos(Plane plane, Transform relativeTransform)
        {
            var pos = plane.normal * plane.distance + relativeTransform.position;
            Gizmos.DrawLine(pos, pos + plane.normal * 0.1f);
        }

        public static void DrawSphereOnPlane(Plane plane, float radius, Transform relativeTransform)
        {
            var pos = plane.normal * plane.distance + relativeTransform.position;
            Gizmos.DrawWireSphere(pos, radius);
        }
    }
}