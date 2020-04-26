using System;
using System.Diagnostics;
using System.IO;
using SkiaSharp;

namespace SkiaDraw
{
	public class SKIcon
	{
		public SKRect bounds;
		public string[] paths;
		public string[] fills;
	}

	public class SkiaDrawer
	{
		public static SKIcon iconBarChart = new SKIcon()
		{
			bounds = new SKRect(42.666f, 85.333f, 981.334f, 938.668f),
			paths = new string[] { "M938.667 85.333h-170.667c-25.6 0-42.667 17.067-42.667 42.667v768c0 25.6 17.067 42.667 42.667 42.667h170.667c25.6 0 42.667-17.067 42.667-42.667v-768c0-25.6-17.067-42.667-42.667-42.667zM896 853.333h-85.333v-682.667h85.333v682.667z", "M597.333 298.667h-170.667c-25.6 0-42.667 17.067-42.667 42.667v554.667c0 25.6 17.067 42.667 42.667 42.667h170.667c25.6 0 42.667-17.067 42.667-42.667v-554.667c0-25.6-17.067-42.667-42.667-42.667zM554.667 853.333h-85.333v-469.333h85.333v469.333z", "M256 512h-170.667c-25.6 0-42.667 17.067-42.667 42.667v341.333c0 25.6 17.067 42.667 42.667 42.667h170.667c25.6 0 42.667-17.067 42.667-42.667v-341.333c0-25.6-17.067-42.667-42.667-42.667zM213.333 853.333h-85.333v-256h85.333v256z", },
			fills = new string[] { },
		};

		public static SKBitmap DrawIcon(SKIcon icon, int boxwidth = 200)
		{
			Debug.WriteLine("DrawIcon");

			float scalefactor = boxwidth / icon.bounds.Width;
			int width = boxwidth;
			int height = (int) Math.Ceiling(icon.bounds.Height * scalefactor);

			var bmp = new SKBitmap(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

			using(var canvas = new SKCanvas(bmp))
			{
				canvas.Translate(-icon.bounds.Left * scalefactor, -icon.bounds.Top * scalefactor);
				canvas.Scale(scalefactor, scalefactor);
				canvas.Clear();

				SKPaint sp = new SKPaint()
				{
					Color = SKColors.Black,
					IsAntialias = true
				};

				foreach(var svgpath in icon.paths)
				{
					var svg = SKPath.ParseSvgPathData(svgpath);
					//var svg = SvgSkiaParser.FromPath(svgpath);
					canvas.DrawPath(svg, sp);
				}
			}
			return bmp;
		}
	}
}