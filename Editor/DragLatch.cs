using System;

namespace AltCurves;

/// <summary>
/// A little state machine helping with dragging operations. 
/// The 2D accumulator will accumulate mouse movement deltas until a threshold is reached,
/// at which point it will latch to the nearest cardinal direction.
/// </summary>
internal class DragLatch
{
	internal enum LatchState
	{
		Inactive,
		Accumulating,
		Latched
	}
	public LatchState State { get; private set; }

	/// <summary>
	/// Once we're Latched, this is the direction along which movement should be constrained
	/// </summary>
	public Vector2 LatchDirection { get; private set; }

	/// <summary>
	/// Pixel distance before we choose the snap direction
	/// </summary>
	private const float MOUSE_LATCH_THRESHOLD = 15.0f;
	private Vector2 _accumMouse;

	/// <summary>
	/// Dragging ended, reset any ongoing latch state
	/// </summary>
	internal void Reset()
	{
		State = LatchState.Inactive;
	}

	/// <summary>
	/// Dragging begun
	/// </summary>
	internal void Start()
	{
		State = LatchState.Accumulating;
		_accumMouse = Vector2.Zero;	
	}

	/// <summary>
	/// Accumulate the mouse position delta, transition into latched when ready
	/// </summary>
	internal void Accumulate( Vector2 positionDelta )
	{
		if ( State == LatchState.Accumulating )
		{
			_accumMouse += positionDelta;

			// Going over a distance threshold results in snapping to the nearest cardinal direction
			if ( _accumMouse.Length > MOUSE_LATCH_THRESHOLD )
			{
				State = LatchState.Latched;

				if (Math.Abs( _accumMouse.x ) > Math.Abs( _accumMouse.y ) )
				{
					LatchDirection = Vector2.Right;
				}
				else
				{
					LatchDirection = Vector2.Up;
				}
			}
		}
	}
}
