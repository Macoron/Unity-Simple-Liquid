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

        [Tooltip("How fast liquid split from container")]
        public float splitSpeed = 2f;

        public ParticleSystem particlesPrefab;

        #region Particles
        private ParticleSystem particles;
        public ParticleSystem Particles
        {
            get
            {
                if (!particlesPrefab)
                    return null;

                if (!particles)
                    particles = Instantiate(particlesPrefab, transform);
                return particles;
            }
        }

        private void StartEffect(Vector3 splitPos, float scale)
        {
            var particlesInst = Particles;
            if (!particlesInst)
                return;

            var mainModule = particlesInst.main;
            mainModule.startColor = liquidContainer.LiquidColor;

            particlesInst.transform.localScale = Vector3.one * botleneckRadius * scale;
            particlesInst.transform.position = splitPos;
            particlesInst.Play();
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
        private const float splashSize = 0.05f;

        public bool IsSpliting { get; private set; }

        private void CheckSpliting()
        {
            IsSpliting = false;

            if (liquidContainer == null)
                return;

            // Do we have something to split?
            if (liquidContainer.FillAmountPercent <= 0f)
                return;
            if (!liquidContainer.IsOpen)
                return;

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
                botleneckPos = GenerateBotleneckPos();
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

            // Check rotation of liquid container
            // It conttolls how many liquid we lost and particles size
            var howLow = Vector3.Dot(Vector3.up, liquidContainer.transform.up);
            var flowScale = 1f - (howLow + 1) * 0.5f + 0.2f;

            var liquidStep = botleneckRadius * splitSpeed * Time.deltaTime * flowScale;
            var newLiquidAmmount = liquidContainer.FillAmountPercent - liquidStep;

            // Check if amount is negative and change it to zero
            if (newLiquidAmmount < 0f)
            {
                liquidStep = liquidContainer.FillAmountPercent;
                newLiquidAmmount = 0f;
            }

            // Transfer liquid to other container (if possible)
            liquidContainer.FillAmountPercent = newLiquidAmmount;
            TransferLiquid(splitPos, liquidStep, flowScale);

            // Start particles effect
            StartEffect(splitPos, flowScale);
        }

        private void TransferLiquid(Vector3 splitPos, float lostPercentAmount, float scale)
        {
            var ray = new Ray(splitPos, Vector3.down);

            // Check all colliders under ours
            var hits = Physics.SphereCastAll(ray, splashSize);
            hits = hits.OrderBy((h) => h.distance).ToArray();

            foreach (var hit in hits)
            {
                // does it even a split controller
                var liquid = hit.collider.GetComponent<SplitController>();
                if (liquid && liquid != this)
                {
                    var otherBotleneck = liquid.GenerateBotleneckPos();
                    var radius = liquid.botleneckRadius;

                    var hitPoint = hit.point;

                    // Does we touched botleneck?
                    var insideRadius = Vector3.Distance(hitPoint, otherBotleneck) < radius + splashSize * scale;
                    if (insideRadius)
                    {
                        var lostAmount = liquidContainer.Volume * lostPercentAmount;
                        liquid.liquidContainer.FillAmount += lostAmount;
                    }

                    break;
                }
            }
        }

        #endregion

        private void Update()
        {
            CheckSpliting();
        }
    }
}