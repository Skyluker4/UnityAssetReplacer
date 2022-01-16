# UnityAssetReplacer

[![Build](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/build.yml/badge.svg)](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/build.yml)
[![CodeQL](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/codeql-analysis.yml)
[![Lint](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/linter.yml/badge.svg)](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/linter.yml)

A tool to dump or replace assets in a Unity asset bundle.

## Usage

This program requires an **uncompressed** Unity asset bundle. Use [Unity Assets Bundle Extractor Avalonia (UABEA)](https://github.com/nesrak1/UABEA) to decompress an asset bundle if it's compressed.
It also requires the name of the ```member``` (the **field** of an object in the assets file, **NOT** the game object itself) you'd like interact with in the asset bundle (ex. ```m_Name```), except for when you are dealing with textures.
The best way to find members/fields for an object is to open the asset bundle in [UABEA](https://github.com/nesrak1/UABEA) or a similar tool, then open the asset file and then the object, where you'll find a list of all members for that object.
To extract, you also need to set the path of where you'd like to dump to.

To replace assets, you need to specify the path where the assets to import are and the name of the new asset bundle you're exporting. The files inside of the directory must match the asset names **exactly** in order to be imported.

### Arguments

- ```-h```, ```-?```, ```--help```: show the help message and then exit.
- ```-b```, ```--bundle=BUNDLE```: the original asset bundle path you want to read from.
- ```-m```, ```--member=MEMBER```: the member to dump or replace.
- ```-t```, ```--texture```: will deal with textures instead of members. Uses PNG the format.

#### Dumping

- ```-d```, ```--dump=DUMP```: the path of the directory you wish to dump the assets to.

#### Replacing

- ```-i```, ```--input=INPUT```: the input directory of the assets you wish to overwrite with.
- ```-o```, ```--output=OUTPUT```: the path and name of the asset bundle you wish to write to when replacing.

### Examples

- Dump all assets from ```assetBundle.bun``` with member ```m_Script``` to ```extractedAssets/```:
  - ```UnityAssetReplacer -b assetBundle.bun -m m_Script -d extractedAssets```
- Replace assets in ```assetBundle.bun``` with member ```m_Script```, reading from ```newAssets/```, to ```newAssetBundle.bun```:
  - ```UnityAssetReplacer -b assetBundle.bun -m m_Script -i newAssets -o newAssetBundle.bun```
- Dump all textures from ```assetBundle.bun``` to ```extractedTextures/```:
  - ```UnityAssetReplacer -b assetBundle.bun -t -d extractedTextures```
- Replace textures in ```assetBundle.bun```, reading from ```newTextures/```, to ```newAssetBundle.bun```:
  - ```UnityAssetReplacer -b assetBundle.bun -t -i newTextures -o newAssetBundle.bun```

## Building

### Requirements

- .NET 5
- Visual Studio 2019 (Optional)

### Using ```dotnet```

1. Restore any packages with ```dotnet restore``` or skip and let it run implicitly when running the next step.
2. Run ```dotnet build``` in the project root.

### Using ```Visual Studio```

1. Open ```UnityAssetReplacer.sln``` in Visual Studio
2. Press ```Build -> Build Solution``` to build or the green arrow to build and run.

## Resources

- [AssetTools.NET](https://github.com/nesrak1/AssetsTools.NET): The library used to extract assets from Unity asset bundles.
- [Unity Assets Bundle Extractor Avalonia (UABEA)](https://github.com/nesrak1/UABEA): A program that runs on AssetTools.NET. Useful for seeing how to use the library.
