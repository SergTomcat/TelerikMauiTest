using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebsoftWorkspaceM.Messenger;


public interface IPhotogenic
{
	const string RES_TYPE_Image = "img";

	string ID { get; }
	string? Name { get; }
	string? ResUrl { get; }
	string? ResType { get; }

	string ClassName { get; }

	IPhotogenic VirtualPhotoElement { get; }


}

public class User : IPhotogenic
{
	public User() { }


	public static User Create(string id)
	{

		return new User() { ID = id };

	}


	[JsonPropertyName("id")]
	public required string ID { get; init; }

	[JsonPropertyName("fullname")]
	public string? Name { get; set; }

	//bool isMe;
	//[JsonIgnore]
	public bool IsMe { get; set; }

	[JsonPropertyName("icon_url")]
	public string? ResUrl { get; set; }

	[JsonIgnore]
	public string ResType => IPhotogenic.RES_TYPE_Image;

	[JsonIgnore]
	public string ClassName => nameof(User);


	WeakEventManager? _weakEventManager = null;

	
	[JsonIgnore]
	public IPhotogenic VirtualPhotoElement => this;

}



public class Attachment : IPhotogenic
{
	[JsonPropertyName("id")]
	public required string ID { get; set; }

	[JsonPropertyName("message_id")]
	public string? MessageID { get; set; }

	[JsonIgnore]
	public string? Name { get => FileName; set => FileName = value; }

	[JsonPropertyName("name")]
	public string? FileName { get; set; }

	[JsonPropertyName("type")]
	public string? ResType { get; set; }

	[JsonPropertyName("url")]
	public string? ResUrl { get; set; }

	[JsonPropertyName("message_date")]
	
	public DateTime MessageDate { get; set; }

	[JsonIgnore]
	public Attachment Me => this;

	[JsonIgnore]
	public string ClassName => nameof(Attachment);

	[JsonIgnore]
	public IPhotogenic VirtualPhotoElement => this;






	[JsonIgnore]
	public Attachment[]? Tribe { get; set; }
}


public class Message : INotifyPropertyChanged
{
	public enum MessageType
	{
		Text,
		System,
		Forwarded,
		ARTIFICIAL_TIME,
		BIG_MESSAGE,
	}

	public class MessageTextBlock
	{
		[Flags]
		public enum BlockFlags
		{
			None = 0,
			Breaker = 1 << 0,
			Local = 1 << 1,
		}



		public required string Text { get; set; }

		public FontAttributes Attributes { get; set; }
		public TextDecorations Decorations { get; set; }

		public string? LinkUri { get; set; }
		
		public BlockFlags Flags { get; set; }

	}

	public class MessageForward
	{
		public required Message Message { get; set; }
		public string? ConversationId { get; set; }
		public string? Title { get; set; }
	}

	public string? ID { get; set; }
	public string? BlockMessageID { get; set; }
	public MessageTextBlock[]? TextBlocks { get; set; }


	public string Text
	{
		get
		{
			var blocks = TextBlocks;
			if (blocks == null || blocks.Length == 0)
				return string.Empty;

			if (blocks.Length == 1)
				return blocks[0].Text;

			StringBuilder sb = new StringBuilder();
			foreach (var tb in blocks)
			{
				if (tb.Flags.HasFlag(Message.MessageTextBlock.BlockFlags.Breaker))
					sb.Append(Environment.NewLine);
				else
					sb.Append(tb.Text);
			}
			return sb.ToString();
		}
	}


	public DateTime Time { get; set; }
	public DateTime? EditTime { get; set; }

	public string TimeCute
	{
		get
		{
			var cuteTime = Time.ToString();
			if (EditTime != null)
			{
				cuteTime = string.Format("{0} changed {1}", cuteTime, EditTime.Value.ToString());
			}

			return cuteTime;
		}
	}

	public User? User { get; set; }

	public MessageType Type { get; set; }

	public bool HasSomething => Attachments != null && Attachments.Length > 0;
	public Attachment[]? Attachments { get; set; }


	public bool StreakBreak { get; set; }


	bool activeFlag;
	public bool ActiveFlag { get => activeFlag; set => activeFlag = value; }

	public bool ActiveFlagUpdate { set { if (activeFlag != value) { activeFlag = value; OnPropertyChanged(nameof(ActiveFlag)); } } }

	bool isRead;
	public bool IsRead { get => isRead; set => isRead = value; }
	public bool IsReadUpdate { set { if (isRead != value) { isRead = value; OnPropertyChanged(nameof(IsRead)); } } }

	public bool IsReadByMe { get; set; }

	public Message? Reply { get; set; }
	public MessageForward? Forwarded { get; set; }

	public bool HasForward => Forwarded != null;
	public bool HasReply => Reply != null;

	bool selected;

	public bool Selected { get => selected; set { selected = value; } }
	public bool SelectedLoud { get => selected; set { selected = value; OnPropertyChanged(nameof(SelectedLoud));  } }


	public bool SelectionMode => false;

	public void UpdateSelectionMode()
	{
		OnPropertyChanged(nameof(SelectedLoud));
		OnPropertyChanged(nameof(SelectionMode));
	}


	protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public event PropertyChangedEventHandler? PropertyChanged;


	public override string ToString()
	{
		return $"{User?.Name}({Type}) {Text?.Substring(0, Math.Min(20, Text.Length))}";
	}

	public string? PreviewText => PreviewTextFormat(Text, Attachments?.Length ?? 0);

	public static string? PreviewTextFormat(string? text, int attachments)
	{
		const int maxLength = 50;

		if (string.IsNullOrEmpty(text))
		{
			if (attachments > 0)
				text = $"files: {attachments}";

		}
		else
		{
			text = $"{(attachments > 0 ? "📎 " : string.Empty)}{(text.Length > maxLength ? text = text[0..maxLength] : text)}";
		}
		return text;
	}

	public string ForwardInfo
	{
		get
		{
			string text;
			var fwd = Forwarded;
			if (fwd == null)
				text = string.Empty;
			else
			{
				var title = fwd.Title;
				//var userName = this.User?.Name; //<- this one to show who forwarded
				var userName = fwd.Message?.User?.Name; //<- this one to show who wrote it in the first place

				if (string.IsNullOrEmpty(title))
					text = string.Format("from {0}", userName);
				else
				{
					text = $"{(userName)} {(title)}";
				}
			}
			return text;
		}
	}


	Reaction[]? _reactions;

	public bool HasReactions => _reactions != null && _reactions.Length > 0;
	public Reaction[]? ReactionsLoud
	{
		set
		{
			var oldVal = _reactions;
			_reactions = value;
			OnPropertyChanged(nameof(Reactions));
			if ((value is not null && value.Length > 0) != (oldVal is not null && oldVal.Length > 0))
				OnPropertyChanged(nameof(HasReactions));
		}
	}
	public Reaction[]? Reactions { get => _reactions; set { _reactions = value; } }

}



public class Reaction
{

#pragma warning disable CS8618 // are set by required Id
	string _id;
	string _emoji;
#pragma warning restore CS8618

	[JsonPropertyName("reaction_id")]
	public required string Id
	{
		get => _id; init
		{
			_id = value;

			if (int.TryParse(_id, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var codePoint))
			{
				// handles astral-plane code points


				if (codePoint == 0x2764) //TODO: hardcode for "Heavy Black Heart" to paint it red vith variation selectors, until supported on server
				{
					int variationSelector = 0xFE0F; //red heart variant

					_emoji = $"{char.ConvertFromUtf32(codePoint)}{char.ConvertFromUtf32(variationSelector)}";
				}
				else
					_emoji = char.ConvertFromUtf32(codePoint);
			}
			else
				_emoji = string.Empty;

		}
	}


	public string Emoji => _emoji;

	[JsonPropertyName("reaction_count")]
	public int Counter { get; set; }

	public bool Mine { get; set; }
}


public class Forward
{
	public Forward(IEnumerable<Message>? messages)
	{
		Messages = messages?.ToArray();
		
		conversationsCount =  0;
		msgCount = messages != null ? messages.Count() : 0;
	}

	public Message[]? Messages { get; private set; }
	
	int conversationsCount;
	int msgCount;

	public bool HasSomething => msgCount > 0 && conversationsCount > 0;

	public string? Title
	{
		get
		{
			string? title;
			if (conversationsCount > 0)
			{
				title = "Chat  ";
				if (title?.Length > 50)
					title = $"{title[0..50]}...";
				if (conversationsCount > 1)
					title += $" {String.Format("Chat_AndMore", conversationsCount - 1)}";
			}
			else
			{
				title = String.Empty;
			}

			return title;
		}
	}

	public string? Text
	{
		get
		{
			string? text;
			if (msgCount > 0)
			{
				text = Messages?.First().PreviewText;
				if (msgCount > 1)
					text += $" {String.Format("Chat_AndMore", conversationsCount - 1)}";
			}
			else
			{
				text = String.Empty;
			}

			return text;
		}
	}
}
