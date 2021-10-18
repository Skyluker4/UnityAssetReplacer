using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using AssetsTools.NET;
using AssetsTools.NET.Extra;

using TexturePlugin;

namespace UnityAssetReplacer {
public abstract class Asset {
  // Constants
  protected const string ForwardPathSeparator = "/";
  private const string BackwardPathSeparator = "\\";

  // Asset related member variables
  protected readonly AssetsFileInstance AssetsFile;
  protected readonly AssetsManager AssetsManager = new();
  protected readonly AssetsFileTable AssetsTable;
  private readonly BundleFileInstance _assetsBundleFile;

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
    var assetsReplacer = new AssetsReplacerFromMemory(
        0, assetInfo.index, (int)assetInfo.curFileType,
        AssetHelper.GetScriptIndex(AssetsFile.file, assetInfo), newGoBytes);
    assetReplacers.Add(assetsReplacer);
  }

  // Helper function to run replacer on bundle
  private void ReplaceInBundle(in List<AssetsReplacer> assetReplacers,
                               in string outputAssetBundlePath) {
    // Write changes to the asset bundle
    byte[] newAssetData;

    using (var stream = new MemoryStream()) {
      using var writer = new AssetsFileWriter(stream);
      AssetsFile.file.Write(writer, 0, assetReplacers, 0);
      writer.Close();
      stream.Close();
      newAssetData = stream.ToArray();
    }

    var bundleReplacer = new BundleReplacerFromMemory(
        AssetsFile.name, AssetsFile.name, true, newAssetData, -1);

    // Save the new output file
    var bunWriter = new AssetsFileWriter(File.OpenWrite(outputAssetBundlePath));
    _assetsBundleFile.file.Write(bunWriter,
                                 new List<BundleReplacer> { bundleReplacer });
    _assetsBundleFile.file.Close();
    bunWriter.Close();
  }

  // Helper function to loop through all assets and replace
  protected void
  ReplaceAll(in string inputDirectory, in string outputAssetBundlePath,
             Action<List<AssetsReplacer>, string, string> operation) {
    // Get list of all files in input folder
    var inputFilePaths =
        Directory.GetFiles(inputDirectory, "*", SearchOption.TopDirectoryOnly);

    // Create list to hold replacers
    var assetReplacers = new List<AssetsReplacer>();

    // Loop through every asset in input folder and try to open the name
    foreach (var inputFilePath in inputFilePaths) {
      // Set name and get only filename; no slashes
      var inputFileName = inputFilePath.Split(ForwardPathSeparator)
                              .Last()
                              .Split(BackwardPathSeparator)
                              .Last();

      operation(assetReplacers, inputFileName, inputFilePath);
    }

    ReplaceInBundle(assetReplacers, outputAssetBundlePath);
  }

  // Operation functions
  public abstract void Replace(in string inputDirectory,
                               in string outputAssetBundlePath);
  public abstract void Dump(in string dumpPath);
}

public class RawAsset : Asset {
  // Specific variable for dealing with raw assets
  private readonly string _memberName;

  // Constructor
  public RawAsset(in string inputAssetBundlePath, in string memberName)
      : base(in inputAssetBundlePath) => _memberName = memberName;

  // Method to replace assets in an asset file given an input directory and an
  // output path
  public override void Replace(in string inputDirectory,
                               in string outputAssetBundlePath) {
    ReplaceAll(inputDirectory, outputAssetBundlePath,
               (assetReplacers, inputFileName, inputFilePath) => {
                 // Get specific asset from asset table
                 var assetInfo = AssetsTable.GetAssetInfo(inputFileName);
                 var baseField =
                     AssetsManager.GetTypeInstance(AssetsFile.file, assetInfo)
                         .GetBaseField();

                 // Read from input file
                 var maxFileSize = new FileInfo(inputFilePath).Length;
                 var inBytes = new byte[maxFileSize];
                 File.Open(inputFilePath, FileMode.Open)
                     .Read(inBytes, 0, Convert.ToInt32(maxFileSize));

                 // Get member to replace
                 var member = baseField.Get(_memberName).GetValue();

                 // Check if member is null
                 if (member is null) {
                   // Print error: Member wasn't found in asset
                   Console.Error.WriteLine("ERROR: Can't read member '" +
                                           _memberName + "' in asset '" +
                                           inputFileName + "'!");

                   // Go on to next asset
                   return;
                 }

                 // Replace the member
                 member.Set(inBytes);
                 AddReplacer(baseField, assetInfo, ref assetReplacers);
               });
  }

  // Method to dump bytes to a specified path from assets in an asset file with
  // a given member name
  public override void Dump(in string dumpPath) {
    // Create output folder
    Directory.CreateDirectory(dumpPath);

    // Loop through every asset in asset file
    foreach (var inf in AssetsTable.assetFileInfo) {
      // Get specific asset
      var baseField =
          AssetsManager.GetTypeInstance(AssetsFile.file, inf).GetBaseField();

      // Get the name of the asset
      var assetName = baseField.Get("m_Name").GetValue().AsString();

      // Read the values
      var memberValue = baseField.Get(_memberName).GetValue();

      // Check if member is null
      if (memberValue is null) {
        // Print error: Member wasn't found in asset
        Console.Error.WriteLine("ERROR: Can't read member '" + _memberName +
                                "' in asset '" + assetName + "'!");

        // Go on to next asset
        continue;
      }

      // Output value as an array of bytes
      var memberString = memberValue.AsStringBytes();

      // Save the file
      File.WriteAllBytes(dumpPath + ForwardPathSeparator + assetName,
                         memberString);
    }
  }
}

public class TextureAsset : Asset {
  // Constructor
  public TextureAsset(in string inputAssetBundlePath)
      : base(in inputAssetBundlePath) {}

  // Read texture data. Copied from nesrak1's UABEA TexturePlugin.
  private static bool GetResSTexture(in TextureFile texFile,
                                     in AssetsFileInstance inst) {
    var streamInfo = texFile.m_StreamData;
    if (string.IsNullOrEmpty(streamInfo.path) || inst.parentBundle == null)
      return true;

    // Some versions apparently don't use archive:/
    var searchPath = streamInfo.path;
    if (searchPath.StartsWith("archive:/"))
      searchPath = searchPath[9..];

    searchPath = Path.GetFileName(searchPath);

    var bundle = inst.parentBundle.file;

    var reader = bundle.reader;
    var dirInf = bundle.bundleInf6.dirInf;

    foreach (var info in dirInf) {
      if (info.name != searchPath)
        continue;
      reader.Position = bundle.bundleHeader6.GetFileDataOffset() + info.offset +
                        streamInfo.offset;
      texFile.pictureData = reader.ReadBytes((int)streamInfo.size);
      texFile.m_StreamData.offset = 0;
      texFile.m_StreamData.size = 0;
      texFile.m_StreamData.path = "";
      return true;
    }

    return false;
  }

  // Read texture image bytes. Copied from nesrak1's UABEA TexturePlugin.
  private static byte[] GetRawTextureBytes(in TextureFile texFile,
                                           in AssetsFileInstance inst) {
    var rootPath = Path.GetDirectoryName(inst.path);
    if (texFile.m_StreamData.size == 0 ||
        texFile.m_StreamData.path == string.Empty)
      return texFile.pictureData;
    var fixedStreamPath = texFile.m_StreamData.path;
    if (!Path.IsPathRooted(fixedStreamPath) && rootPath != null)
      fixedStreamPath = Path.Combine(rootPath, fixedStreamPath);

    if (File.Exists(fixedStreamPath)) {
      Stream stream = File.OpenRead(fixedStreamPath);
      stream.Position = texFile.m_StreamData.offset;
      texFile.pictureData = new byte[texFile.m_StreamData.size];
      stream.Read(texFile.pictureData, 0, (int)texFile.m_StreamData.size);
    } else {
      return Array.Empty<byte>();
    }

    return texFile.pictureData;
  }

  // Dump all textures from asset file
  public override void Dump(in string dumpPath) {
    // Create output folder
    Directory.CreateDirectory(dumpPath);

    // Loop through every Texture2D (0x1C) in asset file
    foreach (var inf in AssetsTable.GetAssetsOfType(0x1C)) {
      // Get specific asset
      var baseField =
          AssetsManager.GetTypeInstance(AssetsFile.file, inf).GetBaseField();

      // Get the name of the asset
      var assetName = baseField.Get("m_Name").GetValue().AsString();

      // Get texture file
      var tf = TextureFile.ReadTextureFile(baseField);

      // Read image data
      if (!GetResSTexture(tf, AssetsFile)) {
        Console.Error.WriteLine("ERROR: Can't read image data from '" +
                                assetName + "'!");
        continue;
      }

      // Read texture data
      var data = GetRawTextureBytes(tf, AssetsFile);

      if (data == null || data.Length < 1) {
        // Print error: Texture wasn't able to be read
        Console.Error.WriteLine("ERROR: Can't read texture '" + assetName +
                                "'!");

        // Go on to next asset
        continue;
      }

      _ = TextureImportExport.ExportPng(
          data, dumpPath + ForwardPathSeparator + assetName + ".png",
          tf.m_Width, tf.m_Height, (TextureFormat)tf.m_TextureFormat);
    }
  }

  // Replace found textures in files
  public override void Replace(in string inputDirectory,
                               in string outputAssetBundlePath) {
    ReplaceAll(
        inputDirectory, outputAssetBundlePath,
        (assetReplacers, inputFileName, inputFilePath) => {
          var assetName = inputFileName.Replace(".png", "");

          // Remove directory from asset name
          assetName = assetName.Split("\\").Last();

          // Get specific asset from asset table
          var assetInfo =
              AssetsTable.GetAssetInfo(assetName, 0x1C); // 0x1C is texture

          if (assetInfo is null) {
            // Print error: Asset wasn't found with name
            Console.Error.WriteLine(
                "ERROR: Can't replace texture '" + assetName +
                "'! Make sure texture already exists in the assets file!" +
                "\nQUITTING!");

            // Quit replacing textures
            return;
          }

          var baseField =
              AssetsManager.GetTypeInstance(AssetsFile.file, assetInfo)
                  .GetBaseField();
          var fmt = (TextureFormat)baseField.Get("m_TextureFormat")
                        .GetValue()
                        .AsInt();

          var encImageBytes = TextureImportExport.ImportPng(
              inputFilePath, fmt, out var width, out var height);

          if (encImageBytes == null) {
            Console.Error.WriteLine("ERROR: Could not decode image '" +
                                    inputFilePath + "'!");
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
          var byteArray =
              new AssetTypeByteArray { size = (uint)encImageBytes.Length,
                                       data = encImageBytes };

          // Replace the data
          imageData.GetValue().Set(byteArray);
          AddReplacer(baseField, assetInfo, ref assetReplacers);
        });
  }
}
}
