using System;
using UnityEngine;

public class LinearSpline : MonoBehaviour, ParametricCurve {
	
	public Color gizmoColor = Color.yellow;
	public Transform begin;
	public Transform end;
	
	public Vector3 ValueAt(float u) {
		return Curves.Linear(begin.position, end.position, u);
	}
	
	public Vector3 DerivAt(float u) {
		return end.position - begin.position;
	}
	
	public void SampleValues(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var p1 = end.position;
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = p0 + u * (p1 - p0);
		}
	}
	
	public void SampleDerivs(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var d = end.position - begin.position;
		int len = results.Length;
		for(int i=0; i<len; ++i) {
			results[i] = d;
		}
	}
	
	void OnDrawGizmos() {
		if (begin && end) {
			Gizmos.color = gizmoColor;
			Gizmos.DrawLine(begin.position, end.position);
		}
	}
	
	void OnDrawGizmosSelected() {
		if (begin && end) {
			Gizmos.color = new Color(gizmoColor.g, gizmoColor.b, gizmoColor.r);
			Gizmos.DrawSphere(begin.position, 0.05f);
			Gizmos.DrawCube(end.position, new Vector3(0.1f, 0.1f, 0.1f));
		}
		
	}
}