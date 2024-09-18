using Editor;
using Sandbox;
using Sandbox.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static AltCurves.AltCurve;

namespace AltCurves.GraphicsItems;

/// <summary>
/// Contained inside the AltCurveEditor, the EditableAltCurve handles rendering and manipulation of the AltCurve structure.
/// </summary>
public partial class EditableAltCurve : GraphicsItem
{
	public record struct SelectionStateData( int SelectedKeyframes, float SelectedTime, float SelectedValue );
	public record struct CurveHoverInfo( float HoveredTime, float HoveredValue, bool InvalidKeyframe );

	/// <summary>
	/// The raw curve keyframes that we are manipulating on our canvas. 
	/// Could potentially be in an invalid/unsanitized state with keys sharing times.
	/// </summary>
	private List<Keyframe> _rawCurveKeyframes;

	/// <summary>
	/// The sanitized processed version of _rawCurveKeyframes, feeds into SanitizedCurve and the resulting JSON
	/// </summary>
	private List<Keyframe> _sanitizedKeyframes;

	/// <summary>
	/// Current curve pre-infinity extrapolation (for both raw/sanitized)
	/// </summary>
	private Extrapolation _extrapolationPreInfinity;

	/// <summary>
	/// Current curve post-infinity extrapolation (for both raw/sanitized)
	/// </summary>
	private Extrapolation _extrapolationPostInfinity;

	/// <summary>
	/// The "Sanitized" curve, which is the raw curve after being sanitized to remove duplicate times, fix ordering etc.
	/// Bound to the underlying SerializedProperty that we're representing, writes to this represent external
	/// curve modification (JSON reload or external editing)
	/// </summary>
	public AltCurve SanitizedCurve
	{
		get => _sanitizedCurve;
		// The newly incoming sanitized curve from the binding will stomp any local changes.
		set => SetRawCurve( value.Keyframes, value.PreInfinity, value.PostInfinity );
	}
	private AltCurve _sanitizedCurve;

	/// <summary>
	/// Widget space/curve space transform
	/// </summary>
	public CurveWidgetTransform CurveTransform
	{
		set
		{
			_curveTransform = value;
			foreach ( var keyframe in _keyWidgets ) keyframe.SetTransform( value );
			Update();
		}
	}
	private CurveWidgetTransform _curveTransform;

	/// <summary>
	/// User-defined persistent viewing preferences
	/// </summary>
	public EditorViewConfig ViewConfig
	{
		get => _viewConfig;
		set
		{
			var initialConfig = _viewConfig;
			_viewConfig = value;
			ViewConfigUpdated( initialConfig );
		}
	}
	private EditorViewConfig _viewConfig = new();

	/// <summary>
	/// Curve render color
	/// </summary>
	public Color CurveColor { get; set; } = Theme.Green;

	/// <summary>
	/// Thickness when not hovered
	/// </summary>
	public float CurveThickness { get; set; } = 2.0f;

	/// <summary>
	/// Thickness when hovered
	/// </summary>
	public float CurveThicknessHovered { get; set; } = 3.0f;

	/// <summary>
	/// Is the cursor hovering on the curve specifically? Determined from a hit-test against the specific evaluated curve.
	/// </summary>
	public bool HoveringCurve { get; private set; } = false;

	/// <summary>
	/// Is the cursor hovering on a keyframe specifically? Determined from a hit-test against the specific evaluated curve.
	/// </summary>
	public bool HoveringKeyframe { get; private set; } = false;

	/// <summary>
	///
	/// </summary>
	public bool HoveringTangent { get; private set; } = false;

	/// <summary>
	/// Is the cursor hovering on the curve or a keyframe? Determined from a hit-test against the specific evaluated curve.
	/// </summary>
	public bool HoveringCurveOrKeyframe { get; private set; } = false;

	/// <summary>
	/// True if we actually want the hover tooltip to display, can be suppressed by user preferences
	/// </summary>
	public bool ShowCurveHoverInfo => HoveringCurveOrKeyframe && !HoveringTangent && ViewConfig.ShowCurveTooltip;

	/// <summary>
	/// Time the hover event started
	/// </summary>
	public float CurveStartHoverTime { get; private set; } = -1.0f;

	/// <summary>
	/// If true ignore any attempts at snapping to the nearest gridline, forcefulyl disabled (alt held)
	/// </summary>
	public bool ForceDisableSnap { get; set; } = false;

	/// <summary>
	/// Overall time snapping toggle (bound to toolbar)
	/// </summary>
	public bool SnapTimeEnabled { get; set; } = true;

	/// <summary>
	/// Time snapping method (bound to toolbar)
	/// </summary>
	public TimeSnapOptions SnapTimeMode { get; set; }

	/// <summary>
	/// Time snapping amount (if custom)
	/// </summary>
	public float SnapTimeCustom { get; set; }

	/// <summary>
	/// Overall Value snapping toggle (bound to toolbar)
	/// </summary>
	public bool SnapValueEnabled { get; set; } = true;

	/// <summary>
	/// Value snapping method (bound to toolbar)
	/// </summary>
	public ValueSnapOptions SnapValueMode { get; set; }

	/// <summary>
	/// Value snapping amount (if custom)
	/// </summary>
	public float SnapValueCustom { get; set; }

	/// <summary>
	/// Time to display for the current selected keyframe
	/// </summary>
	public SelectionStateData SelectionState { get; set; }

	/// <summary>
	/// Tracks direction during drag operations to allow shift-snap to cardinal directions
	/// </summary>
	private DragLatch _dragDirectionSnap = new();

	/// <summary>
	/// Triggered each time undo/redo is performed, bool is false for undo/true for redo, string is the operation undone/redone.
	/// </summary>
	public Action<bool, string> OnUndoRedo { get; set; }

	/// <summary>
	/// If not null, all selected keyframes are of this interpolation type/tangent mode
	/// </summary>
	public InterpTangentMode? SelectedInterpolation { get; private set; }

	/// <summary>
	/// True if we have at least 1 selected keyframe
	/// </summary>
	public bool HasSelectedKeyframe => _selectedIndicies.Any();

	private record struct HistoryEntry( string Operation, ImmutableArray<Keyframe> Keyframes, Extrapolation PreInfinity, Extrapolation PostInfinity );
	private readonly Stack<HistoryEntry> _curveHistory = new(); // In lieu of any sensible undo/redo system in S&box, let's just roll our own. I miss the UE transaction system.
	private readonly Stack<HistoryEntry> _curveRedoHistory = new(); // State of the curve gets pushed on undo, popped when redoing

	private readonly List<KeyPair> _keyWidgets = new(); // The keyframe list is created from the associated curve keyframes
	private Vector2 _lastMousePos;
	private bool _draggingSelect = false;
	private Vector2 _dragStartPos;
	private KeyPair? _hoveredKeyframe = null;
	private int _draggingKeyframeIndex = -1;
	private readonly ScrollingGrid _gridBackground;
	private HashSet<int> _selectedIndicies = new(); // Selected raw keyframe indicies
	private Dictionary<int, int> _rawToSanitizedIdMap = new(); // Map indicies between the raw and sanitized keyframes (if they were not removed)
	private Dictionary<int, int> _sanitizedToRawIdMap = new(); // Map indicies between the sanitized keyframe and the raw id (each sanitized keyframe always has a raw keyframe)

	private IEnumerable<Keyframe> SelectedKeyframes => _selectedIndicies.Select( x => _rawCurveKeyframes[x] );

	public EditableAltCurve( AltCurve curve, CurveWidgetTransform curveTransform, ScrollingGrid gridBackground ) : base( null )
	{
		_curveTransform = curveTransform;
		_gridBackground = gridBackground;

		HoverEvents = true;
		Clip = true;
		ClipChildren = true;

		SetRawCurve( curve.Keyframes, curve.PreInfinity, curve.PostInfinity );
	}

	/// <summary>
	/// A entirely brand new curve has been provided (probably from external editing)
	/// </summary>
	private void SetRawCurve( ImmutableArray<Keyframe> keyframes, Extrapolation preInfinity, Extrapolation postInfinity )
	{
		// Clear selection state for any new curves coming in given rebuilding
		_selectedIndicies.Clear();
		SelectionState = new();
		SelectedInterpolation = null;

		_rawCurveKeyframes = keyframes.IsDefaultOrEmpty ? new() { new Keyframe() } : keyframes.ToList();
		_extrapolationPreInfinity = preInfinity;
		_extrapolationPostInfinity = postInfinity;

		// Full rebuild of keyframes for our new curve data
		RebuildKeyframeWidgets();

		RebuildSanitizedCurve();

		Update();
	}

	// Note: important that this is quick, it's called often (ie for each selected item during drag)
	private void SetRawKeyframe( int index, in Keyframe keyframe )
	{
		if ( index < 0 || index >= _rawCurveKeyframes.Count )
			throw new ArgumentOutOfRangeException( nameof( index ), "Keyframe index out of range" );

		_rawCurveKeyframes[index] = keyframe;
		RebuildSanitizedCurve();

		// Update the widget handles
		_keyWidgets[index].VisibleHandle.Keyframe = keyframe;
		_keyWidgets[index].DragHandle.Keyframe = keyframe;
	}

	private void RebuildSanitizedCurve()
	{
		Assert.True( _keyWidgets.Count == _rawCurveKeyframes.Count, "_keyWidgets count mismatch" );

		_sanitizedKeyframes = SanitizeKeyframes( _rawCurveKeyframes ).ToList();

		// Update the automatic tangents with this new sanitized curve data, and feed it back to the raw keyframe data.
		// Generating automatic tangents against the raw potentially invalid curve data will result in incorrect tangents.

		// Build a map for translating between the raw and sanitized keyframe indicies 
		_rawToSanitizedIdMap = _sanitizedKeyframes
			.Select( ( kf, index ) => new { kf, index } )
			.ToDictionary( x => _rawCurveKeyframes.IndexOf( x.kf ), x => x.index );
		_sanitizedToRawIdMap = _rawToSanitizedIdMap.ToDictionary( x => x.Value, x => x.Key );

		// Perform the tangent updates
		UpdateCurveAutoTangents( ref _sanitizedKeyframes, out var updatedSanitizedIds );

		// Feed back the tangents
		foreach ( var updatedId in updatedSanitizedIds )
		{
			// This should always be safe, because the sanitized keyframes are a subset of the raw keyframes
			var rawId = _sanitizedToRawIdMap[updatedId];

			// Intentionally directly setting rather than piping through setters
			_rawCurveKeyframes[rawId] = _sanitizedKeyframes[updatedId];
			_keyWidgets[rawId].VisibleHandle.Keyframe = _sanitizedKeyframes[updatedId];
		}

		_sanitizedCurve = new( _sanitizedKeyframes, _extrapolationPreInfinity, _extrapolationPostInfinity );
		Update();
	}

	/// <summary>
	/// Recreate the curve keyframe widgets, triggered after modifications to the curve that potentially add/remove keyframes
	/// </summary>
	private void RebuildKeyframeWidgets()
	{
		_draggingKeyframeIndex = -1;

		foreach ( var keyframe in _keyWidgets )
		{
			keyframe.Destroy();
		}
		_keyWidgets.Clear();

		// Create the keyframe widgets
		var previousTimes = new HashSet<float>();
		for ( int i = 0; i < _rawCurveKeyframes.Count; i++ )
		{
			var dragWidget = new DragHandle( i, _curveTransform, _rawCurveKeyframes[i], this )
			{
				ZIndex = 2
			};
			dragWidget.OnMouseDown += OnHandleClicked;
			dragWidget.OnMouseUp += OnHandleMouseUp;
			dragWidget.OnDragging += OnHandleDrag;
			dragWidget.OnDragComplete += OnHandleDragComplete;

			var visibleWidget = new KeyVisible( i, _rawCurveKeyframes.Count, _curveTransform, _rawCurveKeyframes[i], ViewConfig.TangentMode, this )
			{
				ZIndex = 3,
				UserSelected = _selectedIndicies.Contains( i ),
				InvalidKeyframe = previousTimes.Contains( _rawCurveKeyframes[i].Time )
			};

			// Tangent controls
			visibleWidget.TangentIn.OnDragStart += () => { PushUndoState( "Dragging Tangent Handle" ); };
			visibleWidget.TangentIn.OnUpdated += ( value ) =>
			{
				switch ( visibleWidget.Keyframe.TangentMode )
				{
					case TangentMode.Mirrored:
						visibleWidget.Keyframe = visibleWidget.Keyframe with { TangentOut = value, TangentIn = value };
						break;
					case TangentMode.Split:
						visibleWidget.Keyframe = visibleWidget.Keyframe with { TangentIn = value };
						break;
					case TangentMode.Automatic: // User edits to an auto tangent will switch to mirrored mode
						visibleWidget.Keyframe = visibleWidget.Keyframe with { TangentOut = value, TangentIn = value, TangentMode = TangentMode.Mirrored };
						CalculateSelectedInterpolation(); // We changed handle interpolation, refresh the state
						break;
				}

				// Push the updated keyframe data into the raw curve
				SetRawKeyframe( visibleWidget.Index, visibleWidget.Keyframe );

			};

			visibleWidget.TangentOut.OnDragStart += () => { PushUndoState( "Dragging Tangent Handle" ); };
			visibleWidget.TangentOut.OnUpdated += ( value ) =>
			{
				switch ( visibleWidget.Keyframe.TangentMode )
				{
					case TangentMode.Mirrored:
						visibleWidget.Keyframe = visibleWidget.Keyframe with { TangentOut = value, TangentIn = value };
						break;
					case TangentMode.Split:
						visibleWidget.Keyframe = visibleWidget.Keyframe with { TangentOut = value };
						break;
					case TangentMode.Automatic: // User edits to an auto tangent will switch to mirrored mode
						visibleWidget.Keyframe = visibleWidget.Keyframe with { TangentOut = value, TangentIn = value, TangentMode = TangentMode.Mirrored };
						CalculateSelectedInterpolation(); // We changed handle interpolation, refresh the state
						break;
				}

				// Push the updated keyframe data into the raw curve
				SetRawKeyframe( visibleWidget.Index, visibleWidget.Keyframe );
			};

			_keyWidgets.Add( new()
			{
				DragHandle = dragWidget,
				VisibleHandle = visibleWidget
			} );

			previousTimes.Add( _rawCurveKeyframes[i].Time );
		}

		BuildSelectionState(); // Update the state of selected keyframes after rebuild
		CalculateSelectedInterpolation(); // Check for any changes to the interpolation settings of our selection
		Update();
	}

	/// <summary>
	/// Triggered each time the user-configurable view settings changes, this can alter things like tangent visiblity, info popup state etc.
	/// </summary>
	private void ViewConfigUpdated( in EditorViewConfig lastViewConfig )
	{
		// If we change tangent visibility mode, pass to keyframes
		if ( lastViewConfig.TangentMode != ViewConfig.TangentMode )
		{
			foreach ( var pair in _keyWidgets )
			{
				pair.VisibleHandle.TangentMode = ViewConfig.TangentMode;
			}
		}
	}

	/// <summary>
	/// Passed from the curve editor when focus is gained
	/// </summary>
	public void OnCurveFocus( FocusChangeReason _ )
	{
		// Forcefully release the alt key, there's an annoying bug with the alt snap override.
		// OnKeyPressed gets called for the alt key, but OnKeyReleased does not after alt tabbing.
		// This will forcefully reset the state when tabbing back in.
		ForceDisableSnap = false;
	}

	public void OnCurveKeyPressed( KeyEvent e )
	{
		// Shift while in a drag operation will start monitoring drag direction, and then snap movement along that axis
		switch ( e.Key )
		{
			case KeyCode.Shift when _draggingKeyframeIndex >= 0:
				_dragDirectionSnap.Start();
				break;
			case KeyCode.Alt:
				e.Accepted = true;
				ForceDisableSnap = true;
				break;
			case KeyCode.Z when e.HasCtrl:
				e.Accepted = true;
				Undo();
				break;
			case KeyCode.Y when e.HasCtrl:
				e.Accepted = true;
				Redo();
				break;
			case KeyCode.Delete:
				e.Accepted = true;
				DeleteSelection();
				break;
			case KeyCode.A when e.HasCtrl:
				e.Accepted = true;
				SelectAll();
				break;
			case KeyCode.Z when !e.HasCtrl:
				e.Accepted = true;
				SnapTimeEnabled = !SnapTimeEnabled;
				Update();
				break;
			case KeyCode.X when !e.HasCtrl:
				e.Accepted = true;
				SnapValueEnabled = !SnapValueEnabled;
				Update();
				break;
			case KeyCode.Num1:
				e.Accepted = true;
				SetSelectionInterpolation( new( Interpolation.Cubic, TangentMode.Automatic ) );
				break;
			case KeyCode.Num2:
				e.Accepted = true;
				SetSelectionInterpolation( new( Interpolation.Cubic, TangentMode.Mirrored ) );
				break;
			case KeyCode.Num3:
				e.Accepted = true;
				SetSelectionInterpolation( new( Interpolation.Cubic, TangentMode.Split ) );
				break;
			case KeyCode.Num4:
				e.Accepted = true;
				SetSelectionInterpolation( new( Interpolation.Linear ) );
				break;
			case KeyCode.Num5:
				e.Accepted = true;
				SetSelectionInterpolation( new( Interpolation.Constant ) );
				break;
			case KeyCode.Num6:
				e.Accepted = true;
				SetSelectionTangentFlat();
				break;
		}
	}

	public void OnCurveKeyReleased( KeyEvent e )
	{
		switch ( e.Key )
		{
			case KeyCode.Shift:
				// Shift while in a drag operation will start monitoring drag direction, and then snap movement along that axis
				_dragDirectionSnap.Reset();
				break;
			case KeyCode.Alt:
				// Force disable snapping when holding alt
				e.Accepted = true;
				ForceDisableSnap = false;
				break;
		}
	}

	public void OnCurveMouseMove( MouseEvent e )
	{
		var prevMousePos = _lastMousePos;
		_lastMousePos = e.LocalPosition - Position;
		var positionDelta = _lastMousePos - prevMousePos;

		if ( _dragDirectionSnap.State == DragLatch.LatchState.Accumulating )
			_dragDirectionSnap.Accumulate( positionDelta );

		// Highlight the curve if we're not dragging (dragging keyframes will override this)
		if ( !_draggingSelect )
			PerformCurveHitDetection( e.LocalPosition );

		// Push updates during mouse movement while dragging to correctly paint selection box
		if ( _draggingSelect )
			Update();

		// Update on mouse movement when we're showing the hover window and we're hovering the curve itself.
		if ( HoveringCurve && !HoveringKeyframe && !HoveringTangent && ShowCurveHoverInfo )
			Update();
	}

	internal void OnCurveMousePress( MouseEvent e )
	{
		// Perform a single hit detection update when pressing down,
		// we want the latest hover state and we might have been hovering
		// a context menu instead of just constantly updating hover state
		PerformCurveHitDetection( e.LocalPosition );

		_lastMousePos = e.LocalPosition - Position;

		if ( e.LeftMouseButton )
		{
			var canDrag = _hoveredKeyframe == null && !HoveringTangent;
			if ( !_draggingSelect && canDrag )
			{
				_draggingSelect = true;
				_dragStartPos = e.LocalPosition - Position;
			}
		}
		else if ( e.MiddleMouseButton )
		{
			if ( HoveringCurveOrKeyframe )
			{
				CreateHoveredKeyframe( e.LocalPosition - Position );
			}
		}

		Update();
	}

	internal void OnCurveMouseRelease( MouseEvent e )
	{
		var localMousePos = e.LocalPosition - Position;

		if ( e.LeftMouseButton && !e.Accepted )
		{
			if ( _draggingSelect )
			{
				_draggingSelect = false;
				BoxSelect( _dragStartPos, localMousePos, append: e.HasShift, toggle: e.HasCtrl, subtract: e.HasAlt );
			}
		}
		else if ( e.RightMouseButton && !e.Accepted )
		{
			// Right-clicking on an unselected keyframe directly will also switch selection to it (or add it), similar to OnHandleClicked
			if ( _hoveredKeyframe.HasValue )
			{
				var clickedIndex = _hoveredKeyframe.Value.Index;
				if ( !_selectedIndicies.Contains( clickedIndex ) )
				{
					var clickedKeyframe = new HashSet<int>() { clickedIndex };
					UpdateSelection( ref clickedKeyframe, append: e.HasShift, toggle: e.HasCtrl, subtract: e.HasAlt );
				}
			}

			OpenContextMenu();
			e.Accepted = true;
		}

		Update();
	}

	private void OnHandleClicked( int clickedIndex, bool shift, bool ctrl, bool alt )
	{
		// If the user is clicking and dragging an unselected keyframe, we want to update the selection to include it.
		if ( !_selectedIndicies.Contains( clickedIndex ) )
		{
			var clickedKeyframe = new HashSet<int>() { clickedIndex };
			UpdateSelection( ref clickedKeyframe, append: shift, toggle: ctrl, subtract: alt );
		}
	}

	private void OnHandleMouseUp( int clickedIndex, bool shift, bool ctrl, bool alt )
	{
		_draggingKeyframeIndex = -1;
		_dragDirectionSnap.Reset();

		Update();
	}

	/// <summary>
	/// Handle the keyframe drag events, snapping the associated curve keyframe when desired
	/// </summary>
	protected void OnHandleDrag( int dragIndex )
	{
		if ( _draggingKeyframeIndex == -1 )
		{
			// First drag event in a sequence, push an undo state
			PushUndoState( $"Moved {_selectedIndicies.Count} keyframe{(_selectedIndicies.Count > 1 ? "s" : "")}" );
		}

		_draggingKeyframeIndex = dragIndex;

		var draggingPair = _keyWidgets[dragIndex];

		var unsnappedTime = draggingPair.DragHandle.Keyframe.Time;
		var unsnappedValue = draggingPair.DragHandle.Keyframe.Value;
		var timeChange = unsnappedTime - draggingPair.DragHandle.StartDragKeyframe.Time;
		var valueChange = unsnappedValue - draggingPair.DragHandle.StartDragKeyframe.Value;

		// If we're latched to a direction, factor out the movement in the opposite direction
		if ( _dragDirectionSnap.State == DragLatch.LatchState.Latched )
		{
			if ( _dragDirectionSnap.LatchDirection == Vector2.Right )
			{
				valueChange = 0.0f;
				unsnappedValue = draggingPair.DragHandle.StartDragKeyframe.Value;
			}
			else
			{
				timeChange = 0.0f;
				unsnappedTime = draggingPair.DragHandle.StartDragKeyframe.Time;
			}
		}

		// Snap the main dragged widget
		var (snappedTime, snappedValue) = SnapTimeValueToGrid( unsnappedTime, unsnappedValue );
		SetRawKeyframe( dragIndex, _rawCurveKeyframes[dragIndex] with { Time = snappedTime, Value = snappedValue } );

		// And drag every other selected widget by the same amount that the dragged keyframe is moving (then snapped to grid)
		foreach ( var idx in _selectedIndicies )
		{
			if ( idx == dragIndex )
				continue;

			// Noteworthy: During this drag we are only updating the visible widget of selected-but-not-dragged keyframes.
			// When the drag completes we will run a pass updating all drag handles to the correct widget positions
			var selectedWidgets = _keyWidgets[idx];
			var selectedBaseKeyframe = selectedWidgets.DragHandle.Keyframe;
			var (snappedOtherTime, snappedOtherValue) = SnapTimeValueToGrid( selectedBaseKeyframe.Time + timeChange, selectedBaseKeyframe.Value + valueChange );

			SetRawKeyframe( idx, _rawCurveKeyframes[idx] with { Time = snappedOtherTime, Value = snappedOtherValue } );
		}

		// Update the validity state for each relevant handle, flagging as invalid if they're going to be culled (because they share a time).
		// The first instance of each time is taken, so we know if we see a repeat time that the keyframe will be lost
		var seenTimes = new HashSet<float>();
		foreach ( var pair in _keyWidgets )
		{
			pair.VisibleHandle.InvalidKeyframe = seenTimes.Contains( pair.VisibleHandle.Keyframe.Time );
			seenTimes.Add( pair.VisibleHandle.Keyframe.Time );
		}

		BuildSelectionState();

		Update();
	}

	/// <summary>
	/// Scan through a list of keyframes that represent a curve
	/// Modify the tangents of auto-interpolating keyframes based on the surrounding keyframe slope.
	/// </summary>
	private static void UpdateCurveAutoTangents( ref List<Keyframe> alteredKeyframes, out List<int> alteredKeyframeIds )
	{
		alteredKeyframeIds = new();

		for ( int i = 0; i < alteredKeyframes.Count; i++ )
		{
			var keyframe = alteredKeyframes[i];
			if ( keyframe.Interpolation == Interpolation.Cubic && keyframe.TangentMode == TangentMode.Automatic )
			{
				// First-last keyframes always flatten the curve for a seamless cycle with itself
				if ( i == 0 || i == alteredKeyframes.Count - 1 )
				{
					alteredKeyframes[i] = keyframe with { TangentIn = 0.0f, TangentOut = 0.0f };
					alteredKeyframeIds.Add( i );
					continue;
				}

				// Calculate the slope between the previous and next keyframes
				var prevKeyframe = alteredKeyframes[i - 1];
				var nextKeyframe = alteredKeyframes[i + 1];
				var slope = (nextKeyframe.Value - prevKeyframe.Value) / (nextKeyframe.Time - prevKeyframe.Time);

				alteredKeyframes[i] = keyframe with { TangentIn = slope, TangentOut = slope };
				alteredKeyframeIds.Add( i );
			}
		}
	}

	/// <summary>
	/// Called when the mouse is released after drag completion
	/// </summary>
	private void OnHandleDragComplete( int dragIndex )
	{
		// Snap all the other drag handles to their correct positions
		_keyWidgets[dragIndex].DragHandle.Keyframe = _keyWidgets[dragIndex].VisibleHandle.Keyframe;
		foreach ( var idx in _selectedIndicies )
		{
			if ( idx == dragIndex )
				continue;

			_keyWidgets[idx].DragHandle.Keyframe = _keyWidgets[idx].VisibleHandle.Keyframe;
		}

		// Update selection state with the drag position
		BuildSelectionState();

		Update();

		_draggingKeyframeIndex = -1;
	}

	protected override void OnPaint()
	{
		float alpha = HoveringCurveOrKeyframe ? 1.0f : 0.8f;
		float thickness = HoveringCurveOrKeyframe ? CurveThicknessHovered : CurveThickness;

		Paint.SetPen( CurveColor.WithAlpha( alpha ), thickness, PenStyle.Solid );
		_sanitizedCurve.DrawPartialCurve( _curveTransform, 1.0f );

		// Paint a little point on the curve near our closest hover position
		if ( HoveringCurve && !HoveringKeyframe && !HoveringTangent && ShowCurveHoverInfo )
		{
			var (hoverTime, hoverValue, _) = GetHoverInfo( _lastMousePos );
			var hoverPos = _curveTransform.CurveToWidgetPosition( new( hoverTime, hoverValue ) );

			Paint.SetPen( Theme.White );
			Paint.DrawCircle( hoverPos, 4.0f );
		}

		// Drag box
		if ( _draggingSelect )
		{
			var dragMins = _dragStartPos.ComponentMin( _lastMousePos );
			var dragMaxs = _dragStartPos.ComponentMax( _lastMousePos );

			Paint.SetBrushAndPen( Color.FromRgba( 0xADD8E644 ), Color.FromRgb( 0X72BCD4 ), style: PenStyle.Dash );
			Paint.DrawRect( Rect.FromPoints( dragMins, dragMaxs ), 4.0f );
		}

		//Paint.DrawText( new Vector2( 100.0f, 100.0f ), $"DragStart{_dragStartPos}, LastMouse{_lastMousePos}" );
		//Paint.DrawText( new Vector2( 100.0f, 100.0f ), $"Selected IDS: {string.Join(',', _selectedIndicies)}" );
		//Paint.DrawText( new Vector2( 100.0f, 100.0f ), $"Selected Type: {_selectedInterpolation}" );
	}

	/// <summary>
	/// Box selection logic based on pixel start/end positions
	/// </summary>
	private void BoxSelect( Vector2 dragStartPos, Vector2 dragEndPos, bool append, bool toggle, bool subtract )
	{
		// Find the keyframes that are within the drag box
		var searchMin = dragStartPos.ComponentMin( dragEndPos );
		var searchMax = dragStartPos.ComponentMax( dragEndPos );

		var curveTimeRangeMin = _curveTransform.WidgetToCurveX( searchMin.x );
		var curveTimeRangeMax = _curveTransform.WidgetToCurveX( searchMax.x );
		var curveValueRangeMax = _curveTransform.WidgetToCurveY( searchMin.y ); // Y curve/widget space inversion
		var curveValueRangeMin = _curveTransform.WidgetToCurveY( searchMax.y );

		var selectedIndicies = new HashSet<int>();
		for ( int i = 0; i < _rawCurveKeyframes.Count; i++ )
		{
			var keyframe = _rawCurveKeyframes[i];
			if ( keyframe.Time >= curveTimeRangeMin && keyframe.Time <= curveTimeRangeMax &&
				 keyframe.Value >= curveValueRangeMin && keyframe.Value <= curveValueRangeMax )
			{
				selectedIndicies.Add( i );
			}
		}

		UpdateSelection( ref selectedIndicies, append, toggle, subtract );
	}

	/// <summary>
	/// Update the selected keyframe set with the given selection. Support shift/ctrl/alt modifiers for adding/toggling/subtracting.
	/// </summary>
	private void UpdateSelection( ref HashSet<int> newSelection, bool append, bool toggle, bool subtract )
	{
		if ( toggle )
		{
			foreach ( var idx in newSelection )
			{
				if ( _selectedIndicies.Contains( idx ) )
				{
					_selectedIndicies.Remove( idx );
				}
				else
				{
					_selectedIndicies.Add( idx );
				}
			}
		}
		else if ( subtract )
		{
			foreach ( var idx in newSelection )
			{
				_selectedIndicies.Remove( idx );
			}
		}
		else if ( append )
		{
			foreach ( var idx in newSelection )
			{
				_selectedIndicies.Add( idx );
			}
		}
		else
		{
			_selectedIndicies = newSelection;
		}

		// Update visible handles with selection state
		foreach ( var keyframe in _keyWidgets )
		{
			keyframe.VisibleHandle.UserSelected = _selectedIndicies.Contains( keyframe.Index );
		}

		CalculateSelectedInterpolation();
		BuildSelectionState();
		Update();
	}

	/// <summary>
	/// Update our selection state, triggered when the selection set changes or selected keyframes are dragged.
	/// Events bubble through to the toolbar.
	/// </summary>
	private void BuildSelectionState()
	{
		SelectionState = new()
		{
			SelectedKeyframes = _selectedIndicies.Count,
			SelectedTime = _selectedIndicies.Count == 1 ? _rawCurveKeyframes[_selectedIndicies.First()].Time : 0.0f,
			SelectedValue = _selectedIndicies.Count == 1 ? _rawCurveKeyframes[_selectedIndicies.First()].Value : 0.0f
		};
	}

	private void PerformCurveHitDetection( Vector2 mousePos )
	{
		const float pixelHitRange = 8.0f;
		var widgetRelativeMouse = mousePos - Position; // Mouse relative to the curve widget

		// Keyframe hover detection, simple distance check to the widget-space keyframe positions
		HoveringKeyframe = false;
		foreach ( var keyPair in _keyWidgets )
		{
			var keyframeWidgetPos = _curveTransform.CurveToWidgetPosition( new( keyPair.DragHandle.Keyframe.Time, keyPair.DragHandle.Keyframe.Value ) );
			if ( keyframeWidgetPos.Distance( widgetRelativeMouse ) < pixelHitRange )
			{
				_hoveredKeyframe = keyPair;
				HoveringKeyframe = true;
				break;
			}
		}

		// If we're still not hovering a direct keyframe, do a second pass against the handles
		HoveringTangent = false;
		if ( !HoveringKeyframe )
		{
			foreach ( var keyPair in _keyWidgets )
			{
				if ( keyPair.VisibleHandle?.TangentIn.Hovered ?? false )
				{
					HoveringTangent = true;
					break;
				}
				if ( keyPair.VisibleHandle?.TangentOut.Hovered ?? false )
				{
					HoveringTangent = true;
					break;
				}
			}
		}

		if ( !HoveringKeyframe )
		{
			_hoveredKeyframe = null;
		}

		// Curve hit-test logic:
		// Get the value of the curve pixelHitRange pixels to the left and right of the mouse position
		// Convert these values to widget-space coordinates
		// The line distance between the mouse and the line segment from pixelMin/pixelMax is compared against pixelHitRange.
		// Hit test against the sanitized rendered curve, not the potentially invalid raw curve.
		var minValue = _sanitizedCurve.Evaluate( _curveTransform.WidgetToCurveX( widgetRelativeMouse.x - pixelHitRange ) );
		var maxValue = _sanitizedCurve.Evaluate( _curveTransform.WidgetToCurveX( widgetRelativeMouse.x + pixelHitRange ) );

		var pixelMin = _curveTransform.CurveToWidgetY( minValue );
		var pixelMax = _curveTransform.CurveToWidgetY( maxValue );

		var lineDist = AltCurveUtils.DistanceToLineSegment(
			widgetRelativeMouse.x - pixelHitRange, pixelMin,
			widgetRelativeMouse.x + pixelHitRange, pixelMax,
			widgetRelativeMouse.x, mousePos.y - Position.y
		);
		HoveringCurve = lineDist < pixelHitRange;

		var wasHovering = HoveringCurveOrKeyframe;
		HoveringCurveOrKeyframe = HoveringCurve || HoveringKeyframe || _draggingKeyframeIndex >= 0 || HoveringTangent; // Additionally consider dragging so it's illuminated, and hovering on a tangent
		if ( wasHovering != HoveringCurveOrKeyframe )
		{
			Update();

			if ( HoveringCurveOrKeyframe )
				CurveStartHoverTime = Time.Now;
		}
	}

	/// <summary>
	/// Middle-mouse creation along a hovered curve
	/// </summary>
	private void CreateHoveredKeyframe( Vector2 cursorPos )
	{
		PushUndoState( "Create keyframe" );

		var newKeyframeTime = _curveTransform.WidgetToCurveX( cursorPos.x );

		// Find the source keyframe we want to copy
		var sourceKeyframe = new Keyframe();
		var (minTime, maxTime) = _sanitizedCurve.TimeRange;
		if ( newKeyframeTime >= minTime && newKeyframeTime <= maxTime )
		{
			// We're within the time range, copy the prior keyframe
			var priorKeyframes = _rawCurveKeyframes.Where( x => x.Time <= newKeyframeTime );
			sourceKeyframe = priorKeyframes.Any() ? priorKeyframes.MaxBy( x => x.Time ) : new Keyframe();
		}
		else if ( newKeyframeTime < minTime && _rawCurveKeyframes.Any() )
		{
			// We're before the curve, first keyframe
			sourceKeyframe = _rawCurveKeyframes.MinBy( x => x.Time );
		}
		else if ( newKeyframeTime > maxTime && _rawCurveKeyframes.Any() )
		{
			// We're after the curve, last keyframe
			sourceKeyframe = _rawCurveKeyframes.MaxBy( x => x.Time );
		}

		// Just stick it on to the end of the list and rebuild widgets
		_rawCurveKeyframes.Add( sourceKeyframe with { Time = newKeyframeTime, Value = _sanitizedCurve.Evaluate( newKeyframeTime ) } );
		RebuildKeyframeWidgets();
		RebuildSanitizedCurve();

		var newSelectSet = new HashSet<int>() { _rawCurveKeyframes.Count - 1 };
		UpdateSelection( ref newSelectSet, false, false, false );
	}

	/// <summary>
	/// Get the time and value of the cursor in curve space, overridden when doing things like dragging
	/// </summary>
	internal CurveHoverInfo GetHoverInfo( Vector2 cursorLocalPos )
	{
		// Show the snapped value when we're dragging keyframes
		if ( _draggingKeyframeIndex >= 0 )
		{
			var handle = _keyWidgets[_draggingKeyframeIndex].VisibleHandle;
			return new( handle.Keyframe.Time, handle.Keyframe.Value, handle.InvalidKeyframe );
		}

		// Show the nearby keyframe if not dragging and hovering near one
		if ( _hoveredKeyframe.HasValue )
		{
			var handle = _hoveredKeyframe.Value.VisibleHandle;
			return new( handle.Keyframe.Time, handle.Keyframe.Value, handle.InvalidKeyframe );
		}

		// Evaluate against the rendered sanitized curve, not the raw underlying curve.
		var time = _curveTransform.WidgetToCurveX( cursorLocalPos.x );
		return new( time, _sanitizedCurve.Evaluate( time ), false );
	}

	/// <summary>
	/// Magnetic time/value snapping
	/// </summary>
	private (float time, float value) SnapTimeValueToGrid( float time, float value )
	{
		if ( ForceDisableSnap )
			return (time, value);

		var snappedTime = time;
		var snappedValue = value;

		if ( SnapTimeEnabled )
		{
			switch ( SnapTimeMode )
			{
				case TimeSnapOptions.Hundredths:
					snappedTime = MathF.Round( time * 100 ) / 100;
					break;
				case TimeSnapOptions.Tenths:
					snappedTime = MathF.Round( time * 10 ) / 10;
					break;
				case TimeSnapOptions.Quarters:
					snappedTime = MathF.Round( time * 4 ) / 4;
					break;
				case TimeSnapOptions.Halfs:
					snappedTime = MathF.Round( time * 2 ) / 2;
					break;
				case TimeSnapOptions.Seconds:
					snappedTime = MathF.Round( time );
					break;
				case TimeSnapOptions.TenSeconds:
					snappedTime = MathF.Round( time / 10 ) * 10;
					break;
				case TimeSnapOptions.OneMinute:
					snappedTime = MathF.Round( time / 60 ) * 60;
					break;
				case TimeSnapOptions.Gridlines:
					if ( _gridBackground.GridStepX > 0.0f )
						snappedTime = (float)(_gridBackground.GridBaseX + Math.Round( (time - _gridBackground.GridBaseX) / _gridBackground.GridStepX ) * _gridBackground.GridStepX);
					break;
				case TimeSnapOptions.Custom:
					if ( SnapValueCustom > 0.0f )
						snappedTime = MathF.Round( time / SnapTimeCustom ) * SnapTimeCustom;
					break;
				default:
					throw new NotImplementedException( "SnapTimeMode not implemented" );
			}
		}

		if ( SnapValueEnabled )
		{
			switch ( SnapValueMode )
			{
				case ValueSnapOptions.Tenth:
					snappedValue = MathF.Round( value * 10 ) / 10;
					break;
				case ValueSnapOptions.Half:
					snappedValue = MathF.Round( value * 2 ) / 2;
					break;
				case ValueSnapOptions.One:
					snappedValue = MathF.Round( value );
					break;
				case ValueSnapOptions.Two:
					snappedValue = MathF.Round( value / 2 ) * 2;
					break;
				case ValueSnapOptions.Five:
					snappedValue = MathF.Round( value / 5 ) * 5;
					break;
				case ValueSnapOptions.Ten:
					snappedValue = MathF.Round( value / 10 ) * 10;
					break;
				case ValueSnapOptions.Fifty:
					snappedValue = MathF.Round( value / 50 ) * 50;
					break;
				case ValueSnapOptions.Hundred:
					snappedValue = MathF.Round( value / 100 ) * 100;
					break;
				case ValueSnapOptions.Gridlines:
					if ( _gridBackground.GridStepY > 0.0f )
						snappedValue = (float)(_gridBackground.GridBaseY + Math.Round( (value - _gridBackground.GridBaseY) / _gridBackground.GridStepY ) * _gridBackground.GridStepY);
					break;
				case ValueSnapOptions.Custom:
					if ( SnapValueCustom > 0.0f )
						snappedValue = MathF.Round( value / SnapValueCustom ) * SnapValueCustom;
					break;
				default:
					break;
			}
		}

		return (snappedTime, snappedValue);
	}

	/// <summary>
	/// Update selection to include all possible keyframes
	/// </summary>
	internal void SelectAll()
	{
		var allIndicies = new HashSet<int>();
		for ( int i = 0; i < _keyWidgets.Count; i++ )
		{
			allIndicies.Add( i );
		}

		UpdateSelection( ref allIndicies, false, false, false );
	}

	/// <summary>
	/// Delete key pressed, remove the selected keyframes
	/// </summary>
	internal void DeleteSelection()
	{
		if ( _selectedIndicies.Count == 0 )
			return;

		PushUndoState( $"Delete {_selectedIndicies.Count} keyframe{(_selectedIndicies.Count > 1 ? "s" : "")}" );

		// Remove the keyframes in reverse order so we don't invalidate the indicies
		foreach ( var idx in _selectedIndicies.OrderByDescending( x => x ) )
		{
			_rawCurveKeyframes.RemoveAt( idx );
		}

		// If we've removed all of the existing keyframes, add a default keyframe back in at 0,0
		if ( !_rawCurveKeyframes.Any() )
		{
			_rawCurveKeyframes.Add( new() );
		}

		_selectedIndicies.Clear();
		RebuildKeyframeWidgets(); // Full rebuild after direct raw curve keyframe removal
		RebuildSanitizedCurve();
	}

	/// <summary>
	/// Get the coordinate range that results in the current selected keyframes being visible on screen (when applied to the curve transform)
	/// </summary>
	internal CoordinateRange2D? GetCoordinateRangeForSelection()
	{
		// Nothing selected, focus on the entire curve.
		var selectedKeyframes = _selectedIndicies.Count == 0 ? _sanitizedKeyframes : SelectedKeyframes;
		if ( !selectedKeyframes.Any() )
		{
			return new( -1.0f, 1.0f, -1.0f, 1.0f ); // No selection and no keyframes to focus on.
		}
		else if ( selectedKeyframes.Count() == 1 )
		{
			var keyframe = selectedKeyframes.Single();

			// Try preserve the zoom level of the current transform and just focus on moving it
			var currentTimeRange = _curveTransform.CurveRange.MaxX - _curveTransform.CurveRange.MinX;
			var currentValueRange = _curveTransform.CurveRange.MaxY - _curveTransform.CurveRange.MinY;

			// Snap range back to default if we don't have any current time range (ie 1 keyframe)
			if ( currentTimeRange <= float.Epsilon )
				currentTimeRange = 2.0f;

			if ( currentValueRange <= float.Epsilon )
				currentValueRange = 2.0f;

			return new( keyframe.Time - (currentTimeRange * 0.5f), keyframe.Time + (currentTimeRange * 0.5f), keyframe.Value - (currentValueRange * 0.5f), keyframe.Value + (currentValueRange * 0.5f) );
		}

		// Translate our selected raw keyframes indicies into sanitized keyframe indicies,
		// we need to determine the evaluated value range of the rendered curve
		var selectedKeyframeSanitized = new List<int>();
		var invalidRawIndicies = new List<int>();

		for ( int i = 0; i < _keyWidgets.Count; i++ )
		{
			if ( _selectedIndicies.Count > 0 && !_selectedIndicies.Contains( i ) )
				continue;

			if ( _rawToSanitizedIdMap.TryGetValue( i, out int sanitizedIndex ) )
			{
				selectedKeyframeSanitized.Add( sanitizedIndex );
			}
			else
			{
				invalidRawIndicies.Add( i );
			}
		}

		float minTime = float.MaxValue;
		float maxTime = float.MinValue;
		float minValue = float.MaxValue;
		float maxValue = float.MinValue;

		// Sanitized full curve value lookup, ensures that curves extending beyond keyframes are included in the zoom.
		if ( selectedKeyframeSanitized.Count > 0 )
		{
			var lowestIdx = selectedKeyframeSanitized.Min();
			var highestIdx = selectedKeyframeSanitized.Max();

			minTime = _sanitizedCurve.Keyframes[lowestIdx].Time;
			maxTime = _sanitizedCurve.Keyframes[highestIdx].Time;

			// If we only have one single selected keyframe then we don't care about the curve range
			if ( selectedKeyframeSanitized.Count == 1 )
			{
				minValue = _sanitizedCurve.Keyframes[lowestIdx].Value;
				maxValue = _sanitizedCurve.Keyframes[highestIdx].Value;
			}
			else
			{
				var valueRange = _sanitizedCurve.KeyframeValueRanges.Skip( lowestIdx ).Take( highestIdx - lowestIdx + 1 );
				minValue = valueRange.Min( x => x.Min );
				maxValue = valueRange.Max( x => x.Max );
			}
		}

		// For any invalid selected keyframes, just ensure their keyframe is in view (because they have no sanitized curve to also fit)
		if ( invalidRawIndicies.Count > 0 )
		{
			// Just focus on all selected directly, ignoring any curve evaluation
			var allInvalidKeyframes = invalidRawIndicies.Select( x => _rawCurveKeyframes[x] );
			minTime = Math.Min( minTime, allInvalidKeyframes.Min( x => x.Time ) );
			maxTime = Math.Max( maxTime, allInvalidKeyframes.Max( x => x.Time ) );
			minValue = Math.Min( minValue, allInvalidKeyframes.Min( x => x.Value ) );
			maxValue = Math.Max( maxValue, allInvalidKeyframes.Max( x => x.Value ) );
		}

		return new CoordinateRange2D( minTime, maxTime, minValue, maxValue ).PadRange( 0.1f );
	}

	/// <summary>
	/// Move all selected keyframes by time/value
	/// </summary>
	internal void TranslateSelection( float time, float value )
	{
		if ( _selectedIndicies.Count == 0 )
			return;

		// Unlike other methods we intentionally don't push an undo state, this is often
		// called very frequently and we push the undo state when they start dragging operations

		foreach ( var idx in _selectedIndicies )
		{
			var keyframe = _rawCurveKeyframes[idx];
			SetRawKeyframe( idx, keyframe with { Time = keyframe.Time + time, Value = keyframe.Value + value } );
		}
	}

	internal void SetSelectionTime( float newTime )
	{
		if ( _selectedIndicies.Count == 0 )
			return;

		PushUndoState( "Update keyframe time" );
		foreach ( var idx in _selectedIndicies )
		{
			SetRawKeyframe( idx, _rawCurveKeyframes[idx] with { Time = newTime } );
		}
	}

	internal void SetSelectionValue( float newValue )
	{
		if ( _selectedIndicies.Count == 0 )
			return;

		PushUndoState( "Update keyframe value" );
		foreach ( var idx in _selectedIndicies )
		{
			SetRawKeyframe( idx, _rawCurveKeyframes[idx] with { Value = newValue } );
		}
	}

	public void SetSelectionInterpolation( InterpTangentMode newMode )
	{
		if ( _selectedIndicies.Count == 0 )
			return;

		PushUndoState( "Update keyframe interpolation" );

		foreach ( var idx in _selectedIndicies )
		{
			SetRawKeyframe( idx, _rawCurveKeyframes[idx] with
			{
				Interpolation = newMode.Interp,
				TangentMode = newMode.Tangent
			} );
		}

		SelectedInterpolation = newMode; // We know all selection must be this interp value now
	}

	/// <summary>
	/// Update data based on the user selection, such as:
	/// - Is the user selecting keyframes all of the same interpolation/tangent mode
	/// </summary>
	private void CalculateSelectedInterpolation()
	{
		SelectedInterpolation = null;
		if ( SelectedKeyframes.Any() )
		{
			var firstInterp = SelectedKeyframes.First();

			if ( SelectedKeyframes.All( x => x.Interpolation == firstInterp.Interpolation && x.TangentMode == firstInterp.TangentMode ) )
				SelectedInterpolation = new( firstInterp.Interpolation, firstInterp.TangentMode );
		}
	}

	/// <summary>
	/// Flatten the tangents of selected keyframes
	/// </summary>
	public void SetSelectionTangentFlat()
	{
		if ( _selectedIndicies.Count == 0 )
			return;

		PushUndoState( "Flatten keyframes" );
		foreach ( var idx in _selectedIndicies )
		{
			var keyframe = _rawCurveKeyframes[idx] with { TangentIn = 0.0f, TangentOut = 0.0f };

			// Also disable auto tangent mode for any cubic keyframes
			if ( keyframe.Interpolation == Interpolation.Cubic && keyframe.TangentMode == TangentMode.Automatic )
				keyframe = keyframe with { TangentMode = TangentMode.Mirrored };

			SetRawKeyframe( idx, keyframe );
		}

		CalculateSelectedInterpolation(); // We might have changed handle interpolation, so refresh the state
	}

	/// <summary>
	/// Called after changes to push a new undo state onto the stack, the user can ctrl-z to roll back curve changes
	/// </summary>
	public void PushUndoState( string operation )
	{
		// Push a COPY of the keyframe onto the stack
		_curveHistory.Push( new( operation, _rawCurveKeyframes.ToImmutableArray(), _extrapolationPreInfinity, _extrapolationPostInfinity ) );
		_curveRedoHistory.Clear(); // A new undo-worthy change flushes the redo stack
	}

	/// <summary>
	/// Pop a change from the undo state stack into the redo stack, applying the change
	/// </summary>
	internal void Undo()
	{
		if ( _curveHistory.Count == 0 )
			return;

		// Do not allow undo operations while mid-drag
		if ( _draggingKeyframeIndex >= 0 )
		{
			Log.Warning( "Ignoring undo attempt mid-keyframe drag." );
			return;
		}

		var undo = _curveHistory.Pop();
		_curveRedoHistory.Push( new( undo.Operation, _rawCurveKeyframes.ToImmutableArray(), _extrapolationPreInfinity, _extrapolationPostInfinity ) );
		OnUndoRedo.Invoke( false, undo.Operation );

		SetRawCurve( undo.Keyframes, undo.PreInfinity, undo.PostInfinity );
	}

	/// <summary>
	/// Pop a change from the redo stack, returning it to the undo stack
	/// </summary>
	internal void Redo()
	{
		if ( _curveRedoHistory.Count == 0 )
			return;

		// Do not allow undo operations while mid-drag
		if ( _draggingKeyframeIndex >= 0 )
		{
			Log.Warning( "Ignoring redo attempt mid-keyframe drag." );
			return;
		}

		var undo = _curveRedoHistory.Pop();
		_curveHistory.Push( new( undo.Operation, _rawCurveKeyframes.ToImmutableArray(), _extrapolationPreInfinity, _extrapolationPostInfinity ) );
		OnUndoRedo.Invoke( true, undo.Operation );

		SetRawCurve( undo.Keyframes, undo.PreInfinity, undo.PostInfinity );
	}

	private void OpenContextMenu()
	{
		var m = new Menu()
		{
			DeleteOnClose = true
		};

		// We want the curve options if we have no selection, or we directly click on the curve (but not a keyframe!)
		var showCurveOptions = _selectedIndicies.Count == 0 || (HoveringCurve && !HoveringKeyframe);
		if ( showCurveOptions )
		{
			m.AddHeading( $"Curve Options:" );
			m.AddOption( "Add Key (Middle-Mouse)", "add", () => CreateHoveredKeyframe( _lastMousePos ) );
		}
		else
		{
			m.AddHeading( $"{_selectedIndicies.Count} Selected Keyframe{(_selectedIndicies.Count == 1 ? "" : "s")}:" );
			m.AddOption( $"Delete (Del)", "delete", () => DeleteSelection() );

			m.AddOption( $"Flatten (6)", "horizontal_rule", () => SetSelectionTangentFlat() )
				.Enabled = SelectedKeyframes.Any( x => x.Interpolation == Interpolation.Cubic ); // Only if we have at least 1 cubic key selected

			// Interpolation 
			m.AddSeparator();
			m.AddHeading( "Keyframe Interpolation:" );

			m.AddOption( new Option( "Cubic - Auto (1)", null, () => SetSelectionInterpolation( new( Interpolation.Cubic, TangentMode.Automatic ) ) )
			{
				Checkable = true,
				Checked = SelectedInterpolation.HasValue && SelectedInterpolation.Value == new InterpTangentMode( Interpolation.Cubic, TangentMode.Automatic ),
			} ).SetIcon( CurveTextures.Instance.CurveCubicAutoPixmap );

			m.AddOption( new Option( "Cubic - Mirror (2)", null, () => SetSelectionInterpolation( new( Interpolation.Cubic, TangentMode.Mirrored ) ) )
			{
				Checkable = true,
				Checked = SelectedInterpolation.HasValue && SelectedInterpolation.Value == new InterpTangentMode( Interpolation.Cubic, TangentMode.Mirrored ),
			} ).SetIcon( CurveTextures.Instance.CurveCubicMirrorPixmap );

			m.AddOption( new Option( "Cubic - Split (3)", null, () => SetSelectionInterpolation( new( Interpolation.Cubic, TangentMode.Split ) ) )
			{
				Checkable = true,
				Checked = SelectedInterpolation.HasValue && SelectedInterpolation.Value == new InterpTangentMode( Interpolation.Cubic, TangentMode.Split ),
			} ).SetIcon( CurveTextures.Instance.CurveCubicBrokenPixmap );

			m.AddOption( new Option( "Linear (4)", null, () => SetSelectionInterpolation( new( Interpolation.Linear ) ) )
			{
				Checkable = true,
				Checked = SelectedInterpolation.HasValue && SelectedInterpolation.Value.Interp == Interpolation.Linear,
			} ).SetIcon( CurveTextures.Instance.CurveLinearPixmap );

			m.AddOption( new Option( "Stepped (5)", null, () => SetSelectionInterpolation( new( Interpolation.Constant ) ) )
			{
				Checkable = true,
				Checked = SelectedInterpolation.HasValue && SelectedInterpolation.Value.Interp == Interpolation.Constant
			} ).SetIcon( CurveTextures.Instance.CurveConstantPixmap );
		}

		// Always show pre/post infinity & select all:
		m.AddSeparator();
		AddExtrapolationMenu( m, "Pre-Infinity", "west", _extrapolationPreInfinity, ( value ) =>
		{
			PushUndoState( "Changed curve pre-infinity" );
			_extrapolationPreInfinity = value;
			RebuildSanitizedCurve();
		} );

		AddExtrapolationMenu( m, "Post-Infinity", "east", _extrapolationPostInfinity, ( value ) =>
		{
			PushUndoState( "Changed curve post-infinity" );
			_extrapolationPostInfinity = value;
			RebuildSanitizedCurve();
		} );

		m.AddSeparator();
		m.AddOption( "Select All (Ctrl-A)", "select_all", () => SelectAll() );

		// Slightly nudged down so we're not obscuring the title
		m.OpenAt( Editor.Application.CursorPosition + (Vector2.Down * -10.0f) );

		void AddExtrapolationMenu( Menu m, string title, string icon, AltCurve.Extrapolation currentExtrap, Action<AltCurve.Extrapolation> onSelectOption )
		{
			var subMenu = m.AddMenu( title, icon );

			var linear = subMenu.AddOption( new Option( "Linear", null, () => onSelectOption.Invoke( Extrapolation.Linear ) )
			{
				Checkable = true,
				Checked = currentExtrap == Extrapolation.Linear
			} );
			linear.SetIcon( CurveTextures.Instance.ExtrapLinearPixmap );
			linear.Tooltip = "Linearly extrapolate";
			// JMCB TODO: Why do tooltips not work in the context menus :(
			linear.Enabled = true;

			subMenu.AddOption( new Option( "Constant", null, () => onSelectOption.Invoke( Extrapolation.Constant ) )
			{
				Checkable = true,
				Checked = currentExtrap == Extrapolation.Constant
			} ).SetIcon( CurveTextures.Instance.ExtrapConstantPixmap );

			subMenu.AddOption( new Option( "Cycle", null, () => onSelectOption.Invoke( Extrapolation.Cycle ) )
			{
				Checkable = true,
				Checked = currentExtrap == Extrapolation.Cycle
			} ).SetIcon( CurveTextures.Instance.ExtrapCyclePixmap );

			subMenu.AddOption( new Option( "Cycle with Offset", null, () => onSelectOption.Invoke( Extrapolation.CycleOffset ) )
			{
				Checkable = true,
				Checked = currentExtrap == Extrapolation.CycleOffset
			} ).SetIcon( CurveTextures.Instance.ExtrapCycleOffsetPixmap );

			subMenu.AddOption( new Option( "Oscillate", null, () => onSelectOption.Invoke( Extrapolation.Oscillate ) )
			{
				Checkable = true,
				Checked = currentExtrap == Extrapolation.Oscillate
			} ).SetIcon( CurveTextures.Instance.ExtrapOscillatePixmap );
		}
	}
}
