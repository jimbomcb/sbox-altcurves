using System;

namespace AltCurves;

/// <summary>
/// A range of coordinates in 2D space
/// The ScrollingGrid uses a CoordinateRange to define the min/max X/Y values it should display,
/// and the CurveWidgetTransform uses a CoordinateRange to define the min/max X/Y values of the curve it's transforming between.
/// </summary>
public readonly record struct CoordinateRange2D
{
	public double MinX { get; init; }
	public double MaxX { get; init; }
	public double MinY { get; init; }
	public double MaxY { get; init; }

	public CoordinateRange2D( double minX, double maxX, double minY, double maxY )
	{
		MinX = minX;
		MaxX = maxX;
		MinY = minY;
		MaxY = maxY;
	}

	/// <summary>
	/// Expand the given coordinate range by a fraction of the x/y range
	/// </summary>
	public readonly CoordinateRange2D PadRange(float fraction)
	{	
		var timeRange = Math.Max( 1.0, MaxX - MinX);
		var valueRange = Math.Max( 1.0, MaxY - MinY );

		return new()
		{
			MinX = MinX - (timeRange * fraction),
			MaxX = MaxX + (timeRange * fraction),
			MinY = MinY - (valueRange * fraction),
			MaxY = MaxY + (valueRange * fraction)
		};
	}
}
