using System;
using System.Collections;
using System.Collections.Generic;
using Muc.Extensions;
using UnityEngine;
using UnityEngine.Serialization;

public class Block : MonoBehaviour {

	public enum SourceType : byte {
		none,
		cpu,
		ddr,
		chipset,
		gpu,
		gddr,
		psu,
		ssd1,
		ssd2,
		ssd3,
		hdd1,
		hdd2,
	}

	public enum FanType : byte {
		none,
		cpu,
		gpu,
		front1,
		front2,
		back,
		psu,
	}

	public SourceType sourceIndex;

	public MaterialProperties material;

	[Range(0, 1)]
	public float materialPercentage = 1;
	[Range(0, 1)]
	public float passability = 1;

	public Vector3 movability = Vector3.one;


	public FanType fanIndex;

	public Vector3 fanDirection;
	[Min(0), FormerlySerializedAs("maxLitres"), Tooltip("m3/h")]
	public float airflow = 100;
	[Min(0)]
	public float minRpm = 450;
	[Min(0)]
	public float maxRpm = 2000;

	public BoundsInt CalcBounds() {
		var fScale = transform.localScale.Abs().Mul(transform.parent.localScale);
		var scale = fScale.RoundInt();
		var fPos = transform.parent.localScale.Abs().Mul(transform.localPosition) - fScale / 2 + transform.parent.localScale.Abs() / 2;
		var pos = fPos.RoundInt();
		return new BoundsInt(pos, scale);
	}

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
	[CustomEditor(typeof(Block), true)]
	public class BlockEditor : Editor {

		Block t => (Block)target;

		public override void OnInspectorGUI() {
			serializedObject.Update();

			DrawDefaultInspector();

			Field(new GUIContent("Bounds"), t.CalcBounds());

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif