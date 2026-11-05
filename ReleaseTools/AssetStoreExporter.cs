using System;
using System.IO;
using UnityEditor;
#if UNITY_6000_6_OR_NEWER
using UnityEditor.AssetPackage;
#endif
using UnityEngine;

namespace HypeRate.ReleaseTools
{
    internal static class AssetStoreExporter
    {
        public static void Export()
        {
            var outputPath = Environment.GetEnvironmentVariable("HYPERATE_EXPORT_PATH");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                throw new InvalidOperationException("HYPERATE_EXPORT_PATH is not set.");
            }

            outputPath = Path.GetFullPath(outputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? throw new InvalidOperationException(
                "The export path does not have a parent directory."));

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
#if UNITY_6000_6_OR_NEWER
            var parameters = new ExportPackageParameters(
                "Assets/HypeRate",
                outputPath,
                string.Empty,
                ExportPackageOptions.Recurse);
            Package.Export(parameters);
#else
            AssetDatabase.ExportPackage(
                "Assets/HypeRate",
                outputPath,
                ExportPackageOptions.Recurse);
#endif

            if (!File.Exists(outputPath))
            {
                throw new InvalidOperationException($"Unity did not create {outputPath}.");
            }

            Debug.Log($"HypeRate Asset Store package exported to {outputPath}");
        }
    }
}
