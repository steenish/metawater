using UnityEngine;

public class HelperFunctions {
    
	public static float InterpolateLinear(float f0, float f1, float t) {
		return Mathf.Lerp(f0, f1, t);
	}

	public static Vector2 InterpolateLinear(Vector2 v0, Vector2 v1, float t) {
		return Vector2.Lerp(v0, v1, t);
	}

	public static Vector3 InterpolateLinear(Vector3 v0, Vector3 v1, float t) {
		return Vector3.Lerp(v0, v1, t);
	}

	public static float InterpolateBilinear(float f00, float f01, float f10, float f11, float t) {
		float value1 = InterpolateLinear(f00, f01, t);
		float value2 = InterpolateLinear(f10, f11, t);
		return InterpolateLinear(value1, value2, t);
	}

	public static Vector2 InterpolateBilinear(Vector2 v00, Vector2 v01, Vector2 v10, Vector2 v11, float t) {
		Vector2 vector1 = InterpolateLinear(v00, v01, t);
		Vector2 vector2 = InterpolateLinear(v10, v11, t);
		return InterpolateLinear(vector1, vector2, t);
	}

	public static Vector3 InterpolateBilinear(Vector3 v00, Vector3 v01, Vector3 v10, Vector3 v11, float t) {
		Vector3 vector1 = InterpolateLinear(v00, v01, t);
		Vector3 vector2 = InterpolateLinear(v10, v11, t);
		return InterpolateLinear(vector1, vector2, t);
	}

	public static float InterpolateTrilinear(float f000, float f001, float f010, float f011,
											 float f100, float f101, float f110, float f111, float t) {
		float value1 = InterpolateBilinear(f000, f001, f010, f011, t);
		float value2 = InterpolateBilinear(f100, f101, f110, f111, t);
		return InterpolateLinear(value1, value2, t);
	}

	public static Vector2 InterpolateTrilinear(Vector2 v000, Vector2 v001, Vector2 v010, Vector2 v011,
											   Vector2 v100, Vector2 v101, Vector2 v110, Vector2 v111, float t) {
		Vector2 vector1 = InterpolateBilinear(v000, v001, v010, v011, t);
		Vector2 vector2 = InterpolateBilinear(v100, v101, v110, v111, t);
		return InterpolateLinear(vector1, vector2, t);
	}

	public static Vector3 InterpolateTrilinear(Vector3 v000, Vector3 v001, Vector3 v010, Vector3 v011,
											   Vector3 v100, Vector3 v101, Vector3 v110, Vector3 v111, float t) {
		Vector3 vector1 = InterpolateBilinear(v000, v001, v010, v011, t);
		Vector3 vector2 = InterpolateBilinear(v100, v101, v110, v111, t);
		return InterpolateLinear(vector1, vector2, t);
	}

	public static Vector2 V3ToHV2(Vector3 v) {
		return new Vector2(v.x, v.z);
	}

	public static Vector3 HV2ToV3(Vector2 v) {
		return new Vector3(v.x, 0.0f, v.y);
	}
}
