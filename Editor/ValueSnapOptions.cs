namespace AltCurves;

/// <summary>
/// User options for snapping values to a grid
/// </summary>
public enum ValueSnapOptions
{
	[Title( "0.1" )]
	Tenth,
	[Title( "0.5" )]
	Half,
	[Title( "1" )]
	One,
	[Title( "2" )]
	Two,
	[Title( "5" )]
	Five,
	[Title( "10" )]
	Ten,
	[Title( "50" )]
	Fifty,
	[Title( "100" )]
	Hundred,
	[Title( "Snap-To-Gridline" )]
	Gridlines,
	[Title( "User-provided amount" )]
	Custom,
};

