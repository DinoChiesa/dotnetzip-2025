#if !NET6_0_OR_GREATER
namespace System.IO
{
    public static class StreamExtensions
    {
        public static void ReadExactly(this Stream stream, byte[] buffer, int offset, int count)
        {
            int bytesRead = stream.Read(buffer, offset, count);
            if (bytesRead != count) {
                throw new System.IO.IOException("unable to read required bytes");
            }
        }
    }
}
#endif
