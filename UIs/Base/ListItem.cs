using System;
using System.Drawing;
using System.Windows.Forms;

namespace Yumu
{
    /// <summary>Represents a hoverable element of a list in a window.</summary>
    class ListItem : Panel
    {
        protected Panel _parent;

        protected Color _defaultBackground;
        protected Color _hoverBackground;

        public ListItem(Panel parent)
        {
            _parent = parent;

            _defaultBackground = parent.BackColor;
            _hoverBackground = Color.FromArgb(190, 210, 210);

            MouseEnter += OnEnter;
            MouseLeave += OnLeave;
            ControlRemoved += OnRemove;

            parent.Controls.Add(this);
        }

        protected virtual void OnEnter(object sender, EventArgs e)
        {
            BackColor = _hoverBackground;
        }

        protected virtual void OnLeave(object sender, EventArgs e)
        {
            BackColor = _defaultBackground;
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