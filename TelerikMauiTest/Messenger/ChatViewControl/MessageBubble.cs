using CommunityToolkit.Maui.Alerts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebsoftWorkspaceM.Styles;

namespace WebsoftWorkspaceM.Messenger.Controls;


public class MessageBubbleIndie : ContentView
{
	

	static public void MessageView_FileTapped(object obj)
	{
		var item = obj as Attachment;
		if (item  != null)
			AttachmentTapped(item);
	}

	static public void AttachmentTapped(Attachment item)
	{
		var t = new Toast()
		{
			Text = $"Opening {item.Name}"
		};
		t.Show();
	}

	static public void LinkClicked_Handler(string s)
	{
		var t = new Toast()
		{
			Text = $"Opening {s}"
		};
		t.Show();
	}


}


