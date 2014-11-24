using UnityEngine;
using System.Collections;

public class Polygon : MonoBehaviour {
	public Color gizmoColor = Color.yellow;
	public Vector3[] verts;
	
	void OnDrawGizmos() {
		if (verts != null) {
			var xform = GetComponent<Transform>();
			var len = verts.Length;
			if (len >= 3) {
				var p0 = xform.TransformPoint(verts[len-1]);
				for(int i=0; i<len; ++i) {
					var p1 = xform.TransformPoint(verts[i]);
					Gizmos.DrawLine(p0, p1);
					p0 = p1;
				}
			}
		}
	}
	
	
}
