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

        private SearchResult[] _searchResults;
        
        private TextBox _searchBar;
        private Panel _resultPanel;

        public TextBox SearchBar {get => _searchBar;}
        public Panel ResultPanel {get => _resultPanel;}

        private CancellationTokenSource _tokenSource;

        private int _selectedIndex;
        
        public int SelectedIndex {
            get => _selectedIndex;
            set => SelectResult(value);
        }

        private bool hasResults {
            get => _searchResults != null && _searchResults.Length > 0;
        }

        private DBAccessor _accessor;
        private Searcher _searcher;
        public Searcher Searcher {get => _searcher;}

        private Dictionary<int, Image> _loadedThumbnails;

        public SearchWindow() : base("Yumu search", 300, 400)
        {
            _accessor = new DBAccessor();
            _searcher = new Searcher(_accessor);

            _loadedThumbnails = new Dictionary<int, Image>();
            _searchResults = new SearchResult[0];

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Activated += OnActivate;

            _searchBar = new TextBox(){
                Location = new Point(X_MARGIN, Y_MARGIN),
                Size = new Size(ClientSize.Width - X_MARGIN * 2, ClientSize.Height - Y_MARGIN * 2),
                Font = new Font(FONT_NAME, 12, FontStyle.Bold),
            };
            _searchBar.TextChanged += OnTextChange;
            Controls.Add(_searchBar);

            ActiveControl = _searchBar;

            int yPos = Y_MARGIN * 2 + _searchBar.PreferredHeight;
            int ySize = ClientRectangle.Height - yPos;
            _resultPanel = new Panel(){
                Location = new Point(X_MARGIN, yPos),
                Size = new Size(ClientSize.Width - X_MARGIN * 2, ySize),
                AutoScroll = true
            };
            Controls.Add(_resultPanel);
        }

        private void OnActivate(object sender, EventArgs e)
        {
            if(_searchBar.Text.Length > 0){
                _searchBar.SelectionStart = 0;
                _searchBar.SelectionLength = _searchBar.Text.Length;
            }
        }

        protected override void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(sender, e);

            switch(e.KeyCode)
            {
                case Keys.Up:
                    e.Handled = true;
                    SelectResult(_selectedIndex - 1);
                    break;

                case Keys.Down:
                    e.Handled = true;
                    SelectResult(_selectedIndex + 1);
                    break;
                
                case Keys.Enter:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if(!hasResults)
                        break;
                    SearchResult selection = _searchResults[_selectedIndex];
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
            if(!hasResults) return;
            
            if(newIndex >= 0 && newIndex < _searchResults.Length){
                if(_selectedIndex < _searchResults.Length)
                    _searchResults[_selectedIndex].Selected = false;
                
                _selectedIndex = newIndex;
                
                SearchResult selection = _searchResults[_selectedIndex];
                selection.Selected = true;
                _resultPanel.ScrollControlIntoView(selection);
            }
        }

        private void ClearSearchResults()
        {
            if(hasResults){
                foreach(SearchResult item in _searchResults){
                    _resultPanel.Controls.Remove(item); // item.Dispose();
                }
            }
        }

        private void BuildSearchResults()
        {
            List<DBImage> results = _searcher.Results;
            
            _searchResults = new SearchResult[results.Count];
            for(int i = 0; i < _searchResults.Length; i++){
                _searchResults[i] = new SearchResult(this, results[i], i);
            }

            if(_resultPanel.VerticalScroll.Visible){
                foreach(SearchResult item in _searchResults){
                    item.Width -= SystemInformation.VerticalScrollBarWidth;
                }
            }
        }

        private void OnTextChange(object sender, EventArgs e)
        {
            _searcher.Search(_searchBar.Text);

            if(!_searcher.AreNewResultsSame()) {
                StopLoadingPreviews();
                ClearSearchResults();
                BuildSearchResults();
                SelectResult(0);
                StartLoadingPreviews();
            }
        }

        private async void StartLoadingPreviews()
        {
            if(!hasResults) return;

            if(_tokenSource != null) {
                while(!_tokenSource.IsCancellationRequested) { }
            }
            _tokenSource = new CancellationTokenSource();
            await Task.Run(() => LoadImagePreviews(_tokenSource.Token));

            foreach(SearchResult item in _searchResults){
                item.AddImagePreview();
            }
        }

        private void StopLoadingPreviews()
        {
            if(_tokenSource != null) {
                _tokenSource.Cancel();
            }
        }

        private void LoadImagePreviews(CancellationToken token)
        {
            foreach(SearchResult result in _searchResults){
                if(token.IsCancellationRequested){
                    return;
                }
                
                int imgId = result.AttachedImage.Id;
                if(_loadedThumbnails.ContainsKey(imgId)) {
                    result.AttachImagePreview(_loadedThumbnails[imgId]);
                } else {
                    Image thumb = result.LoadImagePreview();
                    _loadedThumbnails.Add(imgId, thumb);
                }
            }
        }
    }
}