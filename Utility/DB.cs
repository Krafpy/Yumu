
using System;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Yumu
{
    /// <summary>Encapsulation of low level access to the data files used by the software.</summary>
    static class DB
    {
        private const string DIRECTORIES_DATA_FILE  = "./dirs.dat";
        private const string IMAGES_DATA_FILE       = "./imgs.dat";

        #region data files

        /// <summary>Creates the data files if they don't already exist.</summary>
        public static void CreateDataFiles()
        {
            CreateEmptyFile(DIRECTORIES_DATA_FILE);
            CreateEmptyFile(IMAGES_DATA_FILE);
        }

        private static void CreateEmptyFile(string file)
        {
            if(!File.Exists(file)){
                File.Create(file).Dispose();
            }
        }

        #endregion

        #region generic database accessors

        /// <summary>Appends items to (the end of) a datafile.</summary>
        /// <param name="items">the array of items to append.</param>
        /// <param name="ToDataRow">the function that converts the provided DBItem instance into its binary data row.</param>
        /// <param name="dataFile">the datafile where the new items must be added.</param>
        private static void AppendItemsToDB<T>(T[] items, Func<T, byte[]> ToDataRow, string dataFile)
        where T : DBItem
        {
            List<byte> data = new List<byte>();
            foreach(T item in items){
                data.AddRange(ToDataRow(item));
            }

            using(FileStream fs = new FileStream(dataFile, FileMode.Append))
            {
                fs.Write(data.ToArray(), 0, data.Count);
            }
        }

        /// <summary>Retrieves each contained rows from a datafile.</summary>
        /// <param name="GetDataRow">the function that reads the data row for the stored item type.</param>
        /// <param name="dataFile">the data file where the items must be read from.</param>
        private static IEnumerable<byte[]> GetRawItemsFromDB(Func<BinaryReader, byte[]> GetDataRow, string dataFile)
        {
            using(BinaryReader br = new BinaryReader(File.OpenRead(dataFile)))
            {
                while(br.PeekChar() != -1){
                    yield return GetDataRow(br);
                }
            }
        }

        /// <summary>Retrieves each contained items from a datafile.</summary>
        /// <param name="GetDataRow">the function that reads the data row for the stored item type.</param>
        /// <param name="FromDataRow">the function that converts a binary data row to the corresponding DBItem instance.</param>
        /// <param name="dataFile">the data file where the items must be read from.</param>
        private static IEnumerable<T> GetItemsFromDB<T>(Func<BinaryReader, byte[]> GetDataRow, Func<byte[], T> FromDataRow, string dataFile)
        where T : DBItem
        {
            IEnumerable<byte[]> dataRows = GetRawItemsFromDB(GetDataRow, dataFile);
            foreach(byte[] dataRow in dataRows){
                yield return FromDataRow(dataRow);
            }
        }

        /// <summary>Removes the elements with the provided IDs from the database.</summary>
        /// <param name="ids">the IDs of the items to remove.</param>
        /// <param name="GetDataRow">the function that reads the data row for the stored item type.</param>
        /// <param name="FromDataRow">the function that converts a binary data row to the corresponding DBItem instance.</param>
        /// <param name="dataFile">the data file where the items must be removed from.</param>
        private static void RemoveItemsFromDB<T>(int[] ids, Func<BinaryReader, byte[]> GetDataRow, Func<byte[], T> FromDataRow, string dataFile)
        where T : DBItem
        {
            // Loop through all the rows and builds a list without the specified IDs
            // Thanks to how the database is built, we can assume that the rows
            // are ordered according to their IDs (in ascending order) to make a more
            // efficient removal.
            
            if(!ArrayUtils.IsIntegerArraySorted(ids))
                Array.Sort(ids);

            List<byte> data = new List<byte>();
            IEnumerable<byte[]> allDataRows = GetRawItemsFromDB(GetDataRow, dataFile);

            int idIndex = 0;
            foreach(byte[] dataRow in allDataRows){
                T item = FromDataRow(dataRow);
                
                while(idIndex < ids.Length && item.Id > ids[idIndex]){
                    idIndex++;
                }
                if(idIndex == ids.Length || ids[idIndex] != item.Id){
                    data.AddRange(dataRow);
                }
            }

            File.WriteAllBytes(dataFile, data.ToArray());
        }

        /// <summary>Updates an item data in the database, the binary size of its data row format
        /// must stay the same.</summary>
        /// <param name="updatedItem">the DBItem to overwrite.</param>
        /// <param name="GetDataRow">the function that reads the data row for the stored item type.</param>
        /// <param name="FromDataRow">the function that converts a binary data row to the corresponding DBItem instance.</param>
        /// <param name="ToDataRow">the function that converts the provided DBItem instance into its binary data row.</param>
        /// <param name="dataFile">the data file where the item must be updated.</param>
        private static void UpdateItemInDB<T>(
            T updatedItem, Func<BinaryReader, byte[]> GetDataRow, Func<byte[], T> FromDataRow, Func<T, byte[]> ToDataRow, string dataFile
        ) where T : DBItem
        {
            using(BinaryReader br = new BinaryReader(File.Open(dataFile, FileMode.Open, FileAccess.ReadWrite)))
            {
                Stream fs = br.BaseStream;

                long itemDataPosition = 0;
                
                while(br.PeekChar() != -1){
                    itemDataPosition = fs.Position;
                    T item = FromDataRow(GetDataRow(br));
                    
                    if(item.Id == updatedItem.Id){
                        byte[] data = ToDataRow(updatedItem);
                        fs.Position = itemDataPosition;  
                        fs.Write(data, 0, data.Length);
                        break;
                    }
                }
            }
        }

        #endregion
        
        #region referenced directories access

        /* 
        Data row format for referenced directories :

            signification   : id         | number of images   | path string byte count  | path string (UTF8) 
            byte count      : 4 (int)    | 4 (int)            | 4 (int)                 | [path string byte count] (string)
        */

        /// <summary>Retrieves the next referenced directory data row</summary>
        /// <param name="br">the binary reader of the data file in which read the next row.</param>
        private static byte[] GetReferencedDirectoryDataRow(BinaryReader br)
        {
            byte[] header = br.ReadBytes(12);
            int pathByteCount = ArrayUtils.ReadIntFromByteArray(header, 8);
            byte[] pathBytes = br.ReadBytes(pathByteCount);

            byte[] dataRow = new byte[12 + pathByteCount];
            Array.Copy(header, dataRow, 12);
            Array.Copy(pathBytes, 0, dataRow, 12, pathByteCount);

            return dataRow;
        }

        /// <summary>Converts a raw byte row from the database into its referenced directory instance.</summary>
        /// <param name="dataRow">the byte array representing a referenced directory.</param>
        private static ReferencedDirectory ReferencedDirectoryFromDataRow(byte[] dataRow)
        {
            int id = ArrayUtils.ReadIntFromByteArray(dataRow, 0);
            int imageCount = ArrayUtils.ReadIntFromByteArray(dataRow, 4);
            int pathByteLength = ArrayUtils.ReadIntFromByteArray(dataRow, 8);
            string path = ArrayUtils.ReadStringFromByteArray(dataRow, 12, pathByteLength);

            return new ReferencedDirectory(id, path, imageCount);
        }

        /// <summary>Converts a referenced directory instance into a byte array represenation
        /// that can be saved in the database.</summary>
        /// <param name="dir">the instance of referenced directory to convert.</param>
        private static byte[] ReferencedDirectoryToDataRow(ReferencedDirectory dir)
        {
            int pathByteCount = Encoding.UTF8.GetByteCount(dir.FullPath);

            byte[] data = new byte[12 + pathByteCount];

            ArrayUtils.WriteIntToByteArray(data, 0, dir.Id);
            ArrayUtils.WriteIntToByteArray(data, 4, dir.ImageCount);
            ArrayUtils.WriteIntToByteArray(data, 8, pathByteCount);
            ArrayUtils.WriteStringToByteArray(data, 12, dir.FullPath);

            return data;
        }

        /// <summary>Append new referenced directories to the database.</summary>
        /// <param name="dirs">the array of referenced directories to append.</param>
        public static void AppendReferencedDirectories(ReferencedDirectory[] dirs)
        {
            AppendItemsToDB<ReferencedDirectory>(
                dirs, ReferencedDirectoryToDataRow, DIRECTORIES_DATA_FILE
            );
        }

        /// <summary>Removes an array of referenced directories from the database.</summary>
        /// <param name="ids">the ids of the referenced directories to remove.</param>
        public static void RemoveReferencedDirectories(int[] ids)
        {
            RemoveItemsFromDB<ReferencedDirectory>(
                ids, GetReferencedDirectoryDataRow, ReferencedDirectoryFromDataRow, DIRECTORIES_DATA_FILE
            );
        }

        /// <summary>Gets all the currently referenced directories.</summary>
        public static IEnumerable<ReferencedDirectory> GetReferencedDirectories()
        {
            return GetItemsFromDB<ReferencedDirectory>(
                GetReferencedDirectoryDataRow, ReferencedDirectoryFromDataRow, DIRECTORIES_DATA_FILE
            );
        }

        /// <summary>Updates a referenced directory content. It's byte size must remain 
        /// the same.</summary>
        public static void UpdateReferencedDirectory(ReferencedDirectory dir)
        {
            UpdateItemInDB<ReferencedDirectory>(dir,
                GetReferencedDirectoryDataRow, ReferencedDirectoryFromDataRow, ReferencedDirectoryToDataRow, DIRECTORIES_DATA_FILE
            );
        }

        #endregion

        #region referenced images access
        
        /*
        Data row format for referenced image :

            signification   : id        | directory id  | usage     | simplified name byte count    | file name byte count
            byte count      : 4 (int)   | 4 (int)       | 4 (int)   | 4 (int)                       | 4 (int)

                            | simplified name string (UTF8)     | file name string (UTF8) 
                            | [...]                             | [...]
        */

        /// <summary>Retrieves the next referenced image data row.</summary>
        /// <param name="br">the binary reader of the data file in which read the next row.</param>
        private static byte[] GetReferencedImageDataRow(BinaryReader br)
        {
            byte[] header = br.ReadBytes(20);
            int simpleNameByteCount = ArrayUtils.ReadIntFromByteArray(header, 12);
            int fileNameByteCount = ArrayUtils.ReadIntFromByteArray(header, 16);
            byte[] simpleNameBytes = br.ReadBytes(simpleNameByteCount);
            byte[] fileNameBytes = br.ReadBytes(fileNameByteCount);

            byte[] dataRow = new byte[20 + simpleNameByteCount + fileNameByteCount];
            Array.Copy(header, dataRow, 20);
            Array.Copy(simpleNameBytes, 0, dataRow, 20, simpleNameByteCount);
            Array.Copy(fileNameBytes, 0, dataRow, 20 + simpleNameByteCount, fileNameByteCount);

            return dataRow;
        }

        /// <summary>Converts a raw byte row from the database into its referenced image instance.</summary>
        /// <param name="dataRow">the byte array representing a referenced image.</param>
        private static ReferencedImage ReferencedImageFromDataRow(byte[] dataRow)
        {
            int id = ArrayUtils.ReadIntFromByteArray(dataRow, 0);
            int dirId = ArrayUtils.ReadIntFromByteArray(dataRow, 4);
            int usage = ArrayUtils.ReadIntFromByteArray(dataRow, 8);
            int simpleNameByteCount = ArrayUtils.ReadIntFromByteArray(dataRow, 12);
            int fileNameByteCount = ArrayUtils.ReadIntFromByteArray(dataRow, 16);
            string simpleName = ArrayUtils.ReadStringFromByteArray(dataRow, 20, simpleNameByteCount);
            string fileName = ArrayUtils.ReadStringFromByteArray(dataRow, 20 + simpleNameByteCount, fileNameByteCount);

            return new ReferencedImage(id, dirId, usage, fileName, simpleName);
        }

        /// <summary>Converts a referenced image instance into a byte array represenation
        /// that can be saved in the database.</summary>
        /// <param name="img">the instance of the referenced image to convert.</param>
        private static byte[] ReferencedImageToDataRow(ReferencedImage img)
        {
            int simpleNameByteCount = Encoding.UTF8.GetByteCount(img.SimplifiedName);
            int fileNameByteCount = Encoding.UTF8.GetByteCount(img.FileName);

            byte[] data = new byte[20 + simpleNameByteCount + fileNameByteCount];

            ArrayUtils.WriteIntToByteArray(data, 0, img.Id);
            ArrayUtils.WriteIntToByteArray(data, 4, img.DirId);
            ArrayUtils.WriteIntToByteArray(data, 8, img.Usage);
            ArrayUtils.WriteIntToByteArray(data, 12, simpleNameByteCount);
            ArrayUtils.WriteIntToByteArray(data, 16, fileNameByteCount);
            ArrayUtils.WriteStringToByteArray(data, 20, img.SimplifiedName);
            ArrayUtils.WriteStringToByteArray(data, 20 + simpleNameByteCount, img.FileName);

            return data;
        }

        /// <summary>Append new referenced images to the database.</summary>
        /// <param name="dirs">the array of referenced images to append.</param>
        public static void AppendReferencedImages(ReferencedImage[] imgs)
        {
            AppendItemsToDB<ReferencedImage>(
                imgs, ReferencedImageToDataRow, IMAGES_DATA_FILE
            );
        }

        /// <summary>Removes an array of referenced images from the database.</summary>
        /// <param name="ids">the ids (their directory ids) of the referenced images to remove.</param>
        public static void RemoveReferencedImages(int[] ids)
        {
            RemoveItemsFromDB<ReferencedImage>(
                ids, GetReferencedImageDataRow, ReferencedImageFromDataRow, IMAGES_DATA_FILE
            );
        }

        /// <summary>Gets all the currently referenced images.</summary>
        public static IEnumerable<ReferencedImage> GetReferencedImages()
        {
            return GetItemsFromDB<ReferencedImage>(
                GetReferencedImageDataRow, ReferencedImageFromDataRow, IMAGES_DATA_FILE
            );
        }

        /// <summary>Updates a referenced image. It's byte size must remain 
        /// the same.</summary>
        public static void UpdateReferencedImage(ReferencedImage img)
        {
            UpdateItemInDB<ReferencedImage>(img,
                GetReferencedImageDataRow, ReferencedImageFromDataRow, ReferencedImageToDataRow, IMAGES_DATA_FILE
            );
        }

        #endregion
    }
}