using UnityEngine;

public class QuadraticBezierSpline : MonoBehaviour, ParametricCurve {
	
	public Color gizmoColor = Color.yellow;
	public Transform begin;
	public Transform control;
	public Transform end;
	
	public Vector3 ValueAt(float u) {
		return Curves.QuadraticBezier(begin.position, control.position, end.position, u);
	}
	
	public Vector3 DerivAt(float u) {
		return Curves.QuadraticBezierDeriv(begin.position, control.position, end.position, u);
	}
	
	public void SampleValues(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var p1 = control.position;
		var p2 = end.position;
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = Curves.QuadraticBezier(p0, p1, p2, u);
		}
	}
	
	public void SampleDerivs(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var p1 = control.position;
		var p2 = end.position;
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = Curves.QuadraticBezierDeriv(p0, p1, p2, u);
		}
	}

	void OnDrawGizmos() {
		if (begin && control && end) {
			Gizmos.color = gizmoColor;
			SampleValues(Curves.Scratch);
			Curves.DrawCurveGizmo(Curves.Scratch);
		}
	}
	
	void OnDrawGizmosSelected() {
		if (begin && control && end) {
			Gizmos.color = new Color(gizmoColor.g, gizmoColor.b, gizmoColor.r);
			var p0 = begin.position;
			var p1 = control.position;
			var p2 = end.position;
			Gizmos.DrawLine(p0, p1);
			Gizmos.DrawLine(p1, p2);
			Gizmos.DrawSphere(p0, 0.05f);
			Gizmos.DrawSphere(p1, 0.05f);
			Gizmos.DrawCube(p2, new Vector3(0.1f, 0.1f, 0.1f));
		}
		
	}
}
