using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System;

namespace Yumu
{
    static class ImageUtils
    {
        public static void CopyToClipboard(string imagePath)
        {   
            if(File.Exists(imagePath)) {
                Image img;
                using(Stream s = File.OpenRead(imagePath))
                {
                    img = Image.FromStream(s);
                }
                SetClipboardImage(img, null, null);
            }
        }

        // https://stackoverflow.com/questions/44177115/copying-from-and-to-clipboard-loses-image-transparency

        /// <summary>
        /// Copies the given image to the clipboard as PNG, DIB and standard Bitmap format.
        /// </summary>
        /// <param name="image">Image to put on the clipboard.</param>
        /// <param name="imageNoTr">Optional specifically nontransparent version of the image to put on the clipboard.</param>
        /// <param name="data">Clipboard data object to put the image into. Might already contain other stuff. Leave null to create a new one.</param>
        public static void SetClipboardImage(Image image, Image imageNoTr, DataObject data)
        {            
            Clipboard.Clear();
            if (data == null)
                data = new DataObject();
            if (imageNoTr == null)
                imageNoTr = image;
            using (MemoryStream pngMemStream = new MemoryStream())
            //using (MemoryStream dibMemStream = new MemoryStream())
            using (MemoryStream f17MemStream = new MemoryStream())
            {
                // As standard bitmap, without transparency support
                data.SetData(DataFormats.Bitmap, true, imageNoTr);
                // As PNG. Gimp/Open Office will prefer this over the other two.
                image.Save(pngMemStream, ImageFormat.Png);
                data.SetData("PNG", false, pngMemStream);
                
                // As DIB. This is (wrongly) accepted as ARGB by many applications.
                /*Byte[] dibData = ConvertToDib(image);
                dibMemStream.Write(dibData, 0, dibData.Length);
                data.SetData(DataFormats.Dib, false, dibMemStream);*/

                // As a DIBv5 (Format17)
                Byte[] dibV5Data = ConvertToDIBv5(image);
                f17MemStream.Write(dibV5Data, 0, dibV5Data.Length);
                data.SetData(DataFormats.Dib, false, f17MemStream);
                // The 'copy=true' argument means the MemoryStreams can be safely disposed after the operation.
                Clipboard.SetDataObject(data, true);
            }
        }

        private static Byte[] ConvertToDIBv5(Image image)
        {
            Byte[] bm32bData = GetBM32Data(image);
            Int32 width = image.Width;
            Int32 height = image.Height;

            Int32 hdrSize = 124;
            Byte[] fullImage = new Byte[hdrSize + bm32bData.Length];
            //Int32 bV5Size;
            ArrayUtils.WriteIntToByteArray(fullImage, 0, 4, true, (UInt32)hdrSize);
            //Int32 bV5Width;
            ArrayUtils.WriteIntToByteArray(fullImage, 4, 4, true, (UInt32)width);
            //Int32 bV5Height;
            ArrayUtils.WriteIntToByteArray(fullImage, 8, 4, true, (UInt32)height);
            //Int16 bV5Planes;
            ArrayUtils.WriteIntToByteArray(fullImage, 12, 2, true, 1);
            //Int16 bV5BitCount;
            ArrayUtils.WriteIntToByteArray(fullImage, 14, 2, true, 32);
            //Int32 bV5Compression;
            ArrayUtils.WriteIntToByteArray(fullImage, 16, 4, true, 0);
            //Int32 biSizeImage;
            ArrayUtils.WriteIntToByteArray(fullImage, 20, 4, true, (UInt32)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 bV5XPelsPerMeter = 0;
            //Int32 bV5YPelsPerMeter = 0;
            //Int32 bV5ClrUsed = 0;
            //Int32 bV5ClrImportant = 0;
            //Int32 Red/Green/Blue/Alpha masks
            ArrayUtils.WriteIntToByteArray(fullImage, 40, 4, true, 0x000000FF);
            ArrayUtils.WriteIntToByteArray(fullImage, 44, 4, true, 0x0000FF00);
            ArrayUtils.WriteIntToByteArray(fullImage, 48, 4, true, 0x00FF0000);
            ArrayUtils.WriteIntToByteArray(fullImage, 52, 4, true, 0xFF000000);
            // Int32 bV5CSType : "sRGB"
            ArrayUtils.WriteIntToByteArray(fullImage, 56, 4, true, 1934772034);
            // Rest is all 0

            Array.Copy(bm32bData, 0, fullImage, hdrSize, bm32bData.Length);
            return fullImage;
        }

        /// <summary>
        /// Converts the image to Device Independent Bitmap format of type BITFIELDS.
        /// This is (wrongly) accepted by many applications as containing transparency,
        /// so I'm abusing it for that.
        /// </summary>
        /// <param name="image">Image to convert to DIB</param>
        /// <returns>The image converted to DIB, in bytes.</returns>
        private static Byte[] ConvertToDib(Image image)
        {
            Byte[] bm32bData = GetBM32Data(image);
            Int32 width = image.Width;
            Int32 height = image.Height;

            // BITMAPINFOHEADER struct for DIB.
            Int32 hdrSize = 0x28;
            Byte[] fullImage = new Byte[hdrSize + 12 + bm32bData.Length];
            //Int32 biSize;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x00, 4, true, (UInt32)hdrSize);
            //Int32 biWidth;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x04, 4, true, (UInt32)width);
            //Int32 biHeight;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x08, 4, true, (UInt32)height);
            //Int16 biPlanes;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x0C, 2, true, 1);
            //Int16 biBitCount;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x0E, 2, true, 32);
            //BITMAPCOMPRESSION biCompression = BITMAPCOMPRESSION.BITFIELDS;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x10, 4, true, 3);
            //Int32 biSizeImage;
            ArrayUtils.WriteIntToByteArray(fullImage, 0x14, 4, true, (UInt32)bm32bData.Length);
            // These are all 0. Since .net clears new arrays, don't bother writing them.
            //Int32 biXPelsPerMeter = 0;
            //Int32 biYPelsPerMeter = 0;
            //Int32 biClrUsed = 0;
            //Int32 biClrImportant = 0;

            // The aforementioned "BITFIELDS": colour masks applied to the Int32 pixel value to get the R, G and B values.
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 0, 4, true, 0x00FF0000);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 4, 4, true, 0x0000FF00);
            ArrayUtils.WriteIntToByteArray(fullImage, hdrSize + 8, 4, true, 0x000000FF);
            Array.Copy(bm32bData, 0, fullImage, hdrSize + 12, bm32bData.Length);
            return fullImage;
        }

        private static Byte[] GetBM32Data(Image image)
        {
            Byte[] bm32bData;
            // Ensure image is 32bppARGB by painting it on a new 32bppARGB image.
            using (Bitmap bm32b = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(bm32b))
                    gr.DrawImage(image, new Rectangle(0, 0, bm32b.Width, bm32b.Height));
                // Bitmap format has its lines reversed.
                bm32b.RotateFlip(RotateFlipType.Rotate180FlipX);
                bm32bData = GetImageData(bm32b);
            }

            return bm32bData;
        }

        private static byte[] GetImageData(Bitmap bmp)
        {
            // How it should be done, but doesn't work with png with transparency (artifacts)
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);
            IntPtr ptr = bmpData.Scan0;
            int bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            byte[] rgbaValues = new byte[bytes];
            Marshal.Copy(ptr, rgbaValues, 0, bytes);
            bmp.UnlockBits(bmpData);
            // Normally it should stop there, but doesn't work for png with transparency (transparency
            // not handled and artifacts).
            // return rgbaValues;

            // Bruteforcing transparency.
            // The only way I found to keep transparent parts of images, especially
            // emotes, is by applying a threshold on pixels with transparency.
            // Therefore images that contain transparency can only have either fully
            // transparent pixels (alpha set to 0x00), or fully colored (alpha set to 0xFF).
            // I don't know why, I don't know how, but it's the only way I found to
            // work around :D
            for(int i = 0; i < rgbaValues.Length; i += 4) {
                if(rgbaValues[i + 3] < 50){ // arbitrary hardcoded threshold :D, makes discord emotes look well... ¯\_(ツ)_/¯
                    rgbaValues[i + 0] = 0x00;
                    rgbaValues[i + 1] = 0x00;
                    rgbaValues[i + 2] = 0x00;
                    rgbaValues[i + 3] = 0x00;
                } else {
                    rgbaValues[i + 3] = 0xFF;
                }
            }

            return rgbaValues;
        }
    }
}