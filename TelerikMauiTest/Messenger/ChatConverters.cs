using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;


using System.Runtime.CompilerServices;

using CommunityToolkit.Maui.Converters;
using System.ComponentModel;

namespace WebsoftWorkspaceM.Messenger.Converters;






public class UrlToImageAsyncConverter : IValueConverter
{
	protected ImageSource? initialValue = default;

	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var res = new GenericAsyncViewModel_ImageSource(initialValue);

		res.Start(AsyncConvert(value, targetType, parameter, culture));
		return res;
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		throw new NotImplementedException();
	}


	public UrlToImageAsyncConverter()
	{
		initialValue = ImageSource.FromFile("loader.gif");
	}

	public static void RunFeedImageWithPhotoElement(Image image, IPhotogenic? value)
	{
		Task.Run(() => GetPhotoElementImageSourceAsync(value).ContinueWith(t => MainThread.BeginInvokeOnMainThread(() => { try { image.IsAnimationPlaying = false; image.Source = t.Result; } catch (Exception e) { Console.WriteLine($"{nameof(RunFeedImageWithPhotoElement)} item set Error: {e}"); } })));
	}


	public async static Task<ImageSource?> GetPhotoElementImageSourceAsync(IPhotogenic? photoElement)
	{
		if (photoElement is not null && !string.IsNullOrEmpty(photoElement.ResUrl))
		{
			if (photoElement.ResType != IPhotogenic.RES_TYPE_Image)
			{
				return ImageSource.FromFile("dotnet_bot.png"); ;
			}
			else
			{
				string sVal = photoElement.ResUrl;

				var uri = new Uri(sVal);

			//	await Task.Delay(500).ConfigureAwait(false);

				return ImageSource.FromUri(uri);
			}
		}
		return null;
	}

	public static ImageSource? GetPhotoElementImageSource(FileResult? fr)
	{
		if (fr != null)
		{
			var ct = fr.ContentType;
			if (ct.StartsWith("image/"))
				return ImageSource.FromFile(fr.FullPath);
			else
				return ImageSource.FromFile("dotnet_bot.png");
		}
		return null;
	}

	public async static Task<ImageSource?> GetPhotoElementImageSourceAsync(object? value)
	{
		try
		{
			if (value != null)
			{
				switch (value)
				{
					case IPhotogenic photoElement:
						return await GetPhotoElementImageSourceAsync(photoElement).ConfigureAwait(false);
					case FileResult fr:
						return GetPhotoElementImageSource(fr);
				}

			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"{nameof(UrlToImageAsyncConverter)} error: {ex}");
		}
		return null;
	}






	public Task<ImageSource?> AsyncConvert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		return GetPhotoElementImageSourceAsync(value);
	}
}

public partial class GenericAsyncViewModel_ImageSource : INotifyPropertyChanged
{
	protected ImageSource? _result;
	public event PropertyChangedEventHandler? PropertyChanged;

	public GenericAsyncViewModel_ImageSource(ImageSource? initialValue)
	{
		_result = initialValue;
	}

	public ImageSource? Result
	{
		get => _result;
		set
		{
			_result = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
		}
	}

	public async void Start(Task<ImageSource?> task)
	{
		Result = await task.ConfigureAwait(false);
	}

}


