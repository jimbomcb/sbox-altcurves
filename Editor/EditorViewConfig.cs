namespace AltCurves;

/// <summary>
/// User-defined view settings controlling things like tangent visibility.
/// </summary>
public readonly record struct EditorViewConfig
{
	/// <summary>
	/// How do we handle tangent visiblity, ie do we only show when selected to reduce clutter
	/// </summary>
	public TangentViewMode TangentMode { get; init; }

	/// <summary>
	/// Do we show the time/value tooltip when hoving over the curve?
	/// </summary>
	public bool ShowCurveTooltip { get; init; }

	public EditorViewConfig()
	{
		TangentMode = TangentViewMode.All;
		ShowCurveTooltip = true;
	}
}
