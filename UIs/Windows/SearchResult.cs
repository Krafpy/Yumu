using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace Yumu
{
    class SearchResult : ListItem
    {
        private const int ROW_HEIGHT = 50;
        private const int SEPARATION = 10;
        private const int TITLE_OFFSET = 10;

        private const int FILE_SIZE_LIMIT = 250000; // bytes

        private const int TITLE_LENGTH_LIMIT = 20;

        public DBImage AttachedImage;
        private SearchWindow _searchWindow;
        private int _order;

        private PictureBox _preview;
        public bool HasPreview {get => _preview != null;}

        private bool _imageWasUsed = false;
        
        private bool _selected = false;
        public bool Selected {
            get => _selected;
            set {
                _selected = value;
                BackColor = _selected ? _hoverBackground : _defaultBackground;
            }
        }

        private string _imagePath;

        private Searcher _searcher;
        private DBAccessor _accessor;

        public SearchResult(SearchWindow searchWindow, DBImage attachedImage, int order) : base(searchWindow.ResultPanel)
        {
            _searchWindow = searchWindow;
            AttachedImage = attachedImage;
            _order = order;

            _searcher = _searchWindow.Searcher;
            _accessor = _searcher.Accessor;

            _imagePath = _searcher.GetImageFullPath(AttachedImage);

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

            AddHoverOnElement(titleLab);
            titleLab.MouseDown += OnMouseDown;
            titleLab.DoubleClick += OnDoubleClick;
            titleLab.Cursor = Cursors.Hand;

            Controls.Add(titleLab);
        }

        private void OnDoubleClick(object sender, EventArgs e)
        {
            CopyToClipboard();
            UpdateImageUsage();
            
            _searchWindow.Close();
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

        public Image LoadImagePreview()
        {
            if(!File.Exists(_imagePath))
                return null;

            FileInfo imgFile = new FileInfo(_imagePath);
            if(imgFile.Length > FILE_SIZE_LIMIT)
                return null;
            
            Image img = Image.FromFile(imgFile.FullName);
            
            // Get image thumbnail that stays in the boundaries of a square
            // of size ROW_HEIGHT

            float ratio = (float)Math.Max(img.Width, img.Height) / (float)ROW_HEIGHT;

            int sizeX = Convert.ToInt32((float)img.Width / ratio);
            int sizeY = Convert.ToInt32((float)img.Height / ratio);

            (int posX, int posY) pos = AdaptedImageLocation(img, sizeX, sizeY);
            int posX = pos.posX;
            int posY = pos.posY;

            Image.GetThumbnailImageAbort callback =
                new Image.GetThumbnailImageAbort(() => true);
            Image thumb = img.GetThumbnailImage(sizeX, sizeY, callback, IntPtr.Zero);
            
            CreatePreviewBox(thumb, posX, posY, sizeX, sizeY);

            return thumb;
        }

        public void AttachImagePreview(Image thumb)
        {
            int sizeX = thumb.Width;
            int sizeY = thumb.Height;
            
            (int posX, int posY) pos = AdaptedImageLocation(thumb, sizeX, sizeY);
            int posX = pos.posX;
            int posY = pos.posY;

            CreatePreviewBox(thumb, posX, posY, sizeX, sizeY);
        }

        private static (int, int) AdaptedImageLocation(Image img, int sizeX, int sizeY)
        {
            int posX = 0;
            int posY = 0;

            if(img.Width > img.Height)
                posY = (ROW_HEIGHT - sizeY) / 2;
            else if(img.Width < img.Height)
                posX = (ROW_HEIGHT - sizeX) / 2;

            return (posX, posY);
        }

        private void CreatePreviewBox(Image thumb, int posX, int posY, int sizeX, int sizeY)
        {
            _preview = new PictureBox(){
                Image = thumb,
                Location = new Point(posX, posY),
                Size = new Size(sizeX, sizeY)
            };

            AddHoverOnElement(_preview);
            _preview.MouseDown += OnMouseDown;
            _preview.DoubleClick += OnDoubleClick;
            _preview.Cursor = Cursors.Hand;
        }

        public void AddImagePreview()
        {
            if(_preview != null)
                Controls.Add(_preview);
        }

        public void CopyToClipboard()
        {
            if(File.Exists(_imagePath))
                ImageUtils.CopyToClipboard(_imagePath);
        }

        public void UpdateImageUsage()
        {
            if(!_imageWasUsed){
                _imageWasUsed = true;
                _accessor.UpdateReferencedImage(AttachedImage.Id);
            }
        }
    }
}