using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using ChartIQ.Finsemble;
using log4net;
using Newtonsoft.Json.Linq;
using ChartIQ.Finsemble.Notifications;
using NAction = ChartIQ.Finsemble.Notifications.Models.Action;
using ChartIQ.Finsemble.Notifications.Models;
using ChartIQ.Finsemble.Models;

namespace NotificationExample
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		/// <summary>
		/// The logger
		/// </summary>
		private static readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private Finsemble FSBL;

		//Internal state for a single subscription - note that the client supports multiple subscriptions, we're just using a single one in this component.
		private Subscription subscription = null;

		/// <summary>
		/// The MainWindow is created by the App so that we can get command line arguments passed from Finsemble.
		/// </summary>
		/// <param name="args"></param>
		public MainWindow(string[] args)
		{

			// Trigger actions on close when requested by Finsemble, e.g.:
			this.Closing += MainWindow_Closing;

			FSBL = new Finsemble(args, this); // Finsemble needs the command line arguments to connect and also this Window to manage snapping, docking etc.
			FSBL.Connected += Finsemble_Connected;
			FSBL.Connect();
		}

		private void Notification_1_Click(object sender, RoutedEventArgs e)
		{
			Notification not1 = new Notification();
			not1.IssuedAt = DateTime.Now;
			not1.Source = "WPF NotifyComponent";
			not1.HeaderText = "WPF Internal Actions (No Id)";
			not1.Title = "test notification 1";
			not1.Details = "Should create a new notification in UI every time it's sent (from WPF)";
			not1.Type = "email";
			not1.HeaderLogo = "http://localhost:3375/components/finsemble-notifications/components/shared/assets/email.svg";
			not1.ContentLogo = "http://localhost:3375/components/finsemble-notifications/components/shared/assets/graph.png";

			NAction dismiss = new NAction();
			dismiss.ButtonText = "Dismiss";
			dismiss.Type = ActionTypes.DISMISS;

			NAction snooze = new NAction();
			snooze.ButtonText = "Snooze";
			snooze.Type = ActionTypes.SNOOZE;
			snooze.Milliseconds = 10000;

			NAction welcome = new NAction();
			welcome.ButtonText = "Welcome";
			welcome.Type = ActionTypes.SPAWN;
			welcome.Component = "Welcome Component";

			not1.Actions.Add(snooze);
			not1.Actions.Add(welcome);
			not1.Actions.Add(dismiss);

			FSBL.NotificationClient.Notify((new[] { not1 }), (s, r) => {

				FSBL.Logger.Log(new JToken[] {
					"Notification sent,\nnotification: " + not1.ToString()
					+ "\nresponse: " + (r.response != null ? r.response.ToString() : "null")
					+ "\nerror: " + (r.error != null ? r.error.ToString() : "null")
				});

				//check response for notification id 
			});

		}

		private void Notification_2_Click(object sender, RoutedEventArgs e)
		{
			Notification not2 = new Notification();
			not2.IssuedAt = DateTime.Now;
			not2.Id = "wpf_notification_456";
			not2.Source = "WPF NotifyComponent";
			not2.Title = "test notification 2";
			not2.HeaderText = "WPF Notification Same Id";
			not2.Details = "Should only be in UI once (WPF)";
			not2.Type = "chat";
			not2.HeaderLogo = "http://localhost:3375/components/finsemble-notifications/components/shared/assets/chat.svg";
			not2.ContentLogo = "http://localhost:3375/components/finsemble-notifications/components/shared/assets/sheild.png";

			NAction query = new NAction();
			query.ButtonText = "Send Query";
			query.Type = ActionTypes.QUERY;
			query.Channel = "query-channel";
			query.Payload = new JObject();
			query.Payload.Add("hello", new JValue("world"));


			NAction transmit = new NAction();
			transmit.ButtonText = "Send Transmit";
			transmit.Type = ActionTypes.TRANSMIT;
			transmit.Channel = "transmit-channel";
			transmit.Payload = new JObject { };
			transmit.Payload.Add("foo", new JValue("bar"));

			NAction publish = new NAction();
			publish.ButtonText = "Send Publish";
			publish.Type = ActionTypes.PUBLISH;
			publish.Channel = "publish-channel";
			publish.Payload = new JObject { };
			publish.Payload.Add("xyzzy", new JValue("moo"));

			not2.Actions.Add(query);
			not2.Actions.Add(transmit);
			not2.Actions.Add(publish);

			FSBL.NotificationClient.Notify((new[] { not2 }), (s, r) => {

				FSBL.Logger.Log(new JToken[] {
					"Notification sent,\nnotification: " + not2.ToString()
					+ "\nresponse: " + (r.response != null ? r.response.ToString() : "null")
					+ "\nerror: " + (r.error != null ? r.error.ToString() : "null")
				});

				//check response for notification id 
			});

		}

		private void Notification_3_Click(object sender, RoutedEventArgs e)
		{
			Notification not3 = new Notification();
			not3.IssuedAt = DateTime.Now;
			not3.Source = "WPF NotifyComponent";
			not3.HeaderText = "WPF minimal notification";
			not3.Title = "test notification 3";
			not3.Details = "Should create a new notification in UI every time it's sent (from WPF)";
			not3.Type = "email";
			not3.HeaderLogo = "http://localhost:3375/components/finsemble-notifications/components/shared/assets/email.svg";
			not3.ContentLogo = "http://localhost:3375/components/finsemble-notifications/components/shared/assets/graph.png";

			FSBL.NotificationClient.Notify((new[] { not3 }), (s, r) => {

				FSBL.Logger.Log(new JToken[] {
					"Notification sent,\nnotification: " + not3.ToString()
					+ "\nresponse: " + (r.response != null ? r.response.ToString() : "null")
					+ "\nerror: " + (r.error != null ? r.error.ToString() : "null")
				});

			});
		}

		private void Subscribe_Click(object sender, RoutedEventArgs e)
		{
			Subscription sub = new Subscription();
			sub.Filter = new Filter();
			//sub.filter.include = new Dictionary<String, Object>();
			//sub.filter.include.Add("type", "email");

			EventHandler<FinsembleEventArgs> onSubHandler = (s, r) =>
			{
				FSBL.Logger.Log(new JToken[] {
					"Subscription request sent,\nSubscription: " + sub.ToString()
					+ "\nresponse: " + (r.response != null ? r.response.ToString() : "null")
					+ "\nerror: " + (r.error != null ? r.error.ToString() : "null")
				});
				if (r.response != null)
				{
					subscription = Subscription.FromJObject((JObject)r.response);
					Application.Current.Dispatcher.Invoke(delegate //main thread
					{
						Unsubscribe.IsEnabled = true;
						Subscribe.IsEnabled = false;
					});
				}
			};

			EventHandler<Notification> onNotifyHandler = (s, r) =>
			{
				FSBL.Logger.Log(new JToken[] {
					"Received Notification,\nnotification: " + r.ToString()
				});

				Application.Current.Dispatcher.Invoke(delegate //main thread
				{
					NotificationData.Text = r.ToString();
				});
			};

			FSBL.NotificationClient.Subscribe(sub, onSubHandler, onNotifyHandler);
		}

		private void Unsubscribe_Click(object sender, RoutedEventArgs e)
		{
			if (subscription != null)
			{
				EventHandler<FinsembleEventArgs> onUnsubHandler = (s, r) =>
				{
					FSBL.Logger.Log(new JToken[] {
						"Unsubscribe request sent,\nSubscription id: " + subscription.Id
						+ "\nresponse: " + (r.response != null ? r.response.ToString() : "null")
						+ "\nerror: " + (r.error != null ? r.error.ToString() : "null")
					});
					//TODO: check response doesn't include an error before clearing subscription ID
					subscription = null;
					Application.Current.Dispatcher.Invoke(delegate //main thread
					{
						Unsubscribe.IsEnabled = false;
						Subscribe.IsEnabled = true;
					});
				};

				FSBL.NotificationClient.Unsubscribe(subscription.Id, onUnsubHandler);
			}
			else
			{
				FSBL.Logger.Log(new JToken[] {
					"No subscription Id to unsubscribe!"
				});
			}
		}

		private void Finsemble_Connected(object sender, EventArgs e)
		{
			Application.Current.Dispatcher.Invoke(delegate //main thread
			{
				// Initialize this Window and show it
				InitializeComponent(); // Initialize after Finsemble is connected
				FinsembleHeader.SetBridge(FSBL); // The Header Control needs a connected finsemble instance

				//Styling the Finsemble Header
				FinsembleHeader.GetHandlingService().ActiveBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22262F"));
				FinsembleHeader.GetHandlingService().InactiveBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22262F"));
				FinsembleHeader.GetHandlingService().ButtonHoverBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A8CF4"));
				FinsembleHeader.GetHandlingService().InactiveButtonHoverBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A8CF4"));
				FinsembleHeader.GetHandlingService().CloseButtonHoverBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F26666"));
				FinsembleHeader.GetHandlingService().InactiveCloseButtonHoverBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F26666"));
				FinsembleHeader.GetHandlingService().DockingButtonDockedBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A8CF4"));
				FinsembleHeader.GetHandlingService().TitleForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ACB2C0"));
				FinsembleHeader.GetHandlingService().ButtonForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ACB2C0"));

				FinsembleHeader.GetHandlingService().ButtonFont = new TitlebarFontConfiguration()
				{
					FontFamily = null,
					FontSize = 8,
					FontStyle = FontStyles.Normal,
					FontWeight = FontWeights.Normal
				};
				FinsembleHeader.GetHandlingService().TitleFont = new TitlebarFontConfiguration()
				{
					FontFamily = null,
					FontSize = 12,
					FontStyle = FontStyles.Normal,
					FontWeight = FontWeights.SemiBold
				};

				//Set window title
				FinsembleHeader.GetHandlingService().Title = "Notify Example Component";

				this.Show();
			});
		}

		/// <summary>
		/// Example window close handler
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			/*if (MessageBox.Show("Close Application?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
			{
				// Cancel Closing
				e.Cancel = true;
				return;
			}*/
		}
	}
}
