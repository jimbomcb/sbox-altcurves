using System.Text.Json.Serialization;

namespace AltCurves;

public readonly partial record struct AltCurve
{
	/// <summary>
	/// A curve consists of a series of ordered Keyframes, each with a time, value and interpolation parameters
	/// </summary>
	public readonly record struct Keyframe
	{
		[JsonPropertyName( "x" )]
		public float Time { get; init; }

		[JsonPropertyName( "y" )]
		public float Value { get; init; }

		/// <summary>
		/// How does this keyframe interpolate to the next keyframe
		/// </summary>
		[JsonPropertyName( "i" )]
		public Interpolation Interpolation { get; init; }

		/// <summary>
		/// Incoming tangent rise/run
		/// </summary>
		[JsonPropertyName( "ti" )]
		public float TangentIn { get; init; }

		/// <summary>
		/// Outgoing tangent rise/run
		/// </summary>
		[JsonPropertyName( "to" )]
		public float TangentOut { get; init; }

		/// <summary>
		/// How are the tangents controlled for this keyframe
		/// </summary>
		[JsonPropertyName( "tm" )]
		public TangentMode TangentMode { get; init; }

		public Keyframe() : this( 0.0f, 0.0f, Interpolation.Cubic, TangentMode.Automatic )
		{
		}

		public Keyframe(float time, float value, Interpolation interpolation, TangentMode tangentMode = TangentMode.Automatic, float tangentIn = 0.0f, float tangentOut = 0.0f )
		{
			Time = time;
			Value = value;
			Interpolation = interpolation;
			TangentIn = tangentIn;
			TangentOut = tangentOut;
			TangentMode = tangentMode;
		}
	}
}

