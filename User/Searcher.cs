using System;
using System.Linq;
using System.Collections.Generic;

namespace Yumu
{
    class Searcher
    {
        private const int MIN_SEARCH_LENGTH = 3;
        private const int MAX_RESULTS = 20;

        private DBAccessor _accessor;
        public DBAccessor Accessor {get => _accessor;}

        private List<DBImage> _results;
        public List<DBImage> Results {get => _results;}

        private List<DBImage> _prevResults;
        private List<DBImage> _cache;

        private string _prevSearchString;

        public Searcher(DBAccessor accessor)
        {
            _accessor = accessor;

            _prevSearchString = null;

            _results = new List<DBImage>();
            _prevResults = new List<DBImage>();
            _cache = new List<DBImage>();
        }

        public void Search(string searchInput)
        { 
            string searchString = DBImage.SimplifyString(searchInput);

            _prevResults = new List<DBImage>(_results);

            if(_prevSearchString == searchString) return;
            if(searchString.Length < MIN_SEARCH_LENGTH) {
                ResetResults();
                return;
            }

            List<DBImage> found;

            bool searchInCache = _prevSearchString != null &&
                searchString.Length > _prevSearchString.Length &&
                searchString.Substring(0, _prevSearchString.Length) == _prevSearchString;

            if(searchInCache)
                found = Find(_cache, searchString);
            else
                found = Find(_accessor.Images, searchString);

            found = found.OrderBy(img => img.DisplayName).ToList();
            found = found.OrderByDescending(img => img.Usage).ToList();
            
            _cache = found;
            int numResults = Math.Min(found.Count, MAX_RESULTS);
            _results = found.GetRange(0, numResults);
            
            _prevSearchString = searchString;
        }

        private void ResetResults()
        {
            _prevSearchString = null;

            _results.Clear();
            _cache.Clear();
        }

        private static List<DBImage> Find(List<DBImage> images, string searchString)
        {
            List<DBImage> found = new List<DBImage>();
            foreach(DBImage img in images) {
                if(img.SearchName.Contains(searchString)) {
                    found.Add(img);
                }
            }
            return found;
        }

        public string GetImageFullPath(DBImage img)
        {
            DBDirectory dir = _accessor.GetReferencedDirectory(img.DirId);
            if(dir != null){
                return dir.FullPath + "\\" + img.FileName;
            }
            return null;
        }

        public bool AreNewResultsSame()
        {
            if(_results.Count != _prevResults.Count) {
                return false;
            }
            bool same = true;
            for(int i = 0; i < _results.Count && same; ++i){
                same = _results[i].Id == _prevResults[i].Id;
            }
            return same;
        }
    }
}