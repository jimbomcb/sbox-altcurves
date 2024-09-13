namespace AltCurves;

/// <summary>
/// User options for snapping time to a grid
/// </summary>
public enum TimeSnapOptions
{
	[Title( "0.01s" )]
	Hundredths,
	[Title( "0.1s" )]
	Tenths,
	[Title( "0.25s" )]
	Quarters,
	[Title( "0.5s" )]
	Halfs,
	[Title( "1.0s" )]
	Seconds,
	[Title( "10.0s" )]
	TenSeconds,
	[Title( "60.0s" )]
	OneMinute,
	[Title( "Snap-To-Gridline" )]
	Gridlines,
	[Title( "User-provided amount" )]
	Custom,
};

