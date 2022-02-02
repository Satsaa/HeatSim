using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Muc.Extensions;

[CreateAssetMenu(menuName = nameof(MaterialProperties))]
public class MaterialProperties : ScriptableObject {

	[Tooltip("g/cm3")]
	public float density;

	[Tooltip("Ar, standard(X)")]
	public float standardAtomicWeight;

	[Tooltip("J/(mol*K)")]
	public float molarHeatCapacity;

	[Range(0, 1), Tooltip("Percentage amount of heat transferred per second")]
	public float thermalTransferRate = 0.05f;

	public float heatCapacity => molarHeatCapacity / standardAtomicWeight * density;

	public float molarDensity => density / standardAtomicWeight;

}

#if UNITY_EDITOR
namespace Editors {

	using System;
	using System.Linq;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEditor;
	using Object = UnityEngine.Object;
	using static Muc.Editor.PropertyUtil;
	using static Muc.Editor.EditorUtil;

	[CanEditMultipleObjects]
	[CustomEditor(typeof(MaterialProperties), true)]
	public class MaterialPropertiesEditor : Editor {

		MaterialProperties t => (MaterialProperties)target;

		public override void OnInspectorGUI() {
			serializedObject.Update();

			DrawDefaultInspector();

			using (DisabledScope()) {
				Field(new GUIContent(nameof(MaterialProperties.heatCapacity)), t.heatCapacity);
				Field(new GUIContent(nameof(MaterialProperties.molarDensity)), t.molarDensity);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif