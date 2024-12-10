/*
 *  ThomasJepp.SaintsRow - Saints Row IV and Gat Out Of Hell tools
 *  Copyright (c) 2013-2016, Thomas Jepp
 *  All rights reserved.
 *  https://github.com/saintsrowmods/ThomasJepp.SaintsRow
*/
using System.Runtime.InteropServices;
using System.Text;

namespace RFGM.Formats.Streams
{
    public static class StreamHelpers
    {
        #region Struct helpers

        public static T ReadStruct<T>(this Stream stream)
        {
            return stream.ReadStruct<T>(Marshal.SizeOf(typeof(T)));
        }

        public static T ReadStruct<T>(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            stream.Read(data, 0, data.Length);

            nint ptr = Marshal.AllocHGlobal(length);
            Marshal.Copy(data, 0, ptr, length);
            T structInstance = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);

            return structInstance;
        }

        public static void WriteStruct<T>(this Stream stream, T structToWrite)
        {
            int size = Marshal.SizeOf(structToWrite);
            byte[] data = new byte[size];

            nint ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(structToWrite, ptr, true);
            Marshal.Copy(ptr, data, 0, size);
            Marshal.FreeHGlobal(ptr);

            stream.Write(data, 0, data.Length);
        }
        #endregion

        #region Signed integer helpers
        public static sbyte ReadInt8(this Stream stream)
        {
            return (sbyte)stream.ReadByte();
        }

        public static short ReadInt16(this Stream stream)
        {
            byte[] data = new byte[2];
            stream.Read(data, 0, 2);
            return BitConverter.ToInt16(data, 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, 4);
            return BitConverter.ToInt32(data, 0);
        }

        public static void WriteInt8(this Stream stream, sbyte value)
        {
            stream.WriteByte((byte)value);
        }

        public static void WriteInt16(this Stream stream, short value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 2);
        }

        public static void WriteInt32(this Stream stream, int value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 4);
        }
        #endregion

        #region Unsigned integer helpers
        public static byte ReadUInt8(this Stream stream)
        {
            return (byte)stream.ReadByte();
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            byte[] data = new byte[2];
            stream.Read(data, 0, 2);
            return BitConverter.ToUInt16(data, 0);
        }

        public static uint ReadUInt32(this Stream stream)
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, 4);
            return BitConverter.ToUInt32(data, 0);
        }

        public static void WriteUInt8(this Stream stream, byte value)
        {
            stream.WriteByte(value);
        }

        public static void WriteUInt16(this Stream stream, ushort value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 2);
        }

        public static void WriteUInt32(this Stream stream, uint value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 4);
        }

        public static void WriteUInt64(this Stream stream, ulong value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 8);
        }
        #endregion

        #region String helpers
        public static char ReadChar8(this Stream stream)
        {
            return (char)stream.ReadByte();
        }

        public static char ReadChar16(this Stream stream)
        {
            return (char)stream.ReadUInt16();
        }

        public static string ReadAsciiNullTerminatedString(this Stream stream)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = (char)stream.ReadByte();
                if (c == 0)
                    return sb.ToString();
                else
                    sb.Append(c);
            }
        }

        public static int WriteAsciiNullTerminatedString(this Stream stream, string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteByte(0);
            return bytes.Length + 1;
        }

        public static string ReadAsciiString(this Stream stream, int length)
        {
            byte[] bytes = new byte[length];
            stream.Read(bytes, 0, length);
            return Encoding.ASCII.GetString(bytes);
        }

        public static int WriteAsciiString(this Stream stream, string data)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(data);
            stream.Write(bytes, 0, bytes.Length);
            return bytes.Length;
        }
        #endregion

        #region Alignment helpers
        public static void Align(this Stream stream, uint alignment)
        {
            long position = stream.Position;
            long outBy = position % alignment;

            if (outBy == 0)
                return;
            else
            {
                long offset = alignment - outBy;
                stream.Seek(offset, SeekOrigin.Current);
            }
        }
        #endregion

        #region Boolean helpers
        public static bool ReadBoolean(this Stream stream)
        {
            return stream.ReadBoolean(1);
        }

        public static bool ReadBoolean(this Stream stream, int length)
        {
            byte[] data = new byte[length];
            stream.Read(data, 0, length);
            switch (length)
            {
                case 1: return data[0] != 0;
                case 2: return BitConverter.ToUInt16(data, 0) != 0;
                case 4: return BitConverter.ToUInt32(data, 0) != 0;
            }

            throw new NotImplementedException();
        }
        #endregion

        #region StreamReader helpers
        public static char ReadChar(this StreamReader sr)
        {
            return (char)sr.Read();
        }

        public static char PeekChar(this StreamReader sr)
        {
            return (char)sr.Peek();
        }
        #endregion
    }
}