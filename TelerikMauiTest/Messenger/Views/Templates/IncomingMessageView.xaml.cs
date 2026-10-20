

using CommunityToolkit.Maui.Alerts;
using WebsoftWorkspaceM.Messenger.Controls;

namespace WebsoftWorkspaceM.Messenger;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class IncoimingMessageView : ContentView
{
	static Command<object>? reactionPanelTapped_Command;
	public static Command<object> ReactionPanelTapped_Command { get
		{

			if (reactionPanelTapped_Command is null)
			{
				reactionPanelTapped_Command = new(static (o) =>
				{
					var t = new Toast()
					{
						Text = $"React"
					};
					t.Show();
				});
			}
			return reactionPanelTapped_Command;

			
		}
	}

	public IncoimingMessageView()
	{
		InitializeComponent();
	}

	private void AttachmentTapGestureRecognizer_Handler(object sender, TappedEventArgs e)
	{
		var attachmentObj = ((BindableObject)sender).BindingContext;

		MessageBubbleIndie.MessageView_FileTapped(attachmentObj);
	}

	private void ReplyTap_Handler(object sender, TappedEventArgs e)
	{
		var t = new Toast()
		{
			Text = $"Reply"
		};
		t.Show();
	}

	private void ForwardedTap_Handler(object sender, TappedEventArgs e)
	{
		var t = new Toast()
		{
			Text = $"Forward"
		};
		t.Show();
	}
}