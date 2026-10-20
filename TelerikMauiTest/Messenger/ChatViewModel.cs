using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Web;
using Telerik.Licensing.Status;


namespace WebsoftWorkspaceM.Messenger;


internal class ChatViewModel : INotifyPropertyChanged
{

	public static string? CurrentUserID = "1105387902724063523";

	static ChatViewModel? instance = null;
	public static ChatViewModel Instance => instance ??= new();


	static List<User>? Users;
	private ChatViewModel()
	{
		
	}


	public ObservableCollection<Message>? CurrentMessages { get; private set; }
	public bool IsLoading { get; private set { field = value; MainThread.BeginInvokeOnMainThread(() => OnPropertyChanged(nameof(IsLoading)));  } }

	internal async Task<bool> LoadMessages(int page)
	{
		IsLoading = true;

		if (CurrentMessages == null)
		{
			CurrentMessages = new();
			OnPropertyChanged(nameof(CurrentMessages));
		}


		var lMs = await readMessagesFromJson().ConfigureAwait(false);

		foreach (var m in lMs)
		{
			CurrentMessages.Add(m);
		}






		IsLoading = false;


		return true;
	}




	public event PropertyChangedEventHandler? PropertyChanged;
	public void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));





	static async Task<List<Message>> readMessagesFromJson()
	{

		DateTime lastDate = default;


			var lMs = new List<Message>();

		Message? prevMsg = null;


		var array = await getJsonMessages().ConfigureAwait(false);

		foreach (var jMessage in array)
		{
			var c = readMessageFromJson(jMessage);

			if (c != null)
			{
				User? user;
				if (c.User != null)
				{
					user = Users?.Find(u => u.ID == c.User.ID);
					if (user == null)
					{
						user = c.User;

						if (Users == null)
							Users = new List<User>();

						Users.Add(user);
					}
				}

				var msgDate = c.Time.Date;
				if (msgDate > lastDate)
				{
					lMs.Add(ArtificialTimeMessage(msgDate));
					lastDate = msgDate;
				}


				c.StreakBreak = c.User?.IsMe == false && prevMsg?.User?.ID != c.User?.ID;


				lMs.Add(c);

				prevMsg = c;
			}
		}
		return lMs;
	}


	public static Message ArtificialTimeMessage(DateTime msgDate)
	{
		return new Message()
		{
			ID = $"AD_{msgDate.ToShortDateString()}",
			Time = msgDate,
			Type = Message.MessageType.ARTIFICIAL_TIME,
			IsRead = true,
			IsReadByMe = true,
			
			TextBlocks = [new() { Text = msgDate.ToString() }]
		};
	}





	static Message? readMessageFromJson(JsonElement jMessage, string? ignoreID = null)
	{
		Message? msg;

		string? id = (jMessage.TryGetProperty("id", out var jsonProp) || jMessage.TryGetProperty("message_id", out jsonProp)) && jsonProp.ValueKind == JsonValueKind.String ? jsonProp.GetString() : null;
		if (id != ignoreID && !string.IsNullOrEmpty(id))
		{

			jMessage.TryGetProperty("data", out var jsonData);
			jMessage.TryGetProperty("reply", out var jsonReply);
			jMessage.TryGetProperty("forward", out var jsonForward);


			Reaction[]? reactions;
			if (jMessage.TryGetProperty("reactions", out jsonProp))
			{
				reactions = GetReactionFromJson(jsonProp, jMessage.TryGetProperty("my_reaction", out var jMy) && jMy.ValueKind == JsonValueKind.String ? jMy.GetString() : null);
			}
			else
				reactions = null;

			Message.MessageTextBlock[]? texts;
			Message.MessageType typeId;
			if (jMessage.TryGetProperty("message_incorrect", out jsonProp) && jsonProp.ValueKind == JsonValueKind.Object)
			{
				texts = buildMutatedMessage(ref jsonProp);
				typeId = Message.MessageType.BIG_MESSAGE;
			}
			else
			{
				typeId = convertServerType(jMessage.TryGetProperty("type_id", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String ? jsonProp.GetString() : null);
				texts = ((jMessage.TryGetProperty("text", out jsonProp) || jMessage.TryGetProperty("message_text", out jsonProp)) ? buildMessageTextFromJson(ref jsonProp) : null);
			}
			if (typeId == Message.MessageType.System)
			{
				if (jMessage.TryGetProperty("call", out jsonProp) && jsonProp.ValueKind == JsonValueKind.Object && jsonProp.TryGetProperty("state_id", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String)
				{
					if (texts is null || texts.Length == 0)
						texts = [new() { Text = string.Empty }];

					if (jsonProp.GetString() == "missed")
						texts[0].Text = string.Concat("❌ ", texts[0].Text);
					else
						texts[0].Text = string.Concat("📞 ", texts[0].Text);
				}
			}

			string? blockMessageId = jMessage.TryGetProperty("block_message_id", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String ? jsonProp.GetString() : null;
			string? message_state_id = jMessage.TryGetProperty("message_state_id", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String ? jsonProp.GetString() : null;
			string? recipient_state_id = jMessage.TryGetProperty("recipient_state_id", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String ? jsonProp.GetString() : null;
			string? create_date = jMessage.TryGetProperty("create_date", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String ? (jsonProp.GetString()) : null;
			string? edit_date = jMessage.TryGetProperty("edit_date", out jsonProp) && jsonProp.ValueKind == JsonValueKind.String ? (jsonProp.GetString()) : null;

			jMessage.TryGetProperty("sender", out var jSender);
			jMessage.TryGetProperty("files", out var jFiles);


			//text = text?.SanitizeText();

			msg = composeMessage(id, blockMessageId, typeId, texts, message_state_id, recipient_state_id, in jSender, in jFiles, create_date, edit_date, in jsonData, in jsonReply, in jsonForward, reactions);
		}
		else
			msg = null;

		return msg;
	}


	static Message.MessageTextBlock[]? buildMessageTextFromJson(ref JsonElement jText)
	{
		static string? unescape(string? s)
		{
			if (s is null)
				return null;
			if (s.Contains("&"))
				return HttpUtility.HtmlDecode(s);


			return s;

		}


		Message.MessageTextBlock[]? blocks;

		switch (jText.ValueKind)
		{
			case JsonValueKind.Array:

				var jEnum = jText.EnumerateArray().ToArray();

				if (jEnum.Length > 0)
				{
					blocks = new Message.MessageTextBlock[jEnum.Length];
					for (var i = 0; i < jEnum.Length; i++)
					{
						var jBlock = jEnum[i];
						if (jBlock.ValueKind == JsonValueKind.Object)
						{
							JsonElement jProp;


							if (jBlock.TryGetProperty("type", out jProp) && jProp.ValueKind == JsonValueKind.String && jProp.GetString() == "line_break")
							{
								blocks[i] = new Message.MessageTextBlock()
								{
									Text = String.Empty,
									Flags = Message.MessageTextBlock.BlockFlags.Breaker
								};
								continue;
							}


							blocks[i] = new Message.MessageTextBlock
							{
								Text = jBlock.TryGetProperty("text", out jProp) && jProp.ValueKind == JsonValueKind.String ? unescape(jProp.GetString())
								//?.Replace("<br/>", "\n")
								?? string.Empty : string.Empty,
							};

							if (jBlock.TryGetProperty("url", out jProp) && jProp.ValueKind == JsonValueKind.String)
								blocks[i].LinkUri = jProp.GetString();

							if (jBlock.TryGetProperty("attributes", out jProp) && jProp.ValueKind == JsonValueKind.Array)
								foreach (var jAttr in jProp.EnumerateArray())
									if (jAttr.ValueKind == JsonValueKind.String)
									{
										switch (jAttr.GetString())
										{
											case "b":
												blocks[i].Attributes |= FontAttributes.Bold;
												break;
											case "i":
												blocks[i].Attributes |= FontAttributes.Italic;
												break;
											case "u":
												blocks[i].Decorations |= TextDecorations.Underline;
												break;
											case "s":
												blocks[i].Decorations |= TextDecorations.Strikethrough;
												break;
										}
									}

						}
						else
						{
							blocks[i] = new Message.MessageTextBlock
							{
								Text = "*Error: Invalid source data*",
							};
							Console.WriteLine($"{nameof(buildMessageTextFromJson)}: Invalid text block JSON source data, not an object");
						}
					}
				}
				else
					blocks = null;

				break;

			case JsonValueKind.String:

				blocks = [new() { Text = unescape(jText.GetString()) ?? string.Empty }];
				break;
			default:
				blocks = null;
				break;
		}

		return blocks;
	}



	public static Reaction[]? GetReactionFromJson(in JsonElement jReactions, string? mineReactionId)
	{
		if (jReactions.ValueKind == JsonValueKind.Array)
		{
			try
			{
				var areacts = jReactions.EnumerateArray().Select(j =>
				{
					var r = j.Deserialize<Reaction>()!;
					if (r.Id == mineReactionId)
						r.Mine = true;
					return r;
				}).ToArray();

				if (areacts.Length > 0)
					return areacts;
			}
			catch (Exception e)
			{
				Console.WriteLine($"{nameof(GetReactionFromJson)} error: {e}");
			}
		}
		return null;
	}

	static Message.MessageTextBlock[]? buildMutatedMessage(ref JsonElement jMutatedMessage)
	{
		var block = new Message.MessageTextBlock()
		{
			Text = jMutatedMessage.TryGetProperty("text", out var jProp) && jProp.ValueKind == JsonValueKind.String ? jProp.GetString()! : "* Message text incorrect *",
			LinkUri = jMutatedMessage.TryGetProperty("url", out jProp) && jProp.ValueKind == JsonValueKind.String ? jProp.GetString() : null,
			Flags = Message.MessageTextBlock.BlockFlags.Local,
		};

		return [block];
	}

	static Message.MessageType convertServerType(string? srctype)
	{
		Message.MessageType type;
		switch (srctype)
		{
			case "system":
				type = Message.MessageType.System;
				break;
			case "forward":
				type = Message.MessageType.Forwarded;
				break;
			case "message":
			default:
				type = Message.MessageType.Text;
				break;

		}
		return type;
	}

	public static User? BuildUserFromJson(in JsonElement jUserProp, string idField = "id", string nameField = "fullname", string resUrlField = "icon_url")
	{
		JsonElement jProp;
		var userID = jUserProp.TryGetProperty(idField, out jProp) && jProp.ValueKind == JsonValueKind.String ? jProp.GetString() : null;
		User? user;
		if (string.IsNullOrEmpty(userID))
			user = null;
		else
		{
			user = User.Create(userID);
			
			{
				user.Name = jUserProp.TryGetProperty(nameField, out jProp) && jProp.ValueKind == JsonValueKind.String ? jProp.GetString() : (jUserProp.TryGetProperty("disp_name", out jProp) && jProp.ValueKind == JsonValueKind.String ? jProp.GetString() : "???");
				user.ResUrl = jUserProp.TryGetProperty(resUrlField, out jProp) && jProp.ValueKind == JsonValueKind.String ? jProp.GetString() : null;
				user.IsMe = userID == CurrentUserID; //dResult["cur_user_id"].GetString();
			}

			
		}

		return user;
	}
	static Message composeMessage(string id, string? block_message_id, Message.MessageType type, Message.MessageTextBlock[]? texts, string? message_state_id, string? recipient_state_id, in JsonElement jUserProp, in JsonElement jFiles, string? date, string? editDate, in JsonElement jData, in JsonElement jReply, in JsonElement jForwarded, Reaction[]? enReactions)
	{
		

		var user = BuildUserFromJson(in jUserProp);

		var c = new Message
		{
			ID = id,
			BlockMessageID = block_message_id,
			Type = type,
			User = user,
			TextBlocks = texts,
			IsRead = message_state_id != "not_read",
			IsReadByMe = type == Message.MessageType.ARTIFICIAL_TIME || user?.IsMe == true || recipient_state_id != "not_read",
		};


		if (jReply.ValueKind == JsonValueKind.Object)
			c.Reply = readMessageFromJson(jReply);
		if (type == Message.MessageType.Forwarded && jForwarded.ValueKind == JsonValueKind.Object)
		{
			var forwardedMsg = readMessageFromJson(jForwarded);
			if (forwardedMsg is not null)
			{
				var fwdInfo = new Message.MessageForward() { Message = forwardedMsg, };
				if (jForwarded.TryGetProperty("conversation_id", out var jCN) && jCN.ValueKind == JsonValueKind.String)
				{
					fwdInfo.ConversationId = jCN.GetString();

					if (jForwarded.TryGetProperty("conversation_name", out jCN) && jCN.ValueKind == JsonValueKind.String)
					{
						fwdInfo.Title = jCN.GetString();
					}
				}
				c.Forwarded = fwdInfo;
			}
		}

		if (jFiles.ValueKind == JsonValueKind.Array)
		{
			var jFilesEnum = jFiles.EnumerateArray();

			if (jFilesEnum.Any())
			{
				var l = new List<Attachment>();

				foreach (var jFileInfo in jFilesEnum)
				{
					var fileId = jFileInfo.GetProperty("id").GetString();
					if (fileId is not null)
					{
						var attach = new Attachment() { ID = fileId, FileName = jFileInfo.GetProperty("name").GetString(), ResType = jFileInfo.GetProperty("type").GetString(), ResUrl = jFileInfo.GetProperty("url").GetString(), MessageID = id };

						l.Add(attach);
					}
				}
				var resArr = l.ToArray();

				foreach (var attach in resArr)
				{
					attach.Tribe = resArr;
				}

				c.Attachments = resArr;
			}
		}

		c.Reactions = enReactions;

		DateTime dt;
		if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out dt))
		{
			c.Time = dt;
		}
		if (!string.IsNullOrEmpty(editDate) && DateTime.TryParse(date, out dt))
		{
			c.EditTime = dt;
		}


		return c;

	}


	static async Task<JsonElement.ArrayEnumerator> getJsonMessages()
	{
		using Stream stream = await FileSystem.OpenAppPackageFileAsync("messages.json");

		using JsonDocument document = await JsonDocument.ParseAsync(stream);

		return document.RootElement.Clone().EnumerateArray();

	}
}
