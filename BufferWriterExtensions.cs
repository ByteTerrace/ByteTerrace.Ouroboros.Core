using System.Buffers;

using static ByteTerrace.Ouroboros.Core.Byte;

namespace ByteTerrace.Ouroboros.Core
{
    public static class BufferWriterExtensions
    {
        #region ByteFields
        private static ReadOnlySpan<byte> EscapeSentinelBytes =>
            new byte[1] { EscapeSentinel, };
        private static ReadOnlySpan<byte> FieldSeparatorBytes =>
            new byte[1] { FieldSeparator, };
        private static ReadOnlySpan<byte> LineFeedBytes =>
            new byte[1] { LineFeed, };
        private static ReadOnlySpan<byte> RecordSeparatorBytes =>
            new byte[1] { RecordSeparator, };
        #endregion

        public static void WriteEscapeSentinel(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(EscapeSentinelBytes);
        public static void WriteFieldSeparator(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(FieldSeparatorBytes);
        public static void WriteLineFeed(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(LineFeedBytes);
        public static void WriteRecordSeparator(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(RecordSeparatorBytes);
    }
}
