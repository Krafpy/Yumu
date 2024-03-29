using System;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Threading;

namespace Yumu
{
    class SearchWindow : Window
    {
        private const int X_MARGIN = 10;
        private const int Y_MARGIN = 10;

        private List<SearchResult> _searchResults;
        
        private TextBox _searchBar;
        private Panel _resultPanel;

        public TextBox SearchBar {get => _searchBar;}
        public Panel ResultPanel {get => _resultPanel;}

        private int _selectedIndex;
        
        public int SelectedIndex {
            get => _selectedIndex;
            set => SelectResult(value);
        }

        private bool HasResults {
            get => _searchResults != null && _searchResults.Count > 0;
        }

        private DBAccessor _accessor;
        private Searcher _searcher;
        public Searcher Searcher {get => _searcher;}

        private PreviewsLoader _previewsLoader;
        private SemaphoreSlim _semaphore;

        public SearchWindow() : base("Yumu search", 300, 400)
        {
            _searchResults = new List<SearchResult>();
            
            _accessor = new DBAccessor();
            _searcher = new Searcher(_accessor);

            _previewsLoader = new PreviewsLoader(_searcher);
            _semaphore = new SemaphoreSlim(1, 1);

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

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _previewsLoader.StopLoadingPreviews();
        }

        protected override void OnKeyDown(object sender, KeyEventArgs e)
        {
            base.OnKeyDown(sender, e);
            
            switch(e.KeyCode)
            {
                case Keys.Up:
                    e.Handled = true;
                    SelectedIndex--;
                    break;

                case Keys.Down:
                    e.Handled = true;
                    SelectedIndex++;
                    break;
                
                case Keys.Enter:
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    SelectionToClipboard();
                    break;
                
                case Keys.Back:
                    if(e.Control)
                        SendKeys.Send("^+{LEFT}{BACKSPACE}");
                    break;
            }
        }

        private void SelectionToClipboard()
        {
            if(HasResults)
                _searchResults[_selectedIndex].CopyToClipboard();
        }

        private void SelectResult(int newIndex)
        {
            if(HasResults && newIndex >= 0 && newIndex < _searchResults.Count){
                if(_selectedIndex < _searchResults.Count)
                    _searchResults[_selectedIndex].Selected = false;
                
                _selectedIndex = newIndex;
                
                SearchResult selection = _searchResults[_selectedIndex];
                selection.Selected = true;
                _resultPanel.ScrollControlIntoView(selection);
            }
        }

        private void ClearSearchResults()
        {
            if(HasResults){
                _resultPanel.Controls.Clear();
                foreach(SearchResult item in _searchResults){
                    item.Dispose();
                }
               _searchResults.Clear();
            }
        }

        private void BuildSearchResults()
        {
            List<DBImage> results = _searcher.Results;
            for(int i = 0; i < results.Count; i++){
                _searchResults.Add(new SearchResult(this, results[i], i));
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
                BuildNewResults();
            }
        }

        private async void BuildNewResults()
        {
            // Avoid collisions and iterating through modified
            // lists when OnTextChange is called twice
            // with a small time interval, which causes crash.
            
            _previewsLoader.StopLoadingPreviews();

            await _semaphore.WaitAsync();

            ClearSearchResults();
            BuildSearchResults();
            SelectResult(0);

            await _previewsLoader.LoadImagePreviews(_searchResults);

            _semaphore.Release();
        }
    }
}