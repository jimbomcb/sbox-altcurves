using Editor;

namespace AltCurves.Widgets;

/// <summary>
/// LineEdit, but with an OnCurveMouseRelease override allowing selection in context menus.
/// I see that .AddLineEdit exists, but I don't want an inline title
/// </summary>
internal class ContextMenuLineEdit : LineEdit
{
	public ContextMenuLineEdit( Widget parent ) : base( parent )
	{
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		e.Accepted = true; // Allow clicking without closing the dropdown itself
		base.OnMouseReleased( e );
	}
}
