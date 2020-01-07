using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using App.Metrics;
using Microsoft.Extensions.DependencyInjection;

namespace Frontend
{
    public sealed class MCPacketQueue : IPacketQueue
    {
        private readonly Queue<IWriteablePacket> _toWrite = new Queue<IWriteablePacket>();
        private readonly PipeWriter _writer;
        private readonly IPacketWriterFactory _writerFactory;
        private readonly IMetrics _metrics;
        private readonly IServiceProvider _serviceProvider;

        public bool NeedsWriting => _toWrite.Count != 0;

        public MCPacketQueue(PipeWriter writer, IPacketWriterFactory writerFactory, IMetrics metrics, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _writer = writer;
            _writerFactory = writerFactory;
            _metrics = metrics;
        }

        public void WriteQueued()
        {
            while (_toWrite.TryDequeue(out var packet))
            {
                WritePacketToPipe(packet);
            }
        }

        public void Write(IWriteablePacket packet)
        {
            _toWrite.Enqueue(packet);
        }

        public void WriteImmediate(IWriteablePacket packet)
        {
            WritePacketToPipe(packet);
            _writer.FlushAsync().GetAwaiter().GetResult();
        }

        private void WritePacketToPipe(IWriteablePacket packet)
        {
            var dataSize = CalculateSize(packet) + CountingPacketWriter.GetVarIntSize(packet.Id);
            var packetSize = dataSize + CountingPacketWriter.GetVarIntSize(dataSize);

            var writer = _writerFactory.CreateWriter(_writer.GetMemory(packetSize));
            
            writer.WriteVarInt(dataSize);
            writer.WriteVarInt(packet.Id);
            packet.Write(writer);
            
            _writer.Advance(packetSize);
            _metrics.Measure.Histogram.Update(MetricsRegistry.WritePacketSize, packetSize);
        }

        private int CalculateSize(IWriteablePacket packet)
        {
            var writer = new CountingPacketWriter();
            packet.Write(writer);
            return writer.Size;
        }
        
        private class CountingPacketWriter : IPacketWriter
        {
            public int Size { get; private set; }
            
            public static int GetVarIntSize(int value)
            {
                var size = 0;
                var v = value;
                while ((v & -0x80) != 0)
                {
                    v = (int)(((uint)v) >> 7);
                    size++;
                }

                return size + 1;
            }

            public void WriteVarInt(int value) => Size += GetVarIntSize(value);

            public void WriteString(ReadOnlySpan<char> value)
            {
                WriteVarInt(value.Length);
                Size += value.Length;
            }

            public void WriteBytes(ReadOnlySpan<byte> value)
                => Size += value.Length;

            public void WriteBoolean(bool value)
                => Size += sizeof(byte);

            public void WriteUInt8(byte value)
                => Size += sizeof(byte);

            public void WriteInt8(sbyte value)
                => Size += sizeof(sbyte);

            public void WriteUInt16(ushort value)
                => Size += sizeof(ushort);

            public void WriteInt16(short value)
                => Size += sizeof(short);

            public void WriteUInt32(uint value)
                => Size += sizeof(uint);

            public void WriteInt32(int value)
                => Size += sizeof(int);

            public void WriteUInt64(ulong value)
                => Size += sizeof(ulong);

            public void WriteInt64(long value)
                => Size += sizeof(long);

            public void WriteSingle(float value)
                => Size += sizeof(int);

            public void WriteDouble(double value)
                => Size += sizeof(long);
        }
    }
}