namespace Nexus.Core.Utilities
{
    public static class ByteUtils
    {
        public static byte[]? FloatArrayToBytes(float[]? arr)
        {
            if (arr == null) return null;
            var bytes = new byte[arr.Length * sizeof(float)];
            Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        public static float[]? BytesToFloatArray(object? value)
        {
            if (value == null || value is DBNull) return null;
            var bytes = (byte[])value;
            var floats = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }
    }
}
