using System.Collections.Generic;

namespace AltCurves;

public readonly partial record struct AltCurve
{
	/// <summary>
	/// IComparer comparing keyframes by their time/x axis value
	/// </summary>
	public class KeyframeTimeComparer : IComparer<Keyframe>
	{
		public int Compare( Keyframe x, Keyframe y )
		{
			return x.Time.CompareTo( y.Time );
		}
	}
}
