using System;
using System.Collections.Generic;
using TpacTool.IO.Assimp;
using TpacTool.Lib;

internal static class Program
{
	private static int Main()
	{
		ResolvesSecondMaterialWhenPrimaryIsMissing();
		UsesDefaultWhenNoMaterialResolves();
		IgnoresSecondMaterialWhenExporterDoesNotSupportIt();
		PrefersPrimaryMaterialWhenBothResolve();

		Console.WriteLine("Material assignment regression tests passed.");
		return 0;
	}

	private static void ResolvesSecondMaterialWhenPrimaryIsMissing()
	{
		var second = new Material { Name = "second" };
		var mesh = new Mesh
		{
			Material = new AssetDependence<Material>(Guid.NewGuid()),
			SecondMaterial = new AssetDependence<Material>(second)
		};
		var mapping = new Dictionary<Material, int> { [second] = 2 };

		AssertEqual(2, AbstractAssimpExporter.ResolveMeshMaterialIndex(mesh, mapping, true, 0),
			"an unresolved primary material must fall back to the second material");
	}

	private static void UsesDefaultWhenNoMaterialResolves()
	{
		var mesh = new Mesh
		{
			Material = new AssetDependence<Material>(Guid.NewGuid()),
			SecondMaterial = new AssetDependence<Material>(Guid.NewGuid())
		};
		var mapping = new Dictionary<Material, int>();

		AssertEqual(0, AbstractAssimpExporter.ResolveMeshMaterialIndex(mesh, mapping, true, 0),
			"missing material references must use the default material");
	}

	private static void PrefersPrimaryMaterialWhenBothResolve()
	{
		var primary = new Material { Name = "primary" };
		var second = new Material { Name = "second" };
		var mesh = new Mesh
		{
			Material = new AssetDependence<Material>(primary),
			SecondMaterial = new AssetDependence<Material>(second)
		};
		var mapping = new Dictionary<Material, int>
		{
			[primary] = 1,
			[second] = 2
		};

		AssertEqual(1, AbstractAssimpExporter.ResolveMeshMaterialIndex(mesh, mapping, true, 0),
			"the primary material must keep precedence when it resolves");
	}

	private static void IgnoresSecondMaterialWhenExporterDoesNotSupportIt()
	{
		var second = new Material { Name = "second" };
		var mesh = new Mesh
		{
			Material = new AssetDependence<Material>(Guid.NewGuid()),
			SecondMaterial = new AssetDependence<Material>(second)
		};
		var mapping = new Dictionary<Material, int> { [second] = 2 };

		AssertEqual(0, AbstractAssimpExporter.ResolveMeshMaterialIndex(mesh, mapping, false, 0),
			"exporters without second-material support must use the default");
	}

	private static void AssertEqual(int expected, int actual, string message)
	{
		if (expected != actual)
			throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
	}
}
