using UnityEngine;
using System.Collections;

public interface ParametricCurve {

	Vector3 ValueAt(float u);
	Vector3 DerivAt(float u);
	void SampleValues(Vector3[] samples, float u0, float u1);
	void SampleDerivs(Vector3[] samples, float u0, float u1);
	
}
