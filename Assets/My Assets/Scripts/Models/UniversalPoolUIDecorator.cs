using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;


public class UniversalPoolUIDecorator : MonoBehaviour
{
#if UNITY_EDITOR
    // Кнопка сортировки текущего списка пула объектов и сохранения всех изменений
    [CustomEditor(typeof(UniversalPool))]
    public class ObjectBuilderEditor_ : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            UniversalPool universalPool = (UniversalPool)target;

            if (GUILayout.Button("Sort & Save"))
            {
                universalPool.SortAndSaveData();
            }
        }
    }

#endif
}

