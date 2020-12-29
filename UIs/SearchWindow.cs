using System;
using System.Windows.Forms;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Yumu
{
    class SearchWindow : Window
    {
        private const int X_MARGIN = 10;
        private const int Y_MARGIN = 10;

        private const int MIN_SEARCH_LENGTH = 3;
        private const int MAX_RESULTS = 20;

        ReferencedImage[] searchCache;
        SearchResult[] searchResults;

        private string previousSearchString;
        
        private TextBox searchBar;
        private Panel resultPanel;

        private CancellationTokenSource tokenSource;

        public TextBox SearchBar {get => searchBar;}
        public Panel ResultPanel {get => resultPanel;}

        private int selectedIndex;
        
        public int SelectedIndex {
            get => selectedIndex;
            set => SelectResult(value);
        }

        private bool HasResults {
            get => searchResults != null && searchResults.Length > 0;
        }

        public SearchWindow() : base("Yumu search", 300, 400)
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Activated += OnActivate;

            searchBar = new TextBox(){
                Location = new Point(X_MARGIN, Y_MARGIN),
                Size = new Size(ClientSize.Width - X_MARGIN * 2, ClientSize.Height - Y_MARGIN * 2),
                Font = new Font(FONT_NAME, 12, FontStyle.Bold),
            };
            searchBar.TextChanged += OnTextChange;
            Controls.Add(searchBar);

            ActiveControl = searchBar;
            
            previousSearchString = searchBar.Text;

            int yPos = Y_MARGIN * 2 + searchBar.PreferredHeight;
            int ySize = ClientRectangle.Height - yPos;
            resultPanel = new Panel(){
                Location = new Point(X_MARGIN, yPos),
                Size = new Size(ClientSize.Width - X_MARGIN * 2, ySize),
                AutoScroll = true
            };
            Controls.Add(resultPanel);
        }

        private void OnActivate(object sender, EventArgs e)
        {
            if(searchBar.Text.Length > 0){
                searchBar.SelectionStart = 0;
                searchBar.SelectionLength = searchBar.Text.Length;
            }
        }

        protected override void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(sender, e);

            switch(e.KeyCode)
            {
                case Keys.Up:
                    e.Handled = true;
                    SelectResult(selectedIndex - 1);
                    break;

                case Keys.Down:
                    e.Handled = true;
                    SelectResult(selectedIndex + 1);
                    break;
                
                case Keys.Enter:
                    SearchResult selection = searchResults[selectedIndex];
                    selection.CopyToClipboard();
                    selection.IncrementImageUsage();
                    Close();
                    break;

                case Keys.Back:
                    if(e.Control)
                        SendKeys.Send("^+{LEFT}{BACKSPACE}");
                    break;
            }
        }

        private void SelectResult(int newIndex)
        {
            if(!HasResults) return;
            
            if(selectedIndex >= 0 && newIndex < searchResults.Length){
                if(selectedIndex < searchResults.Length)
                    searchResults[selectedIndex].Selected = false;
                
                selectedIndex = newIndex;

                if(selectedIndex < 0)
                    selectedIndex = 0;
                else if(selectedIndex >= searchResults.Length)
                    selectedIndex = searchResults.Length - 1;
                
                SearchResult selection = searchResults[selectedIndex];
                selection.Selected = true;
                resultPanel.ScrollControlIntoView(selection);
            }
        }

        private void ClearSearchResults()
        {
            if(HasResults){
                foreach(SearchResult item in searchResults){
                    resultPanel.Controls.Remove(item);//item.Dispose();
                }
            }
        }

        private void BuildSearchResults()
        {
            if(searchCache == null || searchCache.Length == 0)
                return;
            
            // Sort images according to their usage
            Array.Sort(searchCache, (ReferencedImage imgA, ReferencedImage imgB) => {
                if(imgA.Usage > imgB.Usage)
                    return -1;
                else if(imgA.Usage < imgB.Usage)
                    return 1;
                return 0;
            });

            int numResults = Math.Min(searchCache.Length, MAX_RESULTS);
            searchResults = new SearchResult[numResults];
            for(int i = 0; i < numResults; i++){
                searchResults[i] = new SearchResult(this, searchCache[i], i);
            }

            if(resultPanel.VerticalScroll.Visible){
                foreach(SearchResult item in searchResults){
                    item.Width -= SystemInformation.VerticalScrollBarWidth;
                }
            }
        }

        private void OnTextChange(object sender, EventArgs e)
        {
            StopLoadingPreviews();

            SearchReferencedImages();
            previousSearchString = searchBar.Text;

            ClearSearchResults();
            BuildSearchResults();
            StartLoadingPreviews();

            SelectResult(0);
        }

        private void SearchReferencedImages()
        {
            string searchString = ReferencedImage.ToSearchString(searchBar.Text);
            if(searchString.Length < MIN_SEARCH_LENGTH){
                searchCache = null;
                return;
            } else if(searchString == previousSearchString){
                return;
            }

            // Do not search in the database but rather in the cached
            // images if the search string only gained new characters
            if(searchString.Length > previousSearchString.Length
                && searchString.StartsWith(previousSearchString)
                && searchCache != null)
            {
                searchCache = SearchInCache(searchString);
            } else {
                searchCache = ReferencedImage.Find(searchString);
            }
        }

        private ReferencedImage[] SearchInCache(string searchString)
        {
            List<ReferencedImage> foundImages = new List<ReferencedImage>();
            foreach(ReferencedImage cachedImage in searchCache){
                if(cachedImage.SimplifiedName.Contains(searchString)){
                    foundImages.Add(cachedImage);
                }
            }
            return foundImages.ToArray();
        }

        private async void StartLoadingPreviews()
        {
            if(!HasResults) return;
            
            tokenSource = new CancellationTokenSource();
            await Task.Run(() => LoadImagePreviews(tokenSource.Token));

            foreach(SearchResult item in searchResults){
                item.AddImagePreview();
            }
        }

        private void StopLoadingPreviews()
        {
            if(tokenSource != null)
                tokenSource.Cancel();
        }

        private void LoadImagePreviews(CancellationToken token)
        {
            foreach(SearchResult item in searchResults){
                if(token.IsCancellationRequested){
                    return;
                }
                item.LoadImagePreview();
            }
        }
    }
}