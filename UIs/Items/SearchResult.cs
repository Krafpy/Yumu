using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Yumu
{
    class SearchResult : ListItem
    {
        public const int ROW_HEIGHT = 50;
        private const int SEPARATION = 10;
        private const int TITLE_OFFSET = 10;
        private const int TITLE_LENGTH_LIMIT = 20;
        private const int INDICATOR_OFFSET = 22;
        
        private SearchWindow _searchWindow;
        public DBImage AttachedImage;

        private PictureBox _preview;
        public bool HasPreview {
            get => _preview != null;
        }

        private string _imagePath;
        private DBAccessor _accessor;
        
        private bool _imageWasUsed = false;

        private bool _selected = false;
        public bool Selected {
            get => _selected;
            set {
                _selected = value;
                if(_selected)
                    ShowHover();
                else
                    HideHover();
            }
        }

        public SearchResult(SearchWindow searchWindow, DBImage attachedImage, int order) 
        : base(searchWindow.ResultPanel, order)
        {
            _searchWindow = searchWindow;
            AttachedImage = attachedImage;

            Searcher searcher = _searchWindow.Searcher;
            _accessor = searcher.Accessor;

            _imagePath = searcher.GetImageFullPath(AttachedImage);

            InitializeComponents();
        }

        private void InitializeComponents()
        {    
            Size = new Size(_parent.Width, ROW_HEIGHT);
            Location = new Point(0, _order * (ROW_HEIGHT + SEPARATION));
            
            MouseDown += OnMouseDown;
            DoubleClick += OnDoubleClick;
            Cursor = Cursors.Hand;

            // Label containing the image title
            string title;
            if(AttachedImage.DisplayName.Length > TITLE_LENGTH_LIMIT)
                title = AttachedImage.DisplayName.Substring(0, TITLE_LENGTH_LIMIT) + "...";
            else
                title = AttachedImage.DisplayName;
            Label titleLab = new Label(){
                Text = title,
                Font = new Font(Window.FONT_NAME, 10, FontStyle.Bold),
                AutoSize = true,
            };
            titleLab.Location = new Point(ROW_HEIGHT + TITLE_OFFSET, (ROW_HEIGHT - titleLab.Height) / 2);
            Controls.Add(titleLab);

            AddHoverOnElement(titleLab);
            titleLab.MouseDown += OnMouseDown;
            titleLab.DoubleClick += OnDoubleClick;
            titleLab.Cursor = Cursors.Hand;

            string ext = Path.GetExtension(AttachedImage.FileName);
            if(ext.ToLower() == ".gif"){
                Label indicLab = new Label(){
                    Text = "GIF",
                    Font = new Font(Window.FONT_NAME, 10, FontStyle.Bold),
                    AutoSize = false,
                    ForeColor = Color.Gray,
                };
                indicLab.Location = new Point(Width - indicLab.PreferredWidth - INDICATOR_OFFSET, (ROW_HEIGHT - indicLab.Height) / 2);
                Controls.Add(indicLab);

                AddHoverOnElement(indicLab);
                indicLab.MouseDown += OnMouseDown;
                indicLab.DoubleClick += OnDoubleClick;
                indicLab.Cursor = Cursors.Hand;
            }
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            CopyToClipboard();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1 && File.Exists(_imagePath))
            {
                DataObject data = new DataObject();
                string[] file = {_imagePath};
                data.SetData(DataFormats.FileDrop, file);
                DragDropEffects effect = DoDragDrop(data, DragDropEffects.Copy);
                if(effect != DragDropEffects.None){ // We consider that the user used the image if he dropped it
                    UpdateImageUsage();
                }
            }
        }

        protected override void OnEnter(object sender, EventArgs e)
        {
            base.OnEnter(sender, e);
            _searchWindow.SelectedIndex = _order;
        }

        protected override void OnLeave(object sender, EventArgs e)
        {
            base.OnLeave(sender, e);
            _searchWindow.SelectedIndex = _order;
        }

        public void AddImagePreview(Image thumb)
        {
            int posX = 0;
            int posY = 0;
            if(thumb.Width > thumb.Height)
                posY = (ROW_HEIGHT - thumb.Height) / 2;
            else if(thumb.Width < thumb.Height)
                posX = (ROW_HEIGHT - thumb.Width) / 2;
            
            _preview = new PictureBox(){
                Image = thumb,
                Location = new Point(posX, posY),
                Size = new Size(thumb.Width, thumb.Height)
            };

            AddHoverOnElement(_preview);
            _preview.MouseDown += OnMouseDown;
            _preview.DoubleClick += OnDoubleClick;
            _preview.Cursor = Cursors.Hand;
            
            Controls.Add(_preview);
        }

        public void CopyToClipboard()
        {
            ImageUtils.CopyToClipboard(_imagePath);
            UpdateImageUsage();
            _searchWindow.Close();
        }

        private void UpdateImageUsage()
        {
            if(!_imageWasUsed){
                _imageWasUsed = true;
                _accessor.UpdateReferencedImage(AttachedImage.Id);
            }
        }
    }
}