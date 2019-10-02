#pragma warning disable 0649

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySimpleLiquid
{
    [ExecuteInEditMode]
    public class SimpleLiquid : MonoBehaviour
    {
        [SerializeField]
        private MeshRenderer liquidRender;

        [Range(0f, 1f)]
        [SerializeField]
        private float fillAmountPercent = 0.5f;

        [SerializeField]
        private Color liquidColor = Color.green;

        [SerializeField]
        private Material liquidMaterial;

        private Vector3 surfaceLevel;
        private Vector3 gravityDirection;

        #region Liquid Amount
        // After this values shader becomes unstable
        private const float minFillAmount = 0.1f;
        private const float maxFillAmount = 0.99f;

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
                fillAmountPercent = value;
                UpdateSurfacePos();
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

        private void Update()
        {
            if (liquidRender == null)
                return;

            UpdateSurfacePos();
        }

        private void OnValidate()
        {
            if (liquidRender == null)
                return;

            LiquidColor = liquidColor;
            FillAmountPercent = fillAmountPercent;
        }

        private Vector3 CalculateWoldSurfaceLevel()
        {
            var bounds = liquidRender.bounds;
            var min = bounds.min.y;
            var max = bounds.max.y;

            var surface = MappedFillAmount * (max - min) + min;
            var center = bounds.center;
            center.y = surface;

            return center;
        }
    }
}

