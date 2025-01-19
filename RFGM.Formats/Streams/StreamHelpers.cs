// Originally based on code from ThomasJepp.SaintsRow:
/*
 *  ThomasJepp.SaintsRow - Saints Row IV and Gat Out Of Hell tools
 *  Copyright (c) 2013-2016, Thomas Jepp
 *  All rights reserved.
 *  https://github.com/saintsrowmods/ThomasJepp.SaintsRow
*/

// Enum helpers from Gibbed.IO:
/* Copyright (c) 2017 Rick (rick 'at' gibbed 'dot' us)
 * 
 *  This software is provided 'as-is', without any express or implied
 *  warranty. In no event will the authors be held liable for any damages
 *  arising from the use of this software.
 *  
 *  Permission is granted to anyone to use this software for any purpose,
 *  including commercial applications, and to alter it and redistribute it
 *  freely, subject to the following restrictions:
 *  
 *  1. The origin of this software must not be misrepresented; you must not
 *     claim that you wrote the original software. If you use this software
 *     in a product, an acknowledgment in the product documentation would
 *     be appreciated but is not required.
 * 
 *  2. Altered source versions must be plainly marked as such, and must not
 *     be misrepresented as being the original software.
 * 
 *  3. This notice may not be removed or altered from any source
 *     distribution.
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

        public static long ReadInt64(this Stream stream)
        {
            byte[] data = new byte[8];
            stream.Read(data, 0, 8);
            return BitConverter.ToInt64(data, 0);
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

        public static void WriteInt64(this Stream stream, long value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 8);
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

        public static ulong ReadUInt64(this Stream stream)
        {
            byte[] data = new byte[8];
            stream.Read(data, 0, 8);
            return BitConverter.ToUInt64(data, 0);
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

        #region Float helpers
        public static float ReadFloat(this Stream stream)
        {
            byte[] data = new byte[4];
            stream.Read(data, 0, 4);
            return BitConverter.ToSingle(data, 0);
        }

        public static void WriteFloat(this Stream stream, float value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 4);
        }

        public static double ReadDouble(this Stream stream)
        {
            byte[] data = new byte[8];
            stream.Read(data, 0, 8);
            return BitConverter.ToDouble(data, 0);
        }

        public static void WriteDouble(this Stream stream, double value)
        {
            byte[] data = BitConverter.GetBytes(value);
            stream.Write(data, 0, 8);
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

        public static long AlignRead(this Stream stream, long alignment)
        {
            long paddingSize = stream.CalcAlignment(alignment);
            stream.Seek(paddingSize, SeekOrigin.Current);
            return paddingSize;
        }

        public static long AlignWrite(this Stream stream, long alignment)
        {
            long paddingSize = stream.CalcAlignment(alignment);
            for (int i = 0; i < paddingSize; i++)
            {
                stream.WriteByte(0);
            }
            return paddingSize;
        }

        public static long CalcAlignment(this Stream stream, long alignment)
        {
            return StreamHelpers.CalcAlignment(stream.Position, alignment);
        }

        public static long CalcAlignment(long position, long alignment)
        {
            long remainder = position % alignment;
            long paddingSize = remainder > 0 ? alignment - remainder : 0;
            return paddingSize;
        }

        public static void Skip(this Stream stream, long bytesToSkip)
        {
            stream.Seek(bytesToSkip, SeekOrigin.Current);
        }
        #endregion

        #region Boolean helpers
        public static bool ReadBoolean(this Stream stream)
        {
            return stream.ReadBoolean8();
        }

        public static void WriteBoolean(this Stream stream, bool value)
        {
            stream.WriteBoolean8(value);
        }

        public static bool ReadBoolean8(this Stream stream)
        {
            return stream.ReadUInt8() != 0;
        }

        public static void WriteBoolean8(this Stream stream, bool value)
        {
            stream.WriteUInt8((byte)(value ? 1 : 0));
        }
        public static bool ReadBoolean16(this Stream stream)
        {
            return stream.ReadUInt16() != 0;
        }
        public static void WriteBoolean16(this Stream stream, bool value)
        {
            stream.WriteUInt16((ushort)(value ? 1 : 0));
        }

        public static bool ReadBoolean32(this Stream stream)
        {
            return stream.ReadUInt32() != 0;
        }

        public static void WriteBoolean32(this Stream stream, bool value)
        {
            stream.WriteUInt32((uint)(value ? 1 : 0));
        }
        #endregion

        #region Byte helpers

        public static byte[] ReadBytes(this Stream stream, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            var data = new byte[length];
            var read = stream.Read(data, 0, length);
            if (read != length)
            {
                throw new EndOfStreamException();
            }
            return data;
        }

        public static void WriteBytes(this Stream stream, byte[] data)
        {
            stream.Write(data, 0, data.Length);
        }

        public static void WriteNullBytes(this Stream stream, long numBytes)
        {
            byte[] bytes = new byte[numBytes];
            stream.Write(bytes);
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

        #region Enum helpers
        private static class EnumTypeCache
        {

            private static TypeCode TranslateType(Type type)
            {
                if (type.IsEnum == true)
                {
                    var underlyingType = Enum.GetUnderlyingType(type);
                    var underlyingTypeCode = Type.GetTypeCode(underlyingType);

                    switch (underlyingTypeCode)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                            {
                                return underlyingTypeCode;
                            }
                    }
                }

                throw new ArgumentException("unknown enum type", "type");
            }

            public static TypeCode Get(Type type)
            {
                return TranslateType(type);
            }
        }

        public static T ReadValueEnum<T>(this Stream stream)
        {
            var type = typeof(T);

            object value;
            switch (EnumTypeCache.Get(type))
            {
                case TypeCode.SByte:
                    {
                        value = stream.ReadInt8();
                        break;
                    }

                case TypeCode.Byte:
                    {
                        value = stream.ReadUInt8();
                        break;
                    }

                case TypeCode.Int16:
                    {
                        value = stream.ReadInt16();
                        break;
                    }

                case TypeCode.UInt16:
                    {
                        value = stream.ReadUInt16();
                        break;
                    }

                case TypeCode.Int32:
                    {
                        value = stream.ReadInt32();
                        break;
                    }

                case TypeCode.UInt32:
                    {
                        value = stream.ReadUInt32();
                        break;
                    }

                case TypeCode.Int64:
                    {
                        value = stream.ReadInt64();
                        break;
                    }

                case TypeCode.UInt64:
                    {
                        value = stream.ReadUInt64();
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }

            return (T)Enum.ToObject(type, value);
        }

        public static void WriteValueEnum<T>(this Stream stream, object value)
        {
            var type = typeof(T);
            switch (EnumTypeCache.Get(type))
            {
                case TypeCode.SByte:
                    {
                        stream.WriteInt8((sbyte)value);
                        break;
                    }

                case TypeCode.Byte:
                    {
                        stream.WriteUInt8((byte)value);
                        break;
                    }

                case TypeCode.Int16:
                    {
                        stream.WriteInt16((short)value);
                        break;
                    }

                case TypeCode.UInt16:
                    {
                        stream.WriteUInt16((ushort)value);
                        break;
                    }

                case TypeCode.Int32:
                    {
                        stream.WriteInt32((int)value);
                        break;
                    }

                case TypeCode.UInt32:
                    {
                        stream.WriteUInt32((uint)value);
                        break;
                    }

                case TypeCode.Int64:
                    {
                        stream.WriteInt64((long)value);
                        break;
                    }

                case TypeCode.UInt64:
                    {
                        stream.WriteUInt64((ulong)value);
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }
        #endregion

        public static List<string> ReadSizedStringList(this Stream stream, long sizeBytes)
        {
            List<string> strings = new();
            if (sizeBytes < 0)
                return strings;
            
            long startPos = stream.Position;
            while (stream.Position - startPos < sizeBytes)
            {
                strings.Add(stream.ReadAsciiNullTerminatedString());

                //Skip extra null terminators that are sometimes present in these lists in RFG formats
                while (stream.Position - startPos < sizeBytes)
                {
                    if (stream.Peek<char>() == '\0')
                        stream.Skip(1);
                    else
                        break;
                }
            }
            
            return strings;
        }

        public static T Peek<T>(this Stream stream) where T : unmanaged
        {
            long startPos = stream.Position;
            T result = stream.ReadStruct<T>();
            stream.Seek(startPos, SeekOrigin.Begin);
            return result;
        }
    }
}