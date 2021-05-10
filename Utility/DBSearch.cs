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
        
        private ReferencedImage[] results;
        public ReferencedImage[] Results {get => results;}

        private ReferencedImage[] prevResults;
        private ReferencedImage[] cache;

        private string prevSearchString;

        private const int MIN_SEARCH_LENGTH = 3;
        private const int MAX_RESULTS = 20;

        public DBSearch(DBAccessor dbAccessor)
        {
            _dbAccessor = dbAccessor;
            
            prevSearchString = null;

            results = new ReferencedImage[0];
            prevResults = new ReferencedImage[0];
            cache = new ReferencedImage[0];
        }

        public bool Search(string searchInput)
        {
            string searchString = ReferencedImage.SimplifyString(searchInput);
            bool same = true;

            if(prevSearchString == searchString) {
                return true;
            }
            if(searchString.Length < MIN_SEARCH_LENGTH) {
                same = results.Length == 0;

                prevSearchString = null;
                
                results = new ReferencedImage[0];
                prevResults = new ReferencedImage[0];
                cache = new ReferencedImage[0];
                
                return same;
            }

            List<ReferencedImage> found;

            bool searchInCache = prevSearchString != null &&
                searchString.Length > prevSearchString.Length &&
                searchString.Substring(0, prevSearchString.Length) == prevSearchString;

            if(searchInCache) {
                found = Find(cache, searchString);
            } else {
                found = Find(_dbAccessor.Images.ToArray(), searchString);
            }

            found = found.OrderBy(img => img.DisplayName).ToList();
            found = found.OrderByDescending(img => img.Usage).ToList();
            cache = found.ToArray();
            
            int numResults = Math.Min(found.Count, MAX_RESULTS);
            results = found.GetRange(0, numResults).ToArray();
            
            prevSearchString = searchString;
            same = AreNewResultsSame();

            prevResults = new ReferencedImage[results.Length];
            Array.Copy(results, prevResults, results.Length);

            return same;
        }

        private static List<ReferencedImage> Find(ReferencedImage[] images, string searchString)
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

        private bool AreNewResultsSame()
        {
            if(results.Length != prevResults.Length) {
                return false;
            }
            bool same = true;
            for(int i = 0; i < results.Length && same; ++i){
                same = results[i].Id == prevResults[i].Id;
            }
            return same;
        }
    }
}