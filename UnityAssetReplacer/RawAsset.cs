using System;
using System.IO;

using AssetsTools.NET.Extra;

namespace UnityAssetReplacer {
	public class RawAsset : Asset {
		// Specific variable for dealing with raw assets
		private readonly string _memberName;

		// Constructor
		public RawAsset(in string inputAssetBundlePath, in string memberName) : base(in inputAssetBundlePath) =>
			_memberName = memberName;

		// Method to replace assets in an asset file given an input directory and an output path
		public override void Replace(in string inputDirectory, in string outputAssetBundlePath) {
			ReplaceAll(inputDirectory, outputAssetBundlePath, (assetReplacers, inputFileName, inputFilePath) => {
				// Get specific asset from asset table
				var assetInfo = AssetsTable.GetAssetInfo(inputFileName);
				var baseField = AssetsManager.GetTypeInstance(AssetsFile.file, assetInfo).GetBaseField();

				// Read from input file
				var maxFileSize = new FileInfo(inputFilePath).Length;
				var inBytes = new byte[maxFileSize];
				File.Open(inputFilePath, FileMode.Open).Read(inBytes, 0, Convert.ToInt32(maxFileSize));

				// Get member to replace
				var member = baseField.Get(_memberName).GetValue();

				// Check if member is null
				if (member is null) {
					// Print error: Member wasn't found in asset
					Console.Error.WriteLine("ERROR: Can't read member '" +
											_memberName +
											"' in asset '" +
											inputFileName +
											"'!");

					// Go on to next asset
					return;
				}

				// Replace the member
				member.Set(inBytes);
				AddReplacer(baseField, assetInfo, ref assetReplacers);
			});
		}

		// Method to dump bytes to a specified path from assets in an asset file with a given member name
		public override void Dump(in string dumpPath) {
			// Create output folder
			Directory.CreateDirectory(dumpPath);

			// Loop through every asset in asset file
			foreach (var inf in AssetsTable.assetFileInfo) {
				// Get specific asset
				var baseField = AssetsManager.GetTypeInstance(AssetsFile.file, inf).GetBaseField();

				// Get the name of the asset
				var assetName = baseField.Get("m_Name").GetValue().AsString();

				// Read the values
				var memberValue = baseField.Get(_memberName).GetValue();

				// Check if member is null
				if (memberValue is null) {
					// Print error: Member wasn't found in asset
					Console.Error.WriteLine("ERROR: Can't read member '" +
											_memberName +
											"' in asset '" +
											assetName +
											"'!");

					// Go on to next asset
					continue;
				}

				// Output value as an array of bytes
				var memberString = memberValue.AsStringBytes();

				// Save the file
				File.WriteAllBytes(dumpPath + ForwardPathSeparator + assetName, memberString);
			}
		}
	}
}
