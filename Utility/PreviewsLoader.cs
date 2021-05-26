using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System;

namespace Yumu
{
    class CustomCancellationSource : CancellationTokenSource
    {
        public bool IsDisposed {get; private set;}

        public CustomCancellationSource() : base() {
            IsDisposed = false;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            IsDisposed = true;
        }
    }

    class PreviewsLoader
    {
        private const int FILE_SIZE_LIMIT = 250000; // bytes
        
        private Dictionary<int, Image> _loadedThumbnails;
        private CustomCancellationSource _tokenSource;
        private Mutex _mutex;

        private Searcher _searcher;

        public PreviewsLoader(Searcher searcher){
            _searcher = searcher;

            _loadedThumbnails = new Dictionary<int, Image>();
            _mutex = new Mutex();
        }

        public void StopLoadingPreviews()
        {
            if(_tokenSource != null && !_tokenSource.IsDisposed) {
                _tokenSource.Cancel();
            }
        }

        public async Task LoadImagePreviews(List<SearchResult> searchResults) {
            _tokenSource = new CustomCancellationSource();
            await Task.Run(() => LoadNewPreviews(searchResults, _tokenSource.Token));

            foreach(SearchResult searchResult in searchResults) {
                if(_tokenSource.Token.IsCancellationRequested){
                    break;
                }
                int imgId = searchResult.AttachedImage.Id;
                if(_loadedThumbnails.ContainsKey(imgId)) {
                    searchResult.AddImagePreview(_loadedThumbnails[imgId]);
                }
            }

            _tokenSource.Dispose();
        }

        private void LoadNewPreviews(List<SearchResult> searchResults, CancellationToken token)
        {
            foreach(SearchResult searchResult in searchResults){
                if(token.IsCancellationRequested){
                    return;
                }
                DBImage img = searchResult.AttachedImage;
                _mutex.WaitOne(); // Just precaution measures, not necessary
                if(!_loadedThumbnails.ContainsKey(img.Id)) {
                    Image thumb = CreateImagePreview(img);
                    if(thumb != null){
                        _loadedThumbnails.Add(img.Id, thumb);
                    }
                }
                _mutex.ReleaseMutex();
            }
        }

        private Image CreateImagePreview(DBImage image) {
            string imagePath = _searcher.GetImageFullPath(image);
            if(!File.Exists(imagePath))
                return null;

            FileInfo imgFile = new FileInfo(imagePath);
            if(imgFile.Length > FILE_SIZE_LIMIT)
                return null;
            
            Image img = Image.FromFile(imgFile.FullName);
            
            // Get image thumbnail that stays in the boundaries of a square
            // of size ROW_HEIGHT

            float ratio = (float)Math.Max(img.Width, img.Height) / (float)SearchResult.ROW_HEIGHT;

            int sizeX = Convert.ToInt32((float)img.Width / ratio);
            int sizeY = Convert.ToInt32((float)img.Height / ratio);

            Image.GetThumbnailImageAbort callback =
                new Image.GetThumbnailImageAbort(() => true);
            Image thumb = img.GetThumbnailImage(sizeX, sizeY, callback, IntPtr.Zero);

            return thumb;
        }
    }
}