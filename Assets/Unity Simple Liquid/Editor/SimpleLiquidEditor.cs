using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnitySimpleLiquid
{
    [CustomEditor(typeof(SimpleLiquid))]
    public class SimpleLiquidEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var liquid = (SimpleLiquid)target;

            if (liquid.CustomVolume)
            {
                var newVolume = EditorGUILayout.FloatField("Volume (liters):", liquid.Volume);
                liquid.Volume = newVolume > 0 ? newVolume : 0.01f;
            }
            else
            {
                GUI.enabled = false;

                var calculatedVolume = liquid.CalculateVolume();
                EditorGUILayout.FloatField("Volume (liters):", calculatedVolume);
                liquid.Volume = calculatedVolume;

                GUI.enabled = true;
            }

 

        }
    }
}