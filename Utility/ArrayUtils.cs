using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Yumu
{
    static class ArrayUtils
    {
        /// <summary>Checks in an array is sorted.</summary>
        /// <param name="arr">the array to check.</param>
        public static bool IsIntegerArraySorted(int[] arr)
        {
            if(arr.Length <= 1) return true;
            for(int i = 0; i < arr.Length - 1; ++i){
                if(arr[i] > arr[i+1])
                    return false;
            }
            return true;
        }
        
        /// <summary>Stores the bytes of an integer in a byte array.</summary>
        /// <param name="data">the array where to write the integer bytes.</param>
        /// <param name="startIndex">the start index where to write in the array.</param>
        /// <param name="value">the integer value to write in the array.</param>
        public static void WriteIntToByteArray(byte[] data, int startIndex, int value)
        {
            if (data.Length < startIndex + 3)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a 4-byte value at offset " + startIndex + ".");
            
            byte[] bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            Array.Copy(bytes, 0, data, startIndex, 4);
        }

        /// <summary>Reads a byte reprensentation of an integer from a byte array.</summary>
        /// <param name="data">the array where to read the integer bytes.</param>
        /// <param name="startIndex">the start index where to read in the array.</param>
        public static int ReadIntFromByteArray(byte[] data, int startIndex)
        {
            if (data.Length < startIndex + 3)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a 4-byte value at offset " + startIndex + ".");
            
            byte[] bytes = new byte[4];
            Array.Copy(data, startIndex, bytes, 0, 4);
            if (BitConverter.IsLittleEndian)
    			Array.Reverse(bytes);
				
			return BitConverter.ToInt32(bytes, 0);
        }

        public static void WriteIntToByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian, UInt32 value)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytes + "-byte value at offset " + startIndex + ".");
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                data[offs] = (Byte)(value >> (8 * index) & 0xFF);
            }
        }

        public static UInt32 ReadIntFromByteArray(Byte[] data, Int32 startIndex, Int32 bytes, Boolean littleEndian)
        {
            Int32 lastByte = bytes - 1;
            if (data.Length < startIndex + bytes)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to read a " + bytes + "-byte value at offset " + startIndex + ".");
            UInt32 value = 0;
            for (Int32 index = 0; index < bytes; index++)
            {
                Int32 offs = startIndex + (littleEndian ? index : lastByte - index);
                value += (UInt32)(data[offs] << (8 * index));
            }
            return value;
        }

        /// <summary>Writes a string bytes into an array.</summary>
        /// <param name="data">the array where to write the string.</param>
        /// <param name="byteIndex">index where to start writing the string bytes in the array.</param>
        /// <param name="s">the string to convert.</param>
        public static void WriteStringToByteArray(byte[] data, int destinationIndex, string s)
        {
            int bytesCount = Encoding.UTF8.GetByteCount(s);
            if (data.Length < bytesCount + destinationIndex)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytesCount + "-byte value at offset " + destinationIndex + ".");
            Encoding.UTF8.GetBytes(s, 0, s.Length, data, destinationIndex);
        }

        /// <summary>Reads a string from a byte array.</summary>
        /// <param name="data">the array where to read the string from.</param>
        /// <param name="startIndex">index in the array where to start reading the bytes.</param>
        /// <param name="bytesCount">the number of bytes to read.</param>
        public static string ReadStringFromByteArray(byte[] data, int startIndex, int bytesCount)
        {
            if (data.Length < bytesCount + startIndex)
                throw new ArgumentOutOfRangeException("startIndex", "Data array is too small to write a " + bytesCount + "-byte value at offset " + startIndex + ".");
            return Encoding.UTF8.GetString(data, startIndex, bytesCount);
        }

        /// <summary>Converts any object into its byte array.</summary>
        /// <param name="obj">the object to convert.</param>
        public static byte[] ObjectToByteArray(object obj)
        {
            if(obj == null)
                return null;
            
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }

        /// <summary>Reads a given struct type from its byte array convertion<summary>
        /// <param name="T">the <c>struct</c> type.</param>
        /// <param name="bytes">the byte array in which the struct data are contained.</param>
        public static T ByteArrayToStructure<T>(byte[] bytes) where T: struct 
        {
            T stuff;
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try {
                stuff = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally {
                handle.Free();
            }
            return stuff;
        }
    }
}