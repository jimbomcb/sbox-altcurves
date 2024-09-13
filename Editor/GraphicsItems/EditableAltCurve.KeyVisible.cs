using Editor;
using System;
using static AltCurves.AltCurve;

namespace AltCurves.GraphicsItems;
public partial class EditableAltCurve
{
	/// <summary>
	/// The visible keyframe dragged via KeyframeHandle
	/// Parent of the visible movable tangent handles
	/// </summary>
	public class KeyVisible : GraphicsItem
	{
		/// <summary>
		/// Index of the keyframe in the curve
		/// </summary>
		public int Index { get; init; }

		public Keyframe Keyframe
		{
			get => _keyframe;
			set
			{
				_keyframe = value;
				Position = _transform.CurveToWidgetPosition( new( _keyframe.Time, _keyframe.Value ) );

				TangentIn.Tangent = _keyframe.TangentIn;
				TangentOut.Tangent = _keyframe.TangentOut;
				UpdateTangentVisiblity();

				Update();
			}
		}
		private Keyframe _keyframe;

		/// <summary>
		/// Selected via drag/click, we can't use normal widget Selectable without interfering with dragging
		/// </summary>
		public bool UserSelected
		{
			get => _userSelected;
			set
			{
				_userSelected = value;

				// Enable/disable tangent controls if we're showing handles on selection only
				if ( _tangentViewMode == TangentViewMode.Selected )
					UpdateTangentVisiblity();
			}
		}
		private bool _userSelected = false;

		/// <summary>
		/// True while this keyframe is in an invalid state (shares a time with another keyframe)
		/// The user is expected to fix this, or lose the key when they close the editor.
		/// </summary>
		public bool InvalidKeyframe { get; set; } = false;

		/// <summary>
		/// Tangent view mode controlling the conditions for drawing tangent handles
		/// </summary>
		public TangentViewMode TangentMode
		{
			get => _tangentViewMode;
			set
			{
				_tangentViewMode = value;
				UpdateTangentVisiblity();
			}
		}
		private TangentViewMode _tangentViewMode;

		/// <summary>
		/// Transform for converting curve/widget space
		/// </summary>
		public CurveWidgetTransform Transform
		{
			get => _transform;
			set
			{
				_transform = value;
				Position = _transform.CurveToWidgetPosition( new( Keyframe.Time, Keyframe.Value ) );
				TangentIn.Transform = value;
				TangentOut.Transform = value;
				Update();
			}
		}
		private CurveWidgetTransform _transform;

		/// <summary>
		/// Should we draw the tangents for this keyframe, based on interpolation type + user settings
		/// </summary>
		public bool TangentsVisible =>  _keyframe.Interpolation == Interpolation.Cubic && (TangentMode == TangentViewMode.All || (TangentMode == TangentViewMode.Selected && UserSelected));

		public KeyTangent TangentIn { get; init; }
		public KeyTangent TangentOut { get; init; }

		private bool _isFirst = false; // JMCB TODO: Not accurate for raw curve
		private bool _isLast = false;

		public KeyVisible( int index, int totalKeyframes, CurveWidgetTransform transform, AltCurve.Keyframe keyframe, TangentViewMode tangentViewMode, GraphicsItem parent ) : base( parent )
		{
			Index = index;
			_isFirst = index == 0;
			_isLast = index == totalKeyframes - 1;
			_keyframe = keyframe;
			_tangentViewMode = tangentViewMode;

			HandlePosition = 0.5f;
			Movable = false;
			HoverEvents = false;
			Clip = false;
			Selectable = false;

			Size = new( 10.0f );
			_transform = transform;
			Position = _transform.CurveToWidgetPosition( new( Keyframe.Time, Keyframe.Value ) );

			TangentIn = new KeyTangent( this, transform, keyframe.TangentIn, true );
			TangentOut = new KeyTangent( this, transform, keyframe.TangentOut, false );
			UpdateTangentVisiblity();
		}

		private void UpdateTangentVisiblity()
		{
			TangentIn.Hidden = !TangentsVisible/* || _isFirst*/; // jmcb todo
			TangentOut.Hidden = !TangentsVisible/* || _isLast*/;
			Update();
		}

		protected override void OnPaint()
		{
			base.OnPaint();

			if ( InvalidKeyframe )
			{
				Paint.SetBrush( Theme.Red );
				Paint.SetDefaultFont();
				Paint.DrawTextBox( LocalRect, "X", Color.White, new( 2.0f, 0.0f, 1.0f, 0.0f ), 1.0f, Sandbox.TextFlag.CenterHorizontally );
				return;
			}

			if ( !TangentIn.Hidden )
			{
				float inAngle = MathF.Atan( _keyframe.TangentIn * _transform.WidgetCurveAspectRatio );
				var inOffset = new Vector2( -MathF.Cos( inAngle ), MathF.Sin( inAngle ) ) * KeyTangent.TANGENT_RADIUS;

				Paint.SetPen( Theme.White.WithAlpha( 0.4f ), style: PenStyle.Dash );
				Paint.DrawLine( Size * 0.5f, inOffset + (TangentIn.Size * 0.5f) );

				Paint.SetPen( Theme.White.WithAlpha( 0.8f ) );
				Paint.SetBrush( TangentIn.Hovered ? Theme.Red : Theme.Red.Darken( 0.3f ) );
				Paint.DrawCircle( inOffset + (TangentIn.Size * 0.5f), TangentIn.Size - 3.0f );
			}

			if ( !TangentOut.Hidden )
			{
				float outAngle = MathF.Atan( _keyframe.TangentOut * _transform.WidgetCurveAspectRatio );
				var outOffset = new Vector2( MathF.Cos( outAngle ), -MathF.Sin( outAngle ) ) * KeyTangent.TANGENT_RADIUS;

				Paint.SetPen( Theme.White.WithAlpha( 0.4f ), style: PenStyle.Dash );
				Paint.DrawLine( Size * 0.5f, outOffset + (TangentOut.Size * 0.5f) );

				Paint.SetPen( Theme.White.WithAlpha( 0.8f ) );
				Paint.SetBrush( TangentOut.Hovered ? Theme.Blue : Theme.Blue.Darken( 0.3f ) );
				Paint.DrawCircle( outOffset + (TangentOut.Size * 0.5f), TangentOut.Size - 3.0f );
			}

			// Per-interpolation icons
			Paint.SetPen( Theme.Black );
			Paint.SetBrush( UserSelected ? Theme.White : Theme.White.Darken( 0.3f ) );
			switch ( _keyframe.Interpolation )
			{
				case Interpolation.Linear:
					// Triangle
					Paint.DrawPolygon( new Vector2[] {
						new(0, Size.y),
						new(Size.x * 0.5f, 0.0f),
						new(Size.x, Size.y)
					} );
					break;
				case Interpolation.Constant:
					// Box
					Paint.DrawRect( LocalRect.Shrink( 1f ) );
					break;
				case Interpolation.Cubic:
					// Diamond
					// Tint slightly green if auto
					if (_keyframe.TangentMode == AltCurve.TangentMode.Automatic)
						Paint.SetBrush( UserSelected ? Theme.Green.Desaturate( 0.5f ) : Theme.Green.Desaturate( 0.5f ).Darken( 0.3f )  );
					// Or blue if broken
					else if ( _keyframe.TangentMode == AltCurve.TangentMode.Split)
						Paint.SetBrush( UserSelected ? Theme.Blue.Desaturate( 0.4f ) : Theme.Blue.Desaturate( 0.4f ).Darken( 0.4f ) );

					Paint.DrawPolygon( new Vector2[] {
						new(Size.x * 0.5f, 0.0f),
						new(0, Size.y * 0.5f),
						new(Size.x * 0.5f, Size.y),
						new(Size.x, Size.y * 0.5f)
					} );
					break;
			}
		}
	}
}
