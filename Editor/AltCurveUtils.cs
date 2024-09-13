using Editor;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AltCurves;

internal static class AltCurveUtils
{
	/// <summary>
	/// Get the distance between pX,pY and the line segment defined by x1,y1 and x2,y2.
	/// </summary>
	internal static float DistanceToLineSegment( float x1, float y1, float x2, float y2, float px, float py )
	{
		float A = px - x1;
		float B = x2 - x1;
		float C = py - y1;
		float D = y2 - y1;

		float dotProduct = A * B + C * D;
		float lenSq = B * B + D * D;
		float param = -1f;

		if ( lenSq != 0f )
		{
			param = dotProduct / lenSq;
		}

		float closestX, closestY;

		if ( param < 0f )
		{
			closestX = x1;
			closestY = y1;
		}
		else if ( param > 1f )
		{
			closestX = x2;
			closestY = y2;
		}
		else
		{
			closestX = x1 + param * B;
			closestY = y1 + param * D;
		}

		return MathF.Sqrt( MathF.Pow( closestX - px, 2 ) + MathF.Pow( closestY - py, 2 ) );
	}

	/// <summary>
	/// Draw the full curve (from the first to last keyframes) onto the given rect
	/// </summary>
	// Honestly I didn't profile this to see if it was worth it, but this caching pattern is used by the stock curve drawing widgets so we'll use it for now 
	static List<Vector2> pointCache = new();

	internal static void DrawFullCurve( this in AltCurve curve, in Rect rect, float spacing = 2.0f )
	{
		pointCache.Clear();

		var (timeMin, timeMax) = curve.TimeRange;
		var (valueMin, valueMax) = curve.ValueRange;

		var step = 1.0f / (rect.Width / spacing);
		for ( float f = 0; f < 1.0f + step * 0.1f; f += step )
		{
			var stepTime = timeMin + (timeMax - timeMin) * f;
			var stepValue = curve.Evaluate( stepTime ).Remap( valueMin, valueMax );

			pointCache.Add( new Vector2( rect.Left + f * rect.Width, rect.Top + rect.Height - stepValue * rect.Height ) );
		}

		Paint.DrawLine( pointCache );
		pointCache.Clear();
	}

	/// <summary>
	/// Draw a portion of a curve onto a widget, using the given widget/curve transform
	/// </summary>

	internal static void DrawPartialCurve( this AltCurve curve, CurveWidgetTransform transform, float spacing = 2.0f, float infinityAlpha = 0.4f )
	{
		pointCache.Clear();

		double widgetWidth = (double)transform.WidgetSize.x;
		double widgetHeight = (double)transform.WidgetSize.y;
		double step = spacing / widgetWidth;

		double curveRangeX = transform.CurveRange.MaxX - transform.CurveRange.MinX;
		double curveRangeY = transform.CurveRange.MaxY - transform.CurveRange.MinY;

		for ( double t = 0; t <= 1.0; t += step )
		{
			double time = transform.CurveRange.MinX + t * curveRangeX;

			double value = curve.Evaluate( (float)time );
			double normalizedValue = (value - transform.CurveRange.MinY) / curveRangeY;

			pointCache.Add( new Vector2(
				(float)(t * widgetWidth),
				(float)(widgetHeight - normalizedValue * widgetHeight)
			) );
		}

		var (minTime, maxTime) = curve.TimeRange;
		var minTimePixelX = transform.CurveToWidgetX( minTime );
		var maxTimePixelX = transform.CurveToWidgetX( maxTime );

		// Ok, we have a pointCache full of vector poisitons that form the line of our curve
		// It's possible that some segments of the line fall either before/after the first/last keyframe,
		// and if so we want to tint it with a different alpha.

		// lastIdxPreCurve is the index of the last point that falls before the first keyframe (or -1)
		// firstIdxPostCurve is the index of the first point that falls after the last keyframe (or -1)

		int lastIdxPreCurve = pointCache.FindLastIndex( point => point.x < minTimePixelX );
		int firstIdxPostCurve = pointCache.FindIndex( point => point.x > maxTimePixelX );

		// Pre-infinity segment
		var initialPen = Paint.Pen;

		if (lastIdxPreCurve > -1)
		{
			Paint.SetPen( initialPen.WithAlpha( infinityAlpha ), Paint.PenSize, Paint.PenStyle );
			Paint.DrawLine( pointCache.Take( lastIdxPreCurve + 1 ) );
		}

		Paint.SetPen( initialPen, Paint.PenSize, Paint.PenStyle );
		if ( firstIdxPostCurve > -1 && lastIdxPreCurve > -1 )
			Paint.DrawLine( pointCache.Skip( lastIdxPreCurve ).Take( firstIdxPostCurve - lastIdxPreCurve + 1 ) );
		else if ( firstIdxPostCurve > -1 )
			Paint.DrawLine( pointCache.Take( firstIdxPostCurve + 1 ) );
		else if ( lastIdxPreCurve > -1 )
			Paint.DrawLine( pointCache.Skip( lastIdxPreCurve ) );
		else
			Paint.DrawLine( pointCache );

		// Post-infinity segment
		if ( firstIdxPostCurve > -1 )
		{
			Paint.SetPen( initialPen.WithAlpha( infinityAlpha ), Paint.PenSize, Paint.PenStyle );
			Paint.DrawLine( pointCache.Skip( firstIdxPostCurve ) );
		}

		pointCache.Clear();
	}
}
