namespace AltCurves;

/// <summary>
/// CurveWidgetTransform helps in transforming between curve and widget coordinate systems.
/// It's an important distinction that in "Curve Space" +Y axis is up (larger curve values are above lower curve values), but in "Widget Space" +Y axis is down.
/// CurveRange acts as a sliding window into the curve data, and WidgetSize is the size of the widget we're transforming between.
/// </summary>
public readonly record struct CurveWidgetTransform
{
	/// <summary>
	/// The section of the curve we're transforming between
	/// X axis being time bounds, Y axis being value bounds
	/// </summary>
	public CoordinateRange2D CurveRange { get; init; }

	/// <summary>
	/// The pixel size of the widget we're transforming between
	/// </summary>
	public Vector2 WidgetSize { get; init; }

	/// <summary>
	/// The aspect ratio between pixels per value/pixels per second, for use in proportional widget/curve scaling
	/// </summary>
	public float WidgetCurveAspectRatio => PixelsPerValue / PixelsPerSecond;

	/// <summary>
	/// How many pixels represent one unit of time in the curve's range.
	/// </summary>
	public float PixelsPerSecond => (float)(WidgetSize.x / (CurveRange.MaxX - CurveRange.MinX));

	/// <summary>
	/// How many pixels represent one unit of value in the curve's range.
	/// </summary>
	public float PixelsPerValue => (float)(WidgetSize.y / (CurveRange.MaxY - CurveRange.MinY));

	/// <summary>
	/// Get a CurveWidgetTransform with an updated range translating us by a given input widget-space pixel amount
	/// </summary>
	public readonly CurveWidgetTransform WithTranslatedRange( Vector2 widgetSpaceTranslation )
	{
		var adjustedX = new Vector2( (float)CurveRange.MinX, (float)CurveRange.MaxX ) - widgetSpaceTranslation.x / (double)WidgetSize.x * (CurveRange.MaxX - CurveRange.MinX);
		var adjustedY = new Vector2( (float)CurveRange.MinY, (float)CurveRange.MaxY ) + widgetSpaceTranslation.y / (double)WidgetSize.y * (CurveRange.MaxY - CurveRange.MinY);
		return this with { CurveRange = new( adjustedX.x, adjustedX.y, adjustedY.x, adjustedY.y ) };
	}

	/// <summary>
	/// Get a CurveWidgetTransform zooming the range by zoomAmount based on zoomOrigin (in widget space)
	/// </summary>
	public readonly CurveWidgetTransform WithZoomedRange( Vector2 zoomAmount, Vector2 zoomOriginWidgetSpace )
	{
		var zoomOriginCurveSpace = WidgetToCurvePosition( zoomOriginWidgetSpace );
		return this with
		{
			CurveRange = new()
			{
				MinX = (zoomOriginCurveSpace.x + (CurveRange.MinX - zoomOriginCurveSpace.x) * (double)zoomAmount.x),
				MaxX = (zoomOriginCurveSpace.x + (CurveRange.MaxX - zoomOriginCurveSpace.x) * (double)zoomAmount.x),
				MinY = (zoomOriginCurveSpace.y + (CurveRange.MinY - zoomOriginCurveSpace.y) * (double)zoomAmount.y),
				MaxY = (zoomOriginCurveSpace.y + (CurveRange.MaxY - zoomOriginCurveSpace.y) * (double)zoomAmount.y)
			}
		};
	}

	/// <summary>
	/// Transform a 2d widget position into curve space.
	/// </summary>
	public readonly Vector2 WidgetToCurvePosition( Vector2 position ) => new( WidgetToCurveX( position.x ), WidgetToCurveY( position.y ) );

	/// <summary>
	/// Transform a 2d curve position into widget space.
	/// </summary>
	public readonly Vector2 CurveToWidgetPosition( Vector2 localCurveVert ) => new( CurveToWidgetX( localCurveVert.x ), CurveToWidgetY( localCurveVert.y ) );

	/// <summary>
	/// Transform curve time into widget space.
	/// </summary>
	public readonly float CurveToWidgetX( float x ) => (float)((x - CurveRange.MinX) / (CurveRange.MaxX - CurveRange.MinX) * WidgetSize.x);

	/// <summary>
	/// Transform curve value into widget space.
	/// </summary>
	public readonly float CurveToWidgetY( float y ) => (float)(WidgetSize.y - (y - CurveRange.MinY) / (CurveRange.MaxY - CurveRange.MinY) * WidgetSize.y);

	/// <summary>
	/// Transform widget X position into curve space.
	/// </summary>
	public readonly float WidgetToCurveX( float screenX ) => (float)(CurveRange.MinX + screenX / WidgetSize.x * (CurveRange.MaxX - CurveRange.MinX));

	/// <summary>
	/// Transform widget Y position into curve space.
	/// </summary>
	public readonly float WidgetToCurveY( float screenY ) => (float)(CurveRange.MaxY - screenY / WidgetSize.y * (CurveRange.MaxY - CurveRange.MinY));
}
