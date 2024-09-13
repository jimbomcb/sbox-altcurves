namespace AltCurves;

public readonly partial record struct AltCurve
{
	/// <summary>
	/// How do we handle cycling the curve outside of the keyframe range
	/// Similar to Maya or Unreal Engine curve pre/post infinity
	/// </summary>
	public enum Extrapolation
	{
		/// <summary>
		/// Use the first keyframe value if time falls before that, and the last keyframe value if time falls after
		/// </summary>
		Constant,
		/// <summary>
		/// Linearly extrapolate from the first/last curve keyframe
		/// </summary>
		Linear,
		/// <summary>
		/// Cycle the curve values, repeating the same set of keyframes.
		/// </summary>
		Cycle,
		/// <summary>
		/// Cycle the curve values, but accumulate any vertical offset from each cycle from/to infinity
		/// </summary>
		CycleOffset,
		/// <summary>
		/// Ping-pong back and forth between the keyframe range
		/// </summary>
		Oscillate
	}
}
