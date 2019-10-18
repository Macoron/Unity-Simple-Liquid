using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitySimpleLiquid
{
    public class EndlessContainer : MonoBehaviour
    {
        private LiquidContainer liquidContainer;

        private void Awake()
        {
            liquidContainer = GetComponent<LiquidContainer>();
        }

        // Update is called once per frame
        void Update()
        {
            liquidContainer.FillAmountPercent = 1f;
        }
    }
}

