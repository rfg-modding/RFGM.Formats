namespace RFGM.Formats.Saves.World.Parsing;

//Direct conversion from IDA decompilation of console_md5_mem function
public static class EndBufferHash
{
    public struct Md5Value
    {
        public uint Val0;
        public uint Val1;
        public uint Val2;
        public uint Val3;
    }

    private static readonly uint[] ShiftAmounts =
    [
        7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
        5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
        4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
        6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21
    ];

    private static readonly uint[] IndexPiece =
    [
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        1, 6, 11, 0, 5, 10, 15, 4, 9, 14, 3, 8, 13, 2, 7, 12,
        5, 8, 11, 14, 1, 4, 7, 10, 13, 0, 3, 6, 9, 12, 15, 2,
        0, 7, 14, 5, 12, 3, 10, 1, 8, 15, 6, 13, 4, 11, 2, 9
    ];

    private static readonly uint[] AcValues =
    [
        0xD76AA478, 0xE8C7B756, 0x242070DB, 0xC1BDCEEE, 0xF57C0FAF, 0x4787C62A,
        0xA8304613, 0xFD469501, 0x698098D8, 0x8B44F7AF, 0xFFFF5BB1, 0x895CD7BE,
        0x6B901122, 0xFD987193, 0xA679438E, 0x49B40821, 0xF61E2562, 0xC040B340,
        0x265E5A51, 0xE9B6C7AA, 0xD62F105D, 0x02441453, 0xD8A1E681, 0xE7D3FBC8,
        0x21E1CDE6, 0xC33707D6, 0xF4D50D87, 0x455A14ED, 0xA9E3E905, 0xFCEFA3F8,
        0x676F02D9, 0x8D2A4C8A, 0xFFFA3942, 0x8771F681, 0x6D9D6122, 0xFDE5380C,
        0xA4BEEA44, 0x4BDECFA9, 0xF6BB4B60, 0xBEBFBC70, 0x289B7EC6, 0xEAA127FA,
        0xD4EF3085, 0x04881D05, 0xD9D4D039, 0xE6DB99E5, 0x1FA27CF8, 0xC4AC5665,
        0xF4292244, 0x432AFF97, 0xAB9423A7, 0xFC93A039, 0x655B59C3, 0x8F0CCC92,
        0xFFEFF47D, 0x85845DD1, 0x6FA87E4F, 0xFE2CE6E0, 0xA3014314, 0x4E0811A1,
        0xF7537E82, 0xBD3AF235, 0x2AD7D2BB, 0xEB86D391
    ];

    private static readonly uint[] InitialState = [0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476];

    public static Md5Value ConsoleMd5Mem(byte[] buffer, uint size)
    {
        int v3 = ((byte)size + 1) & 0x3F;
        byte[] padding = new byte[64];
        Array.Clear(padding, 0, padding.Length);

        int v4 = (v3 != 0) ? 64 - v3 : 0;
        uint v5 = (uint)(63 - v4);
        uint v7 = 0;

        if (v5 != 0)
        {
            Array.Copy(buffer, size - v5, padding, 0, v5);
            v3 = ((byte)size + 1) & 0x3F;
            v7 = v5;
        }

        padding[v7] = 0x80;
        BitConverter.GetBytes(8 * size).CopyTo(padding, 60);

        int v9 = (v3 != 0) ? 64 - v3 : 0;
        uint v10 = InitialState[3];
        uint v11 = InitialState[2];
        uint v12 = (size + 1 + (uint)v9) >> 6;
        uint a = InitialState[0];
        uint v13 = InitialState[1];

        Md5Value result = new();
        uint i = 0;
        uint v41 = 0;
        uint bitSize = 0;
        uint v45 = 0;
        uint v44 = 0;
        uint tempa = 0;
        while (i < v12)
        {
            byte[] currentBlock = (i == v12 - 1) ? padding : new byte[64];
            if (i != v12 - 1)
            {
                Array.Copy(buffer, i * 64, currentBlock, 0, 64);
            }

            uint v15 = 2;
            uint v40 = 2;
            int v16 = 0;

            while (v40 < 0x40)
            {
                uint v17 = v15 - 2;
                uint v18;

                if (v17 >= 0x10)
                {
                    if (v17 >= 0x20)
                    {
                        if (v17 >= 0x30)
                            v18 = ~v10 | v13 ^ v11;
                        else
                            v18 = v13 ^ v11 ^ v10;
                    }
                    else
                    {
                        v18 = v11 ^ v10 & (v13 ^ v11);
                    }
                }
                else
                {
                    v18 = v10 ^ v13 & (v11 ^ v10);
                }

                uint temp = v10;
                uint v19 = v13;
                uint v20 = RotateLeft(a + v18 + AcValues[v16] + BitConverter.ToUInt32(currentBlock, (int)(4 * IndexPiece[v16])), (int)ShiftAmounts[v16]);
                uint v21 = v40 - 1;
                uint v22 = v20 + v13;

                uint v23;
                if (v21 >= 0x10)
                {
                    if (v21 >= 0x20)
                    {
                        if (v21 >= 0x30)
                            v23 = ~v11 | v22 ^ v19;
                        else
                            v23 = v22 ^ v19 ^ v11;
                    }
                    else
                    {
                        v23 = v19 ^ v11 & (v22 ^ v19);
                    }
                }
                else
                {
                    v23 = v11 ^ v22 & (v19 ^ v11);
                }

                uint v24 = v19;
                uint aa = v11;
                uint v25 = v22;
                uint v26 = RotateLeft(temp + v23 + AcValues[v16 + 1] + BitConverter.ToUInt32(currentBlock, (int)(4 * IndexPiece[v16 + 1])), (int)ShiftAmounts[v16 + 1]) + v22;

                uint v27;
                if (v40 >= 0x10)
                {
                    if (v40 >= 0x20)
                    {
                        if (v40 >= 0x30)
                            v27 = ~v19 | v26 ^ v25;
                        else
                            v27 = v26 ^ v25 ^ v19;
                    }
                    else
                    {
                        v27 = v25 ^ v19 & (v26 ^ v25);
                    }
                }
                else
                {
                    v27 = v19 ^ v26 & (v25 ^ v19);
                }

                uint v28 = RotateLeft(aa + v27 + AcValues[v16 + 2] + BitConverter.ToUInt32(currentBlock, (int)(4 * IndexPiece[v16 + 2])), (int)ShiftAmounts[v16 + 2]);
                uint ab = v24;
                v10 = v26;
                uint v29 = v40 + 1;
                uint v30 = v28 + v26;

                uint v31;
                if (v29 >= 0x10)
                {
                    if (v29 >= 0x20)
                    {
                        if (v29 >= 0x30)
                            v31 = ~v25 | v30 ^ v10;
                        else
                            v31 = v30 ^ v10 ^ v25;
                    }
                    else
                    {
                        v31 = v10 ^ v25 & (v30 ^ v10);
                    }
                }
                else
                {
                    v31 = v25 ^ v30 & (v10 ^ v25);
                }

                tempa = v25;
                uint v32 = RotateLeft(ab + v31 + AcValues[v16 + 3] + BitConverter.ToUInt32(currentBlock, (int)(4 * IndexPiece[v16 + 3])), (int)ShiftAmounts[v16 + 3]);
                a = v25;
                v15 = v40 + 4;
                v11 = v30;
                v13 = v32 + v30;
                v40 += 4;
                v16 += 4;
            }

            v44 += tempa;
            result.Val0 = v44;
            v45 += v13;
            result.Val1 = v45;
            bitSize += v11;
            result.Val2 = bitSize;
            v41 += v10;
            result.Val3 = v41;
            i++;
        }

        return result;
    }

    private static uint RotateLeft(uint value, int shift)
    {
        return (value << shift) | (value >> (32 - shift));
    }
}