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
        private static ReadOnlySpan<byte> ZeroBytes =>
            new byte[1] { Zero, };
        #endregion
        #region CharFields
        private static ReadOnlySpan<char> EscapeSentinelChars =>
            new char[1] { ((char)EscapeSentinel), };
        private static ReadOnlySpan<char> FieldSeparatorChars =>
            new char[1] { ((char)FieldSeparator), };
        private static ReadOnlySpan<char> LineFeedChars =>
            new char[1] { ((char)LineFeed), };
        private static ReadOnlySpan<char> RecordSeparatorChars =>
            new char[1] { ((char)RecordSeparator), };
        #endregion

        public static void WriteEscapeSentinel(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(EscapeSentinelBytes);
        public static void WriteEscapeSentinel(this IBufferWriter<char> bufferWriter) =>
            bufferWriter.Write(EscapeSentinelChars);
        public static void WriteFieldSeparator(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(FieldSeparatorBytes);
        public static void WriteFieldSeparator(this IBufferWriter<char> bufferWriter) =>
            bufferWriter.Write(FieldSeparatorChars);
        public static void WriteLineFeed(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(LineFeedBytes);
        public static void WriteLineFeed(this IBufferWriter<char> bufferWriter) =>
            bufferWriter.Write(LineFeedChars);
        public static void WriteRecordSeparator(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(RecordSeparatorBytes);
        public static void WriteRecordSeparator(this IBufferWriter<char> bufferWriter) =>
            bufferWriter.Write(RecordSeparatorChars);
        public static void WriteZero(this IBufferWriter<byte> bufferWriter) =>
            bufferWriter.Write(ZeroBytes);
    }
}
