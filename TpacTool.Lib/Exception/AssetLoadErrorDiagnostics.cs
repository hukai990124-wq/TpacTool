using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace TpacTool.Lib
{
	public enum AssetLoadErrorKind
	{
		TruncatedOrCorrupt,
		InvalidData,
		UnsupportedFormat,
		AccessDenied,
		MissingFile,
		FileSystem,
		MissingDependency,
		Unknown
	}

	public static class AssetLoadErrorDiagnostics
	{
		public static AssetLoadErrorKind Classify(Exception exception)
		{
			var rootCause = GetRootCause(exception);
			if (rootCause == null)
				return AssetLoadErrorKind.Unknown;

			if (rootCause is EndOfStreamException)
				return AssetLoadErrorKind.TruncatedOrCorrupt;
			if (rootCause is InvalidDataException)
				return AssetLoadErrorKind.InvalidData;
			if (rootCause is UnauthorizedAccessException)
				return AssetLoadErrorKind.AccessDenied;
			if (rootCause is FileNotFoundException || rootCause is DirectoryNotFoundException)
				return AssetLoadErrorKind.MissingFile;
			if (rootCause is NotSupportedException)
				return AssetLoadErrorKind.UnsupportedFormat;
			if (IsDependencyError(rootCause.GetType()))
				return AssetLoadErrorKind.MissingDependency;
			if (rootCause is OverflowException || rootCause is ArgumentException ||
			    rootCause is FormatException || rootCause is IndexOutOfRangeException)
				return AssetLoadErrorKind.InvalidData;
			if (rootCause is IOException)
				return AssetLoadErrorKind.FileSystem;

			return AssetLoadErrorKind.Unknown;
		}

		public static string FormatExceptionChain(Exception exception)
		{
			if (exception == null)
				return "No exception details are available.";

			var builder = new StringBuilder();
			var current = exception;
			var depth = 0;
			while (current != null)
			{
				if (depth > 0)
					builder.AppendLine();
				builder.Append(depth == 0 ? string.Empty : "Caused by: ");
				builder.Append(current.GetType().FullName);
				builder.Append(": ");
				builder.Append(string.IsNullOrWhiteSpace(current.Message)
					? "No exception message was provided."
					: current.Message);
				current = current.InnerException;
				depth++;
			}

			return builder.ToString();
		}

		private static Exception GetRootCause(Exception exception)
		{
			while (exception != null && exception.InnerException != null)
				exception = exception.InnerException;
			return exception;
		}

		private static bool IsDependencyError(Type exceptionType)
		{
			var typeInfo = exceptionType.GetTypeInfo();
			if (!typeInfo.IsGenericType)
				return false;

			var genericType = typeInfo.GetGenericTypeDefinition();
			return genericType == typeof(ResolveFailedException<>) ||
			       genericType == typeof(UnresolvedDependenceException<>) ||
			       genericType == typeof(ExpiredDependenceException<>);
		}
	}
}
