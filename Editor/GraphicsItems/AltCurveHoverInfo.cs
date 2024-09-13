using Editor;

namespace AltCurves.GraphicsItems;

/// <summary>
/// Small box that displays when hovering the curve, showing the time/value at that point
/// </summary>
public class AltCurveHoverInfo : GraphicsItem
{
	public float Time { get; set; } = 0.0f;
	public float Value { get; set; } = 0.0f;

	/// <summary>
	/// If true display a warning that this keyframe will be lost if not moved/fixed
	/// </summary>
	public bool InvalidKeyframe { get; set; } = false;

	private readonly float _rowSize;
	private const string INVALID_KEYFRAME = "Invalid keyframe, duplicate time";
	private bool _warningVisible = false;

	public AltCurveHoverInfo( EditableAltCurve parent ) : base( parent )
	{
		Clip = true;
		Size = new Vector2( 90.0f, 35.0f ); // We'll automatically increase our width if this isn't enough
		_rowSize = Height * 0.5f; // Default sizing is for 2 rows, time/value
		ZIndex = 10;
	}

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( Color.White, Theme.WidgetBackground, penSize: 5 );
		Paint.SetDefaultFont( size: 10.0f );

		Paint.DrawRect( new( 0.0f, -4.0f, Width, Height + 8.0f ), 5.0f );

		// Expand height for additional row if showing a warning (and update width)
		if ( InvalidKeyframe != _warningVisible )
		{
			_warningVisible = InvalidKeyframe;

			Height = _rowSize * (InvalidKeyframe ? 3.0f : 2.0f);

			if ( InvalidKeyframe )
			{
				var requiredSize = Paint.MeasureText( INVALID_KEYFRAME );
				if ( requiredSize.x + 10.0f > Width )
					Width = requiredSize.x + 10.0f;
			}
			else
			{
				Width = 90.0f; // Shrink width back to normal size
			}
		}

		var timeString = $"Time:  {Time:0.0##}";
		var valueString = $"Value: {Value:0.0##}";

		// Increase size if our new values put us outside the width
		var maxStringSize = Paint.MeasureText( valueString ).ComponentMax( Paint.MeasureText( timeString ) );
		if ( maxStringSize.x + 10.0f > Width )
			Width = maxStringSize.x + 10.0f;

		Paint.DrawText( new( 5.0f, 0.0f, Width, _rowSize ), timeString, Sandbox.TextFlag.LeftCenter );
		Paint.DrawText( new( 5.0f, _rowSize, Width, _rowSize ), valueString, Sandbox.TextFlag.LeftCenter );

		if ( InvalidKeyframe )
		{
			Paint.SetBrush( Theme.Red );
			Paint.SetPen( Theme.Red );
			Paint.DrawText( new ( 5.0f, _rowSize * 2.0f, Width, _rowSize ), INVALID_KEYFRAME, Sandbox.TextFlag.LeftCenter );
		}
	}
}
