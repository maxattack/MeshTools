using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class VertexGizmo : MonoBehaviour {
	
	#if UNITY_EDITOR
	void OnDrawGizmosSelected() {
		var mesh = GetComponent<MeshFilter>().sharedMesh;
		var xform = GetComponent<Transform>();
		var sz = 0.025f * Vector3.one;
		var vbuf = mesh.vertices;
		var cbuf = mesh.colors;
		for(int i=0; i<vbuf.Length; ++i) {
			var p0 = xform.TransformPoint(vbuf[i]);
			Gizmos.color = cbuf.Length == 0 ? Color.white : cbuf[i];
			Gizmos.DrawCube(p0, sz);		
		}
		
	}
	#endif
	
}
