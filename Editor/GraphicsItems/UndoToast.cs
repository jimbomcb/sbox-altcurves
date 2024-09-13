using Editor;
using Sandbox;

namespace AltCurves.GraphicsItems;

/// <summary>
/// Little fellow who tells us about undo/redo operations
/// </summary>
public class UndoToast : GraphicsItem
{
	public float Expiry { get; init; }
	public bool Expired => RealTime.Now > Expiry;
	public Rect OuterRect
	{
		set
		{
			_outerRect = value;
			_sized = false;
		}
	}

	private string _text;
	private bool _sized = false;
	private Rect _outerRect;

	public UndoToast( Rect outerRect, string text, float duration = 3.0f )
	{
		_outerRect = outerRect;
		_text = text;
		Expiry = RealTime.Now + duration;

		Clip = true;
		Size = new Vector2( 200.0f, 40.0f );
		Position = outerRect.BottomRight - Size - new Vector2( 10.0f );
	}

	protected override void OnPaint()
	{
		Paint.SetBrushAndPen( Theme.WidgetBackground, Theme.White );
		Paint.SetFont( "Poppins", 12, 550 );
		Paint.DrawRect( new( Vector2.Zero, Size ), 5.0f );

		if ( !_sized )
		{
			var textSize = Paint.MeasureText( _text );
			Width = textSize.x + 10.0f;
			Height = textSize.y + 10.0f;
			Position = _outerRect.BottomRight - Size - new Vector2( 10.0f );
			_sized = true;
		}

		Paint.DrawText( new( 5.0f, 5.0f ), _text );
	}
}
