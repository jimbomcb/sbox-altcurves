using Editor;
using System;
using System.Collections.Generic;

namespace AltCurves.GraphicsItems;

/// <summary>
/// A 2D grid that can display any arbitrary coordinate range.
/// +X is right, +Y is up on the rendered grid (opposite Y inversion from normal widget space)
/// </summary>
public class ScrollingGrid : GraphicsItem
{
	/// <summary>
	/// The coordinate range this grid should display
	/// </summary>
	public CoordinateRange2D Range
	{
		get => _rangeInternal;
		set
		{
			_rangeInternal = value;
			Update();
		}
	}

	public int MajorXLines { get; set; } = 8;
	public int MajorYLines { get; set; } = 8;
	public int MinorXLines { get; set; } = 4;
	public int MinorYLines { get; set; } = 4;

	/// <summary>
	/// The (string, pixel position) tuple of current major gridlines on the X axis
	/// </summary>
	public List<(string, float)> MajorLinesX { get; private set; } = new();

	/// <summary>
	/// The pixel position list of current major gridlines on the X axis
	/// </summary>
	public List<float> MinorLinesX { get; private set; } = new();

	/// <summary>
	/// The (string, pixel position) tuple list of current major gridlines on the Y axis
	/// </summary>
	public List<(string, float)> MajorLinesY { get; private set; } = new();

	/// <summary>
	/// The pixel position list of current major gridlines on the Y axis
	/// </summary>
	public List<float> MinorLinesY { get; private set; } = new();

	public double GridBaseX { get; private set; } = 0.0;
	public double GridBaseY { get; private set; } = 0.0;
	public double GridStepX { get; private set; } = 1.0;
	public double GridStepY { get; private set; } = 1.0;

	private CoordinateRange2D _rangeInternal; // Don't modify directly

	public ScrollingGrid()
	{
		Clip = true;
		_rangeInternal = new()
		{
			MinX = -1.0f,
			MaxX = 1.0f,
			MinY = -1.0f,
			MaxY = 1.0f
		};
	}

	protected override void OnPaint()
	{
		base.OnPaint();

#if CURVE_DEBUG 
		Paint.DrawText( new( 30.0f, 30.0f ), $"Bounds:{Range}" );
#endif

		(MajorLinesX, MinorLinesX, GridBaseX, GridStepX) = CalculateGridlines( Range.MinX, Range.MaxX, Width, MajorXLines, MinorXLines );
		(MajorLinesY, MinorLinesY, GridBaseY, GridStepY) = CalculateGridlines( Range.MinY, Range.MaxY, Height, MajorYLines, MinorYLines, invert: true );

		// JMCB TODO: Benchmark, potential optimization around line batching

		// Draw major lines
		Paint.SetPen( Color.White.WithAlpha( 0.3f ), 1.0f, PenStyle.Solid );
		foreach ( var line in MajorLinesX )
		{
			Paint.DrawLine( new( line.Item2, 0.0f ), new( line.Item2, Height ) );
		}
		foreach ( var line in MajorLinesY )
		{
			Paint.DrawLine( new( 0.0f, line.Item2 ), new( Width, line.Item2 ) );
		}

		// Minor lines
		Paint.SetPen( Color.White.WithAlpha( 0.2f ), 0.25f, PenStyle.Dot );
		foreach ( var line in MinorLinesX )
		{
			Paint.DrawLine( new( line, 0.0f ), new( line, Height ) );
		}
		foreach ( var line in MinorLinesY )
		{
			Paint.DrawLine( new( 0.0f, line ), new( Width, line ) );
		}

		// Text
		Paint.SetPen( Color.White.WithAlpha( 1.0f ), style: PenStyle.Solid );
		Paint.SetDefaultFont( size: 10 );
		foreach ( var line in MajorLinesX )
		{
			var textSize = Paint.MeasureText( line.Item1 );
			Paint.DrawText( new( line.Item2 - textSize.x / 2.0f, 0.0f ), line.Item1 );
		}
		foreach ( var line in MajorLinesY )
		{
			var textSize = Paint.MeasureText( line.Item1 );
			Paint.DrawText( new( 0.0f, line.Item2 - textSize.y / 2.0f ), line.Item1 );
		}
	}

	/// <summary>
	/// Given the min/max coordinates on an axis and a given widget pixel size, output the pixel offset
	/// for a series of major and minor gridlines, preferring to round to nice numbers.
	/// </summary>
	private static (List<(string, float)> majorLines, List<float> minorLines, double stepPos, double stepSize)
		CalculateGridlines( double rangeMin, double rangeMax, float widgetDimension, int majorSteps = 8, int minorSteps = 4, bool invert = false )
	{
		var gridLinesMajor = new List<(string, float)>();
		var gridLinesMinor = new List<float>();

		var curveWidth = rangeMax - rangeMin;
		var stepSize = curveWidth / majorSteps;

		// Find the nearest order of magnitude below the step size, then round the step size to a nice number
		var magnitude = Math.Pow( 10, Math.Floor( Math.Log10( stepSize ) ) );
		var scaledStep = Math.Round( stepSize / magnitude );
		if ( scaledStep < 2f )
			scaledStep = 1f;
		else if ( scaledStep < 5f )
			scaledStep = 2f;
		else
			scaledStep = 5f;

		var finalStep = scaledStep * magnitude;
		var start = Math.Floor( rangeMin / finalStep ) * finalStep;
		var end = Math.Ceiling( rangeMax / finalStep ) * finalStep;

		for ( var pos = start; pos <= end; pos += finalStep )
		{
			var majorWidgetSpace = (pos - rangeMin) / (rangeMax - rangeMin) * widgetDimension;
			if ( invert ) majorWidgetSpace = widgetDimension - majorWidgetSpace;

			gridLinesMajor.Add( (pos.ToString( "0.#####" ), (float)majorWidgetSpace) );

			for ( var minorLine = 1; minorLine < minorSteps; minorLine++ )
			{
				var minorPosCurve = pos + minorLine * finalStep / minorSteps;
				var minorPosWidget = (minorPosCurve - rangeMin) / (rangeMax - rangeMin) * widgetDimension;
				if ( invert ) minorPosWidget = widgetDimension - minorPosWidget;

				gridLinesMinor.Add( (float)minorPosWidget );
			}
		}

		return (gridLinesMajor, gridLinesMinor, start, finalStep / minorSteps);
	}
}
