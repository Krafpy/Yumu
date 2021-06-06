using System.Collections.Generic;
using System.Linq;

namespace Yumu
{
    class DBAccessor 
    {
        public const string DIRS_DB_FILE = "./dirs.dat";
        public const string IMGS_DB_FILE = "./imgs.dat";
        
        private List<DBDirectory> _directories;
        public List<DBDirectory> Directories {get => _directories;}

        private List<DBImage> _images;
        public List<DBImage> Images {get => _images;}

        private Dictionary<int, int> _dirsIdMap;
        private Dictionary<int, int> _imagesIdMap;

        public DBAccessor()
        {
            _directories = DB.GetItems<DBDirectory>(DIRS_DB_FILE).
            OrderBy(dir => dir.Id).ToList();

            _images = DB.GetItems<DBImage>(IMGS_DB_FILE).
            OrderBy(img => img.Id).ToList();

            _dirsIdMap = new Dictionary<int, int>();
            _imagesIdMap = new Dictionary<int, int>();

            MapIdsToIndices(_dirsIdMap, _directories);
            MapIdsToIndices(_imagesIdMap, _images);
        }

        private static void MapIdsToIndices<T>(Dictionary<int, int> dict, List<T> items) 
        where T : DBItem
        {
            for(int i = 0; i < items.Count; ++i)
                dict.Add(items[i].Id, i);
        }

        /// <summary>Appends the new directory into the database.</summary>
        public void AppendDirectoryReference(DBDirectory dir)
        {
            if(!DirReferenceExists(dir)) {
                dir.Id = 0;
                if(_directories.Any())
                    dir.Id = _directories.Last().Id + 1;
                List<DBImage> imgs = dir.LookupImageFiles();
                DB.AppendItem<DBDirectory>(dir, DIRS_DB_FILE);
                AppendImageReferences(imgs);
                
                _directories.Add(dir);
                _dirsIdMap.Add(dir.Id, _directories.Count - 1);
            }
        }

        private void AppendImageReferences(List<DBImage> imgs){
            int startId = 0;
            if(_images.Any())
                startId = _images.Last().Id + 1;
            for(int i = 0; i < imgs.Count; ++i) {
                imgs[i].Id = startId + i;
                _imagesIdMap.Add(imgs[i].Id, i);
            }
            DB.AppendItems<DBImage>(imgs.ToArray(), IMGS_DB_FILE);
            
            _images.AddRange(imgs);
        }

        /// <summary>Removes the directory with the specified ID from the
        /// database.</summary>
        public void RemoveReferencedDirectory(int dirId)
        {
            for(int i = 0; i < _directories.Count; ++i) {
                if(_directories[i].Id == dirId) {
                    RemoveReferencedImages(dirId);
                    
                    _directories.RemoveAt(i);
                    _dirsIdMap.Remove(dirId);

                    DB.UpdateContent<DBDirectory>(_directories.ToArray(),
                    DIRS_DB_FILE);
                }
            }
        }

        private void RemoveReferencedImages(int dirId)
        {
            int dirImgCount = GetReferencedDirectory(dirId).ImageCount;
            int swapEnd = _images.Count - 1;
            int removedImgs = 0;
            for(int i = _images.Count - 1; i >= 0 && removedImgs < dirImgCount; --i){
                if(_images[i].DirId == dirId){
                    DBImage tmp = _images[i];
                    _images[i] = _images[swapEnd];
                    _images[swapEnd] = tmp;

                    swapEnd--;

                    _imagesIdMap.Remove(_images[i].Id);
                }
            }

            _images.RemoveRange(swapEnd + 1, dirImgCount);
            _images = _images.OrderBy(img => img.Id).ToList();
            
            DB.UpdateContent<DBImage>(_images.ToArray(), 
            IMGS_DB_FILE);
        }

        /// <summary> Reloads the content of the directory and store the new
        /// content into the database.</summary>
        public void UpdateReferencedDirectory(int dirId)
        {
            DBDirectory dir = GetReferencedDirectory(dirId); 
            if(dir != null) {
                RemoveReferencedImages(dirId);
                List<DBImage> imgs = dir.LookupImageFiles();
                AppendImageReferences(imgs);

                DB.UpdateContent<DBDirectory>(_directories.ToArray(), 
                DIRS_DB_FILE);
            }
        }

        /// <summary> Increments the usage of the specified image and updates
        /// the changes in the database.</summary>
        public void UpdateReferencedImage(int imgId)
        {
            DBImage img = GetReferencedImage(imgId);
            if(img != null){
                img.Usage++;

                DB.UpdateContent<DBImage>(_images.ToArray(), 
                IMGS_DB_FILE);
            }
        }

        private bool DirReferenceExists(DBDirectory searchDir)
        {
            foreach(DBDirectory dir in _directories){
                if(dir.FullPath == searchDir.FullPath)
                    return true;
            }
            return false;
        }

        /// <summary> Retrieves an stored image from its ID </summary>
        public DBImage GetReferencedImage(int id)
        {
            if(_imagesIdMap.ContainsKey(id))
                return _images[_imagesIdMap[id]];
            return null;
        }

        /// <summary> Retrieves an stored directory from its ID </summary>
        public DBDirectory GetReferencedDirectory(int id)
        {
            if(_dirsIdMap.ContainsKey(id))
                return _directories[_dirsIdMap[id]];
            return null;
        }
    }
}