using AltCurves;
using Sandbox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using static AltCurves.AltCurve;

namespace AltCurves.Tests;

[TestClass]
public partial class AltCurveTests
{
	[TestMethod]
	public void KeyframeEquality()
	{
		// This is all overkill but if I'm going to test curves, I want to be certain this is acting like it should
		var keyframe1 = new Keyframe( 0.0f, 10.0f, Interpolation.Linear );
		var keyframe2 = new Keyframe( 0.0f, 10.0f, Interpolation.Linear );
		Assert.AreEqual( keyframe1, keyframe2, "Keyframes with the same values should be equal." );
		Assert.AreEqual( keyframe1.GetHashCode(), keyframe2.GetHashCode(), "Keyframes with the same values should have the same hash code." );

		var keyframe3 = new Keyframe( 1.0f, 10.0f, Interpolation.Linear );
		Assert.AreNotEqual( keyframe1, keyframe3, "Keyframes with different times should not be equal." );
		Assert.AreNotEqual( keyframe1.GetHashCode(), keyframe3.GetHashCode(), "Keyframes with different times should have different hash codes." );

		var keyframe4 = new Keyframe( 0.0f, 20.0f, Interpolation.Linear );
		Assert.AreNotEqual( keyframe1, keyframe4, "Keyframes with different values should not be equal." );
		Assert.AreNotEqual( keyframe1.GetHashCode(), keyframe4.GetHashCode(), "Keyframes with different values should have different hash codes." );

		var keyframe5 = new Keyframe( 0.0f, 10.0f, Interpolation.Cubic );
		Assert.AreNotEqual( keyframe1, keyframe5, "Keyframes with different interpolation should not be equal." );
		Assert.AreNotEqual( keyframe1.GetHashCode(), keyframe5.GetHashCode(), "Keyframes with different interpolation should have different hash codes." );

		var keyframe6 = new Keyframe( 0.0f, 10.0f, Interpolation.Linear, TangentMode.Mirrored, 1.0f, 1.0f );
		var keyframe7 = new Keyframe( 0.0f, 10.0f, Interpolation.Linear, TangentMode.Mirrored, 1.0f, 1.0f );
		Assert.AreEqual( keyframe6, keyframe7, "Keyframes with the same tangent settings should be equal." );
		Assert.AreEqual( keyframe6.GetHashCode(), keyframe7.GetHashCode(), "Keyframes with the same tangent settings should have the same hash code." );

		var keyframe8 = new Keyframe( 0.0f, 10.0f, Interpolation.Linear, TangentMode.Split, 1.0f, 1.0f );
		Assert.AreNotEqual( keyframe6, keyframe8, "Keyframes with different tangent modes should not be equal." );
		Assert.AreNotEqual( keyframe6.GetHashCode(), keyframe8.GetHashCode(), "Keyframes with different tangent modes should have different hash codes." );
	}

	[TestMethod]
	public void AltCurveEquality()
	{
		var curve1 = new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Constant );
		var curve2 = new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreEqual( curve1, curve2 );

		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Linear ) }, Extrapolation.Linear, Extrapolation.Constant ) );
		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Linear ) );
		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Linear ) }, Extrapolation.Linear, Extrapolation.Linear ) );
		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe> { new( 1.0f, 10.0f, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Constant ) );
		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe> { new( 0.0f, 20.0f, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Constant ) );
		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Constant ) }, Extrapolation.Constant, Extrapolation.Constant ) );
		Assert.AreNotEqual( curve1, new AltCurve( new List<Keyframe>
			{
				new(0.0f, 10.0f, Interpolation.Linear),
				new(1.0f, 15.0f, Interpolation.Linear)
			}, Extrapolation.Constant, Extrapolation.Constant ) );

		var cubicCurve1 = new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Cubic, TangentMode.Mirrored, 1.0f, 1.0f ) }, Extrapolation.Constant, Extrapolation.Constant );
		var cubicCurve2 = new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Cubic, TangentMode.Split, 1.0f, 1.0f ) }, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreNotEqual( cubicCurve1, cubicCurve2 );

		var cubicCurve3 = new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Cubic, TangentMode.Mirrored, 2.0f, 1.0f ) }, Extrapolation.Constant, Extrapolation.Constant );
		var cubicCurve4 = new AltCurve( new List<Keyframe> { new( 0.0f, 10.0f, Interpolation.Cubic, TangentMode.Mirrored, 1.0f, 2.0f ) }, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreNotEqual( cubicCurve3, cubicCurve4 );

		var cubicCurve5 = new AltCurve( new List<Keyframe>
			{
				new(0.0f, 10.0f, Interpolation.Cubic, TangentMode.Split, 1.0f, 2.0f),
				new(1.0f, 20.0f, Interpolation.Cubic, TangentMode.Split, 2.0f, 1.0f)
			}, Extrapolation.Constant, Extrapolation.Constant );
		var cubicCurve6 = new AltCurve( new List<Keyframe>
			{
				new(0.0f, 10.0f, Interpolation.Cubic, TangentMode.Mirrored, 1.0f, 1.0f),
				new(1.0f, 20.0f, Interpolation.Cubic, TangentMode.Mirrored, 1.0f, 1.0f)
			}, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreNotEqual( cubicCurve5, cubicCurve6 );
	}

	[TestMethod]
	public void CurveConstruct()
	{
		var curve = new AltCurve();
		Assert.AreEqual( 0, curve.Keyframes.Length );
		Assert.AreEqual( Extrapolation.Constant, curve.PreInfinity );
		Assert.AreEqual( Extrapolation.Constant, curve.PostInfinity );
	}

	[TestMethod]
	public void SingleKeyframe()
	{
		var curve = new AltCurve( new List<Keyframe> { new() { Time = 0.0f, Value = 10.0f, Interpolation = Interpolation.Linear } }, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreEqual( 10, curve.Evaluate( 0 ) );
		Assert.AreEqual( 10, curve.Evaluate( 1 ) );
		Assert.AreEqual( 10, curve.Evaluate( -1 ) );
	}

	[TestMethod]
	public void MultipleKeyframes()
	{
		var curve = new AltCurve( new List<Keyframe>
			{
				new(0, 10, Interpolation.Linear),
				new(10, 20, Interpolation.Linear)
			}, Extrapolation.Constant, Extrapolation.Constant );

		Assert.AreEqual( 10, curve.Evaluate( 0 ) );
		Assert.AreEqual( 15, curve.Evaluate( 5 ) );
		Assert.AreEqual( 20, curve.Evaluate( 10 ) );
	}

	[TestMethod]
	public void ExtrapolationBeforeFirstKeyframe()
	{
		var curve = new AltCurve( new List<Keyframe> { new( 10, 20, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreEqual( 20, curve.Evaluate( 5 ) );
	}

	[TestMethod]
	public void ExtrapolationAfterLastKeyframe()
	{
		var curve = new AltCurve( new List<Keyframe> { new( 10, 20, Interpolation.Linear ) }, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreEqual( 20, curve.Evaluate( 15 ) );
	}

	[TestMethod]
	public void TestInterpolation()
	{
		var curve = new AltCurve( new List<Keyframe>
			{
				new(0, 10, Interpolation.Linear),
				new(10, 20, Interpolation.Linear)
			}, Extrapolation.Constant, Extrapolation.Constant );

		Assert.AreEqual( 15, curve.Evaluate( 5 ) );
	}

	[TestMethod]
	public void CubicInterpolation()
	{
		var curve = new AltCurve( new List<Keyframe>
		{
			new(0, 0, Interpolation.Cubic, TangentMode.Mirrored, 0, 0),
			new(1, 1, Interpolation.Cubic, TangentMode.Mirrored, 1, 1),
			new(2, 0, Interpolation.Cubic, TangentMode.Mirrored, 0, 0)
		}, Extrapolation.Constant, Extrapolation.Constant );

		Assert.AreEqual( 0.0f, curve.Evaluate( 0.0f ), 0.01f );
		Assert.AreEqual( 0.343f, curve.Evaluate( 0.5f ), 0.01f );
		Assert.AreEqual( 1.0f, curve.Evaluate( 1.0f ), 0.01f );
		Assert.AreEqual( 0.656f, curve.Evaluate( 1.5f ), 0.01f );
		Assert.AreEqual( 0.0f, curve.Evaluate( 2.0f ), 0.01f );
	}

	[TestMethod]
	public void JsonSerialization()
	{
		var keyframes = new List<Keyframe>
			{
				new Keyframe(0, 0, Interpolation.Linear),
				new Keyframe(1, 10, Interpolation.Linear)
			};
		var curve = new AltCurve( keyframes, Extrapolation.Linear, Extrapolation.Linear );

		var json = Json.Serialize( curve );
		var deserializedCurve = Json.Deserialize<AltCurve>( json );

		Assert.AreEqual( curve, deserializedCurve );
	}

	[TestMethod]
	public void DeserializeSerializeConsistency()
	{
		// I'm paranoid about weird data issues causing differences... can you tell?

		var data = """{"_ace_v":1,"pri":"Oscillate","poi":"CycleOffset","keys":[{"x":-491.8,"y":-1625},{"x":-398.1,"y":-250,"ti":3.3167496,"to":3.3167496},{"x":-190.3,"y":-625,"ti":1.8491124,"to":1.8491124},{"x":-60.1,"y":375,"i":"Constant","ti":-4.2390842,"to":-4.2390842},{"x":45.6,"y":-1625,"ti":-3.0151618,"to":-3.0151618},{"x":230.1,"y":-500,"ti":130.92972,"to":130.92972,"tm":"Mirrored"},{"x":335,"y":-1250,"ti":-0.5399568,"to":-0.5399568},{"x":461.6,"y":-625,"ti":-6.6666665,"to":-6.6666665},{"x":522.5,"y":-2500,"ti":-4.6159525,"to":-4.6159525},{"x":732.4,"y":-1875}]}""";

		AltCurve expectedCurve = new( new List<Keyframe>
		{
			new() { Time = -491.8f, Value = -1625f },
			new() { Time = -398.1f, Value = -250f, TangentIn = 3.3167496f, TangentOut = 3.3167496f },
			new() { Time = -190.3f, Value = -625f, TangentIn = 1.8491124f, TangentOut = 1.8491124f },
			new() { Time = -60.1f, Value = 375f, Interpolation = Interpolation.Constant, TangentIn = -4.2390842f, TangentOut = -4.2390842f },
			new() { Time = 45.6f, Value = -1625f, TangentIn = -3.0151618f, TangentOut = -3.0151618f },
			new() { Time = 230.1f, Value = -500f, Interpolation = Interpolation.Cubic, TangentMode = TangentMode.Mirrored, TangentIn = 130.92972f, TangentOut = 130.92972f },
			new() { Time = 335f, Value = -1250f, TangentIn = -0.5399568f, TangentOut = -0.5399568f },
			new() { Time = 461.6f, Value = -625f, TangentIn = -6.6666665f, TangentOut = -6.6666665f },
			new() { Time = 522.5f, Value = -2500f, TangentIn = -4.6159525f, TangentOut = -4.6159525f },
			new() { Time = 732.4f, Value = -1875f }
		}, Extrapolation.Oscillate, Extrapolation.CycleOffset );

		AltCurve deserializedCurve = Json.Deserialize<AltCurve>( data );
		Assert.AreEqual( expectedCurve, deserializedCurve );

		string serializedCurve = Json.Serialize( deserializedCurve );
		AltCurve reDeserializedCurve = Json.Deserialize<AltCurve>( serializedCurve );

		Assert.AreEqual( expectedCurve, reDeserializedCurve );
	}

	[TestMethod]
	public void ExtrapolationTests()
	{
		var keyframes = new List<Keyframe>
		{
			new(0, 0, Interpolation.Cubic, TangentMode.Mirrored, 0, 0),
			new(1, 2, Interpolation.Cubic, TangentMode.Mirrored, 1, 1),
			new(2, -1, Interpolation.Cubic, TangentMode.Mirrored, -1, -1),
			new(3, 3, Interpolation.Cubic, TangentMode.Mirrored, 2, 2)
		};

		// Validate the functionality of each extrapolation type

		var constantCurve = new AltCurve( keyframes, Extrapolation.Constant, Extrapolation.Constant );
		Assert.AreEqual( 0.0f, constantCurve.Evaluate( -1.0f ), 0.01f, "Constant extrapolation should use the first keyframe value before the range." );
		Assert.AreEqual( 3.0f, constantCurve.Evaluate( 4.0f ), 0.01f, "Constant extrapolation should use the last keyframe value after the range." );

		var linearCurve = new AltCurve( keyframes, Extrapolation.Linear, Extrapolation.Linear );
		Assert.AreEqual( -2.0f, linearCurve.Evaluate( -1.0f ), 0.01f, "Linear extrapolation should extrapolate linearly before the range." );
		Assert.AreEqual( 7.0f, linearCurve.Evaluate( 4.0f ), 0.01f, "Linear extrapolation should extrapolate linearly after the range." );

		var cycleCurve = new AltCurve( keyframes, Extrapolation.Cycle, Extrapolation.Cycle );
		Assert.AreEqual( -1.0f, cycleCurve.Evaluate( -1.0f ), 0.01f, "Cycle extrapolation should repeat the pattern before the range." );
		Assert.AreEqual( 0.55, cycleCurve.Evaluate( -0.5f ), 0.01f, "Cycle extrapolation should repeat the pattern before the range." );
		Assert.AreEqual( 0.85f, cycleCurve.Evaluate( 3.5f ), 0.01f, "Cycle extrapolation should repeat the pattern after the range." );
		Assert.AreEqual( 2.0f, cycleCurve.Evaluate( 4.0f ), 0.01f, "Cycle extrapolation should repeat the pattern after the range." );

		var cycleOffsetCurve = new AltCurve( keyframes, Extrapolation.CycleOffset, Extrapolation.CycleOffset );
		Assert.AreEqual( -1233.0f, cycleOffsetCurve.Evaluate( -1233.0f ), 0.01f, "CycleOffset extrapolation should accumulate offset before the range." );
		Assert.AreEqual( -19.0f, cycleOffsetCurve.Evaluate( -20.0f ), 0.01f, "CycleOffset extrapolation should accumulate offset before the range." );
		Assert.AreEqual( -4.0f, cycleOffsetCurve.Evaluate( -1.0f ), 0.01f, "CycleOffset extrapolation should accumulate offset before the range." );
		Assert.AreEqual( -2.452f, cycleOffsetCurve.Evaluate( -0.5f ), 0.01f, "CycleOffset extrapolation should accumulate offset before the range." );
		Assert.AreEqual( 3.851f, cycleOffsetCurve.Evaluate( 3.5f ), 0.01f, "CycleOffset extrapolation should accumulate offset before the range." );
		Assert.AreEqual( 5.0f, cycleOffsetCurve.Evaluate( 4.0f ), 0.01f, "CycleOffset extrapolation should accumulate offset after the range." );
		Assert.AreEqual( 8.0f, cycleOffsetCurve.Evaluate( 7.0f ), 0.01f, "CycleOffset extrapolation should accumulate offset after the range." );
		Assert.AreEqual( 5.0f, cycleOffsetCurve.Evaluate( 8.0f ), 0.01f, "CycleOffset extrapolation should accumulate offset after the range." );

		var oscillateCurve = new AltCurve( keyframes, Extrapolation.Oscillate, Extrapolation.Oscillate );
		Assert.AreEqual( 2.0f, oscillateCurve.Evaluate( -1.0f ), 0.01f, "Oscillate extrapolation should oscillate the pattern before the range." );
		Assert.AreEqual( -1.0f, oscillateCurve.Evaluate( 4.0f ), 0.01f, "Oscillate extrapolation should oscillate the pattern after the range." );
		Assert.AreEqual( 2.0f, oscillateCurve.Evaluate( 5.0f ), 0.01f, "Oscillate extrapolation should oscillate the pattern after the range." );
		Assert.AreEqual( 2.0f, oscillateCurve.Evaluate( 1621297.0f ), 0.01f, "Oscillate extrapolation should oscillate the pattern after the range." );
	}

	//[TestMethod]
	public void TimeTest1()
	{
		// Generate 1000 random x/y positions and 1000 random sample positions,
		// We will use the stock sandbox curve, and our alt curve with the same input data and compare performance

		var rand = new Random();

		var positions = new List<Vector2>();
		for( int i = 0; i < 1; i++ )
		{
			positions.Add( new Vector2( Random.Shared.Float( -1000, 1000 ), Random.Shared.Float( -1000, 1000 ) ) );
		}

		var orderedPos = positions.OrderBy( x => x.x );

		var altCurve = new AltCurve( orderedPos.Select( x => new Keyframe( x.x, x.y, Interpolation.Linear ) ).ToList(), Extrapolation.Constant, Extrapolation.Constant );
		var baseCurve = new Curve( orderedPos.Select(x=> new Curve.Frame(x.x, x.y)).ToList() );

		var times = new List<float>();
		for ( int i = 0; i < 1000; i++ )
			times.Add( Random.Shared.Float( -1000, 1000 ) );

		var baseTime = new Stopwatch();
		var ourTime = new Stopwatch();

		var baseTimeSamples = new List<long>();
		var ourTimeSamples = new List<long>();

		for ( int i = 0; i < 10000; i++)
		{
			ourTime.Restart();
			foreach ( var time in times ) altCurve.Evaluate( time );
			ourTime.Stop();

			baseTime.Restart();
			foreach ( var time in times ) baseCurve.Evaluate( time );
			baseTime.Stop();

			baseTimeSamples.Add( baseTime.ElapsedTicks );
			ourTimeSamples.Add( ourTime.ElapsedTicks );
		}

		var baseAverage = baseTimeSamples.Sum() / baseTimeSamples.Count;
		var ourAverage = ourTimeSamples.Sum() / ourTimeSamples.Count;

		Console.WriteLine( $"OurTime: {ourAverage}" );
		Console.WriteLine( $"TheirTime: {baseAverage}" );
		Console.WriteLine(
			$"OurTime is {baseAverage / (float)ourAverage}x faster than TheirTime" );

		// rough preliminary tests shows that our curves are more performant
		//								OurTime		TheirTime   
		// With 1 key, 1000 evals:		30			653			21x faster
		//								32			676			21x faster
		//								33			660			21x faster
		//								32			700			21x faster
		// With 10 key, 1000 evals:		521			2121		4.0x faster
		//								526			2338		4.4x faster
		//								546			2394		4.4x faster
		//								540			1973		3.6x faster
		// With 50 key, 1000 evals:		704			4169		5.9x faster
		// 								735			4998		6.8x faster
		// 								728			4696		6.4x faster
		// 								710			4959		6.4x faster
		// With 1000 key, 1000 evals:	1511		189953		125x faster
		//								1560		188484		120x faster
		//								1653		189336		114x faster
		//								1500		194617		129x faster
		//								1657		218923		132x faster
	}
}
