namespace AltCurves;

public readonly partial record struct AltCurve
{
	/// <summary>
	/// How do we calculate values that fall between keyframes
	/// </summary>
	public enum Interpolation
	{
		/// <summary>
		/// Interpolate with a smooth curve between keyframes
		/// </summary>
		Cubic,

		/// <summary>
		/// Linearly interpolate between keyframes
		/// </summary>
		Linear,

		/// <summary>
		/// Constant interpolation producing a stepped curve
		/// </summary>
		Constant,
	}
}
