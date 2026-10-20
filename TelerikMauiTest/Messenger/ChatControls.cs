using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Layouts;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace WebsoftWorkspaceM.Messenger.Controls;


internal class MessageBlockFormattedLabel : ContentView
{
	static MessageBlockFormattedLabel()
	{
		linkTapCommand = new(LinkClicked_Handler);
	}

	private static void LinkClicked_Handler(Message.MessageTextBlock block)
	{

		var t = new Toast()
		{
			Text = $"Opening {block.LinkUri}"
		};
		t.Show();
	}

	static Command<Message.MessageTextBlock> linkTapCommand;


	[TypeConverter(typeof(ListStringTypeConverter))]
	public IList<string>? LabelStyleClass { get; set; }

	[TypeConverter(typeof(ListStringTypeConverter))]
	public IList<string>? LinkStyleClass { get; set; }

	public LineBreakMode LineBreakMode { get; set; } = LineBreakMode.WordWrap;

	public static readonly BindableProperty MessageDataSourceProperty = BindableProperty.Create(nameof(MessageDataSource), typeof(Message.MessageTextBlock[]), typeof(MessageBlockFormattedLabel), default(Message.MessageTextBlock[]), propertyChanged: OnSourceChanged);

	public Message.MessageTextBlock[]? MessageDataSource
	{
		get { return this.GetValue(MessageDataSourceProperty) as Message.MessageTextBlock[]; }
		set { this.SetValue(MessageDataSourceProperty, value); }
	}

	private static void OnSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var hl = (MessageBlockFormattedLabel)bindable;
		var blocks = (Message.MessageTextBlock[]?)newValue;

		hl.Compile(blocks);
	}

	private void Compile(Message.MessageTextBlock[]? blocks)
	{
		if (blocks is null || blocks.Length == 0)
			this.Content = null;
		else if (blocks.Length == 1)
		{
			this.Content = getContent(blocks[0]);
		}
		else
		{
			var flex = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Direction = Microsoft.Maui.Layouts.FlexDirection.Row, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start, AlignContent = Microsoft.Maui.Layouts.FlexAlignContent.Start };

			//var _i = 0;
			Message.MessageTextBlock? prevBlock = null;
			foreach (var block in blocks)
			{
				var _e = getContent(block, prevBlock);

				//_e.BackgroundColor = _cols[ _i++ % _cols.Length ];

				flex.Add(_e);

				prevBlock = block;
			}
			this.Content = flex;
		}

	}


	/*static Color[] _cols = [
		Colors.LightPink,
		Colors.LightCyan,
		Colors.LightGoldenrodYellow,
		Colors.LightGreen,
		Colors.LightYellow,
		Colors.LightSeaGreen
	];
*/





	View getContent(Message.MessageTextBlock block, Message.MessageTextBlock? prevBlock = null)
	{
		if (block.Flags.HasFlag(Message.MessageTextBlock.BlockFlags.Breaker)) //!!! experiment
		{
			View breakerBox;
			if (prevBlock is not null && prevBlock.Flags.HasFlag(Message.MessageTextBlock.BlockFlags.Breaker))
			{
				breakerBox = new Label()
				{
					Text =
#if IOS
				" "
#else
				string.Empty
#endif

				,
					HorizontalOptions = LayoutOptions.Fill,
					BackgroundColor = Colors.Transparent
				};
				FlexLayout.SetBasis(breakerBox, new FlexBasis(1.0f, true));
			}
			else
			{
				breakerBox = new BoxView() { HeightRequest = 0 };
				FlexLayout.SetBasis(breakerBox, new FlexBasis(1.0f, true));
			}
			return breakerBox;
		}
		else if (block.LinkUri is not null)
			return getLink(block);
		else
			return getLabel(block);
	}


	Label getLink(Message.MessageTextBlock block)
	{
		var link = new Label { Text = block.Text, LineBreakMode = this.LineBreakMode, TextDecorations = block.Decorations, FontAttributes = block.Attributes };
		var linkStyleClass = this.LinkStyleClass;
		if (linkStyleClass is not null)
			link.StyleClass = linkStyleClass;

		if (!string.IsNullOrEmpty(block.LinkUri))
		{
			link.GestureRecognizers.Add(new TapGestureRecognizer() { Command = linkTapCommand, CommandParameter = block });
		}
		return link;
	}
	Label getLabel(Message.MessageTextBlock block)
	{
		var lbl = new Label { Text = block.Text, TextType = TextType.Text, LineBreakMode = this.LineBreakMode, FontAttributes = block.Attributes, TextDecorations = block.Decorations };

		switch (block.Attributes)
		{
			case FontAttributes.Bold:
				lbl.FontFamily = "DefaultFontBold";
				break;
			case FontAttributes.Italic:
				lbl.FontFamily = "DefaultFontItalic";
				break;
			case FontAttributes.Bold | FontAttributes.Italic:
				lbl.FontFamily = "DefaultFontBoldItalic";
				break;
		}

		var lblStyleClass = this.LabelStyleClass;
		if (lblStyleClass != null)
			lbl.StyleClass = lblStyleClass;

		return lbl;
	}

}

internal class HyperLabelLight : ContentView
{

	public IList<string>? LabelStyleClass { get; set; }

	public IList<string>? LinkStyleClass { get; set; }

	public TextType TextType { get; set; } = TextType.Text;
	public LineBreakMode LineBreakMode { get; set; }

	public Command<string>? LinkTapCommand { get; set; }

	static Regex? linkParser = null;

	public void Compile(string rawString)
	{
		this.Content = compileInnards(rawString, LinkStyleClass, LabelStyleClass, LineBreakMode, TextType, LinkTapCommand);
	}

	public static View? QuickBuild(string rawString, IList<string> linkStyleClass, IList<string>? labelStyleClass, LineBreakMode lineBreakMode, TextType textType, System.Windows.Input.ICommand? tapCommand)
	{
		return compileInnards(rawString, linkStyleClass, labelStyleClass, lineBreakMode, textType, tapCommand);
	}

	static View? compileInnards(string rawString, IList<string>? linkStyleClass, IList<string>? labelStyleClass, LineBreakMode lineBreakMode, TextType textType, System.Windows.Input.ICommand? tapCommand)
	{

		if (string.IsNullOrEmpty(rawString))
			return null;

		if (linkParser == null)
			linkParser = new Regex(@"\b(?:\w{3,5}://|www\.)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		var matches = linkParser.Matches(rawString);
		var mCount = matches.Count;
		if (matches.Count > 0)
		{
			var flex = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Direction = Microsoft.Maui.Layouts.FlexDirection.Row, AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Start, AlignContent = Microsoft.Maui.Layouts.FlexAlignContent.Start };

			var split = linkParser.Split(rawString);
			var etr = matches.Select(m => m.Value).GetEnumerator();

			foreach (var s in split)
			{
				if (!string.IsNullOrEmpty(s))
					flex.Add(getLabel(s, labelStyleClass, textType, lineBreakMode));

				if (etr.MoveNext())
					flex.Add(getLink(etr.Current, linkStyleClass, lineBreakMode, tapCommand));

			}

			return flex;

		}
		else
			return getLabel(rawString, labelStyleClass, textType, lineBreakMode);
	}

	static Label getLink(string s, IList<string>? linkStyleClass, LineBreakMode lineBreakMode, System.Windows.Input.ICommand? tapCommand)
	{
		var link = new Label { Text = s, LineBreakMode = lineBreakMode };
		if (linkStyleClass != null)
			link.StyleClass = linkStyleClass;

		if (tapCommand != null)
		{
			link.GestureRecognizers.Add(new TapGestureRecognizer() { Command = tapCommand, CommandParameter = s });
		}
		return link;
	}
	static Label getLabel(string s, IList<string>? labelStyleClass, TextType textType, LineBreakMode lineBreakMode)
	{

		var lbl = new Label { Text = s, TextType = textType, LineBreakMode = lineBreakMode };
		if (labelStyleClass != null)
			lbl.StyleClass = labelStyleClass;


		return lbl;
	}



}


internal class ForwardBlock : ContentView
{
	bool requestedvisible = true;
	bool empty = true;

	public DataTemplate? Template { get; set; }

	public static readonly BindableProperty SourceProperty = BindableProperty.Create("Source", typeof(Forward), typeof(ForwardBlock), null, propertyChanged: OnItemSourceChanged);

	public Forward Source
	{
		get { return (Forward)this.GetValue(SourceProperty); }
		set { this.SetValue(SourceProperty, (object)value); }
	}

	public static new readonly BindableProperty IsVisibleProperty = BindableProperty.Create("IsVisible", typeof(bool), typeof(ForwardBlock), true, propertyChanged: onVisibleChanged);

	public new bool IsVisible
	{

		get => (bool)this.GetValue(IsVisibleProperty);

		set
		{
			requestedvisible = value;

			base.SetValue(VerticalStackLayout.IsVisibleProperty, (object)(requestedvisible && !empty));
		}

	}
	static void onVisibleChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var thisRigidView = bindable as ForwardBlock;
		thisRigidView!.IsVisible = (bool)newValue;
	}


	static void OnItemSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var thisRigidView = bindable as ForwardBlock;

		thisRigidView!.Populate((Forward)newValue);
	}


	void Populate(Forward dataArray)
	{
		//var items = this.Children.ToArray();
		this.Content = null;

		var template = Template;
		if (template != null && dataArray != null && dataArray.HasSomething)
		{
			var content = template.CreateContent() as View;

			content!.BindingContext = dataArray;

			content.ClassId = "MessengerForwardBlock";

			this.Content = content;

			empty = false;

		}
		else
			empty = true;

		IsVisible = requestedvisible;

	}
}







public partial class ChatIcon : Grid
{

	static Command<string> usrTapCommand = new(UsrClicked_Handler);

	Border? child = null;

	Ellipse? pimple = null;

	double hardSize = 0;
	public double HardSize { get => hardSize; set => hardSize = value; }

	public bool Updatable { get; set; }

	string? trackField = null;
	string? trackValue = null;

	public bool Alive { get; set; } = true;

	bool hasPimple = true;
	public bool HasPimple { get => hasPimple; set => hasPimple = value; }

	public string? ElementStyleClassPrefix { get; set; }

	public bool Interactive { get; set; }

	WeakReference<IPhotogenic>? photoElementRef = null;

	public static readonly BindableProperty SourceProperty = BindableProperty.Create(nameof(Source), typeof(IPhotogenic), typeof(ChatIcon), null, propertyChanged: OnItemSourceChanged, defaultBindingMode: BindingMode.OneTime);

	public IPhotogenic Source
	{
		get { return (IPhotogenic)this.GetValue(SourceProperty); }
		set { this.SetValue(SourceProperty, value); }
	}

	public static new readonly BindableProperty IsVisibleProperty = BindableProperty.Create(nameof(IsVisible), typeof(bool), typeof(ChatIcon), true, propertyChanged: OnVisibilityChanged, defaultBindingMode: BindingMode.OneWay);

	static void OnVisibilityChanged(BindableObject bindable, object oldValue, object newValue)
	{
		if ((bool)newValue)
		{
			var thisControl = bindable as ChatIcon;
			if (thisControl!.photoElementRef?.TryGetTarget(out var photoEleme) == true)
				thisControl.PassPhotoElement(photoEleme);
		}
	}

	static void OnItemSourceChanged(BindableObject bindable, object oldValue, object newValue)
	{
		var thisControl = bindable as ChatIcon;
		var photoElement = newValue as IPhotogenic;
		if (photoElement is not null)
			thisControl!.PassPhotoElement(photoElement);
	}

	public void RefreshVisual()
	{
		if (this.photoElementRef?.TryGetTarget(out var photoEleme) == true)
			MainThread.BeginInvokeOnMainThread(() => innerChild(photoEleme));
	}


	public static (string? Initials, int Class) GetTwoLetterAndClass(string? text)
	{
		int i, @class;
		if (string.IsNullOrEmpty(text))
		{
			@class = 0;
		}
		else
		{
			text = text.Trim();
			i = text.IndexOf(' ');
			if (i > 0 && i < text.Length - 1)
			{
				char c = char.MinValue;
				for (var j = i + 1; j < text.Length; j++)
					if (char.IsLetterOrDigit(text, j))
					{
						c = text[j];
						break;
					}
				if (c != char.MinValue)
					text = (text[0..1] + c).ToUpper();
				else if (text.Length > 2)
					text = text[..2];

				//text = (text[0..1] + text.Substring(i + 1, 1)).ToUpper();
			}
			else if (text.Length > 2)
				text = text[..2];

			if (text.Length > 0)
			{
				var iCode = text[0];
				if (iCode >= 1040 && iCode <= 1071)
				{
					@class = iCode - 1039;
				}
				else if (iCode >= 65 && iCode <= 90)
				{
					@class = iCode - 64;
				}
				else
					@class = 0;
			}
			else
			{
				@class = 0;
			}
		}

		return (text, @class);
	}
	public static void SetTwoLettersAndClass(string? text, Label label, View container)
	{
		(text, var @class) = GetTwoLetterAndClass(text);


		label.Text = text;

		label.TextColor = Colors.Blue;
		container.BackgroundColor = Colors.Gray;
	}


	void PassPhotoElement(IPhotogenic photoElement)
	{
		var force = Updatable;

		if (photoElementRef is null || force)
			photoElementRef = new(photoElement);

		if (this.IsVisible && (child is null || force) /* this.Content == null*/)
		{
			innerChild(photoElement);

			if (Interactive && photoElement.ClassName == nameof(User))
			{
				child!.GestureRecognizers.Add(new TapGestureRecognizer() { Command = usrTapCommand, CommandParameter = photoElement.ID });
			}


			if (Alive)
				switch (photoElement)
				{
					case User u:
						if (hasPimple)
						{
							if (pimple is null)
							{
								//photoElement.WE_DataPropertyChanged += PropertyChanged_Handler;
								
								
							
								this.Add(new Ellipse() { VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.End, ZIndex = 9, WidthRequest = 12, HeightRequest = 12, BackgroundColor = Colors.Green });
								this.pimple = new Ellipse() { VerticalOptions = LayoutOptions.Start, HorizontalOptions = LayoutOptions.End, ZIndex = 10, WidthRequest = 8, HeightRequest = 8, Margin = 2, BackgroundColor = Colors.Lime };

								this.Add(pimple);
							}
							
						}
						break;
				}


		}


	}

	void innerChild(IPhotogenic photoElement)
	{
		if (child is not null)
		{
			this.Remove(child);
		}


		child =

				new Border()
				{

				};

		if (hardSize > 0)
		{
			child.WidthRequest = hardSize;
			child.HeightRequest = hardSize;
			child.StrokeShape = new RoundRectangle() { CornerRadius = hardSize / 2 };
		}



		if (string.IsNullOrEmpty(photoElement.ResUrl))
		{
			var label = new Label() { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, StyleClass = [$"{ElementStyleClassPrefix}ChatIconLabel"], BackgroundColor = Colors.Transparent /* HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center*/ };


			SetTwoLettersAndClass(photoElement.Name, label, child);

			child.Content = label;
			child.StyleClass = [$"{ElementStyleClassPrefix}ChatIcon"];

			if (hardSize > 0)
			{
				label.FontSize = hardSize * 9 / 20;
			}
		}
		else
		{
			var core = new Image() { Aspect = Aspect.AspectFill, HorizontalOptions = LayoutOptions.Fill, VerticalOptions = LayoutOptions.Fill, IsAnimationPlaying = true, Source = ImageSource.FromFile("loader.gif") };

			//_ = Converters.UrlToImageAsyncConverter.FeedImageWithPhotoElement(core, photoElement);
			Converters.UrlToImageAsyncConverter.RunFeedImageWithPhotoElement(core, photoElement);

			child.StyleClass = [$"{ElementStyleClassPrefix}ChatIcon"];
			child.Content = core;

		}

		this.Add(child);

	}

	void pimpleColor(string? status)
	{
		if (status == "online")
		{
			
		}
		else
		{
			
		}
	}


	public void Init(bool visible, IPhotogenic photoElem)
	{
		IsVisible = visible;

		if (photoElem != null)
		{
			this.PassPhotoElement(photoElem);
		}
	}

	public ChatIcon()
	{
		//this.StyleClass = new string[] { "ChatIconOut" };
		VerticalOptions = LayoutOptions.Start;
		HorizontalOptions = LayoutOptions.Start;


		//this.BackgroundColor = Colors.Transparent;
	}

	
	private static void UsrClicked_Handler(string usrId)
	{
		//TODO user icon tapped
#if DEBUG
		var t = new Toast() { Text = usrId };
		t.Show();
#endif
	}

}






internal class IOS_crutch_ForwardLine :
#if IOS
ContentView
#else
BoxView
#endif
;

