using Editor;
using Sandbox;
using System;

namespace AltCurves.Widgets
{
	/// <summary>
	/// Basically a FloatProperty, but modified for the specific use case
	/// In particular instead of a single value we're incrementing/decrementing, 
	/// we pipe it back to the AltCurveEditor in the form of X/Y translations
	/// for multiple selected keyframes. 
	/// Mainly because it's not viable to have it rebind to each appropriate key on selections.
	/// </summary>
	internal class CurveCoordinateProperty : Widget
	{
		/// <summary>
		/// Number of selected keyframes, used to determine if we should display the value
		/// </summary>
		public int SelectedKeyframes
		{
			get => _selectedKeyframes;
			set { _selectedKeyframes = value; UpdateText(); }
		}
		private int _selectedKeyframes = 0;

		/// <summary>
		/// The value of the selected keyframe, if we have 1 selected.
		/// </summary>
		public float SelectedValue
		{
			get => _selectedValue;
			set { _selectedValue = value; UpdateText(); }
		}
		private float _selectedValue = 0.0f;

		public string Label { get; set; }
		public string Icon { get; set; } = "multiple_stop";

		public Color HighlightColor = Theme.Green;

		public LineEdit LineEdit { get; init; }

		Vector2 _lastDragPos;
		float _yDragAccum = 0.0f;

		public Action OnDraggingStart { get; set; }
		public Action<float> OnDragging { get; set; }
		public Action OnDraggingEnd { get; set; }

		/// <summary>
		/// Called when we've had a manual input requesting a specific float value
		/// </summary>
		public Action<float> OnManualInput { set; get; }

		public CurveCoordinateProperty( Widget parent ) : base( parent )
		{
			LineEdit = new LineEdit( this );
			LineEdit.TextEdited += LineEdit_TextEdited;
			LineEdit.MinimumSize = Theme.RowHeight;
			LineEdit.NoSystemBackground = true;
			LineEdit.TranslucentBackground = true;
			LineEdit.Alignment = TextFlag.LeftCenter;
			LineEdit.SetStyles( "background-color: transparent;" );
			LineEdit.RegexValidator = "^[-+]?[0-9]*(?:\\.[0-9]*)?$";
			Cursor = CursorShape.SizeH;

			MinimumSize = Theme.RowHeight;
			MaximumSize = new Vector2( 4096, Theme.RowHeight );
		}

		public CurveCoordinateProperty( string label, Widget parent ) : this( parent )
		{
			Label = label;
		}

		private void LineEdit_TextEdited( string obj )
		{
			if ( float.TryParse( obj, out float value ) )
			{
				OnManualInput?.Invoke( value );
			}
		}

		private void UpdateText()
		{
			if ( SelectedKeyframes == 0 )
				LineEdit.Text = "";
			else if ( SelectedKeyframes == 1 )
				LineEdit.Text = SelectedValue.ToString();
			else
				LineEdit.Text = $"{SelectedKeyframes} selected";
		}

		protected override void DoLayout()
		{
			base.DoLayout();
			var h = Size.y;
			LineEdit.Position = new Vector2( h, 0 );
			LineEdit.Size = Size - new Vector2( h, 0 );
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			var h = Size.y;
			bool hovered = IsUnderMouse;
			if ( !Enabled ) hovered = false;

			Paint.Antialiasing = true;
			Paint.TextAntialiasing = true;

			Paint.ClearPen();
			Paint.SetBrush( Theme.ControlBackground );
			Paint.DrawRect( LocalRect, Theme.ControlRadius );

			// icon box
			Paint.ClearPen();
			Paint.SetBrush( HighlightColor.Darken( hovered ? 0.7f : 0.8f ).Desaturate( 0.8f ) );
			Paint.DrawRect( new Rect( 0, 0, h, h ).Grow( -1 ), Theme.ControlRadius - 1.0f );

			// flatten right (we need a DrawRect with uneven corners)
			Paint.DrawRect( new Rect( h - Theme.ControlRadius, 0, Theme.ControlRadius, h ).Grow( -1 ) );

			Paint.SetPen( HighlightColor.Darken( hovered ? 0.0f : 0.1f ).Desaturate( hovered ? 0.0f : 0.2f ) );

			if ( string.IsNullOrEmpty( Label ) )
			{
				Paint.DrawIcon( new Rect( 0, h ), Icon, h - 6, TextFlag.Center );
			}
			else
			{
				Paint.SetFont( "Poppins", 9, 450 );
				Paint.DrawText( new Rect( 1, h - 1 ), Label, TextFlag.Center );
			}
		}

		protected override void OnMousePress( MouseEvent e )
		{
			base.OnMousePress( e );

			if ( e.RightMouseButton )
			{
				LineEdit.Focus();
				LineEdit.SelectAll();
			}

			if ( e.LeftMouseButton && !ReadOnly && e.LocalPosition.x < Height )
			{
				LineEdit.Blur();
				_lastDragPos = e.LocalPosition;
				e.Accepted = true;
				OnDraggingStart?.Invoke();
			}
		}

		protected override void OnMouseReleased( MouseEvent e )
		{
			base.OnMouseReleased( e );

			if ( e.LeftMouseButton && !ReadOnly )
			{
				OnDraggingEnd?.Invoke();
				LineEdit.Focus();
				e.Accepted = true;
			}
		}

		protected override void OnMouseMove( MouseEvent e )
		{
			base.OnMouseMove( e );

			if ( e.ButtonState.HasFlag( MouseButtons.Left ) )
			{
				var delta = e.LocalPosition - _lastDragPos;
				_lastDragPos = e.LocalPosition;
				_yDragAccum += delta.y;

				int bracket = (Math.Abs( _yDragAccum ) / 50).FloorToInt() + 1;
				var incr = (float)(delta.x / 20 * bracket * 3);

				SelectedValue += incr;
				OnDragging?.Invoke( incr );

				SignalValuesChanged();

				e.Accepted = true;
			}
		}
	}
}
