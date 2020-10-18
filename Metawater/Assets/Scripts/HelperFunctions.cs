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

	public static float InterpolateBilinear(float f00, float f10, float f01, float f11, Vector2 t) {
		float value1 = InterpolateLinear(f00, f10, t.x);
		float value2 = InterpolateLinear(f01, f11, t.x);
		return InterpolateLinear(value1, value2, t.y);
	}

	public static Vector2 InterpolateBilinear(Vector2 v00, Vector2 v10, Vector2 v01, Vector2 v11, Vector2 t) {
		Vector2 vector1 = InterpolateLinear(v00, v10, t.x);
		Vector2 vector2 = InterpolateLinear(v01, v01, t.x);
		return InterpolateLinear(vector1, vector2, t.y);
	}

	public static Vector3 InterpolateBilinear(Vector3 v00, Vector3 v10, Vector3 v01, Vector3 v11, Vector2 t) {
		Vector3 vector1 = InterpolateLinear(v00, v10, t.x);
		Vector3 vector2 = InterpolateLinear(v01, v11, t.x);
		return InterpolateLinear(vector1, vector2, t.y);
	}

	public static Vector2 V3ToHV2(Vector3 v) {
		return new Vector2(v.x, v.z);
	}

	public static Vector3 HV2ToV3(Vector2 v) {
		return new Vector3(v.x, 0.0f, v.y);
	}

	public static Vector2 IntegrateRK4(Vector2 position, float step, UniformGrid2DVector2 vectorField) {
		Vector2 v1 = vectorField.Interpolate(position);
		Vector2 v2 = vectorField.Interpolate(position + 0.5f * step * v1);
		Vector2 v3 = vectorField.Interpolate(position + 0.5f * step * v2);
		Vector2 v4 = vectorField.Interpolate(position + step * v3);
		float inverse6 = 1.0f / 6.0f;
		float inverse3 = 1.0f / 3.0f;
		return position + step * (inverse6 * v1 + inverse3 * v2 + inverse3 * v3 + inverse6 * v4);
	}

	public static Vector3 IntegrateRK4(Vector3 position, float step, UniformGrid2DVector2 vectorField) {
		return HV2ToV3(IntegrateRK4(V3ToHV2(position), step, vectorField));
	}
}
