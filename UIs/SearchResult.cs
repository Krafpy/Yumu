using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading.Tasks;

namespace Yumu
{
    class SearchResult : ListItem
    {
        private const int ROW_HEIGHT = 50;
        private const int SEPARATION = 10;
        private const int TITLE_OFFSET = 10;

        private const int FILE_SIZE_LIMIT = 250000; // bytes

        private const int TITLE_LENGTH_LIMIT = 20;

        private ReferencedImage attachedImage;
        private SearchWindow searchWindow;
        private int order;

        private PictureBox preview;

        private bool selected = false;
        private bool imageWasUsed = false;
        
        public bool Selected {
            get => selected;
            set {
                selected = value;
                if(selected)
                    BackColor = hoverBackground;
                else
                    BackColor = defaultBackground;
            }
        }

        public SearchResult(SearchWindow searchWindow, ReferencedImage attachedImage, int order) : base(searchWindow.ResultPanel)
        {
            this.searchWindow = searchWindow;
            this.attachedImage = attachedImage;
            this.order = order;

            InitializeComponents();
        }

        private void InitializeComponents()
        {    
            Size = new Size(parent.Width, ROW_HEIGHT);
            Location = new Point(0, order * (ROW_HEIGHT + SEPARATION));
            
            MouseDown += OnMouseDown;
            DoubleClick += OnDoubleClick;
            Cursor = Cursors.Hand;

            // Label containing the image title
            string title;
            if(attachedImage.DisplayTitle.Length > TITLE_LENGTH_LIMIT)
                title = attachedImage.DisplayTitle.Substring(0, TITLE_LENGTH_LIMIT) + "...";
            else
                title = attachedImage.DisplayTitle;
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
            IncrementImageUsage();
            searchWindow.Close();
        }

        private void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)
            {
                DataObject data = new DataObject();
                string[] file = {attachedImage.FullPath};
                data.SetData(DataFormats.FileDrop, file);
                DragDropEffects effect = DoDragDrop(data, DragDropEffects.Copy);
                if(effect != DragDropEffects.None){ // We consider that the user used the image if he dropped it
                    IncrementImageUsage();
                }
            }
        }

        protected override void OnEnter(object sender, EventArgs e)
        {
            base.OnEnter(sender, e);
            searchWindow.SelectedIndex = this.order;
        }

        protected override void OnLeave(object sender, EventArgs e)
        {
            base.OnLeave(sender, e);
            searchWindow.SelectedIndex = this.order;
        }

        public void LoadImagePreview()
        {
            if(!File.Exists(attachedImage.FullPath))
                return;

            FileInfo imgFile = new FileInfo(attachedImage.FullPath);
            if(imgFile.Length > FILE_SIZE_LIMIT)
                return;
            
            Image img = Image.FromFile(imgFile.FullName);
            
            // Get image thumbnail that stays in the boundaries of a square
            // of size ROW_HEIGHT

            float ratio = (float)Math.Max(img.Width, img.Height) / (float)ROW_HEIGHT;

            int sizeX = Convert.ToInt32((float)img.Width / ratio);
            int sizeY = Convert.ToInt32((float)img.Height / ratio);
            int posX = 0;
            int posY = 0;

            if(img.Width > img.Height)
                posY = (ROW_HEIGHT - sizeY) / 2;
            else if(img.Width < img.Height)
                posX = (ROW_HEIGHT - sizeX) / 2;

            Image.GetThumbnailImageAbort callback =
                new Image.GetThumbnailImageAbort(() => true);
            Image thumb = img.GetThumbnailImage(sizeX, sizeY, callback, IntPtr.Zero);
            
            preview = new PictureBox(){
                Image = thumb,
                Location = new Point(posX, posY),
                Size = new Size(sizeX, sizeY)
            };

            AddHoverOnElement(preview);
            preview.MouseDown += OnMouseDown;
            preview.DoubleClick += OnDoubleClick;
            preview.Cursor = Cursors.Hand;
        }

        public void AddImagePreview()
        {
            if(preview != null)
                Controls.Add(preview);
        }

        public void CopyToClipboard()
        {
            attachedImage.CopyToClipboard();
        }

        public void IncrementImageUsage()
        {
            if(!imageWasUsed){
                imageWasUsed = true;
                attachedImage.Usage++;
                // Update async in order to avoid eventual freezes
                // on form closing.
                Task.Run(() => attachedImage.UpdateInDB());
            }
        }
    }
}