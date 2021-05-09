using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Yumu
{
    class DBSearch
    {
        private DBAccessor _dbAccessor;
        public DBAccessor Accessor {get => _dbAccessor;}
        
        public ReferencedImage[] searchCache;
        public ReferencedImage[] results;

        private string prevSearchString;

        private const int MIN_SEARCH_LENGTH = 3;
        private const int MAX_RESULTS = 20;

        public DBSearch(DBAccessor dbAccessor)
        {
            _dbAccessor = dbAccessor;
            prevSearchString = null;
        }

        public bool Search(string searchInput)
        {
            string searchString = ReferencedImage.SimplifyString(searchInput);

            if(prevSearchString == searchString) {
                return false;
            }
            if(searchString.Length < MIN_SEARCH_LENGTH) {
                searchCache = null;
                prevSearchString = null;
                results = null;
                return true;
            }

            List<ReferencedImage> found;

            bool searchInCache = prevSearchString != null &&
                searchCache != null &&
                searchString.Length > prevSearchString.Length &&
                searchString.Substring(0, prevSearchString.Length) == prevSearchString;

            if(searchInCache) {
                found = Find(searchCache, searchString);
            } else {
                found = Find(_dbAccessor.Images.ToArray(), searchString);
            }

            found = found.OrderBy(img => img.DisplayName).ToList();
            found = found.OrderByDescending(img => img.Usage).ToList();
            int numResults = Math.Min(found.Count, MAX_RESULTS);
            ReferencedImage[] newResults = found.GetRange(0, numResults).ToArray();
            
            bool same = false;
            if(results != null && newResults.Length == results.Length){
                same = true;
                for(int i = 0; i < results.Length && same; ++i){
                    same = newResults[i].Id == results[i].Id;
                }
            }

            results = newResults;
            
            searchCache = found.ToArray();
            prevSearchString = searchString;

            return !same;
        }

        private List<ReferencedImage> Find(ReferencedImage[] images, string searchString)
        {
            List<ReferencedImage> found = new List<ReferencedImage>();
            foreach(ReferencedImage img in images) {
                if(img.SearchName.Contains(searchString)) {
                    found.Add(img);
                }
            }
            return found;
        }

        public string GetImageFullPath(ReferencedImage img)
        {
            ReferencedDirectory dir = _dbAccessor.GetReferencedDirectory(img.DirId);
            if(dir != null){
                return dir.FullPath + "\\" + img.FileName;
            }
            return null;
        }
    }
}