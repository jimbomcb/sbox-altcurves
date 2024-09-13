using AltCurves.GraphicsItems;
using Editor;
using System;
using System.Diagnostics;

namespace AltCurves.Widgets;

/// <summary>
/// This custom editor contains a scrolling grid background and an editable curve widget, this is contained within the AltCurveEditorPopup.
/// </summary>
public class AltCurveEditor : GraphicsView
{
	// The min/max x/y of the curve that we're currently zoomed to
	private CurveWidgetTransform _curveWidgetTransformInternal;
	private CurveWidgetTransform CurveWidgetTransform
	{
		get { return _curveWidgetTransformInternal; }
		set
		{
			_curveWidgetTransformInternal = value;
			_gridBackground.Range = CurveWidgetTransform.CurveRange;
			if ( _curveWidget != null ) _curveWidget.CurveTransform = CurveWidgetTransform;
			Update();
		}
	}

	private const int HEADER_HEIGHT = 24;
	private const int HEADER_SPACING = 2;

	private readonly ScrollingGrid _gridBackground;
	private EditableAltCurve _curveWidget = null;
	private bool _panning = false;
	private Vector2 _startPanPos;
	private Vector2 _lastPanPos;
	private Vector2 _lastMoveLocalPos;
	private AltCurveHoverInfo _hoverInfo = null;
	private UndoToast _lastUndoToast = null;
	private AltCurveEditorToolbar _toolbar;

	public AltCurveEditor( Widget parent ) : base( parent )
	{
		Name = "AltCurveEditor";
		SceneRect = new( 0, Size );
		HorizontalScrollbar = ScrollbarMode.Off;
		VerticalScrollbar = ScrollbarMode.Off;
		CenterOn( new( 100, 10 ) );

		_toolbar = new( this, HEADER_HEIGHT )
		{
			Position = Vector2.Zero
		};

		Add( _gridBackground = new ScrollingGrid
		{
			ZIndex = -1
		} );
	}

	protected override void DoLayout()
	{
		base.DoLayout();

		SceneRect = new( 0, 0, Width, Height );
		_gridBackground.Position = new( 0, HEADER_HEIGHT + HEADER_SPACING );
		_gridBackground.Size = new( Width, Height - HEADER_HEIGHT - HEADER_SPACING );

		if ( _curveWidget != null ) _curveWidget.Size = _gridBackground.Size;

		CurveWidgetTransform = CurveWidgetTransform with { WidgetSize = _gridBackground.Size };

		// Pass to any active toast so it repositions relative to the rect
		if ( _lastUndoToast != null && _lastUndoToast.IsValid )
			_lastUndoToast.OuterRect = LocalRect;
	}

	[EditorEvent.Frame]
	public void Frame()
	{
		if ( !Visible ) return;

		// Curve time/value hover popup 
		if ( _curveWidget != null && _curveWidget.ShowCurveHoverInfo )
		{
			if ( _hoverInfo == null )
			{
				Add( _hoverInfo = new( null )
				{
					Position = _lastMoveLocalPos,
				} );
				UpdateHoverInfoWindow();
			}
		}
		else if ( _hoverInfo != null )
		{
			_hoverInfo.Destroy();
			_hoverInfo = null;
		}

		// Remove expired toast (I don't see any obvious way to handle something like updating logic in a GraphicsItem...
		// I know that in QT land you can create QTTimers, but I can't find anything like that exposed?)
		if ( _lastUndoToast != null && _lastUndoToast.IsValid && _lastUndoToast.Expired )
		{
			_lastUndoToast.Destroy();
			_lastUndoToast = null;
		}
	}
	protected override void OnWheel( WheelEvent e )
	{
		if ( _curveWidget == null )
			return;

		e.Accept();

		float zoomFrac = e.Delta < 0 ? 1.1f : 0.9f;

		var zoomOriginWidgetSpace = e.Position - _curveWidget.Position;
		if ( e.HasShift )  // Vertical zoom
			CurveWidgetTransform = CurveWidgetTransform.WithZoomedRange( new Vector2( 1.0f, zoomFrac ), zoomOriginWidgetSpace );
		else if ( e.HasCtrl )  // Horizontal zoom
			CurveWidgetTransform = CurveWidgetTransform.WithZoomedRange( new( zoomFrac, 1.0f ), zoomOriginWidgetSpace );
		else
			CurveWidgetTransform = CurveWidgetTransform.WithZoomedRange( new Vector2( zoomFrac, zoomFrac ), zoomOriginWidgetSpace );
	}

	protected override void OnMousePress( MouseEvent e )
	{
		_lastMoveLocalPos = e.LocalPosition;

		if ( e.RightMouseButton )
		{
			_panning = true;
			_startPanPos = e.LocalPosition;
			_lastPanPos = e.LocalPosition;
			e.Accepted = true;
		}

		_curveWidget?.OnCurveMousePress( e );

		base.OnMousePress( e );

		// Update hover info window with position update
		if ( _hoverInfo != null )
		{
			UpdateHoverInfoWindow();
		}
	}

	protected override void OnMouseReleased( MouseEvent e )
	{
		_lastMoveLocalPos = e.LocalPosition;

		if ( e.RightMouseButton && _panning )
		{
			_panning = false;

			// Only consume it if we actually moved the mouse, otherwise the context menu will show
			if ( e.LocalPosition.Distance( _startPanPos ) > 5.0f)
				e.Accepted = true;
		}

		_curveWidget?.OnCurveMouseRelease( e );

		base.OnMouseReleased( e );
	}

	protected override void OnMouseMove( MouseEvent e )
	{
		_lastMoveLocalPos = e.LocalPosition;

		if ( _panning )
		{
			var dragDiff = e.LocalPosition - _lastPanPos;
			_lastPanPos = e.LocalPosition;
			CurveWidgetTransform = CurveWidgetTransform.WithTranslatedRange( dragDiff );
		}

		_curveWidget?.OnCurveMouseMove( e );

		base.OnMouseMove( e );

		// Hover window cursor following
		if ( _hoverInfo != null )
		{
			UpdateHoverInfoWindow();
		}
	}

	protected override void OnKeyPress( KeyEvent e )
	{
		if ( e.Key == KeyCode.F ) // Focus on selection or entire curve
		{
			e.Accepted = true;
			ZoomToFit();
		}
		else
		{
			_curveWidget?.OnCurveKeyPressed( e );
		}

		base.OnKeyPress( e );
	}

	protected override void OnKeyRelease( KeyEvent e )
	{
		_curveWidget?.OnCurveKeyReleased( e );
		base.OnKeyRelease( e );
	}

	protected override void OnFocus( FocusChangeReason reason )
	{
		base.OnFocus( reason );

		// Pass focus gain on to the curve widget
		_curveWidget?.OnCurveFocus( reason );
	}

	internal void SetCurve( Func<AltCurve> get, Action<AltCurve> set )
	{
		if ( _curveWidget != null )
		{
			throw new Exception( "Widget suppied with second curve, not set up for this" );
		}

		Enabled = true;

		_curveWidget = new EditableAltCurve( get(), CurveWidgetTransform, _gridBackground )
		{
			Position = _gridBackground.Position,
			Size = _gridBackground.Size
		};
		_curveWidget.OnUndoRedo += OnCurveUndoRedo;
		Add( _curveWidget );

		// Feed changes to the resulting sanitized curve back to the data asset (or other open curve windows)
		_curveWidget.Bind( "SanitizedCurve" ).From( get, set );

		// Bind the toolbar controls onto the editable widget controls for things like snapping
		_toolbar.BindCurveControls( this, _curveWidget );

		Update();
		ZoomToFit();
	}

	private void UpdateHoverInfoWindow()
	{
		Debug.Assert( _hoverInfo != null, "Updating hover info window with no hover info active" );
		Debug.Assert( _curveWidget != null, "Updating hover info with no active curve" );

		// Stick to the bottom right of the mouse (horizontal + vertical flip the offset if we're close to the screen edge)
		const float hoverOffset = 25.0f;
		var widgetClampedMousePos = _lastMoveLocalPos.Clamp( Vector2.Zero, new( Width, Height ) ); // Ensure the widget remains visible during off-screen drags
		var distX = Width - (widgetClampedMousePos.x + _hoverInfo.Width + hoverOffset);
		var distY = Height - (widgetClampedMousePos.y + _hoverInfo.Height + hoverOffset);
		var offset = new Vector2( distX < 5.0f ? -hoverOffset - _hoverInfo.Width : hoverOffset, distY < 5.0f ? -hoverOffset - _hoverInfo.Height : hoverOffset );
		_hoverInfo.Position = widgetClampedMousePos + offset;

		// Times/Values
		// If we're hovering a keyframe, show the time of the keyframe, not the cursor exactly
		(_hoverInfo.Time, _hoverInfo.Value, _hoverInfo.InvalidKeyframe) = _curveWidget.GetHoverInfo( _lastMoveLocalPos - _curveWidget.Position );
	}

	/// <summary>
	/// Focus on the current selection or the entire curve if nothing is selected
	/// </summary>
	public void ZoomToFit()
	{
		var newTransform = _curveWidget?.GetCoordinateRangeForSelection();
		CurveWidgetTransform = CurveWidgetTransform with { CurveRange = newTransform ?? new() };
	}

	/// <summary>
	/// Child curve has undone/redone an operation, pop up a toast window
	/// </summary>
	private void OnCurveUndoRedo( bool isRedo, string actionDone )
	{
		if ( _lastUndoToast != null && _lastUndoToast.IsValid )
		{
			_lastUndoToast.Destroy();
			_lastUndoToast = null;
		}

		var text = $"{(isRedo ? "Redo" : "Undo")}: {actionDone}";
		_lastUndoToast = new UndoToast( LocalRect, text );
		Add( _lastUndoToast );
	}
}
