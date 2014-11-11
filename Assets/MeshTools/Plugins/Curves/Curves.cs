using System.Collections;
using UnityEngine;

public static class Curves {
	
	public static Vector3[] Scratch = new Vector3[128];
	
	public static Vector3 Linear(Vector3 p0, Vector3 p1, float u) { 
		return p0 + u * (p1 - p0); 
	}
	
	public static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float u) {
		return ((1f-u)*(1f-u))*p0 + (2f*(1f-u)*u)*p1 + (u*u)*p2;
	}
	
	public static Vector3 QuadraticBezierDeriv(Vector3 p0, Vector3 p1, Vector3 p2, float u) {
		return (2f*(1f-u))*(p1-p0) + (2f*u)*(p2-p1);
	}
	
	public static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u) {
		return (
			((1f-u) * (1f-u) * (1f-u)) * p0 +
			(3f * (1f-u) * (1f-u) * u) * p1 +
			(3f * (1f-u) * u * u) * p2 +
			(u * u * u) * p3
		);
	}
	
	public static Vector3 CubicBezierDeriv(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float u) {
		return 3f * (
			(-(1f-u) * (1f-u)) * p0 +
			(1f - 4f * u + 3f * u * u) * p1 +
			(2f * u - 3f * u * u) * p2 +
			(u * u) * p3
		);
	}
	public static Vector3 CubicHermite(Vector3 p0, Vector3 m0, Vector3 p1, Vector3 m1, float u) {
		return (
			(2f*u*u*u - 3f*u*u + 1f) * p0 +
			(u*u*u - 2f*u*u + u) * m0 +
			(-2f*u*u*u + 3f *u*u) * p1 +
			(u*u*u - u*u) * m1
		);
	}
	
	public static Vector3 CubicHermiteDeriv(Vector3 p0, Vector3 m0, Vector3 p1, Vector3 m1, float u) {
		return (
			(6f*(u*u - u)) * p0 +
			(3f*u*u - 4f*u + 1f) * m0 +
			(6f*(u - u*u)) * p1 +
			(3f*u*u - 2f*u) * m1
		);
	}
	
	public static void DrawCurveGizmo(Vector3[] samples) {
		int len = samples.Length;
		for(int i=0; i<len-1; ++i) {
			Gizmos.DrawLine(samples[i], samples[i+1]);
		}
	}
	
	
}
