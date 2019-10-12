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

        public ParticleSystem particlesPrefab;

        #region Particles
        private ParticleSystem particles;
        public ParticleSystem Particles
        {
            get
            {
                if (!particles)
                    particles = Instantiate(particlesPrefab, transform);
                return particles;
            }
        }

        #endregion

        #region Botleneck
        private Plane bottleneckPlane, surfacePlane;
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
            // Draws bottleneck direction and radius
            var bottleneckPlane = GenerateBotleneckPlane();
            Gizmos.color = Color.red;
            GizmosHelper.DrawPlaneGizmos(bottleneckPlane, transform);

            // And bottleneck position
            GizmosHelper.DrawSphereOnPlane(bottleneckPlane, botleneckRadius, transform);
        }
        #endregion

        #region Split Logic
        public bool IsSpliting { get; private set; }

        private void CheckSpliting()
        {
            if (liquidContainer == null)
                return;

            IsSpliting = false;

            // Update botleneck and surface from last update
            bottleneckPlane = GenerateBotleneckPlane();
            surfacePlane = liquidContainer.GenerateSurfacePlane();

            // Check if liquid is overflows
            Vector3 overflowsPoint, lineVec;
            var overflows = GeomUtils.PlanePlaneIntersection(out overflowsPoint, out lineVec,
                bottleneckPlane, surfacePlane);

            if (overflows)
            {
                // Translate to contrainers world position
                overflowsPoint += liquidContainer.transform.position;

                // Let's check if overflow point is inside botleneck radius
                var botleneckPos = GenerateBotleneckPos();
                var insideBotleneck = Vector3.Distance(overflowsPoint, botleneckPos) < botleneckRadius;

                if (insideBotleneck)
                {
                    // We are inside botleneck - just start spliting from lowest botleneck point
                    var minPoint = GenerateBotleneckLowesPoint();
                    SplitLogic(minPoint);
                }
                else if (botleneckPos.y < overflowsPoint.y)
                {
                    // Oh, looks like container is upside down - let's check it
                    var dot = Vector3.Dot(bottleneckPlane.normal, surfacePlane.normal);
                    if (dot < 0f)
                    {
                        // Yep, let's split from the botleneck center
                        SplitLogic(botleneckPos);
                    }
                    else
                    {
                        // Well, this weird, let's check if spliting point is even inside our liquid
                        var dist = liquidContainer.liquidRender.bounds.SqrDistance(overflowsPoint);
                        var inBounding = dist < 0.0001f;

                        if (inBounding)
                        {
                            // Yeah, we are inside liquid container
                            var minPoint = GenerateBotleneckLowesPoint();
                            SplitLogic(minPoint);
                        }
                    }
                }
            }
        }

        private void SplitLogic(Vector3 splitPos)
        {
            IsSpliting = true;
        }

        #endregion

        private void Update()
        {
            CheckSpliting();
        }
    }
}