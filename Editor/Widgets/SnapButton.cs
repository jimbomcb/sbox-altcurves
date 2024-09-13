using Editor;
using Sandbox;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AltCurves.Widgets;

/// <summary>
/// Button that controls snapping of time/values, consists of a main toggle button and a dropdown that customizes the snap value
/// </summary>
internal abstract class SnapButton<T> : Widget
{
	/// <summary>
	/// Overall snap toggle state
	/// </summary>
	public bool SnapEnabled
	{
		get { return _snapEnabled; }
		set { _snapEnabled = value; BuildButtonLayout(); }
	}
	private bool _snapEnabled = true;

	/// <summary>
	/// True if this snapping is forcefully disabled via alt-key
	/// </summary>
	public bool ForcefullyDisabled
	{
		get { return _forcefullyDisabled; }
		set { _forcefullyDisabled = value; BuildButtonLayout(); }
	}
	private bool _forcefullyDisabled = false;

	/// <summary>
	/// Selected snap mode
	/// </summary>
	public T CurrentSnapMode { get; set; }

	/// <summary>
	/// The user-provided snap value if we're in custom mode.
	/// </summary>
	public float CustomSnapValue { get; set; }

	private T _customSnapMode;

	/// <summary>
	/// Custom values must match this regex pattern
	/// </summary>
	protected abstract string CustomValueRegexValidation { get; }

	/// <summary>
	/// Placeholder string for custom value input
	/// </summary>
	protected abstract string CustomValuePlaceholderString { get; }

	private Button _snapToggle;
	private Button _showMore;
	private readonly string _text;
	private readonly string _shortcut;

	public SnapButton( Widget parent, string text, string shortcut, T customValue, T defaultValue ) : base( parent )
	{
		MinimumWidth = 80;
		Cursor = CursorShape.Finger;

		CurrentSnapMode = defaultValue;
		CustomSnapValue = 1.0f;
		_customSnapMode = customValue;

		_text = text;
		_shortcut = shortcut;

		Enabled = true;

		Layout = Layout.Row();
		BuildButtonLayout();
	}

	protected void BuildButtonLayout()
	{
		// Not pretty, but Button doesn't respond to changes in styles, and I don't like the default togglebutton, so I'll just toggle between primary/unstyled for now.
		Layout.Clear( true );
		Layout.Add( _snapToggle = new Button( _text )
		{
			VerticalSizeMode = SizeMode.CanGrow,
			ButtonType = SnapEnabled && !ForcefullyDisabled ? "primary" : "",
			ToolTip = $"Toggle {_text} (Shortcut: {_shortcut})"
		} );
		_snapToggle.MouseClick += () => SnapEnabled = !SnapEnabled;

		Layout.Add( _showMore = new Button( "", "more_vert" )
		{
			FixedWidth = 30.0f,
			VerticalSizeMode = SizeMode.CanGrow,
			ToolTip = $"Choose {_text} precision"
		} );
		_showMore.MouseClick += ClickedShowMore;
	}

	/// <summary>
	/// Create the dropdown menu for selecting snap amounts
	/// </summary>
	protected void ClickedShowMore()
	{
		var m = new Menu()
		{
			DeleteOnClose = true
		};

		m.AddHeading( $"{_text} Precision:" );

		var enumValues = typeof( T ).GetEnumValues();
		var enumInfo = DisplayInfo.ForEnumValues( typeof( T ) );
		for ( int i = 0; i < enumValues.Length; i++ )
		{
			var value = enumValues.GetValue( i );
			if ( value.Equals( _customSnapMode ) )
				continue;

			var option = m.AddOption( new Option( enumInfo[i].Name )
			{
				Checkable = true,
				Checked = value.Equals( CurrentSnapMode )
			} );
			option.Triggered += () =>
			{
				CurrentSnapMode = (T)value;
				SnapEnabled = true; // Re-enable disabled snap on option selection
			};
		}

		// Separate custom button logic
		m.AddSeparator();
		var customHeader = m.AddOption( new Option( "Custom:" )
		{
			Checkable = true,
			Checked = EqualityComparer<T>.Default.Equals( CurrentSnapMode, _customSnapMode )
		} );
		customHeader.Triggered += () =>
		{
			CurrentSnapMode = _customSnapMode;
			SnapEnabled = true; // Re-enable disabled snap on option selection
		};
		var snapLine = m.AddWidget( new ContextMenuLineEdit( this )
		{
			PlaceholderText = CustomValuePlaceholderString,
			RegexValidator = CustomValueRegexValidation
		} );
		snapLine.ReturnPressed += () =>
		{
			var match = Regex.Match( snapLine.Text, CustomValueRegexValidation );
			if ( match.Success )
			{
				CustomSnapValue = ParseCustomValue( match.Groups );
				CurrentSnapMode = _customSnapMode;
				SnapEnabled = true; // Re-enable disabled snap on option selection
				m.Close();
			}
			else
			{
				Log.Warning( $"Ignoring '{snapLine.Text}' not matching {CustomValueRegexValidation} regex" );
			}
		};

		if ( EqualityComparer<T>.Default.Equals( CurrentSnapMode, _customSnapMode ) )
			snapLine.Text = CustomValueString();

		m.OpenAt( Editor.Application.CursorPosition );
	}

	protected abstract float ParseCustomValue( GroupCollection groupCollection );
	protected virtual string CustomValueString() => $"{CustomSnapValue}";
}
