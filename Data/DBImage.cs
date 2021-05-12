using System.Text.RegularExpressions;
using System.IO;
using System.Text;

namespace Yumu
{
    class DBImage : DBItem
    {
        private int _dirId;
        public int DirId {get => _dirId;}

        private string _fileName;
        public string FileName {
            get => _fileName;
            set {
                _fileName = value;
                CreateSearchName();
            }
        }

        private string _searchName;
        public string SearchName {
            get => _searchName;
            set => _searchName = value;
        }

        private int _usage;
        public int Usage {
            get => _usage;
            set => _usage = value;
        }

        public bool IsValid {
            get => _searchName.Length > 0;
        }

        public string DisplayName {
            get => Path.GetFileNameWithoutExtension(_fileName);
        }

        public DBImage(int dirId, string fileName) : base()
        {
            _dirId = dirId;
            _fileName = fileName;
            
            CreateSearchName();
        }

        public DBImage() : base() {}

        private void CreateSearchName()
        {
            string noExts = Path.GetFileNameWithoutExtension(_fileName);
            _searchName = SimplifyString(noExts);
        }

        public override void FromDataRow(byte[] data)
        {
            int id = ArrayUtils.ReadIntFromByteArray(data, 0);
            int dirId = ArrayUtils.ReadIntFromByteArray(data, 4);
            int usage = ArrayUtils.ReadIntFromByteArray(data, 8);
            int searchNameByteLength = ArrayUtils.ReadIntFromByteArray(data, 12);
            int fileNameByteLength = ArrayUtils.ReadIntFromByteArray(data, 16);
            string searchName = ArrayUtils.ReadStringFromByteArray(data, 20, searchNameByteLength);
            string fileName = ArrayUtils.ReadStringFromByteArray(data, 20 + searchNameByteLength, fileNameByteLength);

            _id = id;
            _dirId = dirId;
            _usage = usage;
            _searchName = searchName;
            _fileName = fileName;
        }

        public override byte[] ToDataRow()
        {
            int searchNameByteLength = Encoding.UTF8.GetByteCount(_searchName);
            int fileNameByteLength = Encoding.UTF8.GetByteCount(_fileName);

            byte[] data = new byte[20 + searchNameByteLength + fileNameByteLength];

            ArrayUtils.WriteIntToByteArray(data, 0, _id);
            ArrayUtils.WriteIntToByteArray(data, 4, _dirId);
            ArrayUtils.WriteIntToByteArray(data, 8, _usage);
            ArrayUtils.WriteIntToByteArray(data, 12, searchNameByteLength);
            ArrayUtils.WriteIntToByteArray(data, 16, fileNameByteLength);
            ArrayUtils.WriteStringToByteArray(data, 20, _searchName);
            ArrayUtils.WriteStringToByteArray(data, 20 + searchNameByteLength, _fileName);

            return data;
        }

        public static string SimplifyString(string str)
        {
            string newStr = Regex.Replace(str, @"\s+", "");
            newStr = newStr.ToLower();
            return newStr;
        }
    }
}