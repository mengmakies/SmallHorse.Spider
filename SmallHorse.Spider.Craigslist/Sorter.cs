using System;
using System.Collections;
using System.Windows.Forms;
using System.Text;

namespace SmallHorse.Spider.Craigslist
{
    /// <summary>
    /// This class is an implementation of the 'IComparer' interface.
    /// It was lifted from http://support.microsoft.com/kb/319401
    /// "How to sort a ListView control by a column in Visual C#"
    /// </summary>
    public class ListViewColumnSorter : IComparer
    {
        /// <summary>
        /// Specifies the column to be sorted
        /// </summary>
        private int ColumnToSort;
        /// <summary>
        /// Specifies the order in which to sort (i.e. 'Ascending').
        /// </summary>
        private SortOrder OrderOfSort;
        /// <summary>
        /// Case insensitive comparer object
        /// </summary>
        private CaseInsensitiveComparer ObjectCompare;

        /// <summary>
        /// Class constructor.  Initializes various elements
        /// </summary>
        public ListViewColumnSorter()
        {
            // Initialize the column to '0'
            ColumnToSort = 0;

            // Initialize the sort order to 'none'
            OrderOfSort = SortOrder.None;

            // Initialize the CaseInsensitiveComparer object
            ObjectCompare = new CaseInsensitiveComparer();
        }

        /// <summary>
        /// This method is inherited from the IComparer interface.  It compares the two objects passed using a case insensitive comparison.
        /// </summary>
        /// <param name="x">First object to be compared</param>
        /// <param name="y">Second object to be compared</param>
        /// <returns>The result of the comparison. "0" if equal, negative if 'x' is less than 'y' and positive if 'x' is greater than 'y'</returns>
        public int Compare(object x, object y)
        {
            int compareResult = 0;
            ListViewItem listviewX, listviewY;

            // Cast the objects to be compared to ListViewItem objects
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;

            // Compare the two items
            switch (ColumnToSort)
            {
                case 1: // price
                    {
                        Item i1 = (Item)listviewX.Tag;
                        Item i2 = (Item)listviewY.Tag;
                        compareResult = i1.Price - i2.Price;
                    }
                    break;
                case 2: // date
                    {
                        Item i1 = (Item)listviewX.Tag;
                        Item i2 = (Item)listviewY.Tag;
                        compareResult = (int)(i1.Date - i2.Date).TotalSeconds;
                    }
                    break;
                default:
                    {
                        string s1 = listviewX.SubItems[ColumnToSort].Text;
                        string s2 = listviewY.SubItems[ColumnToSort].Text;
                        compareResult = ObjectCompare.Compare(s1, s2);
                    }
                    break;
            }

            // Calculate correct return value based on object comparison
            if (OrderOfSort == SortOrder.Ascending)
            {
                // Ascending sort is selected, return normal result of compare operation
                return compareResult;
            }
            else if (OrderOfSort == SortOrder.Descending)
            {
                // Descending sort is selected, return negative result of compare operation
                return (-compareResult);
            }
            else
            {
                // Return '0' to indicate they are equal
                return 0;
            }
        }

        /// <summary>
        /// Gets or sets the number of the column to which to apply the sorting operation (Defaults to '0').
        /// </summary>
        public int SortColumn
        {
            set
            {
                ColumnToSort = value;
            }
            get
            {
                return ColumnToSort;
            }
        }

        /// <summary>
        /// Gets or sets the order of sorting to apply (for example, 'Ascending' or 'Descending').
        /// </summary>
        public SortOrder Order
        {
            set
            {
                OrderOfSort = value;
            }
            get
            {
                return OrderOfSort;
            }
        }

    }
}
