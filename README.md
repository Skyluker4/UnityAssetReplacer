# UnityAssetReplacer

[![Build](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/build.yml/badge.svg)](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/build.yml)
[![CodeQL](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/codeql-analysis.yml)
[![Lint](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/linter.yml/badge.svg)](https://github.com/Skyluker4/UnityAssetReplacer/actions/workflows/linter.yml)

A tool to dump or replace assets in a Unity asset bundle.

## Usage

This program always requires a **decompressed** Unity asset bundle. Use [Unity Assets Bundle Extractor Avalonia (UABEA)](https://github.com/nesrak1/UABEA) to decompress an asset bundle if it's compressed.
It also always requires the name of the ```member``` (A.K.A. field) you'd like interact with in the asset bundle (ex. ```m_Name```).
To extract, you also need to set the path of where you'd like to dump to.
To replace/import assets, you need to specify the path where the assets to import are and the name of the new asset bundle you're exporting. The files inside of the directory must match the asset names **exactly** in order to be imported.

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

- Dump all assets from ```assetBundle.bun``` with member ```m_Script``` to ```extracted/```:
  - ```UnityAssetReplacer -b assetBundle.bun -m m_Script -d extracted```
- Replace all assets from ```assetBundle.bun``` with member ```m_Script```, reading from ```newAssets/```, to ```newAssetBundle.bun```:
  - ```UnityAssetReplacer -b assetBundle.bun -m m_Script -i newAssets -o newAssetBundle.bun```

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
