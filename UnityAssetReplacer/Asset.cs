using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UnityAssetReplacer {
	public abstract class Asset {
		// Constants
		protected const string ForwardPathSeparator = "/";
		private const string BackwardPathSeparator = "\\";
		private readonly BundleFileInstance _assetsBundleFile;

		// Asset related member variables
		protected readonly AssetsFileInstance AssetsFile;
		protected readonly AssetsManager AssetsManager = new();
		protected readonly AssetsFileTable AssetsTable;

		// Constructor
		protected Asset(in string inputAssetBundlePath) {
			// Open the asset bundle file and get info
			_assetsBundleFile = AssetsManager.LoadBundleFile(inputAssetBundlePath);
			AssetsFile = AssetsManager.LoadAssetsFileFromBundle(_assetsBundleFile, 0);
			AssetsTable = AssetsFile.table;
		}

		// Helper function to add replacer to replacer list
		protected void AddReplacer(in AssetTypeValueField baseField,
								   in AssetFileInfoEx assetInfo,
								   ref List<AssetsReplacer> assetReplacers) {
			var newGoBytes = baseField.WriteToByteArray();

			// Add new replacer to list of replacers
			var assetsReplacer = new AssetsReplacerFromMemory(0, assetInfo.index, (int)assetInfo.curFileType,
															  AssetHelper.GetScriptIndex(AssetsFile.file, assetInfo),
															  newGoBytes);
			assetReplacers.Add(assetsReplacer);
		}

		// Helper function to run replacer on bundle
		private void ReplaceInBundle(in List<AssetsReplacer> assetReplacers, in string outputAssetBundlePath) {
			// Write changes to the asset bundle
			byte[] newAssetData;

			using (var stream = new MemoryStream()) {
				using var writer = new AssetsFileWriter(stream);
				AssetsFile.file.Write(writer, 0, assetReplacers, 0);
				writer.Close();
				stream.Close();
				newAssetData = stream.ToArray();
			}

			var bundleReplacer = new BundleReplacerFromMemory(AssetsFile.name, AssetsFile.name, true, newAssetData, -1);

			// Save the new output file
			var bunWriter = new AssetsFileWriter(File.OpenWrite(outputAssetBundlePath));
			_assetsBundleFile.file.Write(bunWriter, new List<BundleReplacer> { bundleReplacer });
			_assetsBundleFile.file.Close();
			bunWriter.Close();
		}

		// Helper function to loop through all assets and replace
		protected void ReplaceAll(in string inputDirectory,
								  in string outputAssetBundlePath,
								  Action<List<AssetsReplacer>, string, string> operation) {
			// Get list of all files in input folder
			var inputFilePaths = Directory.GetFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly);

			// Create list to hold replacers
			var assetReplacers = new List<AssetsReplacer>();

			// Loop through every asset in input folder and try to open the name
			foreach (var inputFilePath in inputFilePaths) {
				// Set name and get only filename; no slashes
				var inputFileName =
					inputFilePath.Split(ForwardPathSeparator).Last().Split(BackwardPathSeparator).Last();

				operation(assetReplacers, inputFileName, inputFilePath);
			}

			ReplaceInBundle(assetReplacers, outputAssetBundlePath);
		}

		// Operation functions
		public abstract void Replace(in string inputDirectory, in string outputAssetBundlePath);
		public abstract void Dump(in string dumpPath);
	}
}
