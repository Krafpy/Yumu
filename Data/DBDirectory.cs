using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System;

namespace Yumu
{
    class DBDirectory : DBItem
    {   
        private string _fullPath;
        public string FullPath {
            get => _fullPath;
            set => _fullPath = value;
        }

        private int _imageCount;
        public int ImageCount {
            get => _imageCount;
            set => _imageCount = value;
        }

        public string Name {
            get => Path.GetFileName(_fullPath);
        }

        public DBDirectory(string path) : base()
        {
            _fullPath = path;
        }

        public DBDirectory() : base() { }

        public override byte[] ToDataRow()
        {
            int pathByteLength = Encoding.UTF8.GetByteCount(_fullPath);

            byte[] data = new byte[12 + pathByteLength];

            ArrayUtils.WriteIntToByteArray(data, 0, _id);
            ArrayUtils.WriteIntToByteArray(data, 4, _imageCount);
            ArrayUtils.WriteIntToByteArray(data, 8, pathByteLength);
            ArrayUtils.WriteStringToByteArray(data, 12, _fullPath);

            return data;
        }

        public override void FromDataRow(byte[] data)
        {
            int id = ArrayUtils.ReadIntFromByteArray(data, 0);
            int imageCount = ArrayUtils.ReadIntFromByteArray(data, 4);
            int pathByteLength = ArrayUtils.ReadIntFromByteArray(data, 8);
            string path = ArrayUtils.ReadStringFromByteArray(data, 12, pathByteLength);

            _id = id;
            _fullPath = path;
            _imageCount = imageCount;
        }

        public List<DBImage> LookupImageFiles()
        {
            string[] imageFiles = GetAllImageFiles();
            List<DBImage> imgs = new List<DBImage>();
            foreach(string imageFile in imageFiles){
                string fileName = Path.GetFileName(imageFile);
                DBImage img = new DBImage(_id, fileName);
                if(img.IsValid) {
                    imgs.Add(img);
                }
            }
            _imageCount = imgs.Count;

            return imgs;
        }

        private string[] GetAllImageFiles()
        {
            string[] allFiles = Directory.GetFiles(_fullPath);
            string[] extensions = new string[] {
                ".jpg", ".jpeg", ".png", ".gif", ".tiff", ".bmp"
            };
            string[] imageFiles = Array.FindAll(allFiles, file =>
                extensions.Contains(Path.GetExtension(file).ToLower())
            );
            return imageFiles;
        }
    }
}