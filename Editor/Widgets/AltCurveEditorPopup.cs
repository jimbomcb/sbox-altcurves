using Editor;
using Sandbox;
using System;

namespace AltCurves.Widgets;

/// <summary>
/// The AltCurveEditorPopup window contains an AltCurveEditor instance, which then contains a toolbar, grid, and editable curve widget.
/// </summary>
public class AltCurveEditorPopup : Widget
{
	private readonly AltCurveEditor Editor;

	public AltCurveEditorPopup( Widget parent ) : base( parent )
	{
		WindowFlags = WindowFlags.Window;
		WindowTitle = "Alternative Curve Editor";
		DeleteOnClose = true;

		Size = new( 950, 500 );
		MinimumSize = new( 800, 400 );

		Editor = new( this )
		{
			Size = Size,
			MinimumSize = MinimumSize
		};

		Layout = Layout.Column();
		Layout.Margin = 4;
		Layout.Spacing = 4;
		Layout.Add( Editor, 1 );
	}

	public void SetCurve( SerializedProperty serializedProperty, Action onChanged )
	{
		WindowTitle = $"{serializedProperty.Name} - Alternative Curve Editor";
		Editor.SetCurve( () => serializedProperty.GetValue<AltCurve>(), v => { serializedProperty.SetValue( v ); onChanged?.Invoke(); } );
	}
}
