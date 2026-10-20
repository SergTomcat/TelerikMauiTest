
using CommunityToolkit.Maui.Alerts;

namespace WebsoftWorkspaceM.Messenger;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class OutgoingMessageView : ContentView
{
	public OutgoingMessageView()
	{
		InitializeComponent();
	}

	private void AttachmentTapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
	{
		var attachmentObj = ((BindableObject)sender).BindingContext;

		Controls.MessageBubbleIndie.MessageView_FileTapped(attachmentObj);
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