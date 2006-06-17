// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mathias Simmack" email="mathias@simmack.de"/>
//     <version>$Revision$</version>
// </file>

namespace HtmlHelp2
{
	using System;
	using System.Drawing;
	using System.Windows.Forms;
	using ICSharpCode.Core;
	using ICSharpCode.SharpDevelop;
	using ICSharpCode.SharpDevelop.Gui;
	using AxMSHelpControls;
	using MSHelpControls;
	using MSHelpServices;
	using HtmlHelp2.Environment;
	using HtmlHelp2.ControlsValidation;


	public class ShowIndexMenuCommand : AbstractMenuCommand
	{
		public override void Run()
		{
			PadDescriptor index = WorkbenchSingleton.Workbench.GetPad(typeof(HtmlHelp2IndexPad));
			if (index != null) index.BringPadToFront();
		}
	}

	public class HtmlHelp2IndexPad : AbstractPadContent
	{
		protected MsHelp2IndexControl help2IndexControl;

		public override Control Control
		{
			get { return help2IndexControl;	}
		}

		public override void Dispose()
		{
			help2IndexControl.Dispose();
		}

		public override void RedrawContent()
		{
			help2IndexControl.RedrawContent();
		}

		public HtmlHelp2IndexPad()
		{
			help2IndexControl = new MsHelp2IndexControl();
			if (help2IndexControl.IsEnabled) help2IndexControl.LoadIndex();
		}
	}

	public class MsHelp2IndexControl : UserControl
	{
		AxHxIndexCtrl indexControl = null;
		ComboBox filterCombobox    = new ComboBox();
		ComboBox searchTerm        = new ComboBox();
		Label label1               = new Label();
		Label label2               = new Label();
		bool itemClicked           = false;
		bool controlIsEnabled      = false;

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing && indexControl != null) { indexControl.Dispose(); }
		}

		public bool IsEnabled
		{
			get { return this.controlIsEnabled; }
		}

		public void RedrawContent()
		{
			label1.Text = StringParser.Parse("${res:AddIns.HtmlHelp2.FilteredBy}");
			label2.Text = StringParser.Parse("${res:AddIns.HtmlHelp2.LookFor}");
		}

		public MsHelp2IndexControl()
		{
			this.controlIsEnabled = (HtmlHelp2Environment.IsReady &&
			                         Help2ControlsValidation.IsIndexControlRegistered);

			if (this.controlIsEnabled)
			{
				try
				{
					indexControl                  = new AxHxIndexCtrl();
					indexControl.BeginInit();
					indexControl.Dock             = DockStyle.Fill;
					indexControl.ItemClick       += new AxMSHelpControls.IHxIndexViewEvents_ItemClickEventHandler(this.IndexItemClicked);
					indexControl.EndInit();
					Controls.Add(indexControl);
					indexControl.CreateControl();
					indexControl.BorderStyle      = HxBorderStyle.HxBorderStyle_FixedSingle;
					indexControl.FontSource       = HxFontSourceConstant.HxFontExternal;

					HtmlHelp2Environment.FilterQueryChanged += new EventHandler(FilterQueryChanged);
					HtmlHelp2Environment.NamespaceReloaded  += new EventHandler(NamespaceReloaded);					
				}
				catch (Exception ex)
				{
					LoggingService.Error("Help 2.0: Index control failed; " + ex.ToString());
					this.FakeHelpControl();
				}
			}
			else
			{
				this.FakeHelpControl();
			}

			// Filter Combobox
			Panel panel1                          = new Panel();
			Controls.Add(panel1);
			panel1.Dock                           = DockStyle.Top;
			panel1.Height                         = filterCombobox.Height + 7;
			panel1.Controls.Add(filterCombobox);
			filterCombobox.Dock                   = DockStyle.Top;
			filterCombobox.DropDownStyle          = ComboBoxStyle.DropDownList;
			filterCombobox.Sorted                 = true;
			filterCombobox.Enabled                = this.controlIsEnabled;
			filterCombobox.SelectedIndexChanged  += new EventHandler(FilterChanged);

			// Filter label
			Controls.Add(label1);
			label1.Text      = StringParser.Parse("${res:AddIns.HtmlHelp2.FilteredBy}");
			label1.Dock      = DockStyle.Top;
			label1.TextAlign = ContentAlignment.MiddleLeft;
			label1.Enabled   = this.controlIsEnabled;

			// SearchTerm Combobox
			Panel panel2            = new Panel();
			Controls.Add(panel2);
			panel2.Dock             = DockStyle.Top;
			panel2.Height           = searchTerm.Height + 7;
			panel2.Controls.Add(searchTerm);
			searchTerm.Dock         = DockStyle.Top;
			searchTerm.Enabled      = this.controlIsEnabled;
			searchTerm.TextChanged += new EventHandler(SearchTextChanged);
			searchTerm.KeyPress    += new KeyPressEventHandler(KeyPressed);

			// SearchTerm Label
			Controls.Add(label2);
			label2.Text             = StringParser.Parse("${res:AddIns.HtmlHelp2.LookFor}");
			label2.Dock             = DockStyle.Top;
			label2.TextAlign        = ContentAlignment.MiddleLeft;
			label2.Enabled          = this.controlIsEnabled;
		}

		private void FakeHelpControl()
		{
			if (indexControl != null) indexControl.Dispose();
			
			indexControl            = null;
			Controls.Clear();
			Panel nohelpPanel       = new Panel();
			Controls.Add(nohelpPanel);
			nohelpPanel.Dock        = DockStyle.Fill;
			nohelpPanel.BorderStyle = BorderStyle.Fixed3D;
		}

		public void LoadIndex()
		{
			if (!this.controlIsEnabled) return;
			
			try
			{
				searchTerm.Text                       = "";
				searchTerm.Items.Clear();
				indexControl.IndexData                = HtmlHelp2Environment.GetIndex(HtmlHelp2Environment.CurrentFilterQuery);
				filterCombobox.SelectedIndexChanged  -= new EventHandler(FilterChanged);
				HtmlHelp2Environment.BuildFilterList(filterCombobox);
				filterCombobox.SelectedIndexChanged  += new EventHandler(FilterChanged);
			}
			catch
			{
				LoggingService.Error("Help 2.0: cannot connect to IHxIndex interface (Index)");
				indexControl.Enabled = false;
				indexControl.BackColor = SystemColors.ButtonFace;
				filterCombobox.Enabled = false;
				searchTerm.Enabled = false;
			}
		}

		private void FilterChanged(object sender, EventArgs e)
		{
			string selectedString       = filterCombobox.SelectedItem.ToString();

			if (selectedString != "")
			{
				Cursor.Current          = Cursors.WaitCursor;
				indexControl.IndexData  = HtmlHelp2Environment.GetIndex(HtmlHelp2Environment.FindFilterQuery(selectedString));
				Cursor.Current          = Cursors.Default;
			}
		}

		#region Help 2.0 Environment Events
		private void FilterQueryChanged(object sender, EventArgs e)
		{
			Application.DoEvents();

			string currentFilterName = filterCombobox.SelectedItem.ToString();
			if (String.Compare(currentFilterName, HtmlHelp2Environment.CurrentFilterName) != 0)
			{
				filterCombobox.SelectedIndexChanged -= new EventHandler(FilterChanged);
				filterCombobox.SelectedIndex         = filterCombobox.Items.IndexOf(HtmlHelp2Environment.CurrentFilterName);
				indexControl.IndexData               = HtmlHelp2Environment.GetIndex(HtmlHelp2Environment.CurrentFilterQuery);
				filterCombobox.SelectedIndexChanged += new EventHandler(FilterChanged);
			}
		}

		private void NamespaceReloaded(object sender, EventArgs e)
		{
			this.LoadIndex();
		}
		#endregion

		private void SearchTextChanged(object sender, EventArgs e)
		{
			if (!itemClicked && searchTerm.Text != "")
			{
				indexControl.Selection = indexControl.IndexData.GetSlotFromString(searchTerm.Text);
			}
		}

		private void KeyPressed(object sender, KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)13)
			{
				int indexSlot    = indexControl.IndexData.GetSlotFromString(searchTerm.Text);
				string indexTerm = indexControl.IndexData.GetFullStringFromSlot(indexSlot, ",");

				searchTerm.Items.Insert(0,indexTerm);
				searchTerm.SelectedIndex = 0;
				if (searchTerm.Items.Count > 10) searchTerm.Items.RemoveAt(10);

				this.ShowSelectedItemEntry(indexTerm, indexSlot);
			}
		}

		private void IndexItemClicked(object sender, IHxIndexViewEvents_ItemClickEvent e)
		{
			string indexTerm         = indexControl.IndexData.GetFullStringFromSlot(e.iItem, ",");
			int indexSlot            = e.iItem;

			itemClicked              = true;
			searchTerm.Items.Insert(0,indexTerm);
			searchTerm.SelectedIndex = 0;
			itemClicked              = false;

			this.ShowSelectedItemEntry(indexTerm, indexSlot);
		}

		private void ShowSelectedItemEntry(string indexTerm, int indexSlot)
		{
			PadDescriptor indexResults =
				WorkbenchSingleton.Workbench.GetPad(typeof(HtmlHelp2IndexResultsPad));
			if (indexResults == null) return;

			try
			{
				IHxTopicList matchingTopics = indexControl.IndexData.GetTopicsFromSlot(indexSlot);

				try
				{
					((HtmlHelp2IndexResultsPad)indexResults.PadContent).CleanUp();
					((HtmlHelp2IndexResultsPad)indexResults.PadContent).IndexResultsListView.BeginUpdate();

					foreach (IHxTopic topic in matchingTopics)
					{
						ListViewItem lvi = new ListViewItem();
						lvi.Text         = topic.get_Title(HxTopicGetTitleType.HxTopicGetRLTitle,
						                                   HxTopicGetTitleDefVal.HxTopicGetTitleFileName);
						lvi.Tag          = topic;
						lvi.SubItems.Add(topic.Location);
						((HtmlHelp2IndexResultsPad)indexResults.PadContent).IndexResultsListView.Items.Add(lvi);
					}
				}
				finally
				{
					((HtmlHelp2IndexResultsPad)indexResults.PadContent).IndexResultsListView.EndUpdate();
					((HtmlHelp2IndexResultsPad)indexResults.PadContent).SortLV(0);
					((HtmlHelp2IndexResultsPad)indexResults.PadContent).SetStatusMessage(indexTerm);
				}

				switch (matchingTopics.Count)
				{
					case 0:
						break;
					case 1:
						IHxTopic topic = (IHxTopic)matchingTopics.ItemAt(1);
						if(topic != null) ShowHelpBrowser.OpenHelpView(topic);
						break;
					default:
						indexResults.BringPadToFront();
						break;
				}
			}
			catch (Exception ex)
			{
				LoggingService.Error("Help 2.0: cannot get matching index entries; " + ex.ToString());
			}
		}
	}
}
