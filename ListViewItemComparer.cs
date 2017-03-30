using System;
using System.Collections;
using System.Windows.Forms;

namespace cheat_blocker_gtasa
{
	/// <summary>
	/// Compares ListViewItems using the specified comparison function.
	/// </summary>
	class ListViewItemComparer : IComparer
	{
		public Comparison<ListViewItem> Comparison;
		public bool Descending { get; set; }

		public int Compare(object x, object y)
		{
			if (Comparison == null) return 0;
			return (Descending ? -1 : 1) * Math.Sign(Comparison((ListViewItem)x, (ListViewItem)y));
		}
	}
}
