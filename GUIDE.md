# Guide

This is a more detailed guide on how to use Unity Asset Replace to actually dump and modify Unity asset bundles.

This guide and program may not work for every use case, since it was mainly made to deal with one project. If you run into any trouble, create an issue and modify the source code if necessary (for example, it is currently hard-coded to work with only the first assets file in the asset bundle).

## Requirements

- Unity assets file to dump/modify
- [UnityAssetReplacer (UAR)](https://github.com/Skyluker4/UnityAssetReplacer)
- [Unity Asset Bundle Extractor (UABE)](https://github.com/SeriousCache/UABE) (or another Unity assets viewer)

## Raw Assets

This section is for dumping/replacing single raw data/text sections in assets.

### 1. Decompress and View Assets

https://user-images.githubusercontent.com/10624379/164995881-4090c5f9-6087-45c5-ad9a-19c3ef1b582b.mp4

First, you need to identify what you are trying to modify.
Usually a search for the string you are trying to modify in the game files will turn up the correct file/asset bundle you are looking for.

Once you've got the asset bundle you want, open it in UABE and decompress it (saving it to disk) if necessary.
From there, you can view all of the assets in the file to find what field (refered to as a member in this program) needs modified.

In this example, we found that `m_Script` from inside the assets is what needed modified.


### 2. Dump Text and View Output

https://user-images.githubusercontent.com/10624379/164995659-292970e9-f27d-47af-b96b-610b87d48453.mp4

Next, you need to dump all of the assets with that specific member. UAR will only ouput the one member of that asset, creating a new file for each asset in the specified output directory.
In the directory will be the name of each asset (extracted from the member `m_Name`) with its data being whatever data was the specified member (`m_Script` in this case).

The command to dump the files was along the lines of:
`./UnityAssetReplacer -b "uncompressed_bundle_file" -m m_Script -d "bundle_dump_folder"`

Notice that the working directory was where UAR was, and the full paths to the files and folders were used. The quotations are used since the path has spaces in the name.
If you have UAR on your path, or would like to reference UAR using its full path instead of the target files and folders, you can do that instead.
`m_Script` is just the example being used and should be replaced with whatever member you are dumping.

The files that it outputs are the raw data of that member of the assets. Luckily in this case it's already raw text, but for other assets, the extracted data may need to be converted further using other tools. If that's the case, then when re-importing you need to convert back to the same data format.

### 3. Modify Assets

https://user-images.githubusercontent.com/10624379/164995679-01493509-6f93-4721-a2f4-19a3104456f1.mp4

In this video, we can see that the assets were copied to another folder and modified to have translated text.
Notice that the filenames are the same as before (no file extensions added) and only the data inside of them has changed.

These will be the files that we use to modify the asset bundle.

### 4. Replace Assets

https://user-images.githubusercontent.com/10624379/164995724-71079e45-6565-4916-8ca5-6759970bdef5.mp4

To replace the assets run:
`./UnityAssetReplacer -b "uncompressed_asset_bundle" -o "modififed_asset_bundle" -i "modified_assets"`

Again, the full paths to each directory/file are being used and the current working directory of the terminal is where UAR is located.

It may to a few seconds to finish replacing.

The new asset bundle will be called whatever you specified, but for usage in the Unity game, the filename of it needs to be changed to the original name.

### 5. Verify Modified Assets

https://user-images.githubusercontent.com/10624379/164995749-40c29bcf-1c83-4eeb-9357-048f14e72b72.mp4

Finally, we make sure that the specified member of the assets are replaced successfully. Then the older asset bundles are deleted and the modified one is renamed to the original name. All the extraction folders are modified folders can be deleted as well.

The modified asset bundle is not compressed, but it does not need to be. If you would like to compress it after, you can use UABE to do so.

## Textures

### 1. Decompress and View Asset List

https://user-images.githubusercontent.com/10624379/164995890-39e403a0-735f-4c8c-aaa2-395513cb5b98.mp4

To find which asset bundle to modify, try looking for large asset bundles and names that would be fitting to hold textures (like `ui`).

Once you've found the asset bundle you'd like to modify, decompress and save it to disk using UABE, if necessary.

Make sure that the assets file contains `Texture2D` asset types, since these are what will be extracted.

### 2. Dump Textures

https://user-images.githubusercontent.com/10624379/164995893-8ecc2547-b6bc-4ce6-aa0c-5b126ce7d69c.mp4

The textures can be dumped by using (this may take a few minutes):
`./UnityAssetReplacer -b "uncompressed_bundle_file" -t -d "dumped_textures_folder"`

The full paths to each directory and file are being used and the current working directory of the terminal is where UAR is located. The `-t` option is used to specify to work with all textures in the assets file.

They will all be dumped to the specified folder as PNGs.

### 3. Modify Textures

From here, modify the textures to your liking, but don't change their names.

### 4. Replace Textures

https://user-images.githubusercontent.com/10624379/164995907-bafae651-21f2-4401-b72a-776d9a207c8e.mp4

Finally, to replace the textures, run (this may take a few minutes and some memory):
`./UnityAssetReplacer -b "uncompressed_bundle_file" -t -i "modified_textures_folder" -o "modified_bundle_file"`

The working directory of the shell is where UAR is located and full paths are specified in this example again.

Once the modified asset bundle has been created, you can delete all the original and intermediate files and directories and rename the modified one to the original name.

The modified asset bundle is not compressed in this instance, but can be with UABE.
