using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class VertexGizmo : MonoBehaviour {
	
	#if UNITY_EDITOR
	void OnDrawGizmosSelected() {
		Gizmos.color = Color.yellow;
		var mesh = GetComponent<MeshFilter>().sharedMesh;
		var xform = GetComponent<Transform>();
		var sz = 0.025f * Vector3.one;
		foreach(var vert in mesh.vertices) {
			Gizmos.DrawCube(xform.TransformPoint(vert), sz);		
		}
		
	}
	#endif
	
}
