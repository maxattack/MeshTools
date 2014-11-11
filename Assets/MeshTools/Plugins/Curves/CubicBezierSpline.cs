using UnityEngine;

public class CubicBezierSpline : MonoBehaviour, ParametricCurve {
	
	public Color gizmoColor = Color.yellow;
	public Transform begin;
	public Transform control1;
	public Transform control2;
	public Transform end;
	
	public Vector3 ValueAt(float u) {
		return Curves.CubicBezier(begin.position, control1.position, control2.position, end.position, u);
	}
	
	public Vector3 DerivAt(float u) {
		return Curves.CubicBezierDeriv(begin.position, control1.position, control2.position, end.position, u);
	}
	
	public void SampleValues(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var p1 = control1.position;
		var p2 = control2.position;
		var p3 = end.position;
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = Curves.CubicBezier(p0, p1, p2, p3, u);
		}
	}
	
	public void SampleDerivs(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var p1 = control1.position;
		var p2 = control2.position;
		var p3 = end.position;
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = Curves.CubicBezierDeriv(p0, p1, p2, p3, u);
		}
	}
	
	void OnDrawGizmos() {
		if (begin && control1 && control2 && end) {
			Gizmos.color = gizmoColor;
			SampleValues(Curves.Scratch);
			Curves.DrawCurveGizmo(Curves.Scratch);
		}
	}
	
	void OnDrawGizmosSelected() {
		if (begin && control1 && control2 && end) {
			Gizmos.color = new Color(gizmoColor.g, gizmoColor.b, gizmoColor.r);
			var p0 = begin.position;
			var p1 = control1.position;
			var p2 = control2.position;
			var p3 = end.position;
			Gizmos.DrawLine(p0, p1);
			Gizmos.DrawLine(p1, p2);
			Gizmos.DrawLine(p2, p3);
			Gizmos.DrawSphere(p0, 0.05f);
			Gizmos.DrawSphere(p1, 0.05f);
			Gizmos.DrawCube(p2, new Vector3(0.1f, 0.1f, 0.1f));
			Gizmos.DrawCube(p3, new Vector3(0.1f, 0.1f, 0.1f));
		}
		
	}
}
