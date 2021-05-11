using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace Yumu
{
    class Searcher
    {
        private DBAccessor _accessor;
        public DBAccessor Accessor {get => _accessor;}
        
        private ReferencedImage[] _results;
        public ReferencedImage[] Results {get => _results;}

        private ReferencedImage[] _prevResults;
        private ReferencedImage[] _cache;

        private string _prevSearchString;

        private const int MIN_SEARCH_LENGTH = 3;
        private const int MAX_RESULTS = 20;

        public Searcher(DBAccessor accessor)
        {
            _accessor = accessor;
            
            _prevSearchString = null;

            _results = new ReferencedImage[0];
            _prevResults = new ReferencedImage[0];
            _cache = new ReferencedImage[0];
        }

        public bool Search(string searchInput)
        {
            string searchString = ReferencedImage.SimplifyString(searchInput);
            bool same = true;

            if(_prevSearchString == searchString) {
                return true;
            }
            if(searchString.Length < MIN_SEARCH_LENGTH) {
                same = _results.Length == 0;

                _prevSearchString = null;
                
                _results = new ReferencedImage[0];
                _prevResults = new ReferencedImage[0];
                _cache = new ReferencedImage[0];
                
                return same;
            }

            List<ReferencedImage> found;

            bool searchInCache = _prevSearchString != null &&
                searchString.Length > _prevSearchString.Length &&
                searchString.Substring(0, _prevSearchString.Length) == _prevSearchString;

            if(searchInCache) {
                found = Find(_cache, searchString);
            } else {
                found = Find(_accessor.Images.ToArray(), searchString);
            }

            found = found.OrderBy(img => img.DisplayName).ToList();
            found = found.OrderByDescending(img => img.Usage).ToList();
            _cache = found.ToArray();
            
            int numResults = Math.Min(found.Count, MAX_RESULTS);
            _results = found.GetRange(0, numResults).ToArray();
            
            _prevSearchString = searchString;
            same = AreNewResultsSame();

            _prevResults = new ReferencedImage[_results.Length];
            Array.Copy(_results, _prevResults, _results.Length);

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
            ReferencedDirectory dir = _accessor.GetReferencedDirectory(img.DirId);
            if(dir != null){
                return dir.FullPath + "\\" + img.FileName;
            }
            return null;
        }

        private bool AreNewResultsSame()
        {
            if(_results.Length != _prevResults.Length) {
                return false;
            }
            bool same = true;
            for(int i = 0; i < _results.Length && same; ++i){
                same = _results[i].Id == _prevResults[i].Id;
            }
            return same;
        }
    }
}