//#define TELERIK14  //new version (may 2026)

using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

#if !TELERIK14 && ANDROID
using Sigil;
#endif

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Telerik.Maui.Controls;
using Telerik.Maui.Controls.Chat;
using Telerik.Maui.Controls.ItemsView;


namespace WebsoftWorkspaceM.Messenger.Controls;


public class ChatViewTK1 : RadChatListView
{


	public enum ChatViewEvents
	{
		NewMessage_ButIHaventScrolled,
		NewMessage_AndIScrolled,
		HitTheBottom,
		Levitating,
	}

	public bool Chill;

	int msgsCount = -1;

	//public static ObservableCollection<Message>? Messages => ChatViewModel.Instance?.CurrentMessages;

	public string? InitialTargetMessageID = null;

	int pagesLoadedUp = 0;


	public ChatViewTK1()
	{
		// Configure automatic message type detection
		this.AutomationId = "chat";
		lastStartIndex = 0;
		lastBottomIndex = 0;

		this.ItemTemplate = new ChatTemplateSelector();

		//	Loaded += this.OnLoaded;
		this.Unloaded += OnUnloaded;

#if TELERIK14
		this.LoadOnDemandMode = Telerik.Maui.LoadOnDemandMode.Automatic;

		this.Scrolled += OnScrolled;
		ItemHoldCommand = new Command(ChatViewTK1_Hold_Handler);

		ItemViewStyle = separatorRemovalStyle;

#else
		ItemHolding += ChatViewTK1_Holding_Handler;



		ItemsVirtualLayout? itemsLayout = itemsLayoutField?.GetValue(this) as ItemsVirtualLayout;
		var handler = Delegate.CreateDelegate(scrolChangeEvent.EventHandlerType!, this, onScrollChangedMethodInfo!);
		scrolChangeEvent_addMethod.Invoke(itemsLayout, [handler]);


		var onLoadedHandler = radChatListViewLoadedEventFieldInfo?.GetValue(this) as Delegate;
		if (onLoadedHandler != null)
		{
			foreach (Delegate del in onLoadedHandler.GetInvocationList())
			{
				radChatListViewLoadedEvent.RemoveEventHandler(this, del);
			}
		}


#endif







	}

	private void OnLoaded(object? sender, EventArgs e)
	{

		//	InitialScrollPosition(InitialTargetMessageID!);


		this.Loaded -= this.OnLoaded;
	}
	

#if TELERIK14
	private double lastStartIndex;
	private double lastBottomIndex;

	static ChatViewTK1()
	{
		
		separatorRemovalStyle = new Style(typeof(RadCollectionViewItemView))
		{
			Setters =
			{
				new Setter { Property = RadCollectionViewItemView.BorderColorProperty, Value = Colors.Transparent },
				new Setter { Property = RadCollectionViewItemView.BorderThicknessProperty, Value = 0 },
			}
		};


	}

	static Style separatorRemovalStyle;





	private void OnScrolled(object? sender, ScrolledEventArgs eventArgs)
	{
		if (Chill)
			return;


		var startIndex = eventArgs.ScrollY;

		if (startIndex == 0 && lastStartIndex > startIndex)
		{
			var viewModel = ChatViewModel.Instance;

			int pagesLoaded = pagesLoadedUp;

			if (pagesLoaded > 0)
			{
				_ = fetchPrevousOlderMessagesPack();
			}
			else if (pagesLoaded < 0)
				lastStartIndex = -1;
		}
		if (lastStartIndex >= 0 && startIndex > lastStartIndex)
		{
			lastStartIndex = startIndex;// ?? 0;
		}

		var endIndex = eventArgs.ScrollY + this.Height;
		var bottomOffset = this.BottomOffset;

		if (bottomOffset < 50 && msgsCount > 0 && lastBottomIndex <= endIndex + 50)
		{
			lastBottomIndex = endIndex;
			RaiseChatViewEvent(ChatViewEvents.HitTheBottom); //[PICK IT UP] Not hitting!!!!!!!
		}
		else if (endIndex > 0 && bottomOffset > 100)
		{
			RaiseChatViewEvent(ChatViewEvents.Levitating);
		}


	}

	private void OnUnloaded(object? sender, EventArgs e)
	{
		this.Scrolled -= this.OnScrolled;
		this.Unloaded -= this.OnUnloaded;
	}

#else

	private int lastStartIndex;
	private int lastBottomIndex;


	object? itemsViewVerticalLayoutStrategy = null;



	//public bool Alive { get; private set; }

	//public const string ScrollDropDownHandlerName = "ScrollDropDownHandler";
	//private static readonly PropertyInfo? viewportBoundsProperty;
	private static readonly PropertyInfo itemsViewVerticalLayoutStrategyProperty = null!;
	private static readonly FieldInfo itemsVirtualLayoutTypeStartIndexField = null!;
	private static readonly FieldInfo itemsVirtualLayoutTypeEndIndexField = null!;
	private static readonly FieldInfo itemsLayoutField = null!;
	private static readonly EventInfo scrolChangeEvent = null!;
	private static readonly MethodInfo scrolChangeEvent_addMethod = null!;
	private static readonly MethodInfo onScrollChangedMethodInfo = null!;

	public static Type radChatListViewType = null!;
	private static Type radItemsViewType = null!;
	private static Type itemsVirtualLayoutType = null!;

	private static readonly EventInfo radChatListViewLoadedEvent = null!;
	private static readonly FieldInfo? radChatListViewLoadedEventFieldInfo;

#if ANDROID

	static Func<object?, int> GetStartIndexDelegate = null!;

	static Func<object?, int> GetEndIndexDelegate = null!;

#endif

	static ChatViewTK1()
	{
		if (itemsVirtualLayoutTypeStartIndexField == null)
		{

			radItemsViewType = Type.GetType("Telerik.Maui.Controls.RadItemsView, Telerik.Maui.Controls")
			    ?? throw new InvalidOperationException("Could not find type 'Telerik.Maui.Controls.ItemsView.RadItemsView'. Ensure Telerik UI for MAUI is referenced.");

			itemsVirtualLayoutType = Type.GetType("Telerik.Maui.Controls.ItemsView.ItemsVirtualLayout, Telerik.Maui.Controls")
			    ?? throw new InvalidOperationException("Could not find type 'Telerik.Maui.Controls.ItemsView.ItemsVirtualLayout'. Ensure Telerik UI for MAUI is referenced.");

			var itemsViewVerticalLayoutStrategyType = Type.GetType("Telerik.Maui.Controls.ItemsView.ItemsViewVerticalLayoutStrategy, Telerik.Maui.Controls")
			    ?? throw new InvalidOperationException("Could not find type 'Telerik.Maui.Controls.ItemsView.ItemsViewVerticalLayoutStrategy'. Ensure Telerik UI for MAUI is referenced.");

			//viewportBoundsProperty = itemsVirtualLayoutType.GetProperty("ViewportBounds", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

			itemsViewVerticalLayoutStrategyProperty = itemsVirtualLayoutType.GetProperty("LayoutStrategy", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

			itemsVirtualLayoutTypeStartIndexField = itemsViewVerticalLayoutStrategyType.GetField("startIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
			itemsVirtualLayoutTypeEndIndexField = itemsViewVerticalLayoutStrategyType.GetField("endIndex", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

			itemsLayoutField = radItemsViewType.GetField("itemsLayout", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

			scrolChangeEvent = itemsVirtualLayoutType.GetEvent("ScrollChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
			scrolChangeEvent_addMethod = itemsVirtualLayoutType.GetMethod("add_ScrollChanged", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;

			onScrollChangedMethodInfo = typeof(ChatViewTK1).GetMethod(nameof(OnScrollChanged), BindingFlags.Instance | BindingFlags.NonPublic)!;

			radChatListViewType = typeof(RadItemsView);
			radChatListViewLoadedEvent = radChatListViewType.GetEvent("Loaded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!;
			radChatListViewLoadedEventFieldInfo = typeof(VisualElement).GetField("_loaded", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

#if ANDROID
			var GetStartIndexEmitter = Emit<Func<object?, int>>.NewDynamicMethod("GetStartIndexField")
			.LoadArgument(0)
			.CastClass(itemsViewVerticalLayoutStrategyType)
			.LoadField(itemsVirtualLayoutTypeStartIndexField)
			.Return();

			var GetEndIndexEmitter = Emit<Func<object?, int>>.NewDynamicMethod("GetEndIndexField")
			.LoadArgument(0)
			.CastClass(itemsViewVerticalLayoutStrategyType)
			.LoadField(itemsVirtualLayoutTypeEndIndexField)
			.Return();


			GetStartIndexDelegate = GetStartIndexEmitter.CreateDelegate();
			GetEndIndexDelegate = GetEndIndexEmitter.CreateDelegate();

#endif

		}
	}

	object? obtainReflectedItemLayoutStrategy(ItemsVirtualLayout? itemsLayout)
	{
		if (itemsViewVerticalLayoutStrategy is null)
			itemsViewVerticalLayoutStrategy = itemsViewVerticalLayoutStrategyProperty?.GetValue(itemsLayout);

		return itemsViewVerticalLayoutStrategy;
	}



	private void OnScrollChanged(object sender, EventArgs eventArgs)
	{
		if (Chill)
			return;

		ItemsVirtualLayout? itemsLayout = sender as ItemsVirtualLayout;

		//var ViewPortScrollBounds = viewportBoundsProperty?.GetValue(itemsLayout) as Rect?;
		//Trace.WriteLine($"OnScrollChanged: {ViewPortScrollBounds?.X}:{ViewPortScrollBounds?.Y}");

		//		Telerik.Maui.Controls.ItemsView.ItemsViewVerticalLayoutStrategy

		var itemsViewVerticalLayoutStrategy = obtainReflectedItemLayoutStrategy(itemsLayout);

#if ANDROID
		var startIndex = GetStartIndexDelegate(itemsViewVerticalLayoutStrategy);
#else
		var startIndexN = itemsVirtualLayoutTypeStartIndexField?.GetValue(itemsViewVerticalLayoutStrategy) as int?;
		var startIndex = startIndexN ?? 0;
#endif



		//if (startIndex == 0) Tools.Toast($"pagesloaded={ChatViewModel.Instance?.SelectedConversation?.pagesloaded}; lastStartIndex={lastStartIndex}");



		if (startIndex == 0 && lastStartIndex > startIndex)
		{
			var viewModel = ChatViewModel.Instance;

			int pagesLoaded = pagesLoadedUp;




			if (pagesLoaded > 0)
			{
				_ = fetchPrevousOlderMessagesPack();
			}
			else if (pagesLoaded < 0)
				lastStartIndex = -1;
		}
		if (lastStartIndex >= 0 && startIndex > lastStartIndex)
		{
			lastStartIndex = startIndex;// ?? 0;
		}

#if ANDROID
		int endIndex = GetEndIndexDelegate(itemsViewVerticalLayoutStrategy);
#else
		var endIndexRefl = itemsVirtualLayoutTypeEndIndexField?.GetValue(itemsViewVerticalLayoutStrategy);
		int endIndex = endIndexRefl is null? -1 : (int)endIndexRefl;
#endif

		if (endIndex >= msgsCount - 1 && msgsCount > 0 && lastBottomIndex != endIndex)
		{
			lastBottomIndex = endIndex;
			RaiseChatViewEvent(ChatViewEvents.HitTheBottom); //[PICK IT UP] Not hitting!!!!!!!
		}
		else if (endIndex > 0 && msgsCount - 3 > endIndex)
		{
			RaiseChatViewEvent(ChatViewEvents.Levitating);
		}

	}
	private void OnUnloaded(object? sender, EventArgs e)
	{
		this.Unloaded -= this.OnUnloaded;
	}
#endif




	public delegate void ChatViewEvent(ChatViewTK1 chatView, ChatViewEvents eventHappening);
	public event ChatViewEvent? SomethingHappened;

	public void RaiseChatViewEvent(ChatViewEvents eventHappening)
	{
		SomethingHappened?.Invoke(this, eventHappening);
	}


	async Task fetchPrevousOlderMessagesPack()
	{
		var viewModel = ChatViewModel.Instance;
		if (viewModel != null && !Chill)
		{


			Chill = true;


			//await viewModel.LoadMoreMessages().ConfigureAwait(false);


			Chill = false;

		}
	}

	public void ScrollTo(object dataItem, bool animate = false)
	{
#if TELERIK14
		MainThread.BeginInvokeOnMainThread(() => base.ScrollItemIntoView(dataItem, animate));
#else
		MainThread.BeginInvokeOnMainThread(() => base.ScrollTo(dataItem));
#endif
	}

	public void InitialScrollPosition(string? msgid = null)
	{
		if (!string.IsNullOrEmpty(msgid))
		{
			ScrollToSpecificMessge(msgid!);
		}
		else
			ScrollDropDown();
	}

	public void ScrollToSpecificMessge(string id)
	{
		var msgs = this.ItemsSource as ObservableCollection<Message>;
		if (msgs != null && msgs.Count > 0)
		{
			var targetMsg = msgs.FirstOrDefault(m => m.ID == id);
			//Todo unread! IsRead flag is faulty today
			if (targetMsg != null)
			{
				this.ScrollTo(targetMsg);
			}
		}
	}

	public void ScrollDropDown(bool firstUnread = true)
	{
		var msgs = this.ItemsSource as ObservableCollection<Message>;
		if (msgs != null && msgs.Count > 0)
		{
			Message targetMsg;
			//Todo unread! IsRead flag is faulty today
			if (firstUnread)
				targetMsg = /*msgs.FirstOrDefault(m => !m.IsRead) ??*/ msgs.Last();
			else
				targetMsg = msgs.Last();

			this.ScrollTo(targetMsg);
		}
	}


	public void SetItemSource(ObservableCollection<Message> itemSource)
	{
		if (this.ItemsSource is not null)
			(this.ItemsSource as ObservableCollection<Message>)!.CollectionChanged -= collectionChangedHandler;
		else
			this.ItemsSource = itemSource;

		itemSource.CollectionChanged += collectionChangedHandler;
		msgsCount = itemSource.Count;
	}
	void manageUpEvents(bool born, View? view)
	{
		var msgs = this.ItemsSource as ObservableCollection<Message>;
		if (msgs != null)
		{
			if (born)
			{
				SetItemSource(msgs);
			}
			else
			{
				msgs.CollectionChanged -= collectionChangedHandler;
				msgsCount = -1;
			}
		}

	}

	bool isMyMessage(object? item)
	{
		var m = (item as Message);

		var u = m?.User;

		return u is null ? false : u.IsMe;
	}

	void collectionChangedHandler(object? _, NotifyCollectionChangedEventArgs a)
	{
		if (a.Action == NotifyCollectionChangedAction.Add)
		{
			var items = a.NewItems;
			var count = items is null ? 0 : items.Count;
			if (count == 1 || count == 2 /*usually it is artificial message added beforehand, maybe check for it explicitly?*/)
			{
				var shiftedIndex = count - 1;
				var probeMessage = items![shiftedIndex];
				if (isMyMessage(probeMessage))
				{
					ScrollTo(probeMessage!);
					SomethingHappened?.Invoke(this, ChatViewEvents.NewMessage_AndIScrolled);
				}
				else
				{
					SomethingHappened?.Invoke(this, ChatViewEvents.NewMessage_ButIHaventScrolled);
				}

			}

		}
		var itemSrc = this.ItemsSource as ObservableCollection<Message>;
		msgsCount = itemSrc is null ? 0 : itemSrc.Count;
	}

	public static void LongClickEventHandler(Message? m)
	{
		Shell.Current.DisplayAlertAsync(null, $"{m?.Text} long tapped.", "Ok");
	}

	void ChatViewTK1_Hold_Handler(object item)
	{
		ChatViewTK1.LongClickEventHandler(item as Message);
	}

	private void ChatViewTK1_Holding_Handler(object? sender, Telerik.Maui.Controls.ItemsView.ItemViewHoldingEventArgs e)
	{
		var view = e.View;

		var item = e.Item as Message;

		ChatViewTK1.LongClickEventHandler(item);

		e.Handled = true;
	}


}