using Sandbox.UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AltCurves;

/// <summary>
/// AltCurve, Curves with an extended editor and better performance.
/// A valid, initialized AltCurve must have 1 or more keyframes in distinct ascending time order.
/// </summary>
[JsonConverter( typeof( JsonConverter ) )]
public readonly partial record struct AltCurve
{
	/// <summary>
	/// The bulk of the curve data, each keyframe contains a time/value and interpolates to the next keyframe
	/// </summary>
	public ImmutableArray<Keyframe> Keyframes { get; init; }

	/// <summary>
	/// How to handle values before the first keyframe
	/// </summary>
	public Extrapolation PreInfinity { get; init; }

	/// <summary>
	/// How to handle values after the last keyframe
	/// </summary>
	public Extrapolation PostInfinity { get; init; }

	/// <summary>
	/// The min/max time range of the provided keyframe set
	/// </summary>
	public (float Min, float Max) TimeRange { get; init; }

	/// <summary>
	/// The total timespan of the curve, from the first keyframe to the last
	/// </summary>
	public float TimeSpan => TimeRange.Max - TimeRange.Min;

	/// <summary>
	/// The min/max value observed across all keyframes, including curves that extend above/below their keyframes.
	/// Cached at construction time to avoid recalculating each time.
	/// </summary>
	public (float Min, float Max) ValueRange { get; init; }

	/// <summary>
	/// The min/max value observed for each keyframe segment, including curves that extend above/below their keyframes.
	/// Cached at construction time to avoid recalculating each time. Only valid if we have > 1 keyframe (has n-1 entries, 1 per segment)
	/// </summary>
	public ImmutableArray<(float Min, float Max)> KeyframeValueRanges { get; init; }

	public AltCurve( IEnumerable<Keyframe> keyframes, Extrapolation preInfinity, Extrapolation postInfinity )
	{
		// Don't allow a curve consisting of 0 keyframes, if none exist then add one default keyframe.
		Keyframes = keyframes.Any() ? keyframes.ToImmutableArray() : ImmutableArray.Create( new Keyframe() );
		PreInfinity = preInfinity;
		PostInfinity = postInfinity;

		TimeRange = (Keyframes[0].Time, Keyframes[^1].Time);
		ValueRange = (Keyframes[0].Value, Keyframes[0].Value);

		if ( Keyframes.Length > 1 )
		{
			var ranges = new List<(float Min, float Max)>( Keyframes.Length );
			for ( int i = 0; i < Keyframes.Length - 1; i++ )
			{
				// Bare minimum this value range should contain the min/max of the values themselves
				float min = Math.Min( Keyframes[i].Value, Keyframes[i + 1].Value );
				float max = Math.Max( Keyframes[i].Value, Keyframes[i + 1].Value );

				// If it's a cubic curve, sample along the curve and find any other larger extents
				if ( Keyframes[i].Interpolation == Interpolation.Cubic )
				{
					const int steps = 10;
					for ( float t = 0.0f; t <= 1.0f; t += (1.0f / steps) )
					{
						float value = GetInterpolatedValue( Keyframes[i], Keyframes[i + 1], Keyframes[i].Time + (Keyframes[i + 1].Time - Keyframes[i].Time) * t );
						min = Math.Min( min, value );
						max = Math.Max( max, value );
					}
				}

				ranges.Add( (min, max) );
			}

			KeyframeValueRanges = ranges.ToImmutableArray();
			ValueRange = (ranges.Min( r => r.Min ), ranges.Max( r => r.Max ));
		}
	}

	/// <summary>
	/// Default constructor contains one single default keyframe
	/// </summary>
	public AltCurve() : this( ImmutableArray.Create( new Keyframe() ), Extrapolation.Constant, Extrapolation.Constant )
	{
	}

	public AltCurve( AltCurve copy, IEnumerable<Keyframe> replacementKeyframes ) :
		this( replacementKeyframes, copy.PreInfinity, copy.PostInfinity )
	{
	}

	/// <summary>
	/// Create a copy of this curve, replacing the keyframes with a copy of the given input keyframe enumerable
	/// </summary>
	public readonly AltCurve WithKeyframes( IEnumerable<Keyframe> keyframes ) => new( this, keyframes );

	// The push towards more clearly defined immutability with record structs is nice.
	// But what's the point when you get shit like ImmutableArrays implementing IEquality but only equating the array REFERENCE and not the array VALUES..?
	// https://github.com/dotnet/runtime/issues/77183 Unfortunately it sounds like this will never be a sensible default
	// So for now just provide our own equality that will be used instead (even though it's not referenced, 
	// https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/record-structs#equality-members)
	public readonly bool Equals( AltCurve other )
	{
		if ( PreInfinity != other.PreInfinity || PostInfinity != other.PostInfinity )
			return false;

		// Skip SequenceEqual if the keyframe arrays have not been initialized (ie this is default(AltCurve))
		if ( Keyframes.IsDefault || other.Keyframes.IsDefault )
			return Keyframes.IsDefault == other.Keyframes.IsDefault;
		else
			return Keyframes.SequenceEqual( other.Keyframes );
	}

	public override int GetHashCode() => HashCode.Combine( Keyframes, PreInfinity, PostInfinity );

	/// <summary>
	/// Evaluate the curve value for a given input time.
	/// Time complexity: O(log n)
	/// </summary>
	[MethodImpl( MethodImplOptions.AggressiveInlining )] // Notable gains from profiling
	public readonly float Evaluate( float time )
	{
		// Evaluations on an empty curve will always evaluate as 0
		if ( Keyframes.IsDefault || Keyframes.Length == 0 )
			return 0.0f;

		if ( Keyframes.Length == 1 )
			return Keyframes[0].Value; // Only one keyframe, return its value

		// Normalize the time & calculate offset for any times that fall outside our keyframe range
		var normalizedTime = time;
		var accumulatedOffset = 0.0f;

		if ( time < TimeRange.Min )
			(normalizedTime, accumulatedOffset) = HandlePreInfinity( time );
		else if ( time > TimeRange.Max )
			(normalizedTime, accumulatedOffset) = HandlePostInfinity( time );

		// Binary search for the given time (or the next index)
		int baseIndex = Keyframes.BinarySearch( new() { Time = normalizedTime }, new KeyframeTimeComparer() );
		if ( baseIndex >= 0 )
			return Keyframes[baseIndex].Value + accumulatedOffset; // Exact match found

		// No exact match, so the bitwise compliment of the index is the next nearest keyframe.
		baseIndex = ~baseIndex;
		if ( baseIndex == 0 )
			return Keyframes[0].Value + accumulatedOffset; // Time is less than the first keyframe's time
		if ( baseIndex >= Keyframes.Length )
			return Keyframes[^1].Value + accumulatedOffset; // Time is greater than the last keyframe's time

		return GetInterpolatedValue( Keyframes[baseIndex - 1], Keyframes[baseIndex], normalizedTime ) + accumulatedOffset;
	}

	/// <summary>
	/// Handle pre-infinity extrapolation and return adjusted time and vertical offset.
	/// </summary>
	private readonly (float adjustedTime, float accumulatedOffset) HandlePreInfinity( float time )
	{
		var firstKey = Keyframes[0];

		switch ( PreInfinity )
		{
			case Extrapolation.Constant:
				return (firstKey.Time, 0.0f);

			case Extrapolation.Linear:
				var secondKey = Keyframes[1];
				float linearSlope = (secondKey.Value - firstKey.Value) / (secondKey.Time - firstKey.Time);
				return (firstKey.Time, linearSlope * (time - firstKey.Time));

			case Extrapolation.Cycle:
				float cycleOffset = TimeRange.Min - time;
				return (TimeRange.Max - (cycleOffset % TimeSpan), 0.0f);

			case Extrapolation.CycleOffset:
				var cycleOffsetOffset = TimeRange.Min - time;
				float cycleVerticalOffset = -(float)((Math.Floor( cycleOffsetOffset / TimeSpan ) + 1.0) * (Keyframes[^1].Value - Keyframes[0].Value));
				return (TimeRange.Max - (cycleOffsetOffset % TimeSpan), cycleVerticalOffset);

			case Extrapolation.Oscillate:
				float oscillateOffset = TimeRange.Min - time;
				int oscillateCycles = (int)Math.Floor( oscillateOffset / TimeSpan );
				float oscillateAdjustedTime = TimeRange.Min + (oscillateOffset % TimeSpan);
				if ( oscillateCycles % 2 == 1 ) oscillateAdjustedTime = TimeRange.Max - (oscillateAdjustedTime - TimeRange.Min);
				return (oscillateAdjustedTime, 0.0f);

			default:
				throw new NotImplementedException( "PreInfinity not implemented" );
		}
	}

	/// <summary>
	/// Handle post-infinity extrapolation and return adjusted time and vertical offset.
	/// </summary>
	private readonly (float adjustedTime, float accumulatedOffset) HandlePostInfinity( float time )
	{
		var lastKey = Keyframes[^1];

		switch ( PostInfinity )
		{
			case Extrapolation.Constant:
				return (lastKey.Time, 0.0f);

			case Extrapolation.Linear:
				var secondLastKey = Keyframes[^2];
				float linearSlope = (lastKey.Value - secondLastKey.Value) / (lastKey.Time - secondLastKey.Time);
				return (lastKey.Time, linearSlope * (time - lastKey.Time));

			case Extrapolation.Cycle:
				float cycleOffset = time - TimeRange.Min;
				return (TimeRange.Min + (cycleOffset % TimeSpan), 0.0f);

			case Extrapolation.CycleOffset:
				float cycleOffsetOffset = time - TimeRange.Min;
				float cycleVerticalOffset = (float)(Math.Floor( cycleOffsetOffset / TimeSpan ) * (Keyframes[^1].Value - Keyframes[0].Value));
				return (TimeRange.Min + (cycleOffsetOffset % TimeSpan), cycleVerticalOffset);

			case Extrapolation.Oscillate:
				float oscillateOffset = time - TimeRange.Min;
				int oscillateCycles = (int)Math.Floor( oscillateOffset / TimeSpan );
				float oscillateAdjustedTime = TimeRange.Min + (oscillateOffset % TimeSpan);
				if ( oscillateCycles % 2 == 1 ) oscillateAdjustedTime = TimeRange.Max - (oscillateAdjustedTime - TimeRange.Min);
				return (oscillateAdjustedTime, 0.0f);

			default:
				throw new NotImplementedException( "PostInfinity not implemented" );
		}
	}

	/// <summary>
	/// Interpolates the value between two keyframes at a given time.
	/// </summary>
	private static float GetInterpolatedValue( Keyframe keyframeA, Keyframe keyframeB, float time )
	{
		switch ( keyframeA.Interpolation )
		{
			case Interpolation.Constant:
				return keyframeA.Value;

			case Interpolation.Linear:
				{
					// The intermediate math operations are done with doubles to help preserve accuracy
					var interpTime = (time - keyframeA.Time) / (keyframeB.Time - keyframeA.Time);
					return keyframeA.Value + (keyframeB.Value - keyframeA.Value) * interpTime;
				}

			case Interpolation.Cubic:
				{
					// Build the cubic Bezier curve where the first/last points are the keyframe values, and the middle points are the tangent offset positions
					var interpTime = (time - keyframeA.Time) / (keyframeB.Time - keyframeA.Time);
					var tangentFactor = 0.4f;
					return Bezier1D( interpTime,
						keyframeA.Value,
						keyframeA.Value + (keyframeA.TangentOut * (keyframeB.Time - keyframeA.Time) * tangentFactor),
						keyframeB.Value - (keyframeB.TangentIn * (keyframeB.Time - keyframeA.Time) * tangentFactor),
						keyframeB.Value );
				}

			default:
				throw new NotImplementedException( "Interpolation type not implemented" );
		}
	}

	/// <summary>
	/// Return a version of this AltCurve but with a sanitized set of keyframes, in particular:
	/// - No keys may share an identical time
	/// - All keys must be in ascending time order
	/// </summary>
	public readonly AltCurve Sanitize( bool silent = false )
	{
		var cleanKeyframes = SanitizedKeyframes;

#if DEBUG
		if ( !silent )
		{
			// Warnings if we're actually making any modifications
			if ( cleanKeyframes.Count() != Keyframes.Length )
			{
				Log.Warning( "Removed invalid AltCurve keyframes, multiple keyframes can not share a time. Either move the invalid keyframe, or it will be removed automatically." );
			}
			else
			{
				for ( int i = 1; i < cleanKeyframes.Count(); i++ )
				{
					if ( cleanKeyframes.ElementAt( i ).Time != Keyframes[i].Time )
					{
						Log.Warning( "Invalid AltCurve keyframe ordering detected, times were automatically reordered." );
						break;
					}
				}
			}
		}
#endif

		return new( this, cleanKeyframes );
	}

	/// <summary>
	/// Return an enumerable of the sanitized keys of this AltCurve, in particular:
	/// - No keys may share an identical time
	/// - All keys must be in ascending time order
	/// </summary>
	public readonly IEnumerable<Keyframe> SanitizedKeyframes => Keyframes.DistinctBy( x => x.Time ).OrderBy( x => x.Time );

	/// <summary>
	/// De Casteljau's algorithm bezier curve (6 lerps)
	/// </summary>
	[MethodImpl( MethodImplOptions.AggressiveInlining )]
	private static float Bezier1D( float t, float p0, float p1, float p2, float p3 )
	{
		var line1Frac = p0 + t * (p1 - p0);
		var line2Frac = p1 + t * (p2 - p1);
		var line3Frac = p2 + t * (p3 - p2);

		var subLine1Frac = line1Frac + t * (line2Frac - line1Frac);
		var subLine2Frac = line2Frac + t * (line3Frac - line2Frac);

		return subLine1Frac + t * (subLine2Frac - subLine1Frac);
	}
}
