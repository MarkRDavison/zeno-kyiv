namespace mark.davison.common.Utility;

public static class GuidUtilities
{
    public static Guid CombineTwoGuids(Guid guid1, Guid guid2)
    {
        const int BYTECOUNT = 16;
        byte[] destByte = new byte[BYTECOUNT];
        byte[] guid1Byte = guid1.ToByteArray();
        byte[] guid2Byte = guid2.ToByteArray();

        for (int i = 0; i < BYTECOUNT; i++)
        {
            destByte[i] = (byte)(guid1Byte[i] ^ guid2Byte[i]);
        }
        return new Guid(destByte);
    }
}
