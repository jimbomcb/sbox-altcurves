using Editor;

namespace AltCurves;

internal class CurveTextures
{
	// Curve interpolations:
	public Pixmap CurveLinearPixmap { get; init; }
	public Pixmap CurveConstantPixmap { get; init; }
	public Pixmap CurveCubicMirrorPixmap { get; init; }
	public Pixmap CurveCubicBrokenPixmap { get; init; }
	public Pixmap CurveCubicAutoPixmap { get; init; }

	// Curve extrapolations:
	public Pixmap ExtrapLinearPixmap { get; init; }
	public Pixmap ExtrapConstantPixmap { get; init; }
	public Pixmap ExtrapCyclePixmap { get; init; }
	public Pixmap ExtrapCycleOffsetPixmap { get; init; }
	public Pixmap ExtrapOscillatePixmap { get; init; }

	internal CurveTextures()
	{
		CurveLinearPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( CurveLinearPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();
			Paint.SetBrush( Color.Black );
			Paint.SetPen( Color.White, 3.0f );
			Paint.DrawLine( new( 2, 30 ), new( 30, 2 ) );

			Paint.DrawCircle( new( 2, 30 ), 4.0f );
			Paint.DrawCircle( new( 30, 2 ), 4.0f );
		}

		CurveConstantPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( CurveConstantPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();
			Paint.SetBrush( Color.Black );
			Paint.SetPen( Color.White, 3.0f );
			Paint.DrawLine( new( 2, 30 ), new( 30, 30 ) );
			Paint.DrawLine( new( 30, 2 ), new( 30, 30 ) );

			Paint.DrawCircle( new( 2, 30 ), 4.0f );
			Paint.DrawCircle( new( 30, 2 ), 4.0f );
			Paint.DrawCircle( new( 30, 30 ), 4.0f );
		}

		CurveCubicMirrorPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( CurveCubicMirrorPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( Color.Transparent );

			Paint.SetPen( Color.White, 3.0f );
			Paint.DrawCircle( new( 16.0f, 45.0f ), 50.0f );

			Paint.SetPen( Color.White.Darken( 0.3f ), 2.0f, PenStyle.Dash );
			Paint.DrawLine( new( 0, 15 ), new( 32, 15 ) );

			Paint.SetBrushAndPen( Color.White );
			Paint.DrawCircle( new( 16, 16 ), 8.0f );
			Paint.DrawCircle( new( 2, 15 ), 5.0f );
			Paint.DrawCircle( new( 30, 15 ), 5.0f );
		}

		CurveCubicBrokenPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( CurveCubicBrokenPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( Color.Transparent );

			Paint.SetPen( Color.White, 3.0f );

			Paint.DrawCircle( new( 16.0f, -45.0f ), 80.0f );

			Paint.SetPen( Color.White.Darken( 0.3f ), 2.0f, PenStyle.Dash );
			Paint.DrawLine( new( 6, 4 ), new( 17, 19 ) );
			Paint.DrawLine( new( 17, 19 ), new( 30, 10 ) );

			Paint.SetBrushAndPen( Color.White, Color.White, penSize: 2.0f );
			Paint.DrawCircle( new( 6, 4 ), 3.0f );
			Paint.DrawCircle( new( 30, 10 ), 3.0f );
			Paint.DrawCircle( new( 17, 21 ), 5.0f );

			Paint.DrawLine( new( 0, 10 ), new( 17, 21 ) );
			Paint.DrawLine( new( 17, 21 ), new( 32, 19 ) );
		}

		ExtrapLinearPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( ExtrapLinearPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();
			Paint.SetBrush( Color.Black );

			Paint.SetPen( Color.White, 3.0f );
			Paint.DrawLine( new( 0, 32 ), new( 16, 16 ) );
			Paint.DrawCircle( new( 2, 30 ), 4.0f );
			Paint.DrawCircle( new( 16, 16 ), 4.0f );

			Paint.SetPen( Color.White.Darken( 0.3f ), 2.0f, PenStyle.Solid );
			Paint.DrawLine( new( 16, 16 ), new( 32, 0 ) );
		}

		ExtrapConstantPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( ExtrapConstantPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();
			Paint.SetBrush( Color.Black );

			Paint.SetPen( Color.White, 3.0f );
			Paint.DrawLine( new( 0, 16 ), new( 32, 16 ) );

		}

		ExtrapCyclePixmap = new( 32, 32 );
		using ( Paint.ToPixmap( ExtrapCyclePixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( Color.Transparent );

			Paint.SetPen( Color.White, 2.0f );
			PaintCurves.DrawCubicBezier( new( 2, 30 ), new( 6, 30 ), new( 12, 2 ), new( 16, 2 ) );

			Paint.SetPen( Color.White.Darken( 0.5f ), 2.0f );
			Paint.DrawLine( new( 16, 0 ), new( 16, 32 ) );
			PaintCurves.DrawCubicBezier( new( 16, 30 ), new( 20, 30 ), new( 28, 2 ), new( 30, 2 ) );
		}

		ExtrapCycleOffsetPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( ExtrapCycleOffsetPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( Color.Transparent );

			Paint.SetPen( Color.White, 2.0f );
			PaintCurves.DrawCubicBezier( new( 0, 30 ), new( 12, 32 ), new( 8, 16 ), new( 16, 16 ) );

			Paint.SetPen( Color.White.Darken( 0.5f ), 2.0f );
			PaintCurves.DrawCubicBezier( new( 16, 16 ), new( 28, 16 ), new( 24, 0 ), new( 32, 0 ) );
		}

		ExtrapOscillatePixmap = new( 32, 32 );
		using ( Paint.ToPixmap( ExtrapOscillatePixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( Color.Transparent );

			Paint.SetPen( Color.White, 2.0f );
			Paint.DrawLine( new( 0, 0 ), new( 8, 32 ) );
			Paint.DrawLine( new( 8, 32 ), new( 16, 8 ) );

			Paint.SetPen( Color.White.Darken( 0.5f ), 2.0f );
			Paint.DrawLine( new( 16, 8 ), new( 24, 32 ) );
			Paint.DrawLine( new( 24, 32 ), new( 32, 0 ) );
		}
		
		// JMCB TODO: This kinda sucks, how do I best represent automatic tangents?
		CurveCubicAutoPixmap = new( 32, 32 );
		using ( Paint.ToPixmap( CurveCubicAutoPixmap ) )
		{
			Paint.Antialiasing = true;
			Paint.ClearPen();

			Paint.SetBrush( Color.Transparent );

			Paint.SetPen( Color.White, 2.0f );


			PaintCurves.DrawCubicBezier( new( 0, 30 ), new( 12, 32 ), new( 8, 16 ), new( 16, 16 ) );
			PaintCurves.DrawCubicBezier( new( 16, 16 ), new( 28, 16 ), new( 24, 0 ), new( 32, 0 ) );
		}
	}

	private static CurveTextures _instance = null;
	internal static CurveTextures Instance
	{
		get
		{
			if ( _instance != null ) return _instance;
			return _instance = new CurveTextures();
		}
	}
}
