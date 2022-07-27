using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using ImageSharp = SixLabors.ImageSharp;

namespace TestImageSharpWpf
{
	/// <summary>
	/// MainWindow.xaml のインタラクションロジック
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		/// <summary>
		/// ファイルとして保存する処理なので右側には何も表示しない。
		/// 参考：https://stackoverflow.com/questions/50860392/how-to-combine-two-images
		/// </summary>
		private void Process1(string fileName)
		{
			try
			{
				using (var img1 = ImageSharp.Image.Load<Rgba32>(fileName)) // load up source images
				using (var img2 = ImageSharp.Image.Load<Rgba32>(fileName))
				using (var outputImage = new Image<Rgba32>(200, 150)) // create output image of the correct dimensions
				{
					// reduce source images to correct dimensions
					// skip if already correct size
					// if you need to use source images else where use Clone and take the result instead
					img1.Mutate(o => o.Resize(new ImageSharp.Size(100, 150)));
					img2.Mutate(o => o.Resize(new ImageSharp.Size(100, 150)));

					// take the 2 source images and draw them onto the image
					outputImage.Mutate(o => o
							.DrawImage(img1, new ImageSharp.Point(0, 0), 1f) // draw the first one top left
							.DrawImage(img2, new ImageSharp.Point(100, 0), 1f) // draw the second next to it
					);

					outputImage.Save("output.png");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private void Process2(string fileName)
		{
			try
			{
				using (var img1 = ImageSharp.Image.Load<Rgba32>(fileName))
				using (var outputImage = new Image<Rgba32>(img1.Width, img1.Height))
				{
					img1.Mutate(x => x.Crop(new ImageSharp.Rectangle(0, 0, img1.Width, img1.Height / 2)));
					outputImage.Mutate(o => o
							.DrawImage(img1, new ImageSharp.Point(0, 0), 1f)
							.DrawImage(img1, new ImageSharp.Point(0, img1.Height), 1f)
					);

					// WPF の Image として表示させる処理
					DestinationImage.Source = ImageSharpToImageSource(outputImage);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

		private ImageSource ImageSharpToImageSource(ImageSharp.Image<Rgba32> image)
		{
			var bmp = new WriteableBitmap(image.Width, image.Height, image.Metadata.HorizontalResolution, image.Metadata.VerticalResolution, PixelFormats.Bgra32, null);

			bmp.Lock();
			try
			{
				var backBuffer = bmp.BackBuffer;

				for (var y = 0; y < image.Height; y++)
				{
					for (var x = 0; x < image.Width; x++)
					{
						var backBufferPos = backBuffer + (y * image.Width + x) * 4;
						var rgba = image[x, y];
						var color = rgba.A << 24 | rgba.R << 16 | rgba.G << 8 | rgba.B;

						System.Runtime.InteropServices.Marshal.WriteInt32(backBufferPos, color);
					}
				}

				bmp.AddDirtyRect(new Int32Rect(0, 0, image.Width, image.Height));
			}
			finally
			{
				bmp.Unlock();
			}
			return bmp;
		}

		private void SourceImage_DragOver(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				e.Effects = DragDropEffects.All;
			}
			else
			{
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
		}

		private void SourceImage_Drop(object sender, DragEventArgs e)
		{
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				var fileNames = (string[])e.Data.GetData(DataFormats.FileDrop);
				if (fileNames.Length == 0) return;
				var fileName = fileNames[0];

				var bmpImage = new BitmapImage();
				using (var stream = File.OpenRead(fileName))
				{
					bmpImage.BeginInit();
					bmpImage.StreamSource = stream;
					bmpImage.DecodePixelWidth = 500;
					bmpImage.CacheOption = BitmapCacheOption.OnLoad;
					bmpImage.CreateOptions = BitmapCreateOptions.None;
					bmpImage.EndInit();
				}

				SourceImage.Source = bmpImage;
				ImageSourceLabel.Opacity = 0;

				//Process1(fileName);
				Process2(fileName);
			}
		}
	}
}
