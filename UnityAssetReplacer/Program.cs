using System;

using Mono.Options;

namespace UnityAssetReplacer {
	internal static class Program {
		// Arguments
		private static string _inputAssetBundlePath;
		private static string _member;
		private static string _outputAssetBundlePath;
		private static string _inputDirectory;
		private static string _dumpPath;
		private static bool _textures;
		private static bool _help;

		// Instructions
		private static readonly OptionSet options = new OptionSet {
			{ "b|bundle=", "the original asset {BUNDLE} path", v => _inputAssetBundlePath = v },
			{ "i|input=", "the {INPUT} directory of the assets to overwrite with.", v => _inputDirectory = v },
			{ "o|output=", "the path of the asset bundle you wish to {OUTPUT}.", v => _outputAssetBundlePath = v },
			{ "d|dump=", "the path of the directory you wish to {DUMP} to.", v => _dumpPath = v },
			{ "m|member=", "the {MEMBER} you dump/overwrite.", v => _member = v },
			{ "t|texture", "Interact with {TEXTURE}s in the asset bundle.", v => _textures = v != null },
			{ "h|?|help", "show this message for and then exit.", v => _help = v != null }
		};

		// Entry point
		public static void Main(string[] args) {
			// Parse arguments
			try { options.Parse(args); }
			catch (OptionException e) {
				Console.WriteLine(e.Message);
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			// Display help command if specified and then exit
			if (_help) {
				options.WriteOptionDescriptions(Console.Out);
				return;
			}

			// Make sure bundle and member are not null
			if (_inputAssetBundlePath != null) {
				// Check what can be run
				var dump = _dumpPath != null;
				var replace = _inputDirectory != null && _outputAssetBundlePath != null;

				// If nothing can be run, display help message and exit
				if (!(dump || replace)) {
					Console.Error.WriteLine("To DUMP, the BUNDLE, MEMBER, and DUMP arguments must be specified.");
					Console.Error
						.WriteLine("To REPLACE, the BUNDLE, MEMBER, INPUT, and OUTPUT arguments must be specified.");
					Help();
					return;
				}

				// Textures
				if (_textures) {
					HandleTextures(dump, replace);
				}

				// Raw members
				else if (_member != null) {
					HandleMembers(dump, replace);
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

		// Function to handle raw members
		private static void HandleMembers(bool dump, bool replace) {
			// Initialize
			var uar = new UnityAssetReplacer(_inputAssetBundlePath, _member);

			// Dump if arguments are provided
			if (dump) uar.DumpAssets(_dumpPath);

			// Replace
			if (replace)
				uar.ReplaceAssets(_inputDirectory, _outputAssetBundlePath);
		}

		// Function to handle textures
		private static void HandleTextures(bool dump, bool replace) {
			// Initialize
			var uar = new UnityAssetReplacer(_inputAssetBundlePath);

			// Dump
			if (dump) uar.DumpTextures(_dumpPath);

			// Replace
			if (replace) uar.ReplaceTextures(_inputDirectory, _outputAssetBundlePath);
		}

		// Function to show user how to ask for more help
		private static void Help() {
			Console.Error.WriteLine("Try `" + AppDomain.CurrentDomain.FriendlyName + " --help' for more information.");
		}
	}
}
