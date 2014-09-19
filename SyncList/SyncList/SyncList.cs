// Quickly pulled this togethor from several sources and myself to get a databinding class that automatically invokes and also support filtering and searching.  May not fully work
// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace daw.Collections {
	public class SyncList<T>: BindingList<T>, IBindingListView {

		// Invoke stuff for threaded forms
		private readonly ISynchronizeInvoke _syncObject;
		private Action<ListChangedEventArgs> _fireEventAction;

		private readonly List<T> _originalListValue = new List<T>( );
		public List<T> OriginalList {
			get { return _originalListValue; }
		}

		public SyncList( ): this( null ) {
		}

		
		public SyncList( ISynchronizeInvoke syncObject, IEnumerable<T> values = null ) {
			if( values != null ) {
				foreach( T value in values ) {
					this.Items.Add( value );
				}
			}
			_syncObject = syncObject;
			_fireEventAction = FireEvent;
		}


		// Sort Stuff
		private bool _isSorted = false;
		private ListSortDirection _sortDirection = ListSortDirection.Ascending;
		private PropertyDescriptor _sortProperty = null;

		public bool SupportsAdvancedSorting {
			get { return false; }
		}

		public ListSortDescriptionCollection SortDescriptions {
			get { return null; }
		}

		protected override ListSortDirection SortDirectionCore {
			get { return _sortDirection; }
		}

		protected override PropertyDescriptor SortPropertyCore {
			get { return _sortProperty; }
		}

		protected override bool IsSortedCore {
			get { return _isSorted; }
		}

		protected override void RemoveSortCore( ) {
			base.RemoveSortCore( );
			_isSorted = false;
			_sortProperty = null;
		}

		public void ApplySort( ListSortDescriptionCollection sorts ) {
			throw new NotSupportedException( );
		}

		protected override bool SupportsSortingCore {
			get { return true; }
		}

		protected override void ApplySortCore( PropertyDescriptor prop, ListSortDirection direction ) {
			_sortDirection = direction;
			_sortProperty = prop;

			var listRef = this.Items as List<T>;
			if( null == listRef ) {
				return;
			}
			_isSorted = true;
			var comparer = new SortComparer<T>( _sortProperty, _sortDirection );

			listRef.Sort( comparer );

			OnListChanged( new ListChangedEventArgs( ListChangedType.Reset, -1 ) );
		}


		private void FireEvent( ListChangedEventArgs args ) {
			base.OnListChanged( args );
		}

		protected override int FindCore( PropertyDescriptor prop, object key ) {
			if( null == key ) {
				return -1;
			}
			var propInfo = typeof( T ).GetProperty( prop.Name );
			for( var n = 0; n < Count; ++n ) {
				if( propInfo.GetValue( Items[n], null ).Equals( key ) ) {
					return n;
				}
			}
			return -1;
		}

		public int Find( string property, object key ) {
			var properties = TypeDescriptor.GetProperties( typeof( T ) );
			var prop = properties.Find( property, true );
			if( null == prop ) {
				return -1;
			}
			return FindCore( prop, key );
		}

		public bool SupportsFiltering {
			get { return true; }
		}

		public void RemoveFilter( ) {
			Filter = null;
		}

		private string _filterValue = null;

		public string Filter {
			get { return _filterValue; }
			set {
				if( _filterValue == value ) {
					return;
				}

				// If the value is not null or empty, but doesn't
				// match expected format, throw an exception.
				if( !String.IsNullOrEmpty( value ) && !Regex.IsMatch( value, BuildRegExForFilterFormat( ), RegexOptions.Singleline ) ) {
					throw new ArgumentException( "Filter is not in the format: propName[<>=]'value'." );
				}
				//Turn off list-changed events.
				RaiseListChangedEvents = false;

				// If the value is null or empty, reset list.
				if( String.IsNullOrEmpty( value ) ) {
					ResetList( );
				} else {
					var count = 0;
					var matches = value.Split( new string[] { " AND " }, StringSplitOptions.RemoveEmptyEntries );

					while( count < matches.Length ) {
						var filterPart = matches[count].ToString( );

						// Check to see if the filter was set previously.
						// Also, check if current filter is a subset of 
						// the previous filter.
						if( !String.IsNullOrEmpty( _filterValue ) && !value.Contains( _filterValue ) ) {
							ResetList( );
						}
						// Parse and apply the filter.
						var filterInfo = ParseFilter( filterPart );
						ApplyFilter( filterInfo );
						++count;
					}
				}
				// Set the filter value and turn on list changed events.
				_filterValue = value;
				RaiseListChangedEvents = true;
				OnListChanged( new ListChangedEventArgs( ListChangedType.Reset, -1 ) );
			}
		}

		private void ResetList( ) {
			this.ClearItems( );
			foreach( var t in _originalListValue ) {
				this.Items.Add( t );
			}

			if( IsSortedCore && null != SortPropertyCore ) {
				ApplySortCore( SortPropertyCore, SortDirectionCore );
			}
		}

		protected override void OnListChanged( ListChangedEventArgs e ) {
			// If the list is reset, check for a filter. If a filter 
			// is applied don't allow items to be added to the list.
			if( e.ListChangedType == ListChangedType.Reset ) {
				AllowNew = string.IsNullOrEmpty( Filter );
			}
			// Add the new item to the original list.
			if( e.ListChangedType == ListChangedType.ItemAdded ) {
				OriginalList.Add( this[e.NewIndex] );
				if( !String.IsNullOrEmpty( Filter ) ) {
					var cachedFilter = this.Filter;
					this.Filter = string.Empty;
					this.Filter = cachedFilter;
				}
			}
			// Remove the new item from the original list.
			if( e.ListChangedType == ListChangedType.ItemDeleted ) {
				OriginalList.RemoveAt( e.NewIndex );
			}

			if( null == _syncObject ) {
				base.OnListChanged( e );
				FireEvent( e );
			} else if( null != _syncObject ) {
				_syncObject.Invoke( new Action( ( ) => {
					base.OnListChanged( e );
					FireEvent( e );
				} ), new object[] { } );
			}
		}

		public static String BuildRegExForFilterFormat( ) {
			var regex = new StringBuilder( );

			// Look for optional literal brackets, 
			// followed by word characters or space.
			regex.Append( @"\[?[\w\s]+\]?\s?" );

			// Add the operators: > < or =.
			regex.Append( @"[><=]" );

			//Add optional space followed by optional quote and
			// any character followed by the optional quote.
			regex.Append( @"\s?'?.+'?" );

			return regex.ToString( );
		}

		internal void ApplyFilter( SingleFilterInfo filterParts ) {

			// Check to see if the property type we are filtering by implements
			// the IComparable interface.
			var interfaceType = TypeDescriptor.GetProperties( typeof( T ) )[filterParts.PropName].PropertyType.GetInterface( "IComparable" );

			if( null == interfaceType ) {
				throw new InvalidOperationException( "Filtered property must implement IComparable." );
			}

			var results = new List<T>( );

			// Check each value and add to the results list.
			foreach( var item in this ) {
				if( null == filterParts.PropDesc.GetValue( item ) ) {
					continue;
				}
				var compareValue = filterParts.PropDesc.GetValue( item ) as IComparable;
				if( null == compareValue && null == filterParts.CompareValue ) {
					continue;
				}
				var result = compareValue.CompareTo( filterParts.CompareValue );
				if( filterParts.OperatorValue == FilterOperator.EqualTo && result == 0 ) {
					results.Add( item );
				} else if( filterParts.OperatorValue == FilterOperator.GreaterThan && result > 0 ) {
					results.Add( item );
				} else if( filterParts.OperatorValue == FilterOperator.LessThan && result < 0 ) {
					results.Add( item );
				}
			}
			this.ClearItems( );
			foreach( var itemFound in results ) {
				this.Add( itemFound );
			}

		}

		internal SingleFilterInfo ParseFilter( string filterPart ) {
			Debug.Assert( !string.IsNullOrEmpty( filterPart ), "filterPart cannot be null" );

			var filterInfo = new SingleFilterInfo( );
			filterInfo.OperatorValue = DetermineFilterOperator( filterPart );

			var filterStringParts = null == filterPart ? string.Empty.Split( ) : filterPart.Split( new char[] { (char)filterInfo.OperatorValue } );

			filterInfo.PropName = filterStringParts[0].Replace( "[", "" ).Replace( "]", "" ).Replace( " AND ", "" ).Trim( );

			// Get the property descriptor for the filter property name.
			var filterPropDesc = TypeDescriptor.GetProperties( typeof( T ) )[filterInfo.PropName];

			// Convert the filter compare value to the property type.
			if( null == filterPropDesc ) {
				throw new InvalidOperationException( string.Format( "Specified property to filter {0} on does not exist on type: {1}", filterInfo.PropName, typeof( T ).Name ) );
			}
			filterInfo.PropDesc = filterPropDesc;

			var comparePartNoQuotes = StripOffQuotes( filterStringParts[1] );
			try {
				TypeConverter converter = TypeDescriptor.GetConverter( filterPropDesc.PropertyType );
				filterInfo.CompareValue = converter.ConvertFromString( comparePartNoQuotes );
			} catch( NotSupportedException ) {
				throw new InvalidOperationException( string.Format( "Specified filter value {0} can not be converted from string. Implement a type converter for {1}", comparePartNoQuotes, filterPropDesc.PropertyType.ToString( ) ) );
			}
			return filterInfo;
		}

		internal FilterOperator DetermineFilterOperator( string filterPart ) {
			// Determine the filter's operator.
			if( Regex.IsMatch( filterPart, "[^>^<]=" ) ) {
				return FilterOperator.EqualTo;
			} else if( Regex.IsMatch( filterPart, "<[^>^=]" ) ) {
				return FilterOperator.LessThan;
			} else if( Regex.IsMatch( filterPart, "[^<]>[^=]" ) ) {
				return FilterOperator.GreaterThan;
			}
			return FilterOperator.None;
		}

		internal static string StripOffQuotes( string filterPart ) {
			// Strip off quotes in compare value if they are present.
			if( !Regex.IsMatch( filterPart, "'.+'" ) ) {
				return filterPart;
			}
			var quote = filterPart.IndexOf( '\'' );
			filterPart = filterPart.Remove( quote, 1 );
			quote = filterPart.LastIndexOf( '\'' );
			filterPart = filterPart.Remove( quote, 1 );
			filterPart = filterPart.Trim( );
			return filterPart;
		}
	}

	internal class SortComparer<T>: Comparer<T> {
		private readonly PropertyDescriptor _propDesc;		
		private readonly ListSortDirection _direction = ListSortDirection.Ascending;

		public SortComparer( PropertyDescriptor propDesc, ListSortDirection direction ) {
			if( propDesc.ComponentType != typeof( T ) ) {
				throw new MissingMemberException( typeof( T ).Name, propDesc.Name );
			}
			_propDesc = propDesc;
			_direction = direction;

		}

		public override int Compare( T x, T y ) {
			var xValue = _propDesc.GetValue( x );
			var yValue = _propDesc.GetValue( y );
			int retValue = 0;
			if( null == yValue ) {
				if( null != xValue ) {
					retValue = 1;
				} else {
					retValue = 0;
				}
			} else if( null == xValue ) {
				if( null != yValue ) {
					retValue = -1;
				} else {
					retValue = 0;
				}
			} else if( xValue is IComparable ) {   //can ask the x value
				retValue = (xValue as IComparable).CompareTo( yValue );
			} else if( !xValue.Equals( yValue ) ) { //not comparable, compare string representations
				var strX = xValue.ToString( );
				var strY = yValue.ToString( );
				retValue = System.String.Compare( strX, strY, System.StringComparison.OrdinalIgnoreCase );
			}

			if( ListSortDirection.Descending == _direction ) {
				retValue *= -1;
			}

			return retValue;
		}
	}


	public struct SingleFilterInfo {
		internal string PropName;
		internal PropertyDescriptor PropDesc;
		internal Object CompareValue;
		internal FilterOperator OperatorValue;
	}

	// Enum to hold filter operators. The chars 
	// are converted to their integer values.
	public enum FilterOperator {
		EqualTo = '=',
		LessThan = '<',
		GreaterThan = '>',
		None = ' '
	}

}
