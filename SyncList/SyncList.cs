// Quickly pulled this togethor from several sources and myself to get a databinding class that automatically invokes and also support filtering and searching.  May not fully work
// 
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace daw.Collections {
    public class SyncList<T>: BindingList<T>, IBindingListView {

        // Invoke stuff for threaded forms
        private ISynchronizeInvoke _SyncObject;
        private Action<ListChangedEventArgs> _FireEventAction;
        
        private List<T> originalListValue = new List<T>( );
        public List<T> OriginalList {
            get { return originalListValue; }
        }

        public SyncList( )
            : this( null ) {
        }

        public SyncList( ISynchronizeInvoke syncObject, IList<T> values = null ) {
            if( values != null ) {
                foreach( T value in values ) {
                    this.Items.Add( value );
                }
            }
            _SyncObject = syncObject;
            _FireEventAction = FireEvent;
        }

        public bool SupportsAdvancedSorting {
            get { return false; }
        }

        public ListSortDescriptionCollection SortDescriptions {
            get { return null; }
        }

        public void ApplySort( ListSortDescriptionCollection sorts ) {
            throw new NotSupportedException( );
        }

        private void FireEvent( ListChangedEventArgs args ) {
            base.OnListChanged( args );
        }

        // Sort Stuff
        private bool m_Sorted = false;
        private ListSortDirection m_SortDirection = ListSortDirection.Ascending;
        private PropertyDescriptor m_SortProperty = null;

        protected override bool SupportsSortingCore {
            get { return true; }
        }

        protected override int FindCore( PropertyDescriptor prop, object key ) {
            if( null != key ) {
                PropertyInfo propInfo = typeof( T ).GetProperty( prop.Name );
                for( int n = 0; n < Count; ++n ) {
                    if( propInfo.GetValue( Items[n], null ).Equals( key ) ) {
                        return n;
                    }
                }
            }
            return -1;
        }

        public int Find( string property, object key ) {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties( typeof( T ) );
            PropertyDescriptor prop = properties.Find( property, true );
            if( null == prop ) {
                return -1;
            }
            return FindCore( prop, key );
        }

        public bool SupportsFiltering
        {
            get { return true; }
        }

        public void RemoveFilter()
        {
            if (Filter != null) Filter = null;
        }

        private string filterValue = null;

        public string Filter {
            get { return filterValue; }
            set {
                if( filterValue == value ) {
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
                    int count = 0;
                    string[] matches = value.Split( new string[] { " AND " }, StringSplitOptions.RemoveEmptyEntries );

                    while( count < matches.Length ) {
                        String filterPart = matches[count].ToString( );

                        // Check to see if the filter was set previously.
                        // Also, check if current filter is a subset of 
                        // the previous filter.
                        if( !String.IsNullOrEmpty( filterValue ) && !value.Contains( filterValue ) ) {
                            ResetList( );
                        }
                        // Parse and apply the filter.
                        SingleFilterInfo filterInfo = ParseFilter( filterPart );
                        ApplyFilter( filterInfo );
                        ++count;
                    }
                }
                // Set the filter value and turn on list changed events.
                filterValue = value;
                RaiseListChangedEvents = true;
                OnListChanged( new ListChangedEventArgs( ListChangedType.Reset, -1 ) );
            }
        }

        protected override bool IsSortedCore {
            get { return m_Sorted; }
        }

        protected override ListSortDirection SortDirectionCore {
            get { return m_SortDirection; }
        }

        protected override PropertyDescriptor SortPropertyCore {
            get { return m_SortProperty; }
        }

        protected override void ApplySortCore( PropertyDescriptor prop, ListSortDirection direction ) {
            m_SortDirection = direction;
            m_SortProperty = prop;
            var listRef = this.Items as List<T>;
            if( listRef == null ) {
                return;
            }
            var comparer = new SortComparer<T>( prop, direction );

            listRef.Sort( comparer );

            OnListChanged( new ListChangedEventArgs( ListChangedType.Reset, -1 ) );
        }

        private void ResetList( ) {
            this.ClearItems( );
            foreach( T t in originalListValue ) {
                this.Items.Add( t );
            }

            if( IsSortedCore ) {
                ApplySortCore( SortPropertyCore, SortDirectionCore );
            }
        }

        protected override void OnListChanged( ListChangedEventArgs e ) {
            // If the list is reset, check for a filter. If a filter 
            // is applied don't allow items to be added to the list.
            if( e.ListChangedType == ListChangedType.Reset ) {
                if( Filter == null || Filter == String.Empty ) {
                    AllowNew = true;
                } else {
                    AllowNew = false;
                }
            }
            // Add the new item to the original list.
            if( e.ListChangedType == ListChangedType.ItemAdded ) {
                OriginalList.Add( this[e.NewIndex] );
                if( !String.IsNullOrEmpty( Filter ) )
                //if (Filter == null || Filter == "")
                {
                    string cachedFilter = this.Filter;
                    this.Filter = String.Empty;
                    this.Filter = cachedFilter;
                }
            }
            // Remove the new item from the original list.
            if( e.ListChangedType == ListChangedType.ItemDeleted ) {
                OriginalList.RemoveAt( e.NewIndex );
            }            

            if( _SyncObject == null ) {
                base.OnListChanged( e );
                FireEvent( e );
            } else {
                if( null != _SyncObject ) {
                    _SyncObject.Invoke( new Action( ( ) => {
                        base.OnListChanged( e );
                        FireEvent( e );
                    } ), new object[] { } );
                }
            }   
        }

        public static String BuildRegExForFilterFormat( ) {
            StringBuilder regex = new StringBuilder( );

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
            List<T> results;

            // Check to see if the property type we are filtering by implements
            // the IComparable interface.
            Type interfaceType =
                TypeDescriptor.GetProperties( typeof( T ) )[filterParts.PropName]
                .PropertyType.GetInterface( "IComparable" );

            if( interfaceType == null )
                throw new InvalidOperationException( "Filtered property" +
                " must implement IComparable." );

            results = new List<T>( );

            // Check each value and add to the results list.
            foreach( T item in this ) {
                if( filterParts.PropDesc.GetValue( item ) != null ) {
                    IComparable compareValue =
                        filterParts.PropDesc.GetValue( item ) as IComparable;
                    int result =
                        compareValue.CompareTo( filterParts.CompareValue );
                    if( filterParts.OperatorValue ==
                        FilterOperator.EqualTo && result == 0 )
                        results.Add( item );
                    if( filterParts.OperatorValue ==
                        FilterOperator.GreaterThan && result > 0 )
                        results.Add( item );
                    if( filterParts.OperatorValue ==
                        FilterOperator.LessThan && result < 0 )
                        results.Add( item );
                }
            }
            this.ClearItems( );
            foreach( T itemFound in results )
                this.Add( itemFound );
        }

        internal SingleFilterInfo ParseFilter( string filterPart ) {
            SingleFilterInfo filterInfo = new SingleFilterInfo( );
            filterInfo.OperatorValue = DetermineFilterOperator( filterPart );

            string[] filterStringParts =
                filterPart.Split( new char[] { (char)filterInfo.OperatorValue } );

            filterInfo.PropName =
                filterStringParts[0].Replace( "[", "" ).
                Replace( "]", "" ).Replace( " AND ", "" ).Trim( );

            // Get the property descriptor for the filter property name.
            PropertyDescriptor filterPropDesc =
                TypeDescriptor.GetProperties( typeof( T ) )[filterInfo.PropName];

            // Convert the filter compare value to the property type.
            if( filterPropDesc == null )
                throw new InvalidOperationException( "Specified property to " +
                    "filter " + filterInfo.PropName +
                    " on does not exist on type: " + typeof( T ).Name );

            filterInfo.PropDesc = filterPropDesc;

            string comparePartNoQuotes = StripOffQuotes( filterStringParts[1] );
            try {
                TypeConverter converter =
                    TypeDescriptor.GetConverter( filterPropDesc.PropertyType );
                filterInfo.CompareValue =
                    converter.ConvertFromString( comparePartNoQuotes );
            } catch( NotSupportedException ) {
                throw new InvalidOperationException( "Specified filter" +
                    "value " + comparePartNoQuotes + " can not be converted" +
                    "from string. Implement a type converter for " +
                    filterPropDesc.PropertyType.ToString( ) );
            }
            return filterInfo;
        }

        internal FilterOperator DetermineFilterOperator( string filterPart ) {
            // Determine the filter's operator.
            if( Regex.IsMatch( filterPart, "[^>^<]=" ) )
                return FilterOperator.EqualTo;
            else if( Regex.IsMatch( filterPart, "<[^>^=]" ) )
                return FilterOperator.LessThan;
            else if( Regex.IsMatch( filterPart, "[^<]>[^=]" ) )
                return FilterOperator.GreaterThan;
            else
                return FilterOperator.None;
        }

        internal static string StripOffQuotes( string filterPart ) {
            // Strip off quotes in compare value if they are present.
            if( Regex.IsMatch( filterPart, "'.+'" ) ) {
                int quote = filterPart.IndexOf( '\'' );
                filterPart = filterPart.Remove( quote, 1 );
                quote = filterPart.LastIndexOf( '\'' );
                filterPart = filterPart.Remove( quote, 1 );
                filterPart = filterPart.Trim( );
            }
            return filterPart;
        }
    }

    internal class SortComparer<T>: IComparer<T> {
        private PropertyDescriptor m_PropDesc = null;
        private ListSortDirection m_Direction = ListSortDirection.Ascending;

        public SortComparer( PropertyDescriptor propDesc, ListSortDirection direction ) {
            m_PropDesc = propDesc;
            m_Direction = direction;
        }

        int IComparer<T>.Compare( T x, T y ) {
            return CompareValues( x, y, m_Direction );
        }

        private int CompareValues( T xValue, T yValue, ListSortDirection direction ) {
            int retValue = 0;
            if( xValue is IComparable ) {   //can ask the x value
                retValue = ((IComparable)xValue).CompareTo( yValue );
            } else if( yValue is IComparable ) {    //can ask the y value
                retValue = ((IComparable)yValue).CompareTo( xValue );
            } else if( !xValue.Equals( yValue ) ) { //not comparable, compare string representations
                retValue = xValue.ToString( ).CompareTo( yValue.ToString( ) );
            }

            if( ListSortDirection.Descending == direction ) {
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
