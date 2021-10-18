using System;
using System.IO;

using Mono.Options;

namespace UnityAssetReplacer {
	internal static class Program {
		private static readonly OptionVariables Options = new();

		private static readonly OptionSet OptionParser = new() {
			{ "b|bundle=", "the original asset {BUNDLE} path", v => Options.InputAssetBundlePath = v },
			{ "i|input=", "the {INPUT} directory of the assets to overwrite with.", v => Options.InputDirectory = v },
			{
				"o|output=",
				"the path of the asset bundle you wish to {OUTPUT}.",
				v => Options.OutputAssetBundlePath = v
			},
			{ "d|dump=", "the path of the directory you wish to {DUMP} to.", v => Options.DumpPath = v },
			{ "m|member=", "the {MEMBER} you dump/overwrite.", v => Options.Member = v },
			{ "t|texture", "Interact with {TEXTURE}s in the asset bundle.", v => Options.Textures = v != null },
			{ "h|?|help", "show this message for and then exit.", v => Options.Help = v != null }
		};

		// Entry point
		public static void Main(string[] args) {
			// Parse arguments
			try { OptionParser.Parse(args); }
			catch (OptionException e) {
				Console.WriteLine(e.Message);
				OptionParser.WriteOptionDescriptions(Console.Out);
				return;
			}

			// Display help command if specified and then exit
			if (Options.Help) {
				OptionParser.WriteOptionDescriptions(Console.Out);
				return;
			}

			// ERROR: No input bundle specified
			if (Options.InputAssetBundlePath == null) {
				Console.Error.WriteLine("An asset BUNDLE must be specified.");
				ShowHelp();
				return;
			}

			// Check what can be run
			var dump = Options.DumpPath != null;
			var replace = Options.InputDirectory != null && Options.OutputAssetBundlePath != null;

			// ERROR: Not enough information for operations was specified
			if (!(dump || replace)) {
				Console.Error.WriteLine("To DUMP, the BUNDLE, MEMBER, and DUMP arguments must be specified.");
				Console.Error
					   .WriteLine("To REPLACE, the BUNDLE, MEMBER, INPUT, and OUTPUT arguments must be specified.");
				ShowHelp();
				return;
			}

			try { // Textures
				if (Options.Textures) {
					var uar = new TextureAsset(Options.InputAssetBundlePath);

					RunOperation(uar, dump, replace);
				}
				// Raw members
				else if (Options.Member != null) {
					var uar = new RawAsset(Options.InputAssetBundlePath, Options.Member);

					RunOperation(uar, dump, replace);
				}
				// ERROR: Not enough arguments provided
				else {
					Console.Error.WriteLine("A MEMBER must be specified or interact with TEXTURES.");
					ShowHelp();
				}
			}
			// ERROR: Could not open the file
			catch (IOException) { Console.Error.WriteLine("ERROR: Could not open the asset bundle!"); }
		}

		private static void RunOperation(Asset uar, bool dump, bool replace) {
			// Dump
			if (dump) uar.Dump(Options.DumpPath);

			// Replace
			if (replace) uar.Replace(Options.InputDirectory, Options.OutputAssetBundlePath);
		}

		// Function to show user how to ask for more help
		private static void ShowHelp() {
			Console.Error.WriteLine("Try `" + AppDomain.CurrentDomain.FriendlyName + " --help' for more information.");
		}

		// Arguments
		private class OptionVariables {
			public string DumpPath;
			public bool Help;
			public string InputAssetBundlePath;
			public string InputDirectory;
			public string Member;
			public string OutputAssetBundlePath;
			public bool Textures;
		}
	}
}
