using Editor;
using System.Collections.Generic;

namespace AltCurves;

public static class PaintCurves
{
	/// <summary>
	/// Paint.DrawLine along a cubic bezier curve
	/// </summary>
	public static void DrawCubicBezier( Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int steps = 15 )
	{
		var points = new List<Vector2>( steps );

		for ( int i = 0; i < steps; i++ )
		{
			var t = i / (float)(steps - 1);
			points.Add( CalcCubicBezier2D( t, p0, p1, p2, p3 ) );
		}

		Paint.DrawPoints( points );
	}

	private static Vector2 CalcCubicBezier2D( float t, Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3 )
	{
		var p01 = Vector2.Lerp( p0, p1, t );
		var p12 = Vector2.Lerp( p1, p2, t );
		var p23 = Vector2.Lerp( p2, p3, t );
		var p012 = Vector2.Lerp( p01, p12, t );
		var p123 = Vector2.Lerp( p12, p23, t );
		return Vector2.Lerp( p012, p123, t );
	}
}
