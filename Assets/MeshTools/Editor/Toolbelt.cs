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
	
	static GameObject CreatePrimitive(string name) {
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
	
	public static GameObject CreateCube() { return CreateType(PrimitiveType.Cube); }
	public static GameObject CreateCapsule() { return CreateType(PrimitiveType.Capsule); }
	public static GameObject CreateSphere() { return CreateType(PrimitiveType.Sphere); }
	public static GameObject CreateCylinder() { return CreateType(PrimitiveType.Cylinder); } 
	public static GameObject CreatePlane() { return CreateType(PrimitiveType.Quad); }
	public static GameObject CreateDiamond() { return Create("diamond"); }
	public static GameObject CreatePyramid() { return Create("pyramid"); }
	
}
