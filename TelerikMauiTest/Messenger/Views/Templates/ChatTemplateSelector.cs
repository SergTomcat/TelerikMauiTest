using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebsoftWorkspaceM.Messenger
{
	public class ChatTemplateSelector : DataTemplateSelector
	{
		DataTemplate incomingDataTemplate;
		DataTemplate outgoingDataTemplate;
		DataTemplate systemDataTemplate;

		public ChatTemplateSelector()
		{
			this.incomingDataTemplate = new DataTemplate(typeof(IncoimingMessageView));
			this.outgoingDataTemplate = new DataTemplate(typeof(OutgoingMessageView));
			this.systemDataTemplate = new DataTemplate(typeof(SystemMessageView));
		}

		protected override DataTemplate? OnSelectTemplate(object item, BindableObject container)
		{
			var messageVm = item as Message;
			if (messageVm == null)
				return null;

			DataTemplate dt;
			if (messageVm.User == null || messageVm.Type == Message.MessageType.System || messageVm.Type == Message.MessageType.ARTIFICIAL_TIME)
				dt = systemDataTemplate;
			else if (messageVm.User.IsMe)
				dt = outgoingDataTemplate;
			else 
				dt = incomingDataTemplate;

			return dt;
		}

	}

}
