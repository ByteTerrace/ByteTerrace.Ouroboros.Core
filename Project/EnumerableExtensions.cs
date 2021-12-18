using Microsoft.Toolkit.HighPerformance;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System.Buffers;
using System.Text;

using static ByteTerrace.Ouroboros.Core.ByteLiteral;

namespace ByteTerrace.Ouroboros.Core
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<ArrayPoolBufferWriter<char>> Decode(
            this IEnumerable<ReadOnlySequence<byte>> source,
            Decoder? decoder = default,
            int initialBufferSize = 4096
        ) {
            if (decoder is null) {
                decoder = Encoding.UTF8.GetDecoder();
            }

            using var decodedBlock = new ArrayPoolBufferWriter<char>(initialCapacity: initialBufferSize);

            foreach (var encodedBlock in source) {
                bool isDecodingCompleted;

                do {
                    decoder.Convert(
                        bytes: in encodedBlock,
                        charsUsed: out _,
                        completed: out isDecodingCompleted,
                        flush: false,
                        writer: decodedBlock
                    );

                    yield return decodedBlock;

                    decodedBlock.Clear();
                } while (!isDecodingCompleted);
            }

            decoder.Convert(
                bytes: in ReadOnlySequence<byte>.Empty,
                charsUsed: out var numberOfCharsUsed,
                completed: out _,
                flush: true,
                writer: decodedBlock
            );

            if (0 < numberOfCharsUsed) {
                yield return decodedBlock;
            }
        }
        public static IEnumerable<MemoryOwner<ReadOnlyMemory<byte>>> ReadDelimitedFields(
            this IEnumerable<ReadOnlyMemory<byte>> source,
            byte delimiter = FieldSeparator
        ) {
            foreach (var yChunk in source) {
                var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
                var xIndices = valueListBuilder.BuildValueList(ref yChunk.Span.DangerousGetReference(), yChunk.Length, delimiter);
                var loopLimit = xIndices.Length;
                var previousIndex = 0;

                using var xChunk = MemoryOwner<ReadOnlyMemory<byte>>.Allocate(size: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices[loopIndex];

                        if (currentIndex != previousIndex) {
                            xChunk.Span[loopIndex] = yChunk[previousIndex..currentIndex];
                        }
                        else {
                            xChunk.Span[loopIndex] = ReadOnlyMemory<byte>.Empty;
                        }

                        previousIndex = (currentIndex + 1);
                    } while (++loopIndex < loopLimit);
                }

                if (previousIndex < yChunk.Span.Length) {
                    xChunk.Span[^1] = yChunk[previousIndex..];
                }
                else {
                    xChunk.Span[^1] = ReadOnlyMemory<byte>.Empty;
                }

                yield return xChunk;
            }
        }
        public static IEnumerable<MemoryOwner<ReadOnlyMemory<char>>> ReadDelimitedFields(
            this IEnumerable<ReadOnlyMemory<char>> source,
            char delimiter = ','
        ) {
            foreach (var yChunk in source) {
                var valueListBuilder = new ValueListBuilder<int>(stackalloc int[64]);
                var xIndices = valueListBuilder.BuildValueList(ref yChunk.Span.DangerousGetReference(), yChunk.Length, delimiter);
                var loopLimit = xIndices.Length;
                var previousIndex = 0;

                using var xChunk = MemoryOwner<ReadOnlyMemory<char>>.Allocate(size: (loopLimit + 1));

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        var currentIndex = xIndices[loopIndex];

                        if (currentIndex != previousIndex) {
                            xChunk.Span[loopIndex] = yChunk[previousIndex..currentIndex];
                        }
                        else {
                            xChunk.Span[loopIndex] = ReadOnlyMemory<char>.Empty;
                        }

                        previousIndex = (currentIndex + 1);
                    } while (++loopIndex < loopLimit);
                }

                if (previousIndex < yChunk.Span.Length) {
                    xChunk.Span[^1] = yChunk[previousIndex..];
                }
                else {
                    xChunk.Span[^1] = ReadOnlyMemory<char>.Empty;
                }

                yield return xChunk;
            }
        }
        public static IEnumerable<ReadOnlyMemory<byte>> ReadDelimitedRecords(
            this IEnumerable<ReadOnlyMemory<byte>> source,
            byte delimiter = RecordSeparator,
            int initialBufferSize = 256
        ) {
            using var buffer = new ArrayPoolBufferWriter<byte>(initialCapacity: initialBufferSize);

            foreach (var chunk in source) {
                var delimiterIndex = chunk.Span.IndexOf(delimiter);
                var offset = 0;

                if (-1 != delimiterIndex) {
                    do {
                        if (0 == buffer.WrittenCount) {
                            yield return chunk.Slice(offset, delimiterIndex);
                        }
                        else {
                            buffer.Write(chunk.Span.Slice(offset, delimiterIndex));

                            yield return buffer.WrittenMemory;

                            buffer.Clear();
                        }

                        offset += (delimiterIndex + 1);
                        delimiterIndex = chunk.Span[offset..].IndexOf(delimiter);
                    } while (-1 != delimiterIndex);
                }

                buffer.Write(chunk.Span[offset..]);
            }
        }
        public static IEnumerable<ReadOnlyMemory<char>> ReadDelimitedRecords(
            this IEnumerable<ReadOnlyMemory<char>> source,
            char delimiter = '\n',
            int initialBufferSize = 256
        ) {
            using var buffer = new ArrayPoolBufferWriter<char>(initialCapacity: initialBufferSize);

            foreach (var chunk in source) {
                var delimiterIndex = chunk.Span.IndexOf(delimiter);
                var offset = 0;

                if (-1 != delimiterIndex) {
                    do {
                        if (0 == buffer.WrittenCount) {
                            yield return chunk.Slice(offset, delimiterIndex);
                        }
                        else {
                            buffer.Write(chunk.Span.Slice(offset, delimiterIndex));

                            yield return buffer.WrittenMemory;

                            buffer.Clear();
                        }

                        offset += (delimiterIndex + 1);
                        delimiterIndex = chunk.Span[offset..].IndexOf(delimiter);
                    } while (-1 != delimiterIndex);
                }

                buffer.Write(chunk.Span[offset..]);
            }
        }
        public static IEnumerable<ArrayPoolBufferWriter<byte>> ToBtdr(
            this IEnumerable<MemoryOwner<ReadOnlyMemory<byte>>> source,
            bool isBinaryFieldSupportEnabled = false
        ) {
            using var fieldBuffer = new ArrayPoolBufferWriter<byte>();
            using var recordBuffer = new ArrayPoolBufferWriter<byte>();

            foreach (var record in source) {
                var fieldMemory = ReadOnlyMemory<byte>.Empty;
                var recordMemory = record.Memory;
                var loopLimit = (recordMemory.Length - 1);

                if (0 < loopLimit) {
                    var loopIndex = 0;

                    do {
                        fieldMemory = recordMemory.Span[loopIndex++];

                        if (0 < fieldMemory.Length) {
                            if (!isBinaryFieldSupportEnabled || (-1 == fieldMemory.Span.IndexOfAny(FieldSeparator, RecordSeparator, EscapeSentinel))) {
                                recordBuffer.Write(fieldMemory.Span);
                            }
                            else {
                                recordBuffer.WriteEscapeSentinel();
                                fieldMemory.Span.CobsEncode(EscapeSentinel, fieldBuffer);
                                recordBuffer.Write(fieldBuffer.WrittenSpan);
                                recordBuffer.WriteEscapeSentinel();
                                fieldBuffer.Clear();
                            }
                        }

                        recordBuffer.WriteFieldSeparator();
                    } while (loopIndex < loopLimit);
                }

                fieldMemory = recordMemory.Span[^1];

                if (0 < fieldMemory.Length) {
                    if (!isBinaryFieldSupportEnabled || (-1 == fieldMemory.Span.IndexOfAny(FieldSeparator, RecordSeparator, EscapeSentinel))) {
                        recordBuffer.Write(fieldMemory.Span);
                    }
                    else {
                        recordBuffer.WriteEscapeSentinel();
                        fieldMemory.Span.CobsEncode(EscapeSentinel, fieldBuffer);
                        recordBuffer.Write(fieldBuffer.WrittenSpan);
                        recordBuffer.WriteEscapeSentinel();
                        fieldBuffer.Clear();
                    }
                }

                recordBuffer.WriteRecordSeparator();

                yield return recordBuffer;

                recordBuffer.Clear();
            }
        }
    }
}
