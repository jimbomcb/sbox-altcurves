using AltCurves.GraphicsItems;
using Editor;

namespace AltCurves.Widgets;

/// <summary>
/// Collection of curve editor toolbar buttons
/// </summary>
internal class AltCurveEditorToolbar : Widget
{
	private readonly Button _visibilityOptions;
	private readonly Button _zoomButton;
	private readonly SnapButtonTime _snapTimeButton;
	private readonly SnapButtonValue _snapValueButton;
	//private readonly EnumProperty<AltCurve.Extrapolation> _preInfinity;
	//private readonly EnumProperty<AltCurve.Extrapolation> _postInfinity;
	private readonly CurveCoordinateProperty _timeCoord;
	private readonly CurveCoordinateProperty _valueCoord;
	private readonly InterpolationControl _interpolation;
	private readonly Button _flattenButton;

	public AltCurveEditorToolbar( Widget parent, int HEADER_HEIGHT = 30 ) : base( parent )
	{
		Layout = Layout.Row();
		Layout.Margin = 0;
		Layout.Spacing = 5.0f;

		_visibilityOptions = Layout.Add(new Button("View", "visibility", this )
		{
			ToolTip = "Toggle visibility of the curve",
			Enabled = true,
			VerticalSizeMode = SizeMode.CanGrow,
		} );

		_zoomButton = Layout.Add( new Button( "Zoom to Fit", "search", this )
		{
			ToolTip = "Zoom to Fit (Shortcut: F)",
			Enabled = true,
			VerticalSizeMode = SizeMode.CanGrow,
		} );

		const float coordWidth = 90.0f;
		_timeCoord = Layout.Add( new CurveCoordinateProperty( "X", this )
		{
			FixedWidth = coordWidth,
			HighlightColor = Theme.Red
		} );

		_valueCoord = Layout.Add( new CurveCoordinateProperty( "Y", this )
		{
			FixedWidth = coordWidth,
			HighlightColor = Theme.Green
		} );

		_snapTimeButton = Layout.Add( new SnapButtonTime( this, TimeSnapOptions.Tenths )
		{
			VerticalSizeMode = SizeMode.CanGrow,
		} );

		_snapValueButton = Layout.Add( new SnapButtonValue( this, ValueSnapOptions.Gridlines )
		{
			VerticalSizeMode = SizeMode.CanGrow,
		} );

		////Layout.Add( new Label( "Pre-Infinity Extrapolation:", this ) );
		//_preInfinity = Layout.Add( new EnumProperty<AltCurve.Extrapolation>( this )
		//{
		//	Position = new( 150, 0 ),
		//	Size = new( 100, HEADER_HEIGHT ),
		//	MinimumWidth = 100
		//} );
		//
		////Layout.Add( new Label( "Post-Infinity Extrapolation:", this ) );
		//_postInfinity = Layout.Add( new EnumProperty<AltCurve.Extrapolation>( this )
		//{
		//	Position = new( 150, 0 ),
		//	Size = new( 100, HEADER_HEIGHT ),
		//	MinimumWidth = 100
		//} );

		_interpolation = Layout.Add( new InterpolationControl( this ) );

		_flattenButton = Layout.Add( new Button( "Flatten", "horizontal_rule", this )
		{
			ToolTip = "Flatten  (Shortcut: 6): Flatten the selected curve tangents.",
			VerticalSizeMode = SizeMode.CanGrow,
		} );

		Layout.AddStretchCell(); // Pad remaining width
	}

	/// <summary>
	/// Hook these toolbar controls onto the given curve widget properties
	/// </summary>
	internal void BindCurveControls( AltCurveEditor editor, EditableAltCurve curveWidget )
	{
		// Visiblity dropdown/binding
		_visibilityOptions.Pressed += () =>
		{
			var m = new Menu()
			{
				DeleteOnClose = true
			};

			m.AddHeading( "View Options" );

			{
				m.AddHeading( "Tangent Visibility" );

				var always = m.AddOption( "Always", null, () => curveWidget.ViewConfig = curveWidget.ViewConfig with { TangentMode = TangentViewMode.All } );
				always.Checkable = true;
				always.Checked = curveWidget.ViewConfig.TangentMode == TangentViewMode.All;

				var selected = m.AddOption( "Selected", null, () => curveWidget.ViewConfig = curveWidget.ViewConfig with { TangentMode = TangentViewMode.Selected } );
				selected.Checkable = true;
				selected.Checked = curveWidget.ViewConfig.TangentMode == TangentViewMode.Selected;

				var none = m.AddOption( "None", null, () => curveWidget.ViewConfig = curveWidget.ViewConfig with { TangentMode = TangentViewMode.None } );
				none.Checkable = true;
				none.Checked = curveWidget.ViewConfig.TangentMode == TangentViewMode.None;
			}
			m.AddSeparator();

			var showTooltip = new Option( "Show Curve Tooltip", null, () => curveWidget.ViewConfig = curveWidget.ViewConfig with { ShowCurveTooltip = !curveWidget.ViewConfig.ShowCurveTooltip } )
			{
				Checkable = true,
				Checked = curveWidget.ViewConfig.ShowCurveTooltip
			};
			m.AddOption( showTooltip );

			m.OpenAtCursor();
		};

		// Zoom to focus
		_zoomButton.Pressed += () => editor.ZoomToFit();

		// Time snap
		curveWidget.Bind( "SnapTimeEnabled" ).From( _snapTimeButton, "SnapEnabled" );
		curveWidget.Bind( "SnapTimeMode" ).From( _snapTimeButton, "CurrentSnapMode" );
		curveWidget.Bind( "SnapTimeCustom" ).From( _snapTimeButton, "CustomSnapValue" );
		_snapTimeButton.Bind( "ForcefullyDisabled" ).ReadOnly().From( curveWidget, "ForceDisableSnap" ); // Read-only reverse bind for alt-hold override feedback

		// Value snap
		curveWidget.Bind( "SnapValueEnabled" ).From( _snapValueButton, "SnapEnabled" );
		curveWidget.Bind( "SnapValueMode" ).From( _snapValueButton, "CurrentSnapMode" );
		curveWidget.Bind( "SnapValueCustom" ).From( _snapValueButton, "CustomSnapValue" );
		_snapValueButton.Bind( "ForcefullyDisabled" ).ReadOnly().From( curveWidget, "ForceDisableSnap" );  // Read-only reverse bind for alt-hold override feedback

		// // Pre-infinity
		// _preInfinity.Bind( "Value" ).From( () => curveWidget.RawCurve.PreInfinity, v => curveWidget.RawCurve = curveWidget.RawCurve with { PreInfinity = v } );
		// 
		// // Post-infinity
		// _postInfinity.Bind( "Value" ).From( () => curveWidget.RawCurve.PostInfinity, v => curveWidget.RawCurve = curveWidget.RawCurve with { PostInfinity = v });

		// Time coord
		_timeCoord.Bind( "SelectedKeyframes" ).ReadOnly().From( () => curveWidget.SelectionState.SelectedKeyframes, x => { } );
		_timeCoord.Bind( "SelectedValue" ).ReadOnly().From( () => curveWidget.SelectionState.SelectedTime, x => { } );
		_timeCoord.OnDraggingStart = () => curveWidget.PushUndoState( $"Translate {curveWidget.SelectionState.SelectedKeyframes} keyframe{(curveWidget.SelectionState.SelectedKeyframes > 1 ? "s" : "")}" );
		_timeCoord.OnDragging += ( amt ) => curveWidget.TranslateSelection( amt, 0.0f );
		_timeCoord.OnManualInput += ( val ) => curveWidget.SetSelectionTime( val );

		// Value coord
		_valueCoord.Bind( "SelectedKeyframes" ).ReadOnly().From( () => curveWidget.SelectionState.SelectedKeyframes, x => { } );
		_valueCoord.Bind( "SelectedValue" ).ReadOnly().From( () => curveWidget.SelectionState.SelectedValue, x => { } );
		_valueCoord.OnDraggingStart = () => curveWidget.PushUndoState( $"Translate {curveWidget.SelectionState.SelectedKeyframes} keyframe{(curveWidget.SelectionState.SelectedKeyframes > 1 ? "s" : "")}" );
		_valueCoord.OnDragging += ( amt ) => curveWidget.TranslateSelection( 0.0f, amt );
		_valueCoord.OnManualInput += ( val ) => curveWidget.SetSelectionValue( val );

		// Selected keyframe interpolation
		_interpolation.Bind( "HighlightedInterpolation" ).ReadOnly().From( curveWidget, "SelectedInterpolation" );
		_interpolation.Bind( "HasSelectedKeyframe" ).ReadOnly().From( curveWidget, "HasSelectedKeyframe" );
		_interpolation.SelectedInterp = (newTangentMode) => curveWidget.SetSelectionInterpolation( newTangentMode );

		// Flatten button
		//_flattenButton.Bind( "Enabled" ).ReadOnly().From( () => 
		//	curveWidget.SelectedInterpolation.HasValue && curveWidget.SelectedInterpolation.Value.Interp == AltCurve.Interpolation.Cubic,
		//	x => { } ); // Disable for now, allow flattening tangents even if we don't share an interpolation type
		_flattenButton.Bind( "Enabled" ).ReadOnly().From( curveWidget, "HasSelectedKeyframe" );
		_flattenButton.Pressed = () => curveWidget.SetSelectionTangentFlat();
	}
}
