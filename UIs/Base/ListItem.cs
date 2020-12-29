using System;
using System.Drawing;
using System.Windows.Forms;

namespace Yumu
{
    /// <summary>Represents a hoverable element of a list in a window.</summary>
    class ListItem : Panel
    {
        protected Panel parent;

        protected Color defaultBackground;
        protected Color hoverBackground;

        public ListItem(Panel parent)
        {
            this.parent = parent;

            defaultBackground = parent.BackColor;
            hoverBackground = Color.FromArgb(190, 210, 210);

            MouseEnter += OnEnter;
            MouseLeave += OnLeave;
            ControlRemoved += OnRemove;

            parent.Controls.Add(this);
        }

        protected virtual void OnEnter(object sender, EventArgs e)
        {
            BackColor = hoverBackground;
        }

        protected virtual void OnLeave(object sender, EventArgs e)
        {
            BackColor = defaultBackground;
        }

        protected virtual void OnRemove(object sender, EventArgs e)
        {
            Dispose();
        }

        protected void AddHoverOnElement(Control control)
        {
            control.MouseEnter += OnEnter;
            control.MouseLeave += OnLeave;
        }
    }
}