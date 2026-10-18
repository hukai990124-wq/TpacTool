using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
		ReportsMalformedPackageErrorsWithCause();
		ClassifiesCommonLoadErrors();

		Console.WriteLine("Regression tests passed.");
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

	private static void ReportsMalformedPackageErrorsWithCause()
	{
		var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "TpacTool-load-error-" + Guid.NewGuid().ToString("N")));
		directory.Create();
		var packagePath = Path.Combine(directory.FullName, "broken.tpac");
		File.WriteAllBytes(packagePath, Array.Empty<byte>());

		try
		{
			var manager = new AssetManager();
			Exception loadError = null;
			using (var completed = new ManualResetEventSlim())
			{
				manager.Load(directory, (package, packageCount, fileName, isCompleted) =>
				{
					if (isCompleted)
					{
						loadError = manager.LoadError;
						completed.Set();
					}
					return true;
				});

				if (!completed.Wait(TimeSpan.FromSeconds(15)))
					throw new InvalidOperationException("The loader did not report completion after the package failed.");
			}

			var packageError = loadError as AssetPackageLoadException;
			if (packageError == null)
				throw new InvalidOperationException("The loader must expose the package error instead of terminating its worker thread.");
			if (!string.Equals(packagePath, packageError.PackagePath, StringComparison.OrdinalIgnoreCase))
				throw new InvalidOperationException("The error must identify the package that failed to load.");
			if (!(packageError.InnerException is EndOfStreamException))
				throw new InvalidOperationException("The error must preserve the underlying truncation cause.");
			AssertEqual(AssetLoadErrorKind.TruncatedOrCorrupt,
				AssetLoadErrorDiagnostics.Classify(loadError),
				"a truncated TPAC must be diagnosed as truncated or corrupt");
		}
		finally
		{
			directory.Delete(true);
		}
	}

	private static void ClassifiesCommonLoadErrors()
	{
		AssertEqual(AssetLoadErrorKind.InvalidData,
			AssetLoadErrorDiagnostics.Classify(new InvalidDataException("invalid TPAC signature")),
			"invalid signatures must be reported as invalid data");
		AssertEqual(AssetLoadErrorKind.UnsupportedFormat,
			AssetLoadErrorDiagnostics.Classify(new NotSupportedException("unsupported TPAC version")),
			"unsupported package versions must be reported as unsupported formats");
		AssertEqual(AssetLoadErrorKind.MissingDependency,
			AssetLoadErrorDiagnostics.Classify(new UnresolvedDependenceException<AssetItem>()),
			"unresolved asset references must be reported as missing dependencies");

		var details = AssetLoadErrorDiagnostics.FormatExceptionChain(
			new IOException("package load failed", new EndOfStreamException("unexpected end of file")));
		if (!details.Contains("package load failed") || !details.Contains("unexpected end of file"))
			throw new InvalidOperationException("The diagnostic details must preserve the complete exception chain.");
	}

	private static void AssertEqual<T>(T expected, T actual, string message)
	{
		if (!EqualityComparer<T>.Default.Equals(expected, actual))
			throw new InvalidOperationException($"{message}: expected {expected}, got {actual}");
	}
}
