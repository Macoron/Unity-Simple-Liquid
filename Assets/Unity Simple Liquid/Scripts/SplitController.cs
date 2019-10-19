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
        [SerializeField]
        private float bottleneckRadius = 0.1f;
        public float BottleneckRadiusWorld { get; private set; }


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

            particlesInst.transform.localScale = Vector3.one * BottleneckRadiusWorld * scale;
            particlesInst.transform.position = splitPos;
            particlesInst.Play();
        }
        #endregion

        #region Bottleneck
        public Plane bottleneckPlane { get; private set; }
        public Plane surfacePlane { get; private set; }
        public Vector3 BottleneckPos { get; private set; }

        private Plane GenerateBottleneckPlane()
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

        private Vector3 GenerateBottleneckPos()
        {
            if (!liquidContainer)
                return Vector3.zero;

            var tr = liquidContainer.transform;
            var pos = bottleneckPlane.normal * bottleneckPlane.distance + tr.position;
            return pos;
        }

        private Vector3 GenerateBottleneckLowesPoint()
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
                var x = BottleneckRadiusWorld * Mathf.Cos(a);
                var z = BottleneckRadiusWorld * Mathf.Sin(a);

                localPoints.Add(new Vector3(x, 0, z));
            }

            // Transfer points from local to global
            var worldPoints = new List<Vector3>();
            foreach (var locPoint in localPoints)
            {
                var worldPoint = BottleneckPos + containerOrientation * locPoint;
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
            var bottleneckPlane = GenerateBottleneckPlane();
            BottleneckRadiusWorld = bottleneckRadius * transform.lossyScale.magnitude;

            Gizmos.color = Color.red;
            GizmosHelper.DrawPlaneGizmos(bottleneckPlane, transform);

            // And bottleneck position
            GizmosHelper.DrawSphereOnPlane(bottleneckPlane, BottleneckRadiusWorld, transform);

			///
			
		}
		void OnDrawGizmos()
		{
			// Draw a yellow sphere at the transform's position
			Gizmos.color = Color.yellow;
			Gizmos.DrawSphere(raycasthit, 0.01f);
		}
		#endregion

		#region Split Logic
		private const float splashSize = 0.025f;

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

            // Check if liquid is overflows
            Vector3 overflowsPoint, lineVec;
            var overflows = GeomUtils.PlanePlaneIntersection(out overflowsPoint, out lineVec,
                bottleneckPlane, surfacePlane);

            // Translate to contrainers world position
            overflowsPoint += liquidContainer.transform.position;

            if (overflows)
            {
                // Let's check if overflow point is inside bottleneck radius
                var insideBottleneck = Vector3.Distance(overflowsPoint, BottleneckPos) < BottleneckRadiusWorld;

                if (insideBottleneck)
                {
                    // We are inside bottleneck - just start spliting from lowest bottleneck point
                    var minPoint = GenerateBottleneckLowesPoint();
                    SplitLogic(minPoint);
                    return;
                }
            }

            if (BottleneckPos.y < overflowsPoint.y)
            {
                // Oh, looks like container is upside down - let's check it
                var dot = Vector3.Dot(bottleneckPlane.normal, surfacePlane.normal);
                if (dot < 0f)
                {
                    // Yep, let's split from the bottleneck center
                    SplitLogic(BottleneckPos);
                }
                else
                {
                    // Well, this weird, let's check if spliting point is even inside our liquid
                    var dist = liquidContainer.liquidRender.bounds.SqrDistance(overflowsPoint);
                    var inBounding = dist < 0.0001f;

                    if (inBounding)
                    {
                        // Yeah, we are inside liquid container
                        var minPoint = GenerateBottleneckLowesPoint();
                        SplitLogic(minPoint);
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

            var liquidStep = BottleneckRadiusWorld * splitSpeed * Time.deltaTime * flowScale;
            var newLiquidAmmount = liquidContainer.FillAmountPercent - liquidStep;

            // Check if amount is negative and change it to zero
            if (newLiquidAmmount < 0f)
            {
                liquidStep = liquidContainer.FillAmountPercent;
                newLiquidAmmount = 0f;
            }

            // Transfer liquid to other container (if possible)
            liquidContainer.FillAmountPercent = newLiquidAmmount;
            TransferLiquid(splitPos, liquidStep, flowScale, this.gameObject);

            // Start particles effect
            StartEffect(splitPos, flowScale);
        }

		public Vector3 raycasthit;
        private void TransferLiquid(Vector3 splitPos, float lostPercentAmount, float scale, GameObject ignoreCollision)
        {
            var ray = new Ray(splitPos, Vector3.down);

            // Check all colliders under ours
            var hits = Physics.SphereCastAll(ray, splashSize);
            hits = hits.OrderBy((h) => h.distance).ToArray();

            foreach (var hit in hits)
            {				
				//Ignore ourself
				if (!GameObject.ReferenceEquals(hit.collider.gameObject, ignoreCollision) && !hit.collider.isTrigger)
				{
					
					// does it even a split controller
					var liquid = hit.collider.GetComponent<SplitController>();
					if (liquid && liquid != this)
					{
						var otherBottleneck = liquid.GenerateBottleneckPos();
						var radius = liquid.BottleneckRadiusWorld;

						var hitPoint = hit.point;

						// Does we touched bottleneck?
						var insideRadius = Vector3.Distance(hitPoint, otherBottleneck) < radius + splashSize * scale;
						if (insideRadius)
						{
							var lostAmount = liquidContainer.Volume * lostPercentAmount;
							liquid.liquidContainer.FillAmount += lostAmount;
						}

						break;
					}


					//Something other than a liquid splitter is in the way
					if (!liquid)
					{
						//Simulate the liquid running off an object it hits and continuing down from the edge of the liquid
						//Does not take velocity into account

						///First get the slope direction
						Vector3 slope = GetSlopeDirection(Vector3.up, hit.normal);

						//Next we try to find the edge of the object the liquid would roll off
						//This really only works for primitive objects, it would look weird on other stuff
						Vector3 edgePosition = TryGetSlopeEdge(slope, hit);
						if (edgePosition != Vector3.zero)
						{
							//edge position found, surface must be tilted
							//Now we can try to transfer the liquid from this position
							TransferLiquid(edgePosition, lostPercentAmount, scale,hit.collider.gameObject);
							
						}
						break;
					}
				}
            }
        }
		
		#endregion

		private Vector3 GetSlopeDirection(Vector3 up, Vector3 normal)
		{
			https://forum.unity.com/threads/making-a-player-slide-down-a-slope.469988/#post-3062204			
			return Vector3.Cross(Vector3.Cross(up, normal), normal);
		}

		private Vector3 TryGetSlopeEdge(Vector3 slope, RaycastHit hit)
		{
			Vector3 edgePosition = Vector3.zero;
			
			GameObject objHit = hit.collider.gameObject;

			//flip a raycast so it faces backwards towards the object we hit, move it slightly down so it will hit the edge of the object
			Vector3 moveDown = new Vector3(0f, -0.0001f, 0f);
			Vector3 reverseRayPos = hit.point + moveDown + (slope.normalized);

			Ray backwardsRay = new Ray(reverseRayPos, -slope.normalized);

			RaycastHit[] revHits = Physics.RaycastAll(backwardsRay);

			foreach (var revHit in revHits)
			{
				// https://answers.unity.com/questions/752382/how-to-compare-if-two-gameobjects-are-the-same-1.html
				//We only want to get this position on the original object we hit off of
				if (GameObject.ReferenceEquals(revHit.collider.gameObject, objHit))
				{
					//We hit the object the liquid is running down!
					raycasthit = edgePosition = revHit.point;
					break;
				}
			}
			return edgePosition;
		}

		private void Update()
        {
            // Update bottleneck and surface from last update
            bottleneckPlane = GenerateBottleneckPlane();
            BottleneckPos = GenerateBottleneckPos();
            surfacePlane = liquidContainer.GenerateSurfacePlane();
            BottleneckRadiusWorld = bottleneckRadius * transform.lossyScale.magnitude;


            // Now check spliting
            CheckSpliting();
        }
    }
}