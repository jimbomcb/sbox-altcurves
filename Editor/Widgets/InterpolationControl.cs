using Editor;
using System;
using static AltCurves.AltCurve;

namespace AltCurves.Widgets;

/// <summary>
/// Contains the set of toolbar controls for choosing the interpolation of the selected keyframes
/// Highlights the button of the current interpolation if all selected share a mode.
/// It's not ideal to be recreating these buttons each time, but I hate the default button check toggling 
/// and you can't change a style of an existing button without recreating it (that I've found..)
/// </summary>
internal class InterpolationControl : Widget
{
	/// <summary>
	/// The current interp/tangent modes that we want to highlight, 
	/// updated via binds if we're currently selecting keyframes that share a common type.
	/// </summary>
	public InterpTangentMode? HighlightedInterpolation
	{
		set
		{
			_highlightedInterp = value;
			BuildButtonLayout();
		}
	}
	private InterpTangentMode? _highlightedInterp;

	/// <summary>
	/// True if the user has any selected keyframe, updated via binds
	/// </summary>
	public bool HasSelectedKeyframe
	{
		set
		{
			_hasSelectedKeyframe = value;
			BuildButtonLayout();
		}
	}
	private bool _hasSelectedKeyframe = false;

	/// <summary>
	/// Called when we select a new interpolation/tangent mode
	/// </summary>
	public Action<InterpTangentMode> SelectedInterp { get; set; }

	public InterpolationControl( Widget parent ) : base( parent )
	{
		MinimumWidth = 80;
		Cursor = CursorShape.Finger;

		Enabled = true;

		Layout = Layout.Row();
		Layout.Spacing = 1.0f;
		BuildButtonLayout();
	}

	protected void BuildButtonLayout()
	{
		Layout.Clear( true );

		Layout.Add( new Button( "Auto" )
		{
			VerticalSizeMode = SizeMode.CanGrow,
			Enabled = _hasSelectedKeyframe,
			ButtonType = _highlightedInterp.HasValue && _highlightedInterp == new InterpTangentMode( Interpolation.Cubic, TangentMode.Automatic ) ? "primary" : "",
			Pressed = () => SelectedInterp?.Invoke( new( Interpolation.Cubic, TangentMode.Automatic ) ),
			ToolTip = "Cubic - Automatic (Shortcut: 1): Smoothly interpolate, automatically calculate tangents based on surrounding keyframes."
		} ).SetIcon( CurveTextures.Instance.CurveCubicAutoPixmap );

		Layout.Add( new Button( "Mirror" )
		{
			VerticalSizeMode = SizeMode.CanGrow,
			Enabled = _hasSelectedKeyframe,
			ButtonType = _highlightedInterp.HasValue && _highlightedInterp == new InterpTangentMode( Interpolation.Cubic, TangentMode.Mirrored ) ? "primary" : "",
			Pressed = () => SelectedInterp?.Invoke( new( Interpolation.Cubic, TangentMode.Mirrored ) ),
			ToolTip = "Cubic - Mirror (Shortcut: 2): Smoothly interpolate with mirrored tangents."
		} ).SetIcon( CurveTextures.Instance.CurveCubicMirrorPixmap );

		Layout.Add( new Button( "Split" )
		{
			VerticalSizeMode = SizeMode.CanGrow,
			Enabled = _hasSelectedKeyframe,
			ButtonType = _highlightedInterp.HasValue && _highlightedInterp == new InterpTangentMode( Interpolation.Cubic, TangentMode.Split) ? "primary" : "",
			Pressed = () => SelectedInterp?.Invoke( new( Interpolation.Cubic, TangentMode.Split ) ),
			ToolTip = "Cubic - Split (Shortcut: 3): Smoothly interpolate with split/broken tangents."
		} ).SetIcon( CurveTextures.Instance.CurveCubicBrokenPixmap );

		Layout.Add( new Button( "Linear" )
		{
			VerticalSizeMode = SizeMode.CanGrow,
			Enabled = _hasSelectedKeyframe,
			ButtonType = _highlightedInterp.HasValue && _highlightedInterp.Value.Interp == Interpolation.Linear ? "primary" : "",
			Pressed = () => SelectedInterp?.Invoke( new( Interpolation.Linear ) ),
			ToolTip = "Linear (Shortcut: 4): Linearly interpolate from one keyframe to another."
		} ).SetIcon( CurveTextures.Instance.CurveLinearPixmap );

		Layout.Add( new Button( "Stepped" )
		{
			VerticalSizeMode = SizeMode.CanGrow,
			Enabled = _hasSelectedKeyframe,
			ButtonType = _highlightedInterp.HasValue && _highlightedInterp.Value.Interp == Interpolation.Constant ? "primary" : "",
			Pressed = () => SelectedInterp?.Invoke( new( Interpolation.Constant ) ),
			ToolTip = "Constant/Stepped (Shortcut: 5): Stair-stepping between keyframes."
		} ).SetIcon( CurveTextures.Instance.CurveConstantPixmap );
	}
}
