namespace AltCurves.GraphicsItems;

public partial class EditableAltCurve
{
	/// <summary>
	/// Keyframes will be represented in a pair on the graph, one for dragging and one for visibility
	/// We use separate handles because the visible handle can be snapped to the grid, while the dragging handle is not.
	/// </summary>
	private record struct KeyPair
	{
		public DragHandle DragHandle { get; init; }
		public KeyVisible VisibleHandle { get; init; }
		public readonly int Index => VisibleHandle.Index;

		public AltCurve.Keyframe Keyframe
		{
			set
			{
				DragHandle.Keyframe = value;
				VisibleHandle.Keyframe = value;
			}
		}

		public readonly void Destroy()
		{
			DragHandle.Destroy();
			VisibleHandle.Destroy();
		}

		public readonly void SetTransform( CurveWidgetTransform curveTransform )
		{
			DragHandle.Transform = curveTransform;
			VisibleHandle.Transform = curveTransform;
		}
	}
}
