using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Controller : MonoBehaviour {

	public Model model;

	public ComputeShader draw;
	public ComputeShader main;


	public RenderTexture Errors; // float4
	public RenderTexture Source; // int
	public RenderTexture Fan; // int

	public RenderTexture Passability; // float
	public RenderTexture Movability; // float3
	public RenderTexture HeatModel; // float3


	public RenderTexture InVelocity; // float3
	public RenderTexture OutVelocity; // float3
	public RenderTexture InAirPressure; // float
	public RenderTexture OutAirPressure; // float
	public RenderTexture InAirTemp; // float
	public RenderTexture OutAirTemp; // float
	public RenderTexture InMaterialTemp; // float
	public RenderTexture OutMaterialTemp; // float

	public Shader displayShader;
	public Material displayMaterial;

	public bool autoSimulate;
	public bool autoTest;
	public Vector3Int testOrigin = new(15, 15, 15);
	public Vector3Int testSize = Vector3Int.one;
	public float testTemperature = 200f;

	[HideInInspector]
	public Vector3Int dims;

	public void Init() {

		var newDims = model.GetSize();
		var changed = newDims != dims;
		dims = newDims;

		Errors = InitTex(Errors, RenderTextureFormat.ARGBHalf); // float
		Source = InitTex(Source, RenderTextureFormat.RInt);
		Fan = InitTex(Fan, RenderTextureFormat.RInt);
		Passability = InitTex(Passability, RenderTextureFormat.RFloat);
		Movability = InitTex(Movability, RenderTextureFormat.RGB111110Float);
		HeatModel = InitTex(HeatModel, RenderTextureFormat.RGB111110Float);


		InVelocity = InitTex(InVelocity, RenderTextureFormat.RGB111110Float); // float3
		InAirPressure = InitTex(InAirPressure, RenderTextureFormat.RFloat); // float
		InAirTemp = InitTex(InAirTemp, RenderTextureFormat.RFloat); // float
		InMaterialTemp = InitTex(InMaterialTemp, RenderTextureFormat.RFloat); // float
		OutVelocity = InitTex(OutVelocity, RenderTextureFormat.RGB111110Float); // float3
		OutAirPressure = InitTex(OutAirPressure, RenderTextureFormat.RFloat); // float
		OutAirTemp = InitTex(OutAirTemp, RenderTextureFormat.RFloat); // float
		OutMaterialTemp = InitTex(OutMaterialTemp, RenderTextureFormat.RFloat); // float


		ResetInOut();
		if (!displayMaterial) displayMaterial = new Material(displayShader);
		displayMaterial.SetTexture("_AirPressure", InAirPressure);
		displayMaterial.SetTexture("_AirTemp", InAirTemp);
		displayMaterial.SetTexture("_MatTemp", InMaterialTemp);

		draw.SetInts("Origin", 0, 0, 0);
		{
			var kernel = draw.FindKernel("f_Draw");
			draw.SetTexture(kernel, "f_In", Passability);
			draw.SetFloat("f_Value", 1);
			draw.Dispatch(kernel, dims.x, dims.y, dims.z);
		}
		{
			var kernel = draw.FindKernel("f3_Draw");
			draw.SetTexture(kernel, "f3_In", Movability);
			draw.SetFloats("f3_Value", 1, 1, 1);
			draw.Dispatch(kernel, dims.x, dims.y, dims.z);
		}
		{
			// r -> Isobaric volumetric heat capacity
			// g -> Air percentage
			// b -> Thermal conductivity coefficient (W/(m*K))
			var kernel = draw.FindKernel("f3_Draw");
			draw.SetTexture(kernel, "f3_In", HeatModel);
			draw.SetFloats("f3_Value", 0.01f, 1, 0);
			draw.Dispatch(kernel, dims.x, dims.y, dims.z);
		}


		foreach (var block in model.GetBlocks()) {
			var bounds = block.CalcBounds();

			draw.SetInts("Origin", bounds.min.x, bounds.min.y, bounds.min.z);

			if (block.sourceIndex > 0) {
				var kernel = draw.FindKernel("i_Draw");
				draw.SetTexture(kernel, "i_In", Source);
				draw.SetInt("i_Value", (byte)block.sourceIndex);
				draw.Dispatch(kernel, bounds.size.x, bounds.size.y, bounds.size.z);
			}
			{
				var kernel = draw.FindKernel("f3_Draw");
				draw.SetTexture(kernel, "f3_In", HeatModel);
				draw.SetFloats("f3_Value", block.material.heatCapacity, block.materialPercentage, block.material.thermalTransferRate);
				draw.Dispatch(kernel, bounds.size.x, bounds.size.y, bounds.size.z);
			}
			{
				var kernel = draw.FindKernel("f_Draw");
				draw.SetTexture(kernel, "f_In", Passability);
				draw.SetFloat("f_Value", block.passability);
				draw.Dispatch(kernel, bounds.size.x, bounds.size.y, bounds.size.z);
			}
			{
				var kernel = draw.FindKernel("f3_Draw");
				draw.SetTexture(kernel, "f3_In", Movability);
				draw.SetFloats("f3_Value", block.movability.x, block.movability.y, block.movability.z);
				draw.Dispatch(kernel, bounds.size.x, bounds.size.y, bounds.size.z);
			}
			if (block.fanIndex > 0) {
				var kernel = draw.FindKernel("i_Draw");
				draw.SetTexture(kernel, "i_In", Fan);
				draw.SetInt("i_Value", (byte)block.fanIndex);
				draw.Dispatch(kernel, bounds.size.x, bounds.size.y, bounds.size.z);
			}
		}

		OverwriteOut();

		RenderTexture InitTex(RenderTexture old, RenderTextureFormat format) {
			if (!changed && old && old.enableRandomWrite) return old;
			var res = old;
			if (old && old.IsCreated()) old.Release();
			res = new RenderTexture(dims.x, dims.y, 0, format) {
				dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
				enableRandomWrite = true,
				volumeDepth = dims.z,
			};
			res.Create();
			return res;
		}
	}

	public void Simulate() {
		Step();
		OverwriteIn();
	}

	public void Step() {
		var id = main.FindKernel("Step");
		SetInOut(id);

		main.SetInts(nameof(dims), dims.x, dims.y, dims.z);

		main.SetTexture(id, nameof(Errors), Errors);
		main.SetTexture(id, nameof(Source), Source);
		main.SetTexture(id, nameof(Fan), Fan);
		main.SetTexture(id, nameof(Passability), Passability);
		main.SetTexture(id, nameof(Movability), Movability);
		main.SetTexture(id, nameof(HeatModel), HeatModel);

		main.Dispatch(id, dims.x, dims.y, dims.z);
	}

	public void Test() {
		draw.SetInts("Origin", testOrigin.x, testOrigin.y, testOrigin.z);
		{
			var kernel = draw.FindKernel("f_Draw");
			draw.SetTexture(kernel, "f_In", InAirTemp);
			draw.SetFloat("f_Value", testTemperature);
			draw.Dispatch(kernel, testSize.x, testSize.y, testSize.z);
		}
		OverwriteOut();
	}

	public void OverwriteIn() {
		var id = main.FindKernel("OverwriteIn");
		SetInOut(id);
		main.Dispatch(id, dims.x, dims.y, dims.z);

	}

	public void OverwriteOut() {
		var id = main.FindKernel("OverwriteOut");
		SetInOut(id);
		main.Dispatch(id, dims.x, dims.y, dims.z);
	}

	public void ResetInOut() {
		var id = main.FindKernel("ResetInOut");
		SetInOut(id);
		main.Dispatch(id, dims.x, dims.y, dims.z);
	}

	public void SetInOut(int id) {
		main.SetTexture(id, nameof(InVelocity), InVelocity); // float3
		main.SetTexture(id, nameof(InAirPressure), InAirPressure); // float
		main.SetTexture(id, nameof(InAirTemp), InAirTemp); // float
		main.SetTexture(id, nameof(InMaterialTemp), InMaterialTemp); // float
		main.SetTexture(id, nameof(OutVelocity), OutVelocity); // float3
		main.SetTexture(id, nameof(OutAirPressure), OutAirPressure); // float
		main.SetTexture(id, nameof(OutAirTemp), OutAirTemp); // float
		main.SetTexture(id, nameof(OutMaterialTemp), OutMaterialTemp); // float
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
	[CustomEditor(typeof(Controller), true)]
	public class ControllerEditor : Editor {

		Controller t => (Controller)target;

		public override bool RequiresConstantRepaint() => t.autoSimulate;

		public override void OnInspectorGUI() {
			serializedObject.Update();

			DrawDefaultInspector();

			if (t.autoSimulate) {
				if (t.autoTest) t.Test();
				t.Simulate();
			}

			if (ButtonField(new GUIContent("Init"))) {
				t.Init();
			}

			if (ButtonField(new GUIContent("Simulate"))) {
				t.Simulate();
			}

			if (ButtonField(new GUIContent("Test"))) {
				t.Test();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
#endif