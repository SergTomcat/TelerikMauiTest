using CommunityToolkit.Maui.Extensions;
using WebsoftWorkspaceM.Messenger.Controls;
using static WebsoftWorkspaceM.Messenger.ChatViewModel;
using ChatView = WebsoftWorkspaceM.Messenger.Controls.ChatViewTK1;

namespace WebsoftWorkspaceM.Messenger;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class ChatPage : ContentPage
{
	
	string? preinitFocusMessageId = null;


	Task? registrationTask = null;


	public ChatPage()
	{
		var initTask = VerifyRegister();

		InitializeComponent();


		initTask.ContinueWith(_ => InitTheMessageList());

	}

	void InitTheMessageList()
	{
		var msgs = ChatViewModel.Instance?.CurrentMessages;
		if (msgs != null)
		{
			MessagesList.SetItemSource(msgs);

			MessagesList.InitialScrollPosition(this.preinitFocusMessageId);
		}
		
	}


	Task<bool> VerifyRegister(bool initIfNot = true)
	{
		var vm = ChatViewModel.Instance;
		this.BindingContext = vm;


		return Task.Run(() => vm.LoadMessages(0));
	}




}