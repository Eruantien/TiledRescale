using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TiledRescale;

namespace ConsoleTest
{
	internal static class Program
	{
		private static void RunOptionsAndReturnExitCode(Options options)
		{
			var valid = true;

			if (options.Scale == null && options.Width == null && options.Height == null)
			{
				Console.WriteLine("Please provide --scale or --width and --height");
				valid = false;
			}

			if (string.IsNullOrWhiteSpace(options.Directory) && !options.Files.Any())
			{
				Console.WriteLine("Please provide --directory or --files");
				valid = false;
			}

			if (!valid) return;

			var fileNames = GetFileNames(options);

			ProcessFiles(fileNames, options.Width, options.Height, options.Scale);
		}

		private static void ProcessFiles(IEnumerable<string> fileNames, int? width, int? height, float? scale)
		{
			var rescaler = new Rescaler();

			foreach (var fileName in fileNames)
			{
				var sb = new StringBuilder($"{fileName} - ");

				try
				{
					var filePathArray = fileName.Split("\\");
					var fileNameArray = filePathArray.Last().Split(".");
					if (!int.TryParse(fileNameArray[0], out var _)) continue;

					var result = rescaler.RescaleMap(fileName, width, height, scale);

					sb.Append(!string.IsNullOrWhiteSpace(result.ErrorMessage)
						? result.ErrorMessage
						: $"rescaled from [ {result.OldWidth},{result.OldHeight} ] to [ {result.NewWidth},{result.NewHeight} ] ");
				}
				catch (Exception ex)
				{
					sb.Append(ex);
				}

				Console.WriteLine(sb.ToString());
			}
		}

		private static IEnumerable<string> GetFileNames(Options options)
		{
			var files = !string.IsNullOrWhiteSpace(options.Directory)
				? Directory.GetFiles(options.Directory, "*.tmx", SearchOption.AllDirectories).ToList()
				: options.Files.ToList();

			return files;
		}

		private static void HandleParseError(IEnumerable<Error> errors)
		{
			foreach (var error in errors)
			{
				Console.WriteLine(error);
			}
		}

		private static void Main(string[] args)
		{
			Parser.Default.ParseArguments<Options>(args).WithParsed(RunOptionsAndReturnExitCode).WithNotParsed(HandleParseError);
		}
	}

	internal class Options
	{
		[Option('w', "width", HelpText = "Target width for map.")]
		public int? Width { get; set; }
		[Option('h', "height", HelpText = "Target height for map.")]
		public int? Height { get; set; }
		[Option('s', "scale", HelpText = "Scale to change dimensions by.")]
		public float? Scale { get; set; }
		[Option('f', "Files", HelpText = "List of files for processing.")]
		public IEnumerable<string> Files { get; set; }
		[Option('d', "Directory", HelpText = "Directory for processing.")]
		public string Directory { get; set; }
	}
}
