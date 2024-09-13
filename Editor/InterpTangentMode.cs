using static AltCurves.AltCurve;

namespace AltCurves;

public record struct InterpTangentMode( Interpolation Interp, TangentMode Tangent = TangentMode.Automatic );
