using System;
using System.Collections.Generic;
using System.Linq;

namespace Yumu
{
    class DBAccessor 
    {
        private List<ReferencedDirectory> _directories;
        public List<ReferencedDirectory> Directories {get => _directories;}

        private List<ReferencedImage> _images;
        public List<ReferencedImage> Images {get => _images;}

        public DBAccessor()
        {
            _directories = DB.GetItems<ReferencedDirectory>(ReferencedDirectory.DataFile).ToList();
            _images = DB.GetItems<ReferencedImage>(ReferencedImage.DataFile).ToList();

            _directories = _directories.OrderBy(dir => dir.Id).ToList();
            _images = _images.OrderBy(img => img.Id).ToList();
        }

        public void AppendDirectoryReference(ReferencedDirectory dir)
        {
            if(!DirectoryReferenceExists(dir)) {
                dir.Id = 0;
                if(_directories.Count > 0)
                    dir.Id = _directories.Last().Id + 1;
                dir.LookupImageFiles();
                DB.AppendItem<ReferencedDirectory>(dir, ReferencedDirectory.DataFile);
                AppendImageReferences(dir.Images);
                
                _directories.Add(dir);
            }
        }

        private void AppendImageReferences(ReferencedImage[] imgs){
            int startId = 0;
            if(_images.Count > 0)
                startId = _images.Last().Id + 1;
            for(int i = 0; i < imgs.Length; ++i)
                imgs[i].Id = startId + i;
            DB.AppendItems<ReferencedImage>(imgs, ReferencedImage.DataFile);
            
            _images.AddRange(imgs);
        }

        public void RemoveReferencedDirectory(int dirId)
        {
            for(int i = 0; i < _directories.Count; ++i){
                if(_directories[i].Id == dirId){
                    DB.RemoveItem<ReferencedDirectory>(dirId, 
                    ReferencedDirectory.DataFile, 
                    _directories.ToArray());
                    RemoveReferencedImages(dirId);

                    _directories.RemoveAt(i);
                    _directories = _directories.OrderBy(dir => dir.Id).ToList();

                    break;
                }
            }
        }

        private void RemoveReferencedImages(int dirId)
        {
            List<int> removeIds = new List<int>();
            foreach(ReferencedImage img in _images){
                if(img.DirId == dirId) {
                    removeIds.Add(img.Id);
                }
            }

            if(!removeIds.Any()) return;

            DB.RemoveItems<ReferencedImage>(removeIds.ToArray(),
            ReferencedImage.DataFile, 
            _images.ToArray());
            
            int rmIndex = removeIds.Count - 1;
            int swapEnd = _images.Count - 1;
            for(int i = _images.Count - 1; i >= 0 && rmIndex >= 0; --i){
                if(_images[i].Id == removeIds[rmIndex]){
                    ReferencedImage t = _images[i];
                    _images[i] = _images[swapEnd];
                    _images[swapEnd] = t;
                    swapEnd--;
                    rmIndex--;
                }
            }

            _images.RemoveRange(swapEnd + 1, removeIds.Count);
            _images = _images.OrderBy(img => img.Id).ToList();
        }

        public void UpdateReferencedDirectory(int dirId)
        {
            ReferencedDirectory dir = GetReferencedDirectory(dirId); 
            if(dir != null) {
                RemoveReferencedImages(dirId);
                dir.LookupImageFiles();
                AppendImageReferences(dir.Images);

                DB.UpdateContent<ReferencedDirectory>(_directories.ToArray(), 
                ReferencedDirectory.DataFile);
            }
        }

        public void UpdateReferencedImage(int imgId)
        {
            ReferencedImage img = GetReferencedImage(imgId);
            if(img != null){
                DB.UpdateContent<ReferencedImage>(_images.ToArray(), 
                ReferencedImage.DataFile);
            }
        }

        private bool DirectoryReferenceExists(ReferencedDirectory searchDir)
        {
            foreach(ReferencedDirectory dir in _directories){
                if(dir.FullPath == searchDir.FullPath)
                    return true;
            }
            return false;
        }

        public ReferencedDirectory GetReferencedDirectory(int id)
        {
            return FindFromId(_directories, id);
        }

        public ReferencedImage GetReferencedImage(int id)
        {
            return FindFromId(_images, id);
        }

        private static T FindFromId<T>(List<T> items, int id) where T : DBItem
        {
            // Simple binary search implementation
            int start = 0;
            int end = items.Count - 1;
            while(start <= end){
                int mid = (start + end) / 2;
                if(items[mid].Id == id){
                    return items[mid];
                } else if(id < items[mid].Id){
                    end = mid - 1;
                } else {
                    start = mid + 1;
                }
            }
            return null;
        }
    }
}