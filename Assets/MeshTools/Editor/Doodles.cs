using UnityEditor;
using UnityEngine;

public static class Doodles {
	
	
	public static GameObject CreateCubeRing(int n, float r) {
		var go = new GameObject();
		var xf = go.GetTransform();
		for(int i=0; i<n; ++i) {
			var cube = Toolbelt.CreateCube();
			cube.GetTransform().position = new Vector3(r, 0, 0);
			cube.GetTransform().parent = xf;
			xf.Rotate(0f, 360f/n, 0);
		}
		return go;
	}
	
	
	
	[MenuItem("Doodles/Ring")]
	static void Ring() {
		CreateCubeRing(16, 8f);
	}
	
	[MenuItem("Doodles/Chevrons")]
	static void Chevrons() {
		
		var curve = Selection.activeGameObject.GetComponent(typeof(ParametricCurve)) as ParametricCurve;
		if (curve == null) {
			return;
		}
		
		int count = 100;
		var samples = new Vector3[count];
		curve.SampleValues(samples, 0f, 1f);
		
		var root = new GameObject().GetTransform();
		
		for(int i=0; i<count; ++i) {
			var arrow = Toolbelt.CreatePyramid();
			var xform = arrow.GetTransform();
			xform.position = samples[i];
			xform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
			var u = i / (count-1f);
			xform.rotation = 
				Quaternion.LookRotation(curve.DerivAt(u).normalized) *
				Quaternion.Euler(90f, 0f, 0f) ;
			xform.parent = root;
		}
		
	}
	
	
}
