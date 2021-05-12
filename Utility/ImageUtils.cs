using System.Drawing;
using System.Windows.Forms;

namespace Yumu
{
    static class ImageUtils
    {
        public static void CopyToClipboard(string imagePath)
        {
            Clipboard.SetImage(Image.FromFile(imagePath));
        }
    }
}