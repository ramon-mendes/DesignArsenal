using System;
using System.Collections.Generic;
using System.Text;

using Xamarin.Forms;
using SkiaSharp;
using SkiaSharp.Views.Forms;

namespace SkiaSharp
{
	class SKButton : AbsoluteLayout
	{
		private SKCanvasView _bg = new SKCanvasView();
		private StackLayout _stack;

		public bool Round { get; set; }
		public int Radius { get; set; } = 10;
		public Color BtnBgColor { get; set; } = Color.Black;

		public event EventHandler Clicked;

		public SKButton()
		{
			var tapGestureRecognizer = new TapGestureRecognizer();
			tapGestureRecognizer.Tapped += (s, e) => {
				Clicked?.Invoke(s, e);
			};
			GestureRecognizers.Add(tapGestureRecognizer);

			Children.Add(_bg);
			ChildAdded += SKButton_ChildAdded;
			HorizontalOptions = new LayoutOptions(LayoutAlignment.Center, true);
			VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true);

			_bg.PaintSurface += SKButton_PaintSurface;
			SizeChanged += SKButton_SizeChanged;

			if(DesignMode.IsDesignModeEnabled)
			{
				BackgroundColor = Color.Red;
			}
		}

		private void SKButton_ChildAdded(object sender, ElementEventArgs e)
		{
			_stack = e.Element as StackLayout;
		}

		private void SKButton_SizeChanged(object sender, EventArgs e)
		{
			SetLayoutBounds(_bg, new Rectangle(0, 0, Width, Height));
			if(_stack != null)
			{
				//_stack.Orientation = StackOrientation.Horizontal;
				SetLayoutBounds(_stack, new Rectangle(0, 0, Width, Height));
			}
			_bg.InvalidateSurface();
		}

		private void SKButton_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
		{
			SKImageInfo info = e.Info;
			SKSurface surface = e.Surface;
			SKCanvas canvas = surface.Canvas;

			SKPaint paint = new SKPaint
			{
				StrokeWidth = 0,
				Style = SKPaintStyle.Fill,
				Color = BtnBgColor.ToSKColor(),
				IsAntialias = true
			};

			if(Round)
				Radius = int.MaxValue;

			canvas.DrawRoundRect(new SKRoundRect(info.Rect, Radius, Radius), paint);
		}
	}
}