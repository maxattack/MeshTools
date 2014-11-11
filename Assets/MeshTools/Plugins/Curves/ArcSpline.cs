using UnityEngine;
using System.Collections;

public class ArcSpline : MonoBehaviour, ParametricCurve {
	
	public Color gizmoColor = Color.yellow;
	public Transform center;
	public float radius = 1f;
	public float start = 0f;
	public float rotations = 1f;
	public float ascent = 0f;
	
	const float TAU = Mathf.PI + Mathf.PI;
	
	public Vector3 ValueAt (float u) {
		var forward = ascent * u * center.forward;
		u = TAU * (start + u * rotations);
		var p0 = center.position;
		var right = radius * center.right;
		var up = radius * center.up;
		return p0 + Mathf.Cos(u) * right + Mathf.Sin(u) * up + forward;
	}
	
	public Vector3 DerivAt (float u) {
		var forward = (ascent/(TAU * rotations)) * center.forward;
		u = TAU * (start + u * rotations);
		var right = radius * center.right;
		var up = radius * center.up;
		return (-Mathf.Sin(u)) * right + Mathf.Cos(u) * up + forward;		
	}
	
	public void SampleValues (Vector3[] samples, float u0=0f, float u1=1f) {
		var t0 = u0;
		var t1 = u1;
	
		u0 = TAU * (start + u0 * rotations);
		u1 = TAU * (start + u1 * rotations);
		var p0 = center.position;
		var right = radius * center.right;
		var up = radius * center.up;
		var forward = ascent * center.forward;
		
		int len = samples.Length;
		var du = (u1 - u0) / (len-1f);
		Vector2 curr = new Vector2(Mathf.Cos(u0), Mathf.Sin(u0));
		Vector2 rotor = new Vector2(Mathf.Cos(du), Mathf.Sin(du));
		var dt = (t1 - t0) / (len-1f);
		var t = t0;
		for(int i=0; i<len; ++i) {
			samples[i] = p0 + curr.x * right + curr.y * up + t * forward;
			curr = new Vector2(
				curr.x * rotor.x - curr.y * rotor.y,
				curr.x * rotor.y + curr.y * rotor.x
			);
			t += dt;
		}		
	}
	
	public void SampleDerivs(Vector3[] samples, float u0=0f, float u1=1f) {
		u0 = TAU * (start + u0 * rotations);
		u1 = TAU * (start + u1 * rotations);
		var p0 = center.position;
		var right = radius * center.right;
		var up = radius * center.up;
		var forward = (ascent/(TAU * rotations)) * center.forward;
		
		int len = samples.Length;
		var du = (u1 - u0) / (len-1f);
		Vector2 curr = new Vector2(Mathf.Cos(u0), Mathf.Sin(u0));
		Vector2 rotor = new Vector2(Mathf.Cos(du), Mathf.Sin(du));
		for(int i=0; i<len; ++i) {
			samples[i] = p0 + (-curr.y) * right + curr.x * up + forward;
			curr = new Vector2(
				curr.x * rotor.x - curr.y * rotor.y,
				curr.x * rotor.y + curr.y * rotor.x
				);
		}		
		
	}
	
	void OnDrawGizmos() {
		if (center) {
			Gizmos.color = gizmoColor;
			SampleValues(Curves.Scratch);
			Curves.DrawCurveGizmo(Curves.Scratch);
		}
	}
	
	void OnDrawGizmosSelected() {
		if (center) {
			Gizmos.color = new Color(gizmoColor.g, gizmoColor.b, gizmoColor.r);
			Gizmos.DrawSphere(ValueAt(0f), 0.05f);
			Gizmos.DrawCube(ValueAt(1f), new Vector3(0.1f, 0.1f, 0.1f));
		}
		
	}
	
		
}
