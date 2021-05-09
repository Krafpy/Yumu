using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Yumu
{
    static class DB
    {
        #region Data files

        /// <summary>Creates the data files if they don't already exist.</summary>
        public static void CreateDataFiles(string[] fileNames)
        {
            foreach(string fileName in fileNames){
                CreateEmptyFile(fileName);
            }
        }

        public static void CreateEmptyFile(string file)
        {
            if(!File.Exists(file)){
                File.Create(file).Dispose();
            }
        }

        #endregion

        #region Generic data file access

        private static byte[] ToDataRow<T>(T item) where T : DBItem
        {
            byte[] content = item.ToDataRow();
            byte[] data = new byte[content.Length + 4];
            ArrayUtils.WriteIntToByteArray(data, 0, content.Length); // Insert header in front
            Array.Copy(content, 0, data, 4, content.Length);
            
            return data;
        }

        private static T FromNextRow<T>(BinaryReader br) where T : DBItem
        {
            byte[] head = br.ReadBytes(4);
            int dataLength = ArrayUtils.ReadIntFromByteArray(head, 0);
            byte[] data = br.ReadBytes(dataLength);
            T item = (T)Activator.CreateInstance(typeof(T)); //default(T);
            item.FromDataRow(data);
            
            return item;
        }

        /// <summary>Appends items to (the end of) a datafile.</summary>
        /// <param name="items">the array of items to append.</param>
        /// <param name="dataFile">the datafile where the new items must be added.</param>
        public static void AppendItems<T>(T[] items, string dataFile)
        where T : DBItem
        {
            List<byte> data = new List<byte>();
            foreach(T item in items){
                data.AddRange(ToDataRow<T>(item));
            }

            using(FileStream fs = new FileStream(dataFile, FileMode.Append))
            {
                fs.Write(data.ToArray(), 0, data.Count);
            }
        }

        /// <summary>Retrieves each contained items from a datafile.</summary>
        /// <param name="dataFile">the data file where the items must be read from.</param>
        public static IEnumerable<T> GetItems<T>(string dataFile)
        where T : DBItem
        {
            using(BinaryReader br = new BinaryReader(File.OpenRead(dataFile)))
            {
                while(br.PeekChar() != -1){
                    yield return FromNextRow<T>(br);
                }
            }
        }

        /// <summary>Removes the elements with the provided IDs from the database.</summary>
        /// <param name="ids">the IDs of the items to remove.</param>
        /// <param name="dataFile">the data file where the items must be removed from.</param>
        public static void RemoveItems<T>(int[] ids, string dataFile)
        where T : DBItem
        {
            T[] allItems = GetItems<T>(dataFile).OrderBy(item => item.Id).ToArray();
            RemoveItems<T>(ids, dataFile, allItems);
        }

        public static void RemoveItems<T>(int[] ids, string dataFile, T[] allItems)
        where T : DBItem
        {
            // Loop through all the rows and builds a list without the specified IDs
            // we assume that the items are ordered according to their IDs 
            // (in ascending order) for an efficient removal.
            
            if(!ArrayUtils.IsIntegerArraySorted(ids))
                Array.Sort(ids);

            List<byte> data = new List<byte>();

            int idIndex = 0;
            foreach(T item in allItems){
                while(idIndex < ids.Length && item.Id > ids[idIndex]){
                    idIndex++;
                }
                if(idIndex == ids.Length || ids[idIndex] != item.Id){
                    data.AddRange(ToDataRow<T>(item));
                }
            }

            File.WriteAllBytes(dataFile, data.ToArray());
        }

        public static void UpdateContent<T>(T[] newItems, string dataFile) where T : DBItem
        {
            List<byte> data = new List<byte>();
            foreach(T item in newItems) {
                data.AddRange(ToDataRow<T>(item));
            }
            File.WriteAllBytes(dataFile, data.ToArray());
        }

        #endregion

        public static void AppendItem<T>(T item, string dataFile) where T : DBItem
        {
            AppendItems<T>(new T[] {item}, dataFile);
        }

        public static void RemoveItem<T>(int id, string dataFile, T[] allItems)
        where T : DBItem
        {
            RemoveItems<T>(new int[] {id}, dataFile, allItems);
        }

        public static void RemoveItem<T>(int id, string dataFile) where T : DBItem
        {
            RemoveItems<T>(new int[] {id}, dataFile);
        }
    }
}