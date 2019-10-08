#pragma warning disable 0649

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnitySimpleLiquid
{
    /// <summary>
    /// Calculates liquids splitting effect and transfer to other liquid containers
    /// </summary>
    public class SplitController : MonoBehaviour
    {
        public LiquidContainer liquidContainer;
        public float botleneckRadius = 0.1f;

        #region Botleneck
        private Plane bottleneckPlane;
        private Vector3 botleneckPos;

        private Plane GenerateBotleneckPlane()
        {
            if (!liquidContainer)
                return new Plane();

            var mesh = liquidContainer.LiquidMesh;
            if (!mesh)
                return new Plane();

            var max = mesh.bounds.max.y;
            return new Plane(liquidContainer.transform.up,
                max * liquidContainer.transform.lossyScale.y);
        }

        private Vector3 GenerateBotleneckPos()
        {
            if (!liquidContainer)
                return Vector3.zero;

            var tr = liquidContainer.transform;
            var pos = bottleneckPlane.normal * bottleneckPlane.distance + tr.position;
            return pos;
        }

        private Vector3 GenerateBotleneckLowesPoint()
        {
            if (!liquidContainer)
                return Vector3.zero;

            // TODO: This code is not optimal and can be done much better
            // Righ now it caluclates minimal point of the circle (in 3d) by brute force
            var containerOrientation = liquidContainer.transform.rotation;

            // Points on bottleneck radius (local space)
            var angleStep = 0.1f;
            var localPoints = new List<Vector3>();
            for (float a = 0; a < Mathf.PI * 2f; a += angleStep)
            {
                var x = botleneckRadius * Mathf.Cos(a);
                var z = botleneckRadius * Mathf.Sin(a);

                localPoints.Add(new Vector3(x, 0, z));
            }

            // Transfer points from local to global
            var worldPoints = new List<Vector3>();
            foreach (var locPoint in localPoints)
            {
                var worldPoint = botleneckPos + containerOrientation * locPoint;
                worldPoints.Add(worldPoint);
            }

            // Find the lowest one
            var min = worldPoints.OrderBy((pt) => pt.y).First();
            return min;

        }
        #endregion

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            // Draws bottleneck plane
            var bottleneckPlane = GenerateBotleneckPlane();
            Gizmos.color = Color.red;
            GizmosHelper.DrawPlaneGizmos(bottleneckPlane, transform);

            // And bottleneck position
            GizmosHelper.DrawSphereOnPlane(bottleneckPlane, botleneckRadius, transform);
        }
        #endregion
    }
}