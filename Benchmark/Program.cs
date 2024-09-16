using BenchmarkDotNet;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Running;
using Sandbox;
using static AltCurves.AltCurve;
using System.Linq;

namespace AltCurves.Benchmark
{
	internal class Program
	{
		static void Main( string[] args )
		{

			var summary = BenchmarkRunner.Run( typeof( Program ).Assembly );
		}
	}

	[Config( typeof( Config ) )]

	public class CurveBenchmark
	{
		private class Config : ManualConfig
		{
			public Config()
			{
				AddAnalyser( EnvironmentAnalyser.Default );
				//AddExporter( RPlotExporter.Default );
				//AddExporter( CsvMeasurementsExporter.Default );

				//WithOptions( ConfigOptions.DisableOptimizationsValidator );
			}
		}

		private List<Vector2> _positions = [];
		private List<float> _evals = [];

		private Curve _stockCurve;
		private AltCurve _altCurve;

		[Params( 1, 2, 5, 25, 50, 100, 1000 )]
		public int Keys { get; set; }

		[Params( 1 )]
		public int Samples { get; set; }

		public CurveBenchmark()
		{
		}

		[GlobalSetup]
		public void Setup()
		{
			// Generate a lot of random data
			var rand = new Random();
			for ( int i = 0; i < Keys; i++ )
			{
				_positions.Add( new( rand.Float( 1000 ), rand.Float( 1000 ) ) );
			}
			_positions = _positions.OrderBy( x => x.x ).ToList();

			for ( int i = 0; i < Samples; i++ )
			{
				_evals.Add( rand.Float( 1000 ) );
			}

			_stockCurve = new Curve( _positions.Select( x => {
				var randTangent = rand.Float( 10.0f );
				return new Curve.Frame( x.x, x.y, randTangent, -randTangent ); // Stock curve asset uses an inverted amount for the out tangent when flat
			} ).ToList() );

			_altCurve = new AltCurve( _positions.Select( x =>
			{
				var randTangent = rand.Float( 10.0f );
				return new AltCurve.Keyframe( x.x, x.y, Interpolation.Cubic, TangentMode.Automatic, randTangent, randTangent );
			} ), Extrapolation.Linear, Extrapolation.Linear );
		}

		[Benchmark( Baseline = true )]
		public void Curve()
		{
			foreach ( var entry in _evals )
			{
				_stockCurve.Evaluate( entry );
			}
		}

		[Benchmark]
		public void AltCurve()
		{
			foreach ( var entry in _evals )
			{
				_altCurve.Evaluate( entry );
			}
		}
	}
}
