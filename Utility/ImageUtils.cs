using System.Drawing;
using System.Windows.Forms;
using System.IO;

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