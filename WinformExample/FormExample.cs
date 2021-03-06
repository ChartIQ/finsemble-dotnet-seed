﻿using ChartIQ.Finsemble;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;
using Newtonsoft.Json.Bson;
using WinformExample.Controls;
using Color = System.Drawing.Color;

namespace WinformExample
{
	public partial class FormExample : Form
	{
		private SortedDictionary<string, RoundedButton> LinkerGroups = new SortedDictionary<string, RoundedButton>();

		private System.Drawing.Text.PrivateFontCollection finfont = new System.Drawing.Text.PrivateFontCollection();
		public FormExample(String[] args)
		{
#if DEBUG
			System.Diagnostics.Debugger.Launch();
#endif
			InitializeComponent();

			//this.input.KeyPress += new System.Windows.Forms.KeyPressEventHandler(handleKeyPresses);

			//connect to Finsemble
			//Ensure that your window has been created (so that its window handle exists) before connecting to Finsemble.
			FSBL = new Finsemble(args, this);

			// Use handle
			// FSBL = new Finsemble(args, this.Handle);
			//----

			ArrangeComponents();

			FSBL.Connected += FinsembleConnected;
			FSBL.Connect();
		}

		private void ArrangeComponents()
		{
			Activate();
			MessagesRichBox.SendToBack();
		}

		//If you encounter issues with the initial window placement (due to the handling of window borders in Windows 10) SetBoundsCore can be overridden to implement custom logic 
		//protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		//{
		//	base.SetBoundsCore(x, y, width, height, specified);
		//}

		private void FinsembleConnected(object sender, EventArgs e)
		{
			FSBL.Logger.OnLog += Logger_OnLog;

			FSBL.Logger.Log(new JToken[] { "Winform example connected to Finsemble." });
			System.Diagnostics.Debug.WriteLine("FSBL Ready.");

			// Handle Window grouping
			FSBL.RouterClient.Subscribe("Finsemble.WorkspaceService.groupUpdate", HandleWindowGrouping);

			//setup linker channels
			FSBL.LinkerClient.GetAllChannels(HandleLinkerChannelLabels);

			// Listen to Linker state change to render connected channels
			FSBL.LinkerClient.OnStateChange(HandleLinkerStateChange);

			//Example for handling component state in a workspace (and hand-off to getSpawnData if no state found)
			FSBL.WindowClient.GetComponentState(new JObject { ["field"] = "symbol" }, HandleGetComponentState);


			// Example for Handling PubSub data
			FSBL.RouterClient.Subscribe("Finsemble.TestWPFPubSubSymbol", HandlePubSub);

			finfont.AddFontFile(@"Resources\finfont.ttf");
			finfont.AddFontFile(@"Resources\font-finance.ttf");
			var finFont = new System.Drawing.Font(finfont.Families[0], 100);
			var fontFinance = new System.Drawing.Font(finfont.Families[1], 7);
			Invoke(new Action(() =>
			{
				scrim.Font = finFont;

				LinkerButton.Font = fontFinance;
				DragNDropEmittingButton.Font = fontFinance;
				AlwaysOnTopButton.Font = fontFinance;
				DockingButton.Font = fontFinance;
				DockingButton.UseEllipse = true;
				AlwaysOnTopButton.UseEllipse = true;
			}));

			FSBL.DragAndDropClient.SetScrim(scrim);
			FSBL.DragAndDropClient.AddReceivers(new List<KeyValuePair<string, EventHandler<FinsembleEventArgs>>>()
			{
				new KeyValuePair<string, EventHandler<FinsembleEventArgs>>("symbol", HandleDragAndDropReceive)
			});

			// Emitters for data that can be dragged using the drag icon.
			FSBL.DragAndDropClient.SetEmitters(new List<KeyValuePair<string, DragAndDropClient.emitter>>()
			{
				new KeyValuePair<string, DragAndDropClient.emitter>("symbol", HandleDragAndDropEmit)
			});

			// Example for LinkerClient subscribe
			FSBL.LinkerClient.Subscribe("symbol", HandleLinkerData);

			// Example for getting Spawnable component list
			FSBL.ConfigClient.GetValue(new JObject { ["field"] = "finsemble.components" }, HandleComponentsList);

			SetInitialWindowGrouping(FSBL.WindowClient.GetWindowGroups());
		}

		private void Logger_OnLog(object sender, JObject log)
		{
			try
			{
				this.Invoke((MethodInvoker)delegate { MessagesRichBox.Text += log + "\n"; });
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
			}
		}

		private void HandleLinkerChannelLabels(object sender, FinsembleEventArgs args)
		{
			this.Invoke(new Action(() =>
			{
				if (args.error == null)
				{
					var groupLabels = new RoundedButton[] { GroupButton1, GroupButton2, GroupButton3, GroupButton4, GroupButton5, GroupButton6 };
					var allChannels = args.response as JArray;
					int labelcount = 0;
					foreach (JObject item in allChannels)
					{
						var theLabel = groupLabels[labelcount++];
						theLabel.Visible = false;
						if(!LinkerGroups.ContainsKey(item["name"].ToString()))
							LinkerGroups.Add(item["name"].ToString(), theLabel);
						//limit channels to ones we enough labels for
						if (labelcount == groupLabels.Length)
						{
							break;
						}
					}
				}
				else
				{
					FSBL.Logger.Error(new JToken[] { "Error when retrieving linker channels: ", args.error.ToString() });
				}
			}));
		}

		private void HandleLinkerStateChange(Object sender, FinsembleEventArgs response)
		{
			this.Invoke(new Action(() =>
			{
				if (response.error != null)
				{
					FSBL.Logger.Error(new JToken[] { "Error when receiving linker state change data: ", response.error.ToString() });
				}
				else if (response.response != null)
				{
					var visiblePillsBeforeChange = LinkerGroups.Count(x => x.Value.Visible);

					var channels = response.response["channels"] as JArray;
					var allChannels = response.response["allChannels"] as JArray;
					foreach (JObject obj in allChannels)
					{
						LinkerGroups[obj["name"].ToString()].Visible = false;
						LinkerGroups[obj["name"].ToString()].BackColor = ColorTranslator.FromHtml(obj["color"].ToString());
					}

					foreach (JValue channel in channels)
					{
						LinkerGroups[channel.Value.ToString()].Visible = true;
					}

					var visiblePillsAfterChange = LinkerGroups.Count(x => x.Value.Visible);
					AlignAllLinkerPills();
					DragNDropEmittingButton.Location = new Point(DragNDropEmittingButton.Location.X + CalculateDragNDropLeftMarginShift(visiblePillsBeforeChange, visiblePillsAfterChange), DragNDropEmittingButton.Location.Y);
				}
			}));
		}

		private void AlignAllLinkerPills()
		{
			var initialX = GroupButton1.Location.X;
			var counter = 0;

			foreach (var visiblePill in LinkerGroups.Where(x => x.Value.Visible))
			{
				visiblePill.Value.Location = new Point(initialX + (counter * (GroupButton1.Width)), visiblePill.Value.Location.Y);
				++counter;
			}
		}

		private int CalculateDragNDropLeftMarginShift(int visiblePillsBeforeShift, int visiblePillsAfterShift)
		{
			return (visiblePillsAfterShift - visiblePillsBeforeShift) * (GroupButton1.Width+2);
		}

		private void HandleWindowGrouping(object s, FinsembleEventArgs res)
		{
			this.Invoke(new Action(() =>
			{
				if (res.error != null)
				{
					return;
				}

				var groupData = res.response["data"]["groupData"] as JObject;
				var thisWindowGroups = new JObject();

				foreach (var obj in groupData)
				{
					var windowgroupid = obj.Key;
					var windowgroup = groupData[windowgroupid] as JObject;
					var windownames = windowgroup["windowNames"] as JArray;

					thisWindowGroups = FormWindowGroupsTo(windowgroup, windownames);
				}

				UpdateViewDueToWindowGroups(thisWindowGroups);
			}));
		}

		private void SetInitialWindowGrouping(JArray groupdWindowBelongsTo)
		{
			this.Invoke(new Action(() =>
			{
				var thisWindowGroups = new JObject();

				foreach (var group in groupdWindowBelongsTo)
				{
					var windowNames = group["windowNames"] as JArray;
					thisWindowGroups = FormWindowGroupsTo(group, windowNames);
				}

				UpdateViewDueToWindowGroups(thisWindowGroups);
			}));
		}

		private JObject FormWindowGroupsTo(JToken group, JArray windowNames)
		{
			var thisWindowGroups = new JObject
			{
				["dockingGroup"] = "",
				["snappingGroup"] = "",
				["topRight"] = false
			};

			var currentWindowName = FSBL.WindowClient.GetWindowIdentifier()["windowName"].ToString();

			var windowingGroup = false;
			for (int i = 0; i < windowNames.Count; i++)
			{
				var windowName = windowNames[i].ToString();
				if (windowName == currentWindowName)
				{
					windowingGroup = true;
				}
			}

			if (windowingGroup)
			{
				var isMovable = (bool)group["isMovable"];
				if (isMovable)
				{
					thisWindowGroups["dockingGroup"] = group;
					if (group["topRightWindow"].ToString() == currentWindowName)
					{
						thisWindowGroups["topRight"] = true;
					}
				}
				else
				{
					thisWindowGroups["snappingGroup"] = group;
				}
			}

			return thisWindowGroups;
		}

		private void UpdateViewDueToWindowGroups(JObject thisWindowGroups)
		{
			if (!string.IsNullOrEmpty(thisWindowGroups?["dockingGroup"]?.ToString()))
			{
				// docked
				DockingButton.Text = "@";
				DockingButton.ButtonColor = Color.FromArgb(3, 155, 255);
				DockingButton.OnHoverButtonColor = Color.FromArgb(3, 155, 255);
				DockingButton.Visible = true;
			}
			else if (!string.IsNullOrEmpty(thisWindowGroups?["snappingGroup"]?.ToString()))
			{
				// Snapped
				DockingButton.Text = ">";
				DockingButton.ButtonColor = Color.FromArgb(34, 38, 47);
				DockingButton.OnHoverButtonColor = Color.FromArgb(40, 45, 56);
				DockingButton.Visible = true;
			}
			else
			{
				// unsnapped/undocked
				DockingButton.Visible = false;
			}
		}

		private void HandleGetComponentState(object s, FinsembleEventArgs state)
		{
			this.Invoke(new Action(() =>
			{
				if (state.response != null)
				{
					// Example for restoring state
					if (state.response is JValue)
					{
						var symbol = (JValue)state.response;
						if (symbol != null)
						{
							var symbolTxt = symbol?.ToString();
							if (!string.IsNullOrEmpty(symbolTxt))
							{
								DataLabel.Text = symbolTxt;
								DataToSendInput.Text = symbolTxt;
								SourceLabel.Text = "via component state";
							}
						}
					}
					else
					{
						// Example for Handling Spawn data
						FSBL.WindowClient.GetSpawnData(HandleSpawnData);
					}
				}
				else
				{
					// Example for Handling Spawn data
					FSBL.WindowClient.GetSpawnData(HandleSpawnData);
				}
			}));
		}

		private void HandleSpawnData(Object sender, FinsembleEventArgs res)
		{
			this.Invoke(new Action(async () =>
			{
				var receivedData = (JObject)res.response;
				if (res.error != null)
				{
					FSBL.Logger.Error(new JToken[] { "Error when retrieving spawn data: ", res.error.ToString() });
				}
				else if (res.response != null)
				{
					JToken value = receivedData.GetValue("spawndata");
					if (value != null)
					{
						DataLabel.Text = value.ToString();
						DataToSendInput.Text = value.ToString();
						SourceLabel.Text = "via Spawndata";
						await SaveStateAsync();
					}
				}
			}));
		}

		private void HandlePubSub(object sender, FinsembleEventArgs state)
		{
			this.Invoke(new Action(async () =>
			{
				try
				{
					if (state.error != null)
					{
						FSBL.Logger.Error(new JToken[] { "Error when retrieving spawn data: ", state.error.ToString() });
					}
					else if (state.response != null)
					{
						var pubSubData = (JObject)state.response;
						var theData = ((JValue)pubSubData?["data"]?["symbol"])?.ToString();
						if (theData != null)
						{
							DataLabel.Text = theData;
							DataToSendInput.Text = theData;
							SourceLabel.Text = "via PubSub";
							await SaveStateAsync();
						}
					}
				}
				catch (Exception ex)
				{
					FSBL.Logger.Error(new JToken[] { "Error when retrieving linker channels: ", ex.Message });
				}
			}));
		}

		private void HandleDragAndDropReceive(object sender, FinsembleEventArgs args)
		{
			this.Invoke(new Action(async () =>
			{
				if (args.error != null)
				{
					FSBL.Logger.Error(new JToken[] { "Error when receiving drag and drop data: ", args.error.ToString() });
				}
				else if (args.response != null)
				{
					var data = args.response["data"]?["symbol"];
					if (data != null)
					{
						//Check if we received an object (so data.symbol.symbol) or a string (data.symbol) to support variations in the format
						if (data.HasValues)
						{
							data = data?["symbol"];
							DataLabel.Text = data.ToString();
							DataToSendInput.Text = data.ToString();
							SourceLabel.Text = "via Drag and Drop";
							await SaveStateAsync();
						}
					}
				}
			}));
		}

		private JObject HandleDragAndDropEmit()
		{
			return new JObject
			{
				["symbol"] = DataToSendInput.Text,
				["description"] = "Symbol " + DataToSendInput.Text
			};
		}

		private void HandleLinkerData(object sender, FinsembleEventArgs response)
		{
			this.Invoke(new Action(async () =>
			{
				if (response.error != null)
				{
					FSBL.Logger.Error(new JToken[] { "Error when receiving linker data: ", response.error.ToString() });
				}
				else if (response.response != null)
				{
					string value = response.response?["data"]?.ToString();
					DataLabel.Text = value;
					SourceLabel.Text = "via Linker";
					DataToSendInput.Text = value;
					await SaveStateAsync();
				}
			}));
		}

		private void HandleComponentsList(Object sender, FinsembleEventArgs response)
		{
			this.Invoke(new Action(() =>
			{
				if (response.error != null)
				{
					FSBL.Logger.Error(new JToken[] { "Error when receiving spawnable component list: ", response.error.ToString() });
				}
				else if (response.response != null)
				{
					var components = (JObject)response.response?["data"];
					foreach (var property in components?.Properties())
					{
						object value = components?[property.Name]?["foreign"]?["components"]?["App Launcher"]?["launchableByUser"];
						if ((value != null) && bool.Parse(value.ToString()))
						{
							Dispatcher.CurrentDispatcher.Invoke(() =>
							{
								//elimination of duplicate names of components after Finsemble restart
								if (!ComponentDropDown.Items.Contains(property.Name))
								{
									ComponentDropDown.Items.Add(property.Name);
									ComponentDropDown.SelectedIndex = 0;
								}
							});
						}
					}
				}
			}));
		}

		private void handleKeyPresses(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Return)
			{
				this.Invoke(new Action(async () =>
				{
					DataLabel.Text = DataToSendInput.Text;
					SourceLabel.Text = "via Text input";
					await SaveStateAsync();
				}));
				e.Handled = true;
			}
		}

		// Example for saving component state
		private async Task SaveStateAsync()
		{
			await FSBL.WindowClient.SetComponentState(new JObject
			{
				["field"] = "symbol",
				//["value"] = datavalue.Text
			});
		}

		private void LaunchButton_Click(object sender, EventArgs e)
		{
			object selected = ComponentDropDown.Text;
			if (selected == null) return;

			var componentName = selected.ToString();
			FSBL.LauncherClient.Spawn(componentName, new JObject { ["addToWorkspace"] = true }, (s, a) => { });
		}

		private void LinkerButton_Click(object sender, EventArgs e)
		{
			FSBL.LinkerClient.OpenLinkerWindow();
		}

		private async void AlwaysOnTopButton_Click(object sender, EventArgs e)
		{
			var response= (await FSBL.WindowClient.IsAlwaysOnTop())?["data"]?.ToObject<bool>();

			if (response == null) return;

			var newAlwaysOnTop = !response.Value;
			await FSBL.WindowClient.SetAlwaysOnTop(newAlwaysOnTop);

			if (newAlwaysOnTop)
			{
				AlwaysOnTopButton.ButtonColor = Color.FromArgb(3, 155, 255);
				AlwaysOnTopButton.OnHoverButtonColor = Color.FromArgb(3, 155, 255);
			}
			else
			{
				AlwaysOnTopButton.ButtonColor = Color.FromArgb(34, 38, 47);
				AlwaysOnTopButton.OnHoverButtonColor = Color.FromArgb(40, 45, 56);
			}
		}

		private void DockingButton_Click(object sender, EventArgs e)
		{
			var currentWindowName = FSBL.WindowClient.GetWindowIdentifier()["windowName"].ToString();
			if (DockingButton.Text == ">")
			{
				FSBL.RouterClient.Transmit("DockingService.formGroup", new JObject { ["windowName"] = currentWindowName });
			}
			else
			{
				FSBL.RouterClient.Query("DockingService.leaveGroup", new JObject { ["name"] = currentWindowName }, delegate (object s, FinsembleEventArgs res) { });
			}
		}

		private void SendButton_Click(object sender, EventArgs e)
		{
			if (FSBL.FDC3Client is object)
			{
				//FDC3 Usage example 
				//Broadcast
				FSBL.FDC3Client.fdc3.broadcast(new JObject
				{
					["type"] = "fdc3.instrument",
					["name"] = DataToSendInput.Text,
					["id"] = new JObject
					{
						["ticker"] = DataToSendInput.Text
					}
				});
			}
			else
			{
				// Use Default Linker
				FSBL.LinkerClient.Publish(new JObject
				{
					["dataType"] = "symbol",
					["data"] = DataToSendInput.Text
				});
			}

			//	Invoke(new Action(() => //main thread
			//	{
			//		DroppedData.Content = DataToSendInput.Text;
			//		DroppedDataSource.Content = "via Text entry";
			//		await SaveStateAsync();
			//	});
		}

		private void DragNDropEmittingButton_MouseDown(object sender, MouseEventArgs e)
		{
			FSBL.DragAndDropClient.DragStartWithData(sender);
		}

		private void DragNDropEmittingButton_Click(object sender, EventArgs e)
		{

		}

		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			//Dispose of the FSSBL object when the form is closed so that Finsemble is aware we've closed
			FSBL.Dispose();
			base.OnFormClosing(e);
		}
	}
}
