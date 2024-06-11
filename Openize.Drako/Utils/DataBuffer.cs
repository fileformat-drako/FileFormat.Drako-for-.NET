﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Draco.Utils
{
    /// <summary>
    /// The differences between <see cref="DataBuffer"/> and <see cref="RawStreamReader"/> is that this encapsulates the random access of binary data.
    /// </summary>
#if DRACO_EMBED_MODE
    internal
#else
    public
#endif
    class DataBuffer
    {
        private int version;//change versions
        private byte[] data;
        private int length;
        private readonly bool extendable;
        public int Version { get { return version; } }


        public int Capacity
        {
            get { return data == null ? 0 : data.Length; }
            set { EnsureCapacity(value);}
        }

        public DataBuffer()
        {
            extendable = true;

        }

        public DataBuffer(byte[] data)
        {
            this.data = data;
            extendable = false;
        }
        public DataBuffer(Span<byte> data)
        {
            this.data = new byte[data.Length];
            this.length = data.Length;
            data.CopyTo(this.data);
            extendable = false;
        }

        public void Write(int offset, byte[] data, int len)
        {
            Debug.Assert(data != null);
            Debug.Assert(len <= data.Length);
            Write(offset, data, 0, len);
        }

        public void Write(int offset, byte val)
        {
            Length = offset + 1;
            this.data[offset] = val;
        }

        public void Write(int offset, short val)
        {
            Write(offset, (ushort) val);
        }
        public void Write(int offset, ushort val)
        {
            Length = offset + 2;
            Unsafe.PutLE16(this.data, offset, val);
        }
        public void Write(int offset, uint val)
        {
            Length = offset + 4;
            Unsafe.PutLE32(this.data, offset, val);
        }
        public void Write(int offset, int val)
        {
            Write(offset, (uint) val);
        }
        public void Write(int offset, float val)
        {
            Length = offset + 4;
            var uval = Unsafe.FloatToUInt32(val);
            Unsafe.PutLE32(this.data, offset, uval);
        }

        public void Write(int offset, float[] data)
        {
            Length = offset + data.Length * 4;
            Unsafe.ToByteArray(data, 0, data.Length, this.data, offset);
        }
        public void Write(int offset, byte[] data)
        {
            Debug.Assert(data != null);
            Write(offset, data, 0, data.Length);
        }
        public void Write(int offset, byte[] data, int start, int len)
        {
            Debug.Assert(data != null);
            Debug.Assert(len + start <= data.Length);
            Debug.Assert(start >= 0);
            Debug.Assert(offset >= 0);
            version++;
            Length = offset + len;
            Array.Copy(data, start, this.data, offset, len);
        }

        public int Read(int offset, byte[] result)
        {
            return Read(offset, result, 0, result.Length);
        }
        public int Read(int offset, byte[] result, int len)
        {
            return Read(offset, result, 0, len);
        }
        public int Read(int offset, byte[] result, int start, int len)
        {
            Debug.Assert(result != null);
            Debug.Assert(offset+len <= this.data.Length);
            Debug.Assert(start+len <= result.Length);
            Debug.Assert(len <= result.Length);
            Array.Copy(data, offset, result, start, len);
            return len;
        } 
        public float ReadFloat(int offset)
        {
            return BitConverter.ToSingle(data, offset);
        }
        public int ReadInt(int offset)
        {
            return (int)Unsafe.GetLE32(data, offset);
            //return BitConverter.ToInt32(data, offset);
        }
        public byte this[int offset]
        {
            get { return data[offset]; }
            set { data[offset] = value; }
        }
        private void EnsureCapacity(int cap)
        {
            if (data != null && cap <= data.Length)
                return;
            if(!extendable)
                throw new InvalidOperationException("Cannot extend the fixed-length data buffer.");
            int newCap = data == null ? 0 : data.Length;
            while (newCap < cap)
                newCap += 1024;
            Array.Resize(ref this.data, newCap);
        }

        public void Clear()
        {
            Length = 0;
        }
        public int Length
        {
            get { return length; }
            set
            {
                length = value;
                EnsureCapacity(value);
            }
        }
        public byte[] GetBuffer()
        {
            return data;
        }

        public byte[] ToArray()
        {
            byte[] ret = new byte[length];
            Array.Copy(data, ret, length);
            return ret;
        }

        public override string ToString()
        {
            return string.Format("Length={0}, Capacity = {1}", length, Capacity);
        }

        internal IntArray AsIntArray()
        {
            return IntArray.Wrap(data, 0, length);
        }
    }
}
