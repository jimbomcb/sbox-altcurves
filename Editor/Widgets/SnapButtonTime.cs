using Editor;
using System.Text.RegularExpressions;

namespace AltCurves.Widgets;

internal class SnapButtonTime : SnapButton<TimeSnapOptions>
{
	public SnapButtonTime( Widget parent, TimeSnapOptions currentSnapMode ) : base( parent, "Snap Time", "Z", TimeSnapOptions.Custom, currentSnapMode )
	{
	}

	/// <summary>
	/// Times like 1s, 0.1s, 100ms, 1.1s. Seconds if no unit specified.
	/// </summary>
	protected override string CustomValueRegexValidation => "(?i)^([0-9]+(?:\\.[0-9]+)?)(s|ms)?$";
	protected override string CustomValuePlaceholderString => "Format: 0.1s, 100ms";

	protected override float ParseCustomValue( GroupCollection groupCollection )
	{
		float seconds = float.Parse( groupCollection[1].Value );
		if ( groupCollection[2].Success && groupCollection[2].Value == "ms" ) // Only s/ms input supported, already in seconds
		{
			seconds /= 1000.0f;
		}
		return seconds;
	}
	protected override string CustomValueString() => $"{CustomSnapValue}s"; // Append seconds
}
