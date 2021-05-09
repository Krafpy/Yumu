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

        SearchResult[] searchResults;
        
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

        private DBAccessor dbAccessor;
        public DBSearch dbSearch;

        private Dictionary<int, Image> loadedThumbnails;

        public SearchWindow() : base("Yumu search", 300, 400)
        {
            dbAccessor = new DBAccessor();
            dbSearch = new DBSearch(dbAccessor);

            loadedThumbnails = new Dictionary<int, Image>();

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
                    selection.UpdateImageUsage();
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
                    resultPanel.Controls.Remove(item); // item.Dispose();
                }
            }
        }

        private void BuildSearchResults()
        {
            ReferencedImage[] results = dbSearch.searchCache;
            if(results == null || results.Length == 0)
                return;
            
            searchResults = new SearchResult[results.Length];
            for(int i = 0; i < searchResults.Length; i++){
                searchResults[i] = new SearchResult(this, results[i], i);
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

            dbSearch.Search(searchBar.Text);

            ClearSearchResults();
            BuildSearchResults();

            SelectResult(0);

            StartLoadingPreviews();
        }

        private async void StartLoadingPreviews()
        {
            if(!HasResults) return;

            if(tokenSource == null || tokenSource.IsCancellationRequested) {
                tokenSource = new CancellationTokenSource();
                await Task.Run(() => LoadImagePreviews(tokenSource.Token));
            } else {
                while(!tokenSource.IsCancellationRequested) { }
                await Task.Run(() => LoadImagePreviews(tokenSource.Token));
            }

            foreach(SearchResult item in searchResults){
                item.AddImagePreview();
            }
        }

        private void StopLoadingPreviews()
        {
            if(tokenSource != null) {
                tokenSource.Cancel();
            }
        }

        private void LoadImagePreviews(CancellationToken token)
        {
            foreach(SearchResult result in searchResults){
                if(token.IsCancellationRequested){
                    return;
                }
                
                int imgId = result.attachedImage.Id;
                if(loadedThumbnails.ContainsKey(imgId)) {
                    result.AttachImagePreview(loadedThumbnails[imgId]);
                } else {
                    Image thumb = result.LoadImagePreview();
                    loadedThumbnails.Add(imgId, thumb);
                }
            }
        }
    }
}