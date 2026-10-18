using System.IO;

namespace TpacTool.Lib
{
	public sealed class AssetPackageLoadException : IOException
	{
		public string PackagePath { get; private set; }

		public AssetPackageLoadException(string packagePath, System.Exception innerException)
			: base("Failed to load TPAC package: " + packagePath, innerException)
		{
			PackagePath = packagePath;
		}
	}
}
