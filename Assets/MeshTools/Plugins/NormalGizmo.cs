using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class NormalGizmo : MonoBehaviour {
	
	#if UNITY_EDITOR
	void OnDrawGizmosSelected() {
		Gizmos.color = Color.yellow;
		var mesh = GetComponent<MeshFilter>().sharedMesh;
		var xform = GetComponent<Transform>();
		var vbuf = mesh.vertices;
		var nbuf = mesh.normals;
		for(int i=0; i<vbuf.Length; ++i) {
			var p0 = xform.TransformPoint(vbuf[i]);
			var n = xform.TransformDirection(nbuf[i]);
			Gizmos.DrawLine(p0, p0 + 0.1f * n);
		}
		
	}
	#endif
	
}
