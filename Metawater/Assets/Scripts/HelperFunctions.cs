public class HelperFunctions {
    
	public static float InterpolateLinear(float f0, float f1, float t) {
		return (1 - t) * f0 + t * f1;
	}

	public static float InterpolateBilinear(float f00, float f01, float f10, float f11, float t) {
		float value1 = InterpolateLinear(f00, f01, t);
		float value2 = InterpolateLinear(f10, f11, t);
		return InterpolateLinear(value1, value2, t);
	}

	public static float InterpolateTrilinear(float f000, float f001, float f010, float f011,
											 float f100, float f101, float f110, float f111, float t) {
		float value1 = InterpolateBilinear(f000, f001, f010, f011, t);
		float value2 = InterpolateBilinear(f100, f101, f110, f111, t);
		return InterpolateLinear(value1, value2, t);
	}
}
