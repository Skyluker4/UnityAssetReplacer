using System;

using Mono.Options;

namespace UnityAssetReplacer {
	internal static class Program {
		// Entry point
		public static void Main(string[] args) {
			// Arguments
			string inputAssetBundlePath = null;
			string member = null;
			string outputAssetBundlePath = null;
			string inputDirectory = null;
			string dumpPath = null;
			var textures = false;
			var help = false;

			// Instructions
			var options = new OptionSet {
				{ "b|bundle=", "the original asset {BUNDLE} path", v => inputAssetBundlePath = v },
				{ "i|input=", "the {INPUT} directory of the assets to overwrite with.", v => inputDirectory = v },
				{ "o|output=", "the path of the asset bundle you wish to {OUTPUT}.", v => outputAssetBundlePath = v },
				{ "d|dump=", "the path of the directory you wish to {DUMP} to.", v => dumpPath = v },
				{ "m|member=", "the {MEMBER} you dump/overwrite.", v => member = v },
				{ "t|texture", "Interact with {TEXTURE}s in the asset bundle.", v => textures = v != null },
				{ "h|?|help", "show this message for and then exit.", v => help = v != null }
			};

			// Parse arguments
			try { options.Parse(args); }
			catch (OptionException e) {
				Console.WriteLine(e.Message);
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			// Display help command if specified and then exit
			if (help) {
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			// Make sure bundle and member are not null
			if (inputAssetBundlePath != null) {
				// Check what can be run
				var dump = dumpPath != null;
				var replace = inputDirectory != null && outputAssetBundlePath != null;

				// If nothing can be run, display help message and exit
				if (!(dump || replace)) {
					Console.Error.WriteLine("To DUMP, the BUNDLE, MEMBER, and DUMP arguments must be specified.");
					Console.Error
					       .WriteLine("To REPLACE, the BUNDLE, MEMBER, INPUT, and OUTPUT arguments must be specified.");
					Help();
					return;
				}

				// Textures
				if (textures) {
					// Initialize
					var uar = new UnityAssetReplacer(inputAssetBundlePath);

					// Dump 
					if (dump) uar.DumpTextures(dumpPath);

					// Replace
					if (replace) uar.ReplaceTextures(inputDirectory, outputAssetBundlePath);
				}

				// Members
				else if (member != null) {
					// Initialize
					var uar = new UnityAssetReplacer(inputAssetBundlePath, member);

					// Dump if arguments are provided
					if (dump) uar.DumpAssets(dumpPath);

					// Replace
					if (inputDirectory != null && outputAssetBundlePath != null)
						uar.ReplaceAssets(inputDirectory, outputAssetBundlePath);
				}

				// Error - Not enough arguments provided
				else {
					Console.Error.WriteLine("A MEMBER must be specified or interact with TEXTURES.");
					Help();
				}
			}
			else {
				Console.Error.WriteLine("An asset BUNDLE must be specified.");
				Help();
			}
		}

		// Function to show user how to ask for more help
		private static void Help() {
			Console.Error.WriteLine("Try `" + AppDomain.CurrentDomain.FriendlyName + " --help' for more information.");
		}
	}
}
