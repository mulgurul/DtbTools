using System;
using System.IO;

namespace DtbSynthesizerLibrary.Epub
{
    public class DisposeNotifyingStream : Stream
    {
        public event EventHandler Disposed;

        private bool disposeNotCalledBefore = true;

        private Stream BaseStream { get; }
        public DisposeNotifyingStream(Stream baseStream)
        {
            BaseStream = baseStream;
        }
        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            BaseStream.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return BaseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            BaseStream.Write(buffer, offset, count);
        }

        public override bool CanRead => BaseStream.CanRead;
        public override bool CanSeek => BaseStream.CanSeek;
        public override bool CanWrite => BaseStream.CanWrite;
        public override long Length => BaseStream.Length;
        public override long Position { get; set; }

        protected override void Dispose(bool disposing)
        {
            BaseStream.Dispose();
            base.Dispose(disposing);
            if (disposeNotCalledBefore)
            {
                disposeNotCalledBefore = false;
                Disposed?.Invoke(this, new EventArgs());
            }
        }
    }
}