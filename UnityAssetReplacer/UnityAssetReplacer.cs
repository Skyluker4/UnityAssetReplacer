using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UnityAssetReplacer {
	public class UnityAssetReplacer {
		private readonly BundleFileInstance _assetsBundleFile;
		private readonly AssetsFileInstance _assetsFile;

		// Asset related member variables
		private readonly AssetsManager _assetsManager = new();

		private readonly AssetsFileTable _assetsTable;

		// Global arguments
		private readonly string _memberName;

		// Constructor
		public UnityAssetReplacer(string inputAssetBundlePath, string memberName) {
			// Save member to read
			_memberName = memberName;

			// Open the asset bundle file and get info
			_assetsBundleFile = _assetsManager.LoadBundleFile(inputAssetBundlePath);
			_assetsFile = _assetsManager.LoadAssetsFileFromBundle(_assetsBundleFile, 0);
			_assetsTable = _assetsFile.table;
		}

		// Method to replace assets in an asset file given an input directory and an output path
		public void ReplaceAssets(string inputDirectory, string outputAssetBundlePath) {
			// Get list of all files in input folder
			var inputFilePaths = Directory.GetFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly);

			// Create list to hold replacers
			var assetReplacers = new List<AssetsReplacer>();

			// Loop through every asset in input folder and try to open the name
			foreach (var inputFilePath in inputFilePaths) {
				// Set name
				var inputFileName = inputFilePath.Split('\\').Last();

				// Get specific asset from asset table
				var assetInfo = _assetsTable.GetAssetInfo(inputFileName);
				var baseField = _assetsManager.GetTypeInstance(_assetsFile.file, assetInfo).GetBaseField();

				// Read from input file
				var maxFileSize = new FileInfo(inputFilePath).Length;
				var inBytes = new byte[maxFileSize];
				File.Open(inputFilePath, FileMode.Open).Read(inBytes, 0, Convert.ToInt32(maxFileSize));

				// Get member to replace
				var member = baseField.Get(_memberName).GetValue();

				// Check if member is null
				if (member is null) {
					// Print error: Member wasn't found in asset
					Console.Error.WriteLine("ERROR: Can't read member '" + _memberName + "' in asset '" +
											inputFileName + "'!");

					// Go on to next asset
					continue;
				}

				// Replace the member
				member.Set(inBytes);

				var newGoBytes = baseField.WriteToByteArray();

				// Add new replacer to list of replacers
				var assetsReplacer = new AssetsReplacerFromMemory(0, assetInfo.index, (int)assetInfo.curFileType,
					AssetHelper.GetScriptIndex(_assetsFile.file, assetInfo), newGoBytes);
				assetReplacers.Add(assetsReplacer);
			}

			// Write changes to the asset bundle
			byte[] newAssetData;
			using (var stream = new MemoryStream())
			using (var writer = new AssetsFileWriter(stream)) {
				_assetsFile.file.Write(writer, 0, assetReplacers, 0);
				writer.Close();
				stream.Close();
				newAssetData = stream.ToArray();
			}

			var bundleReplacer =
				new BundleReplacerFromMemory(_assetsFile.name, _assetsFile.name, true, newAssetData, -1);

			// Save the new output file
			var bunWriter = new AssetsFileWriter(File.OpenWrite(outputAssetBundlePath));
			_assetsBundleFile.file.Write(bunWriter, new List<BundleReplacer> { bundleReplacer });
			_assetsBundleFile.file.Close();
		}

		// Method to dump bytes to a specified path from assets in an asset file with a given member name
		public void DumpAssets(string dumpPath) {
			// Create output folder
			Directory.CreateDirectory(dumpPath);

			// Loop through every asset in asset file
			foreach (var inf in _assetsTable.assetFileInfo) {
				// Get specific asset
				var baseField = _assetsManager.GetTypeInstance(_assetsFile.file, inf).GetBaseField();

				// Get the name of the asset
				var assetName = baseField.Get("m_Name").GetValue().AsString();

				// Read the values
				var memberValue = baseField.Get(_memberName).GetValue();

				// Check if member is null
				if (memberValue is null) {
					// Print error: Member wasn't found in asset
					Console.Error.WriteLine("ERROR: Can't read member '" + _memberName + "' in asset '" + assetName +
											"'!");

					// Go on to next asset
					continue;
				}

				// Output value as an array of bytes
				var memberString = memberValue.AsStringBytes();

				// Save the file
				File.WriteAllBytes(dumpPath + "/" + assetName, memberString);
			}
		}
	}
}