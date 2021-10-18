using System;
using System.IO;
using System.Linq;

using AssetsTools.NET;
using AssetsTools.NET.Extra;

using TexturePlugin;

namespace UnityAssetReplacer {
	public class TextureAsset : Asset {
		// Constructor
		public TextureAsset(in string inputAssetBundlePath) : base(in inputAssetBundlePath) { }

		// Read texture data. Copied from nesrak1's UABEA TexturePlugin.
		private static bool GetResSTexture(in TextureFile texFile, in AssetsFileInstance inst) {
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

		// Read texture image bytes. Copied from nesrak1's UABEA TexturePlugin.
		private static byte[] GetRawTextureBytes(in TextureFile texFile, in AssetsFileInstance inst) {
			var rootPath = Path.GetDirectoryName(inst.path);
			if (texFile.m_StreamData.size == 0 || texFile.m_StreamData.path == string.Empty) return texFile.pictureData;
			var fixedStreamPath = texFile.m_StreamData.path;
			if (!Path.IsPathRooted(fixedStreamPath) && rootPath != null)
				fixedStreamPath = Path.Combine(rootPath, fixedStreamPath);

			if (File.Exists(fixedStreamPath)) {
				Stream stream = File.OpenRead(fixedStreamPath);
				stream.Position = texFile.m_StreamData.offset;
				texFile.pictureData = new byte[texFile.m_StreamData.size];
				stream.Read(texFile.pictureData, 0, (int)texFile.m_StreamData.size);
			}
			else { return Array.Empty<byte>(); }

			return texFile.pictureData;
		}

		// Dump all textures from asset file
		public override void Dump(in string dumpPath) {
			// Create output folder
			Directory.CreateDirectory(dumpPath);

			// Loop through every Texture2D (0x1C) in asset file
			foreach (var inf in AssetsTable.GetAssetsOfType(0x1C)) {
				// Get specific asset
				var baseField = AssetsManager.GetTypeInstance(AssetsFile.file, inf).GetBaseField();

				// Get the name of the asset
				var assetName = baseField.Get("m_Name").GetValue().AsString();

				// Get texture file
				var tf = TextureFile.ReadTextureFile(baseField);

				// Read image data
				if (!GetResSTexture(tf, AssetsFile)) {
					Console.Error.WriteLine("ERROR: Can't read image data from '" + assetName + "'!");
					continue;
				}

				// Read texture data
				var data = GetRawTextureBytes(tf, AssetsFile);

				if (data == null || data.Length < 1) {
					// Print error: Texture wasn't able to be read
					Console.Error.WriteLine("ERROR: Can't read texture '" + assetName + "'!");

					// Go on to next asset
					continue;
				}

				_ = TextureImportExport.ExportPng(data, dumpPath + ForwardPathSeparator + assetName + ".png",
												  tf.m_Width, tf.m_Height, (TextureFormat)tf.m_TextureFormat);
			}
		}

		// Replace found textures in files
		public override void Replace(in string inputDirectory, in string outputAssetBundlePath) {
			ReplaceAll(inputDirectory, outputAssetBundlePath, (assetReplacers, inputFileName, inputFilePath) => {
				var assetName = inputFileName.Replace(".png", "");

				// Remove directory from asset name
				assetName = assetName.Split("\\").Last();

				// Get specific asset from asset table
				var assetInfo = AssetsTable.GetAssetInfo(assetName, 0x1C); // 0x1C is texture

				if (assetInfo is null) {
					// Print error: Asset wasn't found with name
					Console.Error.WriteLine("ERROR: Can't replace texture '" +
											assetName +
											"'! Make sure texture already exists in the assets file!" +
											"\nQUITTING!");

					// Quit replacing textures
					return;
				}

				var baseField = AssetsManager.GetTypeInstance(AssetsFile.file, assetInfo).GetBaseField();
				var fmt = (TextureFormat)baseField.Get("m_TextureFormat").GetValue().AsInt();

				var encImageBytes = TextureImportExport.ImportPng(inputFilePath, fmt, out var width, out var height);

				if (encImageBytes == null) {
					Console.Error.WriteLine("ERROR: Could not decode image '" + inputFilePath + "'!");
					return;
				}

				// Format and save the image data
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

				// Replace the data
				imageData.GetValue().Set(byteArray);
				AddReplacer(baseField, assetInfo, ref assetReplacers);
			});
		}
	}
}
