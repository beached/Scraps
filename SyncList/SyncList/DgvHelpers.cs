using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SyncList {
	public static class DgvHelpers {

		public static DataGridViewColumn MakeColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			return new DataGridViewColumn { Name = (headerName ?? name), AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = readOnly, DataPropertyName = name, SortMode = (canSort ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable), CellTemplate = new DataGridViewTextBoxCell( ), Visible = !hidden, Tag = @"CanSearch" };
		}

		public static void AddColumn( DataGridView dgv, string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			dgv.Columns.Add( MakeColumn( name, headerName, hidden, canSort, readOnly ) );
		}

		public static DataGridViewColumn MakeLinkColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			var cellTemplate = new DataGridViewLinkCell { TrackVisitedState = false, UseColumnTextForLinkValue = true, LinkBehavior = LinkBehavior.NeverUnderline };
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.CellTemplate = cellTemplate;
			col.DefaultCellStyle.NullValue = String.Empty;			
			return col;
		}		

		public static void AddLinkColumn( DataGridView dgv, string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			dgv.Columns.Add( MakeLinkColumn( name, headerName, hidden, canSort, readOnly ) );
		}

		public static DataGridViewColumn MakeButtonColumn( string name, bool hidden = false ) {
			return new DataGridViewButtonColumn {
				Name = name, HeaderText = string.Empty, Text = name, UseColumnTextForButtonValue = true, Visible = !hidden, Tag = @"CannotSearch"
			};
		}

		public static void AddButtonColumn( DataGridView dgv, string name, bool hidden = false ) {
			dgv.Columns.Add( MakeButtonColumn( name, hidden ) );
		}
 
		public static DataGridViewColumn MakeCheckedColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.DefaultCellStyle.NullValue = false;
			col.CellTemplate = new DataGridViewCheckBoxCell( );
			col.Tag = @"CannotSearch";
			return col;
		}

		public static void AddCheckedColumn( DataGridView dgv, string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			dgv.Columns.Add( MakeCheckedColumn( name, headerName, hidden, canSort, readOnly ) );
		}

		public static DataGridViewColumn MakeDateColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, string dateFormat = @"yyyy-MM-dd", bool readOnly = true ) {
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.DefaultCellStyle.Format = dateFormat;
			return col;
		}

		public static void AddDateColumn( DataGridView dgv, string name, string headerName = null, bool hidden = false, bool canSort = true, string dateFormat = @"yyyy-MM-dd", bool readOnly = true ) {
			dgv.Columns.Add( MakeDateColumn( name, headerName, hidden, canSort, dateFormat, readOnly ) );
		}


		public static int GetColumnIndex( DataGridView dgv, string columnName ) {
			Debug.Assert( null != columnName, "Null column name's do not make sense" );
			var dataGridViewColumn = dgv.Columns[columnName];
			Debug.Assert( null != dataGridViewColumn, "Column names must exist" );
			return dataGridViewColumn.Index;
		}

		public static string GetCellString( DataGridView dgv, int row, string columnName ) {
			if( 0 > row || dgv.RowCount <= row ) {
				return string.Empty;
			}
			return GetCellString( dgv, row, GetColumnIndex( dgv, columnName ) );
		}

		public static string GetCellString( DataGridView dgv, int row, int col ) {
			var result = String.Empty;
			if( 0 > row || 0 > col || dgv.RowCount <= row || dgv.ColumnCount <= col ) {
				return result;
			}
			var cell = dgv.Rows[row].Cells[col];
			if( null == cell || null == cell.Value ) {
				return result;
			}
			var strTmp = cell.Value.ToString( );
			if( !String.IsNullOrEmpty( strTmp ) ) {
				result = strTmp;
			}
			return result;
		}

		public static string GetColumnName( DataGridView dgv, int column ) {
			Debug.Assert( 0 <= column && dgv.Columns.Count >= column, @"An invalid column number was specified" );
			return dgv.Columns[column].Name;
		}

		public static void UnselectAll( ref DataGridView dgv ) {
			foreach( DataGridViewRow row in dgv.Rows ) {
				row.Selected = false;
			}
		}

		public static void SelectCell( ref DataGridView dgv, int row, int col ) {
			if( 0 > row || 0 > col || dgv.RowCount <= row || dgv.ColumnCount <= col ) {
				return;
			}
			UnselectAll( ref dgv );
			dgv.Rows[row].Cells[col].Selected = true;
		}


		public static void AddCopyColumnMenuItem( DataGridView dgv, ref MenuItem m, int row, int col ) {
			if( 0 > row || 0 > col || dgv.RowCount <= row || dgv.ColumnCount <= col ) {
				return;
			}
			if( String.IsNullOrEmpty( GetCellString( dgv, row, col ).Trim( ) ) ) {
				return;
			}

			m.MenuItems.Add( new MenuItem( string.Format( @"Copy {0}", GetColumnName( dgv, col ) ), delegate {
				Clipboard.SetText( dgv.Rows[row].Cells[col].Value.ToString( ) );
			} ) );
		}

		public static void AddCopyColumnMenuItem( DataGridView dgv, ref ContextMenu cm, int row, int col ) {
			if( 0 > row || 0 > col || dgv.RowCount <= row || dgv.ColumnCount <= col ) {
				return;
			}
			if( String.IsNullOrEmpty( GetCellString( dgv, row, col ).Trim( ) ) ) {
				return;
			}

			cm.MenuItems.Add( new MenuItem( string.Format( @"Copy {0}", GetColumnName( dgv, col ) ), delegate {
				Clipboard.SetText( dgv.Rows[row].Cells[col].Value.ToString( ) );
			} ) );
		}

	}
}
