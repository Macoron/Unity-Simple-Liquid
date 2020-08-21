#pragma warning disable 0649

using UnityEngine;

namespace UnitySimpleLiquid
{
    /// <summary>
    /// Container with defined volume and liquid amount
    /// Also controls liquid rendering (color, surface level, wobble effect)
    /// </summary>
    [ExecuteInEditMode]
    public class LiquidContainer : MonoBehaviour
    {
        public MeshRenderer liquidRender;
        private MeshFilter meshFilter;

        [SerializeField]
        [Tooltip("Material prefab (will replace material on mesh renderer)")]
        private Material liquidMaterial;

        [Header("Container settings")]
        [SerializeField]
        private GameObject cap;
        [SerializeField]
        [Tooltip("Is container open and can split?")]
        private bool isOpen = true;

        [Header("Liquid settings")]
        [Tooltip("Smaller inertness - less still liquid")]
        public float inertness = 50;

        [SerializeField]
        private Color liquidColor = Color.green;

        [Range(0f, 1f)]
        [SerializeField]
        private float fillAmountPercent = 0.5f;

        [Header("Container Volume")]
        [SerializeField]
		private bool customVolume;
        private float volume = 1f;

        private SplitController splitController;
        public SplitController GetSplitController
        {
            get { return splitController; }
        }

        private void Start()
        {
            splitController = GetComponent<SplitController>();
        }

        #region Liquid Amount
        // After this values shader might become unstable
        private const float minFillAmount = 0.1f;
        private const float maxFillAmount = 0.99f;

        public bool IsOpen
        {
            get
            {
                return isOpen;
            }
            set
            {
                isOpen = value;
                if (cap)
                    cap.SetActive(!value);
            }
        }

        /// <summary>
        /// Amount of liquid in percents [0,1]
        /// </summary>
        public float FillAmountPercent
        {
            get
            {
                return fillAmountPercent;
            }
            set
            {
                if (value > 0f)
                    fillAmountPercent = value;
                else
                    fillAmountPercent = 0f;
                UpdateSurfacePos();
            }
        }

        /// <summary>
        /// Amount of liquid in liters
        /// </summary>
        public float FillAmount
        {
            get
            {
                return fillAmountPercent * volume;
            }
            set
            {
                var newValuePercent = value / volume;
                fillAmountPercent = Mathf.Clamp01(newValuePercent);
            }
        }

        /// <summary>
		/// Container volume in liters
		/// </summary>
        public float Volume
		{
            get
			{
				return volume;
			}
            set
			{
				volume = value;
			}
		}

		/// <summary>
		/// Volume is fixed and not calculated automatically
		/// </summary>
		public bool CustomVolume
		{
            get
			{
				return customVolume;
			}
		}

		/// <summary>
		/// Need to map fill amount for more stable results
		/// </summary>
		private float MappedFillAmount
        {
            get
            {
                return FillAmountPercent * (maxFillAmount - minFillAmount) + minFillAmount;
            }
        }

        /// <summary>
        /// Calculate container volume based on mesh bounds and transform size 
        /// </summary>
        /// <returns>Container volume in liters</returns>
        public float CalculateVolume()
        {
            var mesh = LiquidMesh;
            if (!mesh)
                return 0f;

            var boundsSize = LiquidMesh.bounds.size;
            var scale = transform.lossyScale;
            return boundsSize.x * boundsSize.y * boundsSize.z *
                scale.x * scale.y * scale.z * 1000;
        }
        #endregion

        #region Liquid Surface
        private Vector3 surfaceLevel;
        /// <summary>
        ///  Surface level of liquid in world-space coordinates
        /// </summary>
        public Vector3 SurfaceLevel
        {
            get
            {
                return surfaceLevel;
            }
            set
            {
                surfaceLevel = value;
                if (MaterialInstance)
                    MaterialInstance.SetVector(SurfaceLevelID, value);
            }
        }

        private void UpdateSurfacePos()
        {
            if (fillAmountPercent > 0f)
            {
                SurfaceLevel = CalculateWoldSurfaceLevel();
                liquidRender.enabled = true;
            }
            else
                liquidRender.enabled = false;
        }

        private Vector3 CalculateWoldSurfaceLevel()
        {
            var bounds = liquidRender.bounds;
            var min = bounds.min.y;
            var max = bounds.max.y;

            var howLow = 1f - Mathf.Abs(Vector3.Dot(Vector3.up, transform.up));
            var mapped = howLow * 0.1f;

            var surface = (MappedFillAmount + mapped * (1f - MappedFillAmount)) * (max - min) + min;
            var center = bounds.center;
            center.y = surface;

            return center;
        }

        /// <summary>
        /// Generate surface plane in world-space coordinates
        /// </summary>
        /// <returns></returns>
        public Plane GenerateSurfacePlane()
        {
            return new Plane(-gravityDirection,
                surfaceLevel.y - transform.position.y);
        }
        #endregion

        #region Gravity

        private Vector3 gravityDirection = Vector3.down;
        /// <summary>
        /// Direction which liquid trying to align to
        /// </summary>
        public Vector3 GravityDirection
        {
            get
            {
                return gravityDirection;
            }
            set
            {
                gravityDirection = value;
                if (MaterialInstance)
                    MaterialInstance.SetVector(GravityDirectionID, value);
            }
        }

        #endregion

        #region Material Settings
        private static int GravityDirectionID = Shader.PropertyToID("_GravityDirection");
        private static int SurfaceLevelID = Shader.PropertyToID("_SurfaceLevel");

        private Material materialInstance;

        /// <summary>
        /// Unique instance of liqud material bounded with mesh render
        /// </summary>
        public Material MaterialInstance
        {
            get
            {
                if (liquidRender == null)
                    return null;

                // Check if there is valid material instance
                if (materialInstance == null || liquidRender.sharedMaterial != materialInstance)
                {
                    if (liquidMaterial != null)
                        materialInstance = new Material(liquidMaterial);
                    else
                    {
                        var shader = Shader.Find("Liquid/SimpleLiquidShader");
                        materialInstance = new Material(shader);
                    }
                    liquidRender.sharedMaterial = materialInstance;
                }

                return materialInstance;
            }
        }

        /// <summary>
        /// Shared mesh that represent liquids
        /// </summary>
        public Mesh LiquidMesh
        {
            get
            {
                if (meshFilter == null)
                    meshFilter = liquidRender?.GetComponent<MeshFilter>();
                return meshFilter?.sharedMesh;
            }
        }

        /// <summary>
        /// Transparent color of the liquid
        /// </summary>
        public Color LiquidColor
        {
            get
            {
                return liquidColor;
            }
            set
            {
                liquidColor = value;
                if (MaterialInstance)
                    MaterialInstance.color = liquidColor;
            }
        }
        #endregion

        #region Wobble Effect
        private const float rotCoef = 0.2f;

        private Vector3 lastPos, lastUp;
        private Vector3 wobbleAcm;

        private void UpdateWoble()
        {
            var velocity = (transform.position - lastPos) / Time.fixedDeltaTime;
            var rotVelocity = (transform.up - lastUp) / Time.fixedDeltaTime;
            wobbleAcm += velocity + rotVelocity * rotCoef;
            wobbleAcm = Vector3.Lerp(wobbleAcm, Vector3.zero, Time.fixedDeltaTime);

            var sin = Mathf.Sin(2 * Mathf.PI * Time.time) / inertness;
            var wobble = wobbleAcm * sin;
            //wobble = wobbleToAdd * (Mathf.Sin((1f + wobbleToAdd.magnitude) * 2 *Mathf.PI)  * inertness);

            Vector3 gravity;
            if (fillAmountPercent > maxFillAmount && !isOpen)
                gravity = Vector3.down;
            else
                gravity = (Vector3.down + wobble).normalized;

            gravity.y = -1;

            GravityDirection = gravity;

            lastPos = transform.position;
            lastUp = transform.up;
        }
        #endregion

        #region Gizmos
        private void OnDrawGizmosSelected()
        {
            // Draws liquid surface
            UpdateSurfacePos();
            var surfacePlane = GenerateSurfacePlane();
            Gizmos.color = Color.green;
            GizmosHelper.DrawPlaneGizmos(surfacePlane, transform);

        }
        #endregion

        private void OnEnable()
        {
            // reset values for voble effect
            lastPos = transform.position;
            lastUp = transform.up;
        }

        private void Update()
        {
            if (liquidRender == null)
                return;

            // Update surface volume
            UpdateSurfacePos();

            // In case transform scale is changed - update volume
            if (!customVolume)
                volume = CalculateVolume();

            if (Application.isPlaying)
                UpdateWoble();
        }

        private void OnValidate()
        {
            if (liquidRender == null)
                return;

            LiquidColor = liquidColor;
            FillAmountPercent = fillAmountPercent;
            IsOpen = isOpen;
        } 

    }
}

