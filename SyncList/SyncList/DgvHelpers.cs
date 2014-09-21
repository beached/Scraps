using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SyncList {
	public class DgvHelpers {

		public static DataGridViewColumn MakeColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			return new DataGridViewColumn { Name = (headerName ?? name), AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells, ReadOnly = readOnly, DataPropertyName = name, SortMode = (canSort ? DataGridViewColumnSortMode.Automatic : DataGridViewColumnSortMode.NotSortable), CellTemplate = new DataGridViewTextBoxCell( ), Visible = !hidden };
		}

		public static DataGridViewColumn MakeLinkColumn( string name, string headerName = null, bool hidden = false, bool canSort = true, bool readOnly = true ) {
			var cellTemplate = new DataGridViewLinkCell { TrackVisitedState = false, UseColumnTextForLinkValue = true, LinkBehavior = LinkBehavior.NeverUnderline };
			var col = MakeColumn( name, headerName, hidden, canSort, readOnly );
			col.CellTemplate = cellTemplate;
			col.DefaultCellStyle.NullValue = String.Empty;
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
			col.DefaultCellStyle.Format = String.IsNullOrEmpty( dateFormat ) ? @"yyyy-MM-dd" : dateFormat;
			return col;
		}

		public static int GetColumnIndex( DataGridView dgv, string columnName ) {
			Debug.Assert( null != columnName, "Null column name's do not make sense" );
			var dataGridViewColumn = dgv.Columns[columnName];
			Debug.Assert( null != dataGridViewColumn, "Column names must exist" );
			return dataGridViewColumn.Index;
		}

		public static string GetCellString( DataGridView dgv, int row, string columnName ) {
			return GetCellString( dgv, row, GetColumnIndex( dgv, columnName ) );
		}

		public static string GetCellString( DataGridView dgv, int row, int col ) {
			var result = String.Empty;
			if( 0 > row ) {
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
			Debug.Assert( 0 <= column && dgv.Columns.Count > column, @"An invalid column number was specified" );
			return dgv.Columns[column].Name;
		}

		public static void UnselectAll( DataGridView dgv ) {
			foreach( DataGridViewRow row in dgv.Rows ) {
				row.Selected = false;
			}
		}

		public static void SelectCell( DataGridView dgv, int row, int col ) {
			if( 0 > row || 0 > col ) {
				return;
			}
			UnselectAll( dgv );
			dgv.Rows[row].Cells[col].Selected = true;
		}


		public static void AddCopyColumnMenuItem( DataGridView dgv, ref MenuItem m, int row, int col ) {
			if( String.IsNullOrEmpty( GetCellString( dgv, row, col ).Trim( ) ) ) {
				return;
			}

			m.MenuItems.Add( new MenuItem( string.Format( @"Copy {0}", GetColumnName( dgv, col ) ), delegate {
				Clipboard.SetText( dgv.Rows[row].Cells[col].Value.ToString( ) );
			} ) );
		}

		public static void AddCopyColumnMenuItem( DataGridView dgv, ref ContextMenu cm, int row, int col ) {
			if( String.IsNullOrEmpty( GetCellString( dgv, row, col ).Trim( ) ) ) {
				return;
			}

			cm.MenuItems.Add( new MenuItem( string.Format( @"Copy {0}", GetColumnName( dgv, col ) ), delegate {
				Clipboard.SetText( dgv.Rows[row].Cells[col].Value.ToString( ) );
			} ) );
		}

	}
}
