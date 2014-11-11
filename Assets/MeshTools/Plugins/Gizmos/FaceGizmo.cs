using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
public class FaceGizmo : MonoBehaviour {
	
	#if UNITY_EDITOR
	void OnDrawGizmosSelected() {
		Gizmos.color = Color.yellow;
		var mesh = GetComponent<MeshFilter>().sharedMesh;
		var xform = GetComponent<Transform>();
		var sz = 0.025f * Vector3.one;
		var vbuf = mesh.vertices;
		for(int i=0; i<vbuf.Length; ++i) {
			vbuf[i] = xform.TransformPoint(vbuf[i]);
		}
		var ibuf = mesh.triangles;
		var nfaces = ibuf.Length / 3;
		var k = 1f / 3f;
		for(int i=0; i<nfaces; ++i) {
			Gizmos.DrawCube(
				k * (vbuf[ibuf[3*i]] + vbuf[ibuf[3*i+1]] + vbuf[ibuf[3*i+2]]),
				sz
			);		
		}
		
	}
	#endif
	
}
