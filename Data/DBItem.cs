namespace Yumu
{
    abstract class DBItem
    {
        protected int _id;
        public int Id {
            get => _id;
            set => _id = value;
        }

        public abstract byte[] ToDataRow();
        public abstract void FromDataRow(byte[] data);
    }
}