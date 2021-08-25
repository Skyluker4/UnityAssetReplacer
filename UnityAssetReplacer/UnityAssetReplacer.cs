using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using AssetsTools.NET;
using AssetsTools.NET.Extra;

using TexturePlugin;

namespace UnityAssetReplacer {
	public class UnityAssetReplacer {
		private readonly BundleFileInstance _assetsBundleFile;
		private readonly AssetsFileInstance _assetsFile;

		// Asset related member variables
		private readonly AssetsManager _assetsManager = new();

		private readonly AssetsFileTable _assetsTable;

		// Global arguments
		private readonly string _memberName;

		// Constructors
		public UnityAssetReplacer(string inputAssetBundlePath) {
			// Open the asset bundle file and get info
			_assetsBundleFile = _assetsManager.LoadBundleFile(inputAssetBundlePath);
			_assetsFile = _assetsManager.LoadAssetsFileFromBundle(_assetsBundleFile, 0);
			_assetsTable = _assetsFile.table;
		}

		public UnityAssetReplacer(string inputAssetBundlePath, string memberName) : this(inputAssetBundlePath) =>
			_memberName = memberName;

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
					Console.Error.WriteLine("ERROR: Can't read member '" +
					                        _memberName +
					                        "' in asset '" +
					                        inputFileName +
					                        "'!");

					// Go on to next asset
					continue;
				}

				// Replace the member
				member.Set(inBytes);

				var newGoBytes = baseField.WriteToByteArray();

				// Add new replacer to list of replacers
				var assetsReplacer = new AssetsReplacerFromMemory(0, assetInfo.index, (int)assetInfo.curFileType,
				                                                  AssetHelper.GetScriptIndex(_assetsFile.file,
					                                                  assetInfo), newGoBytes);
				assetReplacers.Add(assetsReplacer);
			}

			// Write changes to the asset bundle
			byte[] newAssetData;

			using (var stream = new MemoryStream()) {
				using var writer = new AssetsFileWriter(stream);
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
			bunWriter.Close();
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
				File.WriteAllBytes(dumpPath + "/" + assetName, memberString);
			}
		}

		// Read texture data. Copied from nesrak1's UABEA TexturePlugin
		private static bool GetResSTexture(TextureFile texFile, AssetsFileInstance inst) {
			var streamInfo = texFile.m_StreamData;
			if (string.IsNullOrEmpty(streamInfo.path) || inst.parentBundle == null) return true;

			// Some versions apparently don't use archive:/
			var searchPath = streamInfo.path;
			if (searchPath.StartsWith("archive:/"))
				searchPath = searchPath[9..];

			searchPath = Path.GetFileName(searchPath);

			var bundle = inst.parentBundle.file;

			var reader = bundle.reader;
			var dirInf = bundle.bundleInf6.dirInf;

			foreach (var info in dirInf) {
				if (info.name != searchPath) continue;
				reader.Position = bundle.bundleHeader6.GetFileDataOffset() + info.offset + streamInfo.offset;
				texFile.pictureData = reader.ReadBytes((int)streamInfo.size);
				texFile.m_StreamData.offset = 0;
				texFile.m_StreamData.size = 0;
				texFile.m_StreamData.path = "";
				return true;
			}

			return false;
		}

		// Dump all textures from asset file
		public void DumpTextures(string dumpPath) {
			// Create output folder
			Directory.CreateDirectory(dumpPath);

			// Loop through every Texture2D (0x1C) in asset file
			foreach (var inf in _assetsTable.GetAssetsOfType(0x1C)) {
				// Get specific asset
				var baseField = _assetsManager.GetTypeInstance(_assetsFile.file, inf).GetBaseField();

				// Get the name of the asset
				var assetName = baseField.Get("m_Name").GetValue().AsString();

				// Get texture file
				var tf = TextureFile.ReadTextureFile(baseField);

				// Read image data
				if (!GetResSTexture(tf, _assetsFile)) {
					Console.Error.WriteLine("ERROR: Can't read image data from '" + assetName + "'!");
					continue;
				}

				// Read texture data
				var texDat = tf.GetTextureData();

				// Make sure it read successfully
				if (texDat is not { Length: > 0 }) {
					Console.Error.WriteLine("ERROR: Can't read texture data from '" + assetName + "'!");
					continue;
				}

				// Save bitmap
				var bitmap = new Bitmap(tf.m_Width, tf.m_Height, tf.m_Width * 4, PixelFormat.Format32bppArgb,
				                        Marshal.UnsafeAddrOfPinnedArrayElement(texDat, 0));
				bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
				bitmap.Save(dumpPath + "/" + assetName + ".png");
				bitmap.Dispose();
			}
		}

		public void ReplaceTextures(string inputDirectory, string outputAssetBundlePath) {
			// Get list of all files in input folder
			var inputFilePaths = Directory.GetFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly);

			// Create list to hold replacers
			var assetReplacers = new List<AssetsReplacer>();

			// Loop through every asset in input folder and try to open the name
			foreach (var inputFilePath in inputFilePaths) {
				// Set name
				var inputFileName = inputFilePath.Split('\\').Last();

				var assetName = inputFileName.Replace(".png", "");

				// Get specific asset from asset table
				var assetInfo = _assetsTable.GetAssetInfo(assetName, 0x1C); // 0x1C is texture

				if (assetInfo is null) {
					// Print error: Asset wasn't found with name
					Console.Error.WriteLine("ERROR: Can't replace texture '" +
					                        assetName +
					                        "'! Make sure texture already exists in the assets file!");

					// Go on to next asset
					continue;
				}

				var baseField = _assetsManager.GetTypeInstance(_assetsFile.file, assetInfo).GetBaseField();
				var fmt = (TextureFormat)baseField.Get("m_TextureFormat").GetValue().AsInt();

				var encImageBytes = TextureImportExport.ImportPng(inputFilePath, fmt, out var width, out var height);

				if (encImageBytes == null) {
					Console.Error.WriteLine("ERROR: Could not decode image '" + inputFilePath + "'!");
					continue;
				}

				var streamData = baseField.Get("m_StreamData");
				streamData.Get("offset").GetValue().Set(0);
				streamData.Get("size").GetValue().Set(0);
				streamData.Get("path").GetValue().Set("");

				baseField.Get("m_TextureFormat").GetValue().Set((int)fmt);

				baseField.Get("m_Width").GetValue().Set(width);
				baseField.Get("m_Height").GetValue().Set(height);

				var imageData = baseField.Get("image data");
				imageData.GetValue().type = EnumValueTypes.ByteArray;
				imageData.templateField.valueType = EnumValueTypes.ByteArray;
				var byteArray = new AssetTypeByteArray { size = (uint)encImageBytes.Length, data = encImageBytes };
				imageData.GetValue().Set(byteArray);

				var newGoBytes = baseField.WriteToByteArray();

				// Add new replacer to list of replacers
				var assetsReplacer = new AssetsReplacerFromMemory(0, assetInfo.index, (int)assetInfo.curFileType,
				                                                  AssetHelper.GetScriptIndex(_assetsFile.file,
					                                                  assetInfo), newGoBytes);
				assetReplacers.Add(assetsReplacer);
			}

			// Write changes to the asset bundle
			byte[] newAssetData;

			using (var stream = new MemoryStream()) {
				using var writer = new AssetsFileWriter(stream);
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
			bunWriter.Close();
		}
	}
}
