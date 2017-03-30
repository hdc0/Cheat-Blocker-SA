using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace cheat_blocker_gtasa
{
	public class ProcessSelectionForm : Form
	{
		private ListView processList;
		private Button refreshButton, okButton;

		private string[] preselectNames;
		private ListViewItemComparer sorter;

		public Process SelectedProcess { get; private set; }

		private ProcessSelectionForm()
		{
			SuspendLayout();

			// Sorting by column

			sorter = new ListViewItemComparer { Comparison = NameComparison };

			// Process list

			processList = new ListView()
			{
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				FullRowSelect = true,
				GridLines = true,
				HideSelection = false,
				Location = new System.Drawing.Point(12, 12),
				Size = new System.Drawing.Size(376, 347),
				ListViewItemSorter = sorter,
				TabIndex = 0,
				View = View.Details,
			};
			processList.ColumnClick += new ColumnClickEventHandler(processList_ColumnClick);
			processList.DoubleClick += new EventHandler(processList_DoubleClick);

			processList.Columns.AddRange(new[]
			{
				new ColumnHeader() { Tag = new Comparison<ListViewItem>(NameComparison), Text = "Name", Width = 200 },
				new ColumnHeader() { Tag = new Comparison<ListViewItem>(IDComparison),   Text = "ID",   Width = 60 }
			});

			// Buttons

			refreshButton = new Button()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				Location = new System.Drawing.Point(232, 365),
				Size = new System.Drawing.Size(75, 23),
				TabIndex = 1,
				Text = "&Refresh",
			};
			refreshButton.Click += new EventHandler(btnRefresh_Click);

			okButton = new Button()
			{
				Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
				Location = new System.Drawing.Point(313, 365),
				Size = new System.Drawing.Size(75, 23),
				TabIndex = 2,
				Text = "&OK",
			};
			okButton.Click += new EventHandler(btnOK_Click);

			// Form

			AcceptButton = okButton;
			ClientSize = new System.Drawing.Size(400, 400);
			Controls.AddRange(new Control[] { processList, refreshButton, okButton });
			Text = "Select process";
			FormClosed += new FormClosedEventHandler(ProcessSelectionForm_FormClosed);
			Shown += new EventHandler(ProcessSelectionForm_Shown);

			ResumeLayout(false);
		}

		/// <param name="preselectName">Names of the process that should be pre-selected.</param>
		public ProcessSelectionForm(params string[] preselectNames)
			: this()
		{
			this.preselectNames = preselectNames;
		}

		private void ProcessSelectionForm_Shown(object sender, EventArgs e)
		{
			RefreshList();

			// Preselect first process that matches any of the specified names
			if (preselectNames != null)
			{
				var preselectItem = processList.Items.Cast<ListViewItem>().FirstOrDefault(
					item =>
					{
						var processName = GetProcessName(item);
						return preselectNames.Any(
							preselectName =>
							processName.Equals(preselectName, StringComparison.OrdinalIgnoreCase));
					});
				if (preselectItem != null) preselectItem.Selected = true;
			}
		}

		private void ProcessSelectionForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			DisposeProcesses();
		}

		private void processList_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			var column = processList.Columns[e.Column];

			// Toggle ascending/descending search if column is clicked again
			if (sorter.Comparison == (Comparison<ListViewItem>)column.Tag)
			{
				sorter.Descending = !sorter.Descending;
			}
			// Sort ascending if a different column was clicked
			else
			{
				sorter.Descending = false;
				sorter.Comparison = (Comparison<ListViewItem>)column.Tag;
			}

			processList.Sort();
		}

		private void processList_DoubleClick(object sender, EventArgs e)
		{
			SelectProcess();
		}

		// Compares two ListViewItems by process name
		private int NameComparison(ListViewItem x, ListViewItem y)
		{
			return GetProcessName(x).CompareTo(GetProcessName(y));
		}

		// Compares two ListViewItems by process ID
		private int IDComparison(ListViewItem x, ListViewItem y)
		{
			return GetProcessID(x).CompareTo(GetProcessID(y));
		}

		// Returns the process name of the specified ListViewItem
		private string GetProcessName(ListViewItem item)
		{
			return item.SubItems[0].Text;
		}

		// Returns the process ID of the specified ListViewItem
		private int GetProcessID(ListViewItem item)
		{
			return int.Parse(item.SubItems[1].Text);
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			RefreshList();
		}

		private void RefreshList()
		{
			Cursor = Cursors.WaitCursor;
			Update();

			// Clear current list
			DisposeProcesses();
			processList.Items.Clear();

			var processes = Process.GetProcesses();
			var items = new List<ListViewItem>(processes.Length);

			foreach (var process in processes)
			{
				// Try to get process name and ID
				string name;
				int pid;
				try
				{
					name = process.MainModule.ModuleName;
					pid = process.Id;
				}
				catch (Exception ex)
				{
					if (ex is Win32Exception)
					{
						// Skip 64-bit processes
						if (ex.HResult == unchecked((int)0x80004005)) continue;
					}
					throw;
				}

				// Create ListViewItem
				var item = new ListViewItem(new[] { name, pid.ToString() });
				item.Tag = process;
				items.Add(item);
			}

			// Add ListViewItems to ListView
			processList.Items.AddRange(items.ToArray());
			processList.Sort();

			Cursor = Cursors.Default;
		}

		// Disposes the process objects referenced by the ListViewItems' Tag property (except the selected one)
		private void DisposeProcesses()
		{
			foreach (ListViewItem item in processList.Items)
			{
				if (item.Tag != SelectedProcess) ((Process)item.Tag).Dispose();
			}
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			SelectProcess();
		}

		private void SelectProcess()
		{
			// Make sure exactly one process is selected
			var selectedItems = processList.SelectedItems;
			if (selectedItems.Count != 1) return;

			SelectedProcess = (Process)selectedItems[0].Tag;

			Close();
		}
	}
}
