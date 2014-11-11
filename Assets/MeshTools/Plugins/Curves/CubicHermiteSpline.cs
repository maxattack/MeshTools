using UnityEngine;

public static class CubicHermiteSplineExt {
	public static Vector3 ForwardScaled(this Transform xform) {
		return xform.lossyScale.z * xform.forward;
	}
}

public class CubicHermiteSpline : MonoBehaviour, ParametricCurve {
	
	public Color gizmoColor = Color.yellow;
	public Transform begin;
	public Transform end;
	
	public Vector3 ValueAt(float u) {
		return Curves.CubicHermite(begin.position, begin.ForwardScaled(), end.position, end.ForwardScaled(), u);
	}
	
	public Vector3 DerivAt(float u) {
		return Curves.CubicHermiteDeriv(begin.position, begin.ForwardScaled(), end.position, end.ForwardScaled(), u);
	}
	
	public void SampleValues(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var m0 = begin.ForwardScaled();
		var p1 = end.position;
		var m1 = end.ForwardScaled();
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = Curves.CubicHermite(p0, m0, p1, m1, u);
		}
	}
	
	public void SampleDerivs(Vector3[] results, float u0 = 0f, float u1 = 1f) {
		var p0 = begin.position;
		var m0 = begin.ForwardScaled();
		var p1 = end.position;
		var m1 = end.ForwardScaled();
		
		int len = results.Length;
		var u = u0;
		var du = (u1 - u0) / len;
		for(int i=0; i<len; ++i) {
			u += du;
			results[i] = Curves.CubicHermiteDeriv(p0, m0, p1, m1, u);
		}
	}
	
	void OnDrawGizmos() {
		if (begin && end) {
			Gizmos.color = gizmoColor;
			SampleValues(Curves.Scratch);
			Curves.DrawCurveGizmo(Curves.Scratch);
		}
	}
	
	void OnDrawGizmosSelected() {
		if (begin && end) {
			Gizmos.color = new Color(gizmoColor.g, gizmoColor.b, gizmoColor.r);		
			var p0 = begin.position;
			var m0 = begin.ForwardScaled();
			var p1 = end.position;
			var m1 = end.ForwardScaled();
			Gizmos.DrawLine(p0, p0 + m0);
			Gizmos.DrawLine(p1, p1 + m1);			
			Gizmos.DrawSphere(p0, 0.05f);
			Gizmos.DrawCube(p1, new Vector3(0.1f, 0.1f, 0.1f));
		}
	}
}
