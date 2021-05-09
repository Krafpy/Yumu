namespace Yumu
{
    abstract class DBItem
    {
        protected int _id;
        public int Id {
            get => _id;
            set => _id = value;
        }

        protected DBItem(int id)
        {
            _id = id;
        }

        protected DBItem() { }

        public abstract byte[] ToDataRow();
        public abstract void FromDataRow(byte[] data);
    }
}