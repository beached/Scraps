using System;
using System.Windows.Forms;

namespace SyncList {
	public class DataGridViewHelpers {

		public static DataGridViewColumn MakeColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			return new DataGridViewColumn { Name = (headerName ?? name), AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = readOnly, DataPropertyName = name, SortMode = (canSort ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable), CellTemplate = new DataGridViewTextBoxCell( ), Visible = !hidden };
		}

		public static DataGridViewColumn MakeLinkColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			var cellTemplate = new DataGridViewLinkCell {TrackVisitedState = false, UseColumnTextForLinkValue = true, LinkBehavior = LinkBehavior.NeverUnderline};
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.CellTemplate = cellTemplate;
			col.DefaultCellStyle.NullValue = string.Empty;			
			return col;
		}

		public static DataGridViewColumn MakeCheckedColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.DefaultCellStyle.NullValue = false;
			col.CellTemplate = new DataGridViewCheckBoxCell( );
			return col;
		}

		public static DataGridViewColumn MakeDateColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, string dateFormat = null, bool readOnly = true ) {
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.DefaultCellStyle.Format = string.IsNullOrEmpty( dateFormat ) ? @"yyyy/MM/dd": dateFormat;
			return col;
		}
	}
}
