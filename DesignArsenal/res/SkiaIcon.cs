using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace SkiaSharp
{
	public class IconPath
	{
		public SKRect bounds;
		public string[] paths;
		public string[] fills;
	}

	public class IconPainter
	{
		public IconPath Icon { get; set; }
		public int IconPadding { get; set; }
		public Color DefColor { get; set; } = Color.Black;

		public IconPainter() { }

		public IconPainter(IconPath icon)
			: this(icon, Color.Black, 0) { }

		public IconPainter(IconPath icon, Color clr, int padding = 0)
		{
			Icon = icon;
			IconPadding = padding;
			DefColor = clr;
		}

		public void Draw(SKPaintSurfaceEventArgs e)
		{
			SKImageInfo info = e.Info;
			SKSurface surface = e.Surface;
			SKCanvas canvas = surface.Canvas;

			//canvas.Clear(SKColors.Green);

			var path = SKPath.ParseSvgPathData(Icon.paths[0]);
			var m = SKMatrix.MakeTranslation(-Icon.bounds.Left, -Icon.bounds.Top);
			path.Transform(m);
			var contain_min = Math.Min(info.Width, info.Height) - IconPadding * 2;
			m = SKMatrix.MakeScale(contain_min / Icon.bounds.Width, contain_min / Icon.bounds.Width);
			path.Transform(m);
			m = SKMatrix.MakeTranslation(IconPadding, IconPadding);
			path.Transform(m);

			SKPaint paint = new SKPaint
			{
				StrokeWidth = 0,
				Style = SKPaintStyle.Fill,
				Color = DefColor.ToSKColor(),
				IsAntialias = true
			};

			canvas.DrawPath(path, paint);
		}
	}

	public class SKIcon : SKCanvasView
	{
		public IconPath Icon
		{
			get => _painter.Icon;
			set => _painter.Icon = value;
		}

		public int Size
		{
			set => HeightRequest = WidthRequest = value;
		}

		private IconPainter _painter = new IconPainter();

		public SKIcon()
		{
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, false);
			VerticalOptions = new LayoutOptions(LayoutAlignment.Center, false);

			Size = 20;
			PaintSurface += SkiaIcon_PaintSurface;
			SizeChanged += SKIcon_SizeChanged;

			if(DesignMode.IsDesignModeEnabled)
			{
				BackgroundColor = Color.Yellow;
			}
		}

		private void SKIcon_SizeChanged(object sender, EventArgs e)
		{
			InvalidateSurface();
		}

		private void SkiaIcon_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			_painter.Draw(e);
		}
	}
}