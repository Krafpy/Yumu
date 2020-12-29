using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Yumu
{
    /// <summary>Class that represents a referenced directory, i.e. a directory
    /// in which images might be searched.</summary>
    class ReferencedDirectory : DBItem
    {   
        private int imageCount;
        private string fullPath;

        private ReferencedImage[] containedImages;

        public int ImageCount {get => imageCount;}
        public string FullPath {get => fullPath;}

        public string Name {
            get => Path.GetFileName(fullPath);
        }

        /// <summary>Checks if a directory with the same full path
        /// is already referenced.</summary>
        public bool Exists {
            get => FindFromPath(fullPath) != null;
        }
        
        /// <param name="id">the ID that identifies this directory in the database,
        /// if not already in the database it must be set to -1.</param>
        /// <param name="fullPath">the full path to the directory.</param>
        /// <param name="imageCount">the number of images referenced in that directory.</param>
        public ReferencedDirectory(int id, string fullPath, int imageCount) : base(id)
        {
            this.fullPath = fullPath;
            this.imageCount = imageCount;
        }

        /// <param name="fullPath">the full path to the directory.</param>
        public ReferencedDirectory(string fullPath) : this(-1, fullPath, 0)
        {
            
        }

        /// <summary>References (saves) this directory in the database.</summary>
        public void AppendToDB()
        {
            // Get a unique ID, 0 if the database is empty
            // The elements in the database are stored in a way the the IDs are
            // always sorted in an ascending order.
            id = 0;
            IEnumerable<ReferencedDirectory> allDirs = DB.GetReferencedDirectories();
            if(allDirs.Any()){
                id = allDirs.Last().Id + 1;
            }
            
            DB.AppendReferencedDirectories(new ReferencedDirectory[] {this});
        }

        /// <summary>Updates the directory data in the database. Only integer must be modified.</summary>
        public void UpdateInDB()
        {
            DB.UpdateReferencedDirectory(this);
        }

        /// <summary>Removes this directory from the database.</summary>
        public void RemoveFromDB()
        {
            DerefrenceContainedImages();
            DB.RemoveReferencedDirectories(new int[] {id});
            id = -1;
        }

        /// <summary>Lists all the valid picture present in that directory and references them in
        /// the database.</summary>
        public void ReferenceContainedImages()
        {
            string[] imageFiles = GetAllImageFiles();
            List<ReferencedImage> imgs = new List<ReferencedImage>();
            foreach(string imageFile in imageFiles){
                ReferencedImage img = new ReferencedImage(imageFile, id);
                if(img.IsValid)
                    imgs.Add(img);
            }

            containedImages = imgs.ToArray();
            imageCount = imgs.Count;

            ReferencedImage.AppendToDB(containedImages);
        }

        /// <summary>Removes all the referenced pictures for that directory from the
        /// database.</summary>
        public void DerefrenceContainedImages()
        {
            ReferencedImage.RemoveFromDB(id);
            imageCount = 0;
        }

        /// <summary>Returns the list of all images files contained in the directory.</summary>
        private string[] GetAllImageFiles()
        {
            string[] allFiles = Directory.GetFiles(fullPath);
            string[] extensions = new string[] {
                ".jpg", ".jpeg", ".png", ".gif", ".tiff", ".bmp"
            };
            string[] imageFiles = Array.FindAll(allFiles, file =>
                extensions.Contains(Path.GetExtension(file).ToLower())
            );
            return imageFiles;
        }

        /// <summary>Finds the referenced directory instance in the database with the given ID.
        /// Returns <c>null</c> if the directory is not found.</summary>
        public static ReferencedDirectory FindFromId(int searchId)
        {
            IEnumerable<ReferencedDirectory> allDirs = DB.GetReferencedDirectories();

            foreach(ReferencedDirectory dir in allDirs){
                if(dir.Id == searchId)
                    return dir;
            }

            return null;
        }

        /// <summary>Finds the referenced directory instance in the database with the given path.
        /// Returns <c>null</c> if the directory is not found.</summary>
        public static ReferencedDirectory FindFromPath(string searchPath)
        {
            IEnumerable<ReferencedDirectory> allDirs = DB.GetReferencedDirectories();

            foreach(ReferencedDirectory dir in allDirs){
                if(dir.FullPath == searchPath)
                    return dir;
            }

            return null;
        }
    }
}