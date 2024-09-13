using Editor;
using System;

namespace AltCurves.GraphicsItems;
public partial class EditableAltCurve
{
	/// <summary>
	/// The movable tangent handle, each visible keyframe contains an in/out handle.
	/// The rendering of the tangent is handled by the KeyVisible.
	/// </summary>
	public class KeyTangent : GraphicsItem
	{
		public const float TANGENT_RADIUS = 50.0f;

		/// <summary>
		/// Specific tangent value, rise/run of the entering/exiting tangent. Positive values slope up, negative slopes down.
		/// </summary>
		public float Tangent {
			get => _tangent;
			set {
				_tangent = value;
				UpdatePosition();
			}
		}
		private float _tangent;

		/// <summary>
		/// Is this an arriving tangent value (rather than a departing one)
		/// </summary>
		private readonly bool _isArrive;

		public CurveWidgetTransform Transform
		{
			get => _transform;
			set
			{
				_transform = value;
				UpdatePosition();
			}
		}
		private CurveWidgetTransform _transform;

		/// <summary>
		/// Hidden state, disables interaction
		/// </summary>
		public bool Hidden { 
			get => _hidden;
			set
			{
				_hidden = value;

				Movable = !value;
				HoverEvents = !value;
				Cursor = value ? CursorShape.None : CursorShape.Finger;
			}
		}
		private bool _hidden = false;

		/// <summary>
		/// Drag operation starting, trigger undo snapshot
		/// </summary>
		public Action OnDragStart;

		/// <summary>
		/// Tangent has been dragged and we have a new slope value based on the handle position
		/// </summary>
		public Action<float> OnUpdated;

		public KeyTangent( GraphicsItem parent, CurveWidgetTransform transform, float initialValue, bool isArrive ) : base( parent )
		{
			HandlePosition = 0.5f;
			Clip = false;
			Selectable = false;
			Size = 10.0f;

			Hidden = false;
			_transform = transform;
			_isArrive = isArrive;
			Tangent = initialValue;
		}

		//protected override void OnPaint()
		//{
		//	base.OnPaint();
		//
		//	Paint.SetBrush( Hovered ? Color.Red : ( _isArrive ? Theme.Blue : Theme.Pink ));
		//	Paint.DrawRect( LocalRect );
		//}

		protected override void OnHoverEnter( GraphicsHoverEvent e )
		{
			base.OnHoverEnter( e );

			// This isn't pretty, but without it there's a weird issue where mousing over the very edge of this
			// tangent correctly gives a cursor finger, but the parent visible keyframe doesn't paint the tangent handle...
			Parent.Update();
		}

		protected override void OnHoverLeave( GraphicsHoverEvent e )
		{
			base.OnHoverLeave( e );
			Parent.Update();
		}

		protected override void OnMousePressed( GraphicsMouseEvent e )
		{
			base.OnMousePressed( e );

			if (e.LeftMouseButton)
				OnDragStart?.Invoke();
		}

		protected override void OnMouseReleased( GraphicsMouseEvent e )
		{
			UpdatePosition(); // Snap drag widget position to correct tangent offset position
		}

		protected override void OnMouseMove( GraphicsMouseEvent e )
		{
			base.OnMouseMove( e );

			// Prevent a tangent being dragged past the opposite side of the keyframe
			var clampedPositionX = _isArrive ? Math.Min( Position.x, -2f ) : Math.Max( 2f, Position.x );
			_tangent = (-Position.y / clampedPositionX) / _transform.WidgetCurveAspectRatio;

			OnUpdated?.Invoke( Tangent );
		}

		/// <summary>
		/// Snap current Position (local to parent) to the correct tangent slope offset position
		/// </summary>
		private void UpdatePosition()
		{
			float widgetSpaceTangent = Tangent * _transform.WidgetCurveAspectRatio;
			var widgetSpaceSlope = new Vector2( 1.0f, -widgetSpaceTangent ).Normal;
			if ( _isArrive ) widgetSpaceSlope = -widgetSpaceSlope;
			Position = widgetSpaceSlope * TANGENT_RADIUS;
		}
	}
}
