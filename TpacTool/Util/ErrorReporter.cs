using System;
using System.IO;
using System.Windows;
using TpacTool.Lib;
using TpacTool.Properties;

namespace TpacTool
{
	internal static class ErrorReporter
	{
		public static string GetResourceString(string key)
		{
			return Resources.ResourceManager.GetString(key, Resources.Culture) ?? key;
		}

		public static void Show(string operation, string item, string path, Exception error)
		{
			var causeKey = "ErrorCause_" + AssetLoadErrorDiagnostics.Classify(error);
			var likelyCause = Resources.ResourceManager.GetString(causeKey, Resources.Culture);
			if (string.IsNullOrWhiteSpace(likelyCause))
				likelyCause = error.GetType().Name;

			var message = string.Format(GetResourceString("Msgbox_OperationFailed"),
				operation,
				string.IsNullOrWhiteSpace(item) ? GetResourceString("Error_UnknownItem") : item,
				string.IsNullOrWhiteSpace(path) ? GetResourceString("Error_UnknownPath") : path,
				likelyCause,
				AssetLoadErrorDiagnostics.FormatExceptionChain(error));

			MessageBox.Show(message, Resources.Msgbox_Error,
				MessageBoxButton.OK, MessageBoxImage.Error);
		}

		public static string GetPackagePath(Exception error, string fallback)
		{
			var packageError = error as AssetPackageLoadException;
			return packageError != null ? packageError.PackagePath : fallback;
		}

		public static string GetItemName(string path)
		{
			return string.IsNullOrWhiteSpace(path) ? GetResourceString("Error_UnknownItem") : Path.GetFileName(path);
		}
	}
}
