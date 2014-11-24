using UnityEditor;
using UnityEngine;
using System.Collections;

public static class Toolbelt {
	
	public static MeshFilter GetFilter(this GameObject go) { return go.GetComponent<MeshFilter>(); }
	public static MeshRenderer GetFX(this GameObject go) { return go.GetComponent<MeshRenderer>(); }
	public static Transform GetTransform(this GameObject go) { return go.GetComponent<Transform>(); }
	
	
	public static Material GetDefaultMaterial() {
		var primitive = GameObject.CreatePrimitive(PrimitiveType.Plane);
		var result = primitive.GetComponent<MeshRenderer>().sharedMaterial;
		GameObject.DestroyImmediate(primitive);
		return result;
	}
	
	public static GameObject CreatePrimitive(string name) {
		var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
		go.GetFX().castShadows = false;
		go.GetFX().receiveShadows = false;
		go.GetFX().sharedMaterial = GetDefaultMaterial();		
		return go;		
	}
	
	static Mesh LoadMesh(string name) {
		return AssetDatabase.LoadAssetAtPath(
			string.Format("Assets/MeshTools/Primitives/{0}.asset", name), typeof(Mesh)
		) as Mesh;
	}
	
	public static GameObject Create(string name) {
		var result = CreatePrimitive(name);
		result.GetFilter().sharedMesh = LoadMesh(name);
		return result;
	}
	
	public static GameObject CreateType(PrimitiveType pt) {
		var go = GameObject.CreatePrimitive(pt);
		GameObject.DestroyImmediate(go.GetComponent<Collider>());
		go.GetFX().castShadows = false;
		go.GetFX().receiveShadows = false;
		return go;
	}
	
	[MenuItem("Toolbelt/Create Cube")]
	public static GameObject CreateCube() { return CreateType(PrimitiveType.Cube); }
	
	[MenuItem("Toolbelt/Create Capsule")]
	public static GameObject CreateCapsule() { return CreateType(PrimitiveType.Capsule); }
	
	[MenuItem("Toolbelt/Create Sphere")]
	public static GameObject CreateSphere() { return CreateType(PrimitiveType.Sphere); }
	
	[MenuItem("Toolbelt/Create Cylinder")]
	public static GameObject CreateCylinder() { return CreateType(PrimitiveType.Cylinder); } 
	
//	[MenuItem("Toolbelt/Create Plane")]
//	public static GameObject CreatePlane() { return CreateType(PrimitiveType.Quad); }
	
	[MenuItem("Toolbelt/Create Diamond")]
	public static GameObject CreateDiamond() { return Create("diamond"); }
	
	[MenuItem("Toolbelt/Create Pyramid")]
	public static GameObject CreatePyramid() { return Create("pyramid"); }
	
}
