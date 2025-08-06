using System;
using System.IO;
using System.Text;

namespace PythonConsoleControl;

internal class PythonInputStream : Stream
{
    private readonly PythonConsoleProxy _console;
    private readonly Encoding _encoding;
    private byte[] _buffer;
    private int _bufferPos;


    public PythonInputStream(PythonConsoleProxy console, Encoding encoding)
    {
        _console = console;
        _encoding = encoding;
    }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_buffer == null || _bufferPos == _buffer.Length)
        {
            _buffer = null;
            _bufferPos = 0;
            var text = _console.ReadLine()+"\n";
            _buffer = _encoding.GetBytes(text);
        }

        var bytesToCopy = Math.Min(count, _buffer.Length - _bufferPos);
        bytesToCopy = Math.Min(buffer.Length - offset, bytesToCopy);
        Array.Copy(_buffer, _bufferPos, buffer, offset, bytesToCopy);
        _bufferPos += bytesToCopy;
        return bytesToCopy;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length { get; }
    public override long Position { get; set; }
}