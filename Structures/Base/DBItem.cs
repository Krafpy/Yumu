namespace Yumu
{
    /// <summary>Represents an element that can be stored in a data file.</summary>
    class DBItem
    {
        protected int id;

        /// <summary>The unique ID that identifies this item in the database.</summary>
        public int Id {
            get => id;
            set => id = value;
        }

        /// <param name="id">the unique ID that identifies this item in the database.</param>
        public DBItem(int id)
        {
            this.id = id;
        }
    }
}