using Editor;
using System;

namespace AltCurves.GraphicsItems;
public partial class EditableAltCurve : GraphicsItem
{
	/// <summary>
	/// The draggable, invisible handle that represents a keyframe on the curve
	/// The actual visible keyframe position can vary with snapping
	/// </summary>
	public class DragHandle : GraphicsItem
	{
		/// <summary>
		/// Underlying keyframe that we're positioned for, updated on dragging
		/// </summary>
		public AltCurve.Keyframe Keyframe
		{
			get => _keyframe;
			set
			{
				_keyframe = value;
				StartDragKeyframe = _keyframe;
				Position = _transform.CurveToWidgetPosition( new( _keyframe.Time, _keyframe.Value ) );
				Update();
			}
		}
		private AltCurve.Keyframe _keyframe;

		/// <summary>
		/// The keyframe when we started the last dragging operation
		/// </summary>
		public AltCurve.Keyframe StartDragKeyframe { get; private set; }

		public CurveWidgetTransform Transform
		{
			get => _transform;
			set
			{
				_transform = value;
				Position = _transform.CurveToWidgetPosition( new( Keyframe.Time, Keyframe.Value ) );
				Update();
			}
		}
		private CurveWidgetTransform _transform;

		/// <summary>
		/// Called when we're left-clicked for selection logic
		/// Params are index, isShift, isCtrl, isAlt
		/// </summary>
		public Action<int, bool, bool, bool> OnMouseDown { get; set; } = null;
		public Action<int, bool, bool, bool> OnMouseUp { get; set; } = null;

		/// <summary>
		/// Called during drag operations, after the Keyframe has been updated for our new position
		/// Parameter is the index and the pre-drag keyframe.
		/// </summary>
		public Action<int> OnDragging { get; set; } = null;

		/// <summary>
		/// Called when releasing the mouse, the drag has completed
		/// </summary>
		public Action<int> OnDragComplete { get; set; } = null;

		private readonly int _index;
		private Vector2 _mouseDownPos;

		public DragHandle( int index, CurveWidgetTransform transform, AltCurve.Keyframe keyframe, GraphicsItem parent ) : base( parent )
		{
			_index = index;
			_keyframe = keyframe;
			StartDragKeyframe = keyframe;
			HandlePosition = new( 0.5f );
			Cursor = CursorShape.Finger;
			Movable = true;
			Clip = true;
			HoverEvents = true;

			Size = new( 14.0f );
			_transform = transform;
			Position = _transform.CurveToWidgetPosition( new( Keyframe.Time, Keyframe.Value ) );
		}

		protected override void OnMoved()
		{
			base.OnMoved();

			_keyframe = _keyframe with { Time = _transform.WidgetToCurveX( Position.x ), Value = _transform.WidgetToCurveY( Position.y ) };
			OnDragging?.Invoke( _index );
		}

		protected override void OnMousePressed( GraphicsMouseEvent e )
		{
			if ( e.LeftMouseButton )
			{
				StartDragKeyframe = Keyframe;
				_mouseDownPos = e.ScreenPosition;
				OnMouseDown?.Invoke( _index, e.HasShift, e.HasCtrl, e.HasAlt );
			}

			// Begin drag
			base.OnMousePressed( e );
		}

		protected override void OnMouseReleased( GraphicsMouseEvent e )
		{
			base.OnMouseReleased( e );

			// Either the user moved us from the starting location and we're completing a drag,
			// or they just clicked us normally and the mouse up action should trigger
			if ( e.LeftMouseButton )
			{
				if ( e.ScreenPosition != _mouseDownPos )
				{
					OnDragComplete?.Invoke( _index );
				}
				else
				{
					OnMouseUp?.Invoke( _index, e.HasShift, e.HasCtrl, e.HasAlt );
				}
			}
		}
	}
}
