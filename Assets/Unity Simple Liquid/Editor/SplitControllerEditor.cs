using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnitySimpleLiquid
{
    [CustomEditor(typeof(SplitController))]
    public class SplitControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var split = (SplitController)target;

            GUI.enabled = false;
            EditorGUILayout.Toggle("Is Spliting", split.IsSpliting);
            GUI.enabled = true;
        }
    }
}