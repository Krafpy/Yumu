using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace Yumu
{
    /// <summary>Class that represents an image file in one of the referenced directories,
    /// i.e. an image that might be a search result.</summary>
    class ReferencedImage : DBItem
    {
        private string fileName;
        private string fullPath;
        private string simplifiedName;
        private int dirId;
        private int usage;
        
        public string FileName {get => fileName;}
        public int DirId {get => dirId;}

        /// <summary>How many times this picture has been used.</summary>
        public int Usage {
            get => usage;
            set => usage = value;
        }

        /// <summary>A lowercase version of the file name (without the extension) containing
        /// containing only letters and digits. Used for search in the database.</summary>
        public string SimplifiedName {
            get {
                if(simplifiedName != null)
                    return simplifiedName;
                
                string fnameNoExts = Path.GetFileNameWithoutExtension(fileName);
                simplifiedName = ToSearchString(fnameNoExts);
                
                return simplifiedName;
            }
        }
        
        /// <summary>The full path to that image.</summary>
        public string FullPath {
            get {
                if(fullPath != null)
                    return fullPath;

                ReferencedDirectory dir = ReferencedDirectory.FindFromId(dirId);
                if(dir == null)
                    return null;
                fullPath = $@"{dir.FullPath}\{fileName}";
                return fullPath;
            }
        }

        /// <summary>A filtered version of the filename (without extension) which is displayed
        /// on the search window.</summary>
        public string DisplayTitle {
            get {
                string fnameNoExts = Path.GetFileNameWithoutExtension(fileName);
                return Regex.Replace(fnameNoExts, "[^a-zA-Z0-9]+", " ");
            }
        }

        /// <summary><c>true</c> if the length of of the simplified name isn't 0.</summary>
        public bool IsValid {
            get => SimplifiedName.Length > 0;
        }
        
        /// <param name="id">the ID that uniquely indentifies the image in the database,
        /// if not refrenced it must be set to -1.</param>
        /// <param name="dirId">the ID of the referenced directory in which this image is stored.</param>
        /// <param name="usage">how many times this image have been used.</param>
        /// <param name="fileName">the file name only (with extension) to the image.</param>
        /// <param name="simplifiedName">the simplified version of the file name.</param>
        public ReferencedImage(int id, int dirId, int usage, string fileName, string simplifiedName) : base(id)
        {
            this.dirId = dirId;
            this.fileName = fileName;
            this.simplifiedName = simplifiedName;
            this.usage = usage;
        }

        /// <param name="dirId">the ID of the referenced directory in which this image is stored.</param>
        /// <param name="fileName">the file name only (with extension) to the image.</param>
        public ReferencedImage(string fullPath, int dirId) : this(-1, dirId, 0, Path.GetFileName(fullPath), null)
        {
            
        }
        
        /// <summary>Appends an array of images to the database.</summary>
        /// <param name="imgs">the array of images to reference.</param>
        public static void AppendToDB(ReferencedImage[] imgs)
        {
            int startId = 0;

            IEnumerable<ReferencedImage> allImgs = DB.GetReferencedImages();
            if(allImgs.Any()){
                startId = allImgs.Last().Id + 1;
            }

            for(int i = 0; i < imgs.Length; i++){
                imgs[i].Id = startId + i;
            }

            DB.AppendReferencedImages(imgs);
        }

        /// <summary>Removes all the images belonging to a referenced directory identified
        /// with the given ID.</summary>
        /// <param name="dirId">the ID of the referenced directory.</param>
        public static void RemoveFromDB(int dirId)
        {
            List<int> imgIds = new List<int>(); 
            IEnumerable<ReferencedImage> allImgs = DB.GetReferencedImages();
            foreach(ReferencedImage img in allImgs){
                if(img.DirId == dirId)
                    imgIds.Add(img.Id);
            }
            DB.RemoveReferencedImages(imgIds.ToArray());
        }

        /// <summary>Updates the image data in the database. Only the integer properties
        /// must be modified.</summary>
        public void UpdateInDB()
        {
            DB.UpdateReferencedImage(this);
        }

        /// <summary>Loads the image in the clipboard.</summary>
        public void CopyToClipboard()
        {
            // TODO: Doesn't work for GIFs, only the first frame is loaded in the clipboard,
            // and PNG images (as well as GIFs) loose their transparency.

            Clipboard.SetImage(Image.FromFile(FullPath));
        }

        /// <summary>Searches in the database all the referenced images with a simplified name
        /// that contains the given substring.<summary>
        /// <param name="subString">the substring that the simplified name of the found
        /// images must contain.</param>
        public static ReferencedImage[] Find(string subString)
        {
            List<ReferencedImage> found = new List<ReferencedImage>();
            IEnumerable<ReferencedImage> allImgs = DB.GetReferencedImages();
            foreach(ReferencedImage img in allImgs){
                if(img.SimplifiedName.Contains(subString))
                    found.Add(img);
            }

            return found.ToArray();
        }
        
        /// <summary>Converts a string to its simplified version which can be used for
        /// researching images in the database.</summary>
        /// <param name="s">the string to convert.</param>
        public static string ToSearchString(string s)
        {
            return Regex.Replace(s.ToLower(), "[^a-z0-9]+", String.Empty);
        }
    }
}