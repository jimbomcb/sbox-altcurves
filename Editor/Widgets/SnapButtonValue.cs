using Editor;
using System.Text.RegularExpressions;

namespace AltCurves.Widgets;

internal class SnapButtonValue : SnapButton<ValueSnapOptions>
{
	public SnapButtonValue( Widget parent, ValueSnapOptions currentSnapMode ) : base( parent, "Snap Value", "X", ValueSnapOptions.Custom, currentSnapMode )
	{
	}

	protected override string CustomValueRegexValidation => "^([0-9]+(?:\\.[0-9]+)?)?$";
	protected override string CustomValuePlaceholderString => "Format: 0.1, 100";
	protected override float ParseCustomValue( GroupCollection groupCollection ) => float.Parse( groupCollection[1].Value );
}
