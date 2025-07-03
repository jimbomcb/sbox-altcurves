global using Microsoft.VisualStudio.TestTools.UnitTesting;
using AltCurves.Tests;

[TestClass]
public class TestInit
{
	[AssemblyInitialize]
	public static void ClassInitialize( TestContext context )
	{
		Sandbox.Application.InitUnitTest<AltCurveTests>();
	}
}
