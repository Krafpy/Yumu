using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace Yumu
{
    class DirectoryManager : Window
    {
        public const int LEFT_OFFSET = 15;
        public const int RIGHT_OFFSET = 15;
        public const int BAR_HEIGHT = 30;

        private DirectoryControl[] dirControls;
        private Panel dirsPanel;

        // Title label above directories caracteristics
        private Label dirLabel; // Directory name
        private Label imgLabel; // Image count
        private Label btnLabel; // Action buttons

        public Label DirLabel {get => dirLabel;}
        public Label ImgLabel {get => imgLabel;}
        public Label BtnLabel {get => btnLabel;}

        public Panel DirsPanel {get => dirsPanel;}

        public DBAccessor dbAccessor;

        public DirectoryManager() : base("Yumu directories", 500, 400)
        {
            dbAccessor = new DBAccessor();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Events
            Paint += OnPaint;
            
            // Allow dropping files in the window
            AllowDrop = true;

            // Window content
            // Add directory button
            Color addBtnBack = Color.FromArgb(110, 110, 110);
            Color addBtnHover = Color.FromArgb(50, 120, 200);
            Button addBtn = new Button(){
                Image = Image.FromFile("./visuals/plus-16.png"),
                Width = BAR_HEIGHT,
                Height = BAR_HEIGHT,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = addBtnBack,
                AutoSize = true
            };
            addBtn.Click += OpenDialog;
            addBtn.FlatAppearance.BorderSize = 0;
            addBtn.MouseEnter += delegate {addBtn.BackColor = addBtnHover;};
            addBtn.MouseLeave += delegate {addBtn.BackColor = addBtnBack;};
            addBtn.Cursor = Cursors.Hand;
            Controls.Add(addBtn);

            // Labels for directory list
            Color labCol = Color.FromArgb(100, 100, 100);
            Font labFont = new Font(FONT_NAME, 10, FontStyle.Bold);

            // The directory name/path
            dirLabel = new Label(){
                Text = "Directory",
                ForeColor = labCol,
                Font = labFont,
                Location = new Point(LEFT_OFFSET, BAR_HEIGHT)
            };
            Controls.Add(dirLabel);

            // The control buttons for a directory
            btnLabel = new Label(){
                Text = "Reload / Remove",
                ForeColor = labCol,
                Font = labFont,
                AutoSize = true,
            };
            int x = ClientSize.Width - btnLabel.PreferredWidth - RIGHT_OFFSET;
            int y = BAR_HEIGHT;
            btnLabel.Location = new Point(x, y);
            Controls.Add(btnLabel);

            // The number of referenced images in a directory
            imgLabel = new Label(){
                Text = "Images",
                ForeColor = labCol,
                Font = labFont,
                AutoSize = true,
            };
            x = btnLabel.Location.X - imgLabel.PreferredWidth - 50;
            imgLabel.Location = new Point(x, y);
            Controls.Add(imgLabel);

            // Panel which will contain the directory list
            dirsPanel = new Panel(){
                Location = new Point(0, BAR_HEIGHT + dirLabel.Height),
                Size = new Size(ClientSize.Width, ClientSize.Height - BAR_HEIGHT - dirLabel.Height),
                AutoScroll = true
            };
            dirsPanel.BorderStyle = BorderStyle.None;
            Controls.Add(dirsPanel);

            BuildDirectoryList();
        }

        protected override void OnDragEnter(DragEventArgs e)
        {
            base.OnDragEnter(e);

            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effect = DragDropEffects.Copy;
            } else {
                e.Effect = DragDropEffects.None;
            }
        }

        protected override void OnDragDrop(DragEventArgs e)
        {
            base.OnDragDrop(e);

            string[] dirs = GetDirectoriesPathFromDrag(e);
            if(dirs.Length > 0){
                foreach(string dirPath in dirs){
                    AppendNewDirectory(dirPath);
                }
                BuildDirectoryList();
            }
        }

        private string[] GetDirectoriesPathFromDrag(DragEventArgs e)
        {
            List<string> dirs = new List<string>();
            string[] paths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
            
            foreach(string path in paths){
                if(File.GetAttributes(path).HasFlag(FileAttributes.Directory)){
                    dirs.Add(path);
                }
            }

            return dirs.ToArray();
        }

        public void BuildDirectoryList()
        {
            ClearDirectoryList();

            dirControls = new DirectoryControl[dbAccessor.Directories.Count];
            for(int i = 0; i < dirControls.Length; i++){
                dirControls[i] = new DirectoryControl(this, dbAccessor.Directories[i], i);
            }
        }

        private void ClearDirectoryList()
        {
            if(dirControls != null){
                foreach(DirectoryControl dirControl in dirControls)
                    dirsPanel.Controls.Remove(dirControl);
            }
        }

        private void OnPaint(object sender, PaintEventArgs e)
        {
            // Draw the top grey bar
            Graphics graphics = CreateGraphics();
            SolidBrush brush = new SolidBrush(Color.FromArgb(96, 96, 96));
            Rectangle rect = new Rectangle(0, 0, ClientSize.Width, BAR_HEIGHT);
            graphics.FillRectangle(brush, rect);
            graphics.Dispose();
        }

        private void OpenDialog(object sender, EventArgs e)
        {
            using(FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath)){
                    AppendNewDirectory(dialog.SelectedPath);
                    BuildDirectoryList();
                }
            }
        }

        private void AppendNewDirectory(string path)
        {
            ReferencedDirectory dir = new ReferencedDirectory(path);
            dbAccessor.AppendDirectoryReference(dir);
        }
    }
}