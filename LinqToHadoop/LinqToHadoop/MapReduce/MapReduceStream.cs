using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace LinqToHadoop.MapReduce
{
    public class MapReduceStream : Stream
    {
        public enum Mode : byte {
            WritingKey,
            WritingValue,
            ReadingKey,
            ReadingValue,
            Raw,
        }

        private readonly Stream _stream;
        private readonly Lazy<BinaryWriter> _binaryWriter;
        private readonly Lazy<StreamWriter> _streamWriter;
        //private readonly Lazy<BinaryReader> _binaryReader;

        public Serialization.Config Config { get; private set; }
        public Mode WriteMode { get; private set; }
        public BinaryWriter BinaryWriter { get { return this._binaryWriter.Value; } }
        public StreamWriter StreamWriter { get { return this._streamWriter.Value; } }

        public MapReduceStream(Stream stream, Serialization.Config config)
        {
            this.WriteMode = Mode.WritingKey;
            this._stream = stream;
            this.Config = config;

            this._binaryWriter = new Lazy<BinaryWriter>(() => new BinaryWriter(this, this.Config.Encoding));
            this._streamWriter = new Lazy<StreamWriter>(() => new StreamWriter(this, this.Config.Encoding));
        }

        public void FinishKey()
        {
            Throw.InvalidIf(this.WriteMode != Mode.WritingKey);
            this.WriteMode = Mode.Raw;                
            if (!this.Config.SkipValue)
            {
                this.StreamWriter.Write(this.Config.KeyValueSeparator);
                this.WriteMode = Mode.WritingValue;
            }
            else
            {
                this.StreamWriter.Write(Environment.NewLine);
                this.WriteMode = Mode.WritingKey;
            }
        }

        public void FinishValue()
        {
            Throw.InvalidIf(this.WriteMode != Mode.WritingValue);
            this.WriteMode = Mode.Raw;
            this.StreamWriter.Write(Environment.NewLine);
            this.WriteMode = this.Config.SkipKey
                ? Mode.WritingValue
                : Mode.WritingKey;
        }

        #region ---- Stream Implementation ----
        public override bool CanRead
        {
            get { return this._stream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return this._stream.CanWrite; }
        }

        public override void Flush()
        {
            this._stream.Flush();
        }

        public override long Length
        {
            get { return this._stream.Length; }
        }

        private long _positionOffset;

        public override long Position
        {
            get
            {
                return this._stream.Position + this._positionOffset;
            }
            set
            {
                throw Throw.ShouldNeverGetHere("Cannot set the position of a " + typeof(MapReduceStream).Name);
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Throw.InvalidIf(!this.CanRead, "Not a read stream!");
            switch (this.WriteMode)
            {
                case Mode.Raw:
                    return this._stream.Read(buffer, offset, count);
                default:
                    throw Throw.ShouldNeverGetHere("Unexpected read mode " + this.WriteMode);
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw Throw.ShouldNeverGetHere("Cannot set the position of a " + typeof(MapReduceStream).Name);
        }

        public override void SetLength(long value)
        {
            throw Throw.ShouldNeverGetHere("Cannot set the length of a " + typeof(MapReduceStream).Name);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Throw.InvalidIf(!this.CanRead, "Not a write stream!");
            switch (this.WriteMode)
            {
                case Mode.Raw:
                    this._stream.Write(buffer, offset, count);
                    break;
                default:
                    throw Throw.ShouldNeverGetHere("Unexpected write mode " + this.WriteMode);
            }
        }
        #endregion
    }
}
