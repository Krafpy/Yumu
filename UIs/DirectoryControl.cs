using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;

namespace Yumu
{
    class DirectoryControl : ListItem
    {
        public const int ROW_HEIGHT = 35;
        public const int BTN_SEP = 10;
        public const int BTN_WIDTH = 25;

        private const int DIR_MAX_LENGTH = 25;

        private DirectoryManager directoryManager;
        private ReferencedDirectory attachedDirectory;
        private int order;

        private DBAccessor dbAccessor;

        public DirectoryControl(DirectoryManager directoryManager, ReferencedDirectory dir, int order) : base(directoryManager.DirsPanel)
        {
            this.directoryManager = directoryManager;
            this.attachedDirectory = dir;
            this.order = order;

            dbAccessor = this.directoryManager.dbAccessor;

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Location = new Point(0, order * ROW_HEIGHT);
            Size = new Size(parent.Width, ROW_HEIGHT);

            DoubleClick += OnClick;
            
            // Get the title labels (for location match)
            Label imgTitle = directoryManager.ImgLabel;
            Label btnTitle = directoryManager.BtnLabel;

            // Limit numbers of characters in the directory display name 
            string displayName = attachedDirectory.Name;
            if(displayName.Length > DIR_MAX_LENGTH)
                displayName = attachedDirectory.Name.Substring(0, DIR_MAX_LENGTH) + "...";

            // Directory name
            Label dirLab = new Label(){
                Text = displayName,
                Font = new Font(Window.FONT_NAME, 12),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };
            int x = DirectoryManager.LEFT_OFFSET;
            int y = ROW_HEIGHT / 2 - dirLab.Height / 2;
            dirLab.Location = new Point(x, y);
            dirLab.Cursor = Cursors.Hand;
            dirLab.Click += OnClick;

            AddHoverOnElement(dirLab);

            Controls.Add(dirLab);

            // Image count label
            Label imgLab = new Label(){
                Text = attachedDirectory.ImageCount.ToString(),
                Font = new Font(Window.FONT_NAME, 10),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            imgLab.ForeColor = attachedDirectory.ImageCount > 0 ? Color.Black : Color.Red;
            x = imgTitle.Location.X + imgTitle.PreferredWidth /2 - imgLab.PreferredWidth / 2;
            y = ROW_HEIGHT / 2 - imgLab.Height / 2;
            imgLab.Location = new Point(x, y);

            AddHoverOnElement(imgLab);

            Controls.Add(imgLab);

            Color btnFreeBack  = Color.FromArgb(75, 75, 75);

            // Button : delete directory from data file button
            Color delHoverBack = Color.FromArgb(150, 50, 50);
            Button delBtn = new Button(){
                Image = Image.FromFile("./visuals/minus-16.png"),
                Width = BTN_WIDTH,
                Height = BTN_WIDTH,
                FlatStyle = FlatStyle.Flat,
                BackColor = btnFreeBack,
                ImageAlign = ContentAlignment.MiddleCenter,
            };
            x = btnTitle.Location.X + btnTitle.PreferredWidth / 2 + BTN_SEP / 2;
            y = ROW_HEIGHT / 2 - BTN_WIDTH / 2;
            delBtn.Location = new Point(x, y);
            delBtn.FlatAppearance.BorderSize = 0;
            delBtn.Click += OnDelBtnClick;
            delBtn.MouseEnter += delegate {delBtn.BackColor = delHoverBack;};
            delBtn.MouseLeave += delegate {delBtn.BackColor = btnFreeBack;};
            delBtn.Cursor = Cursors.Hand;

            AddHoverOnElement(delBtn);

            Controls.Add(delBtn);

            // Button : reload directory content
            Color relHoverBack = Color.FromArgb(50, 120, 200);
            Button relBtn = new Button(){
                Image = Image.FromFile("./visuals/undo-16.png"),
                Width = BTN_WIDTH,
                Height = BTN_WIDTH,
                FlatStyle = FlatStyle.Flat,
                BackColor = btnFreeBack,
                ImageAlign = ContentAlignment.MiddleCenter,
            };
            x = delBtn.Location.X - BTN_WIDTH - BTN_SEP;
            relBtn.Location = new Point(x, y);
            relBtn.FlatAppearance.BorderSize = 0;
            relBtn.Click += OnRelBtnClick;
            relBtn.MouseEnter += delegate {relBtn.BackColor = relHoverBack;};
            relBtn.MouseLeave += delegate {relBtn.BackColor = btnFreeBack;};
            relBtn.Cursor = Cursors.Hand;

            AddHoverOnElement(relBtn);

            Controls.Add(relBtn);
        }

        private void OnDelBtnClick(object sender, EventArgs e)
        {
            dbAccessor.RemoveReferencedDirectory(attachedDirectory.Id);
            directoryManager.BuildDirectoryList();
        }

        private void OnRelBtnClick(object sender, EventArgs e)
        {
            dbAccessor.UpdateReferencedDirectory(attachedDirectory.Id);
            directoryManager.BuildDirectoryList();
        }

        private void OnClick(object sender, EventArgs e)
        {
            ProcessStartInfo startInfo = 
            new ProcessStartInfo("explorer.exe", attachedDirectory.FullPath);
            Process.Start(startInfo);
        }
    }
}