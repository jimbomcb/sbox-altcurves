namespace AltCurves;

public readonly partial record struct AltCurve
{
	/// <summary>
	/// Controls how the keyframe tangent amounts are calculated
	/// </summary>
	public enum TangentMode
	{
		/// <summary>
		/// Automatically adjust tagent handles based on keyframe positions.
		/// The automatic tangent of a keyframe is based on the slope of the positions of the surrounding keyframes.
		/// Changes to an automatic handle will turn it into a mirrored handle.
		/// </summary>
		Automatic,
		/// <summary>
		/// Changes to one tangent handle are reflected on the opposite handle.
		/// </summary>
		Mirrored,
		/// <summary>
		/// Both keyframe tangents can be controlled independently.
		/// </summary>
		Split,
	}
}

