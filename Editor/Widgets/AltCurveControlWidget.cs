using Editor;
using Sandbox;

namespace AltCurves.Widgets;

/// <summary>
/// The ControlWidget of the AltCurve handles rendering the struct in property panels.
/// </summary>
[CustomEditor( typeof( AltCurve ) )]
public class AltCurveControlWidget : ControlWidget
{
	private Color HighlightColor = Theme.Green;

	public AltCurveControlWidget( SerializedProperty property ) : base( property )
	{
		Cursor = CursorShape.Finger;
	}

	protected override void PaintOver()
	{
		var value = SerializedProperty.GetValue<AltCurve>();

		var col = HighlightColor.WithAlpha( Paint.HasMouseOver ? 1 : 0.75f );
		var inner = LocalRect.Shrink( 2.0f );

		Paint.SetPen( col.WithAlphaMultiplied( 0.1f ), 20 );
		value.DrawFullCurve( inner, 20.0f );

		Paint.SetPen( col.WithAlphaMultiplied( 0.1f ), 10 );
		value.DrawFullCurve( inner, 10.0f );

		Paint.SetPen( col.WithAlphaMultiplied( 0.1f ), 2 );
		value.DrawFullCurve( inner, 6.0f );

		Paint.SetPen( col, 1 );
		value.DrawFullCurve( inner, 0.5f );

		Paint.SetBrushAndPen( Color.Transparent, Theme.ControlBackground, 2 );
		Paint.DrawRect( LocalRect.Shrink( 1 ), 3 );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		base.OnMousePress( e );

		if ( e.LeftMouseButton )
		{
			var editor = new AltCurveEditorPopup( this )
			{
				Visible = true,
				WindowTitle = $"Alternative Curve Editor"
			};
			editor.Position = e.ScreenPosition - new Vector2( editor.Size.x, 0.0f );
			editor.SetCurve( SerializedProperty, Update );
			editor.ConstrainToScreen();
		}
	}
}
