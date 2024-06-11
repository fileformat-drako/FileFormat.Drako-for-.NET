using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace System
{
#if NET46
    static class MemoryExtensions
    {
        public static String GetString(this Encoding encoding, Span<byte> bytes)
        {
            return encoding.GetString(bytes.array, bytes.begin, bytes.length);
        }

        public static void Write(this Stream stream, Span<byte> bytes)
        {
            stream.Write(bytes.array, bytes.begin, bytes.length);
        }
        public static int Read(this Stream stream, Span<byte> bytes)
        {
            return stream.Read(bytes.array, bytes.begin, bytes.length);
        }
        public static Span<byte> AsSpan(this byte[] bytes)
        {
            return new Span<byte>(bytes, 0, bytes.Length);
        }
    }
    class MemoryMarshal
    {
        public static Span<TDest> Cast<TSrc, TDest>(Span<TSrc> src) where TDest : struct where TSrc : struct
        {
            if(src is Span<byte>)
            {
                var bytes = (Span<byte>)(object)(src);
                var size = System.Runtime.InteropServices.Marshal.SizeOf<TDest>();
                var offset = bytes.begin / size;
                var count = bytes.length / size;
                return new Span<TDest>(null, bytes.array, offset, count);
            }
            throw new NotSupportedException();
        }

    }
    public struct Span<T>
    {
        public static readonly Span<T> Empty = new Span<T>(null, 0, 0);
        internal T[] array;
        internal byte[] bytes;
        internal int begin;
        internal int length;
        public Span(T[] array, int offset, int length)
        {
            this.array = array;
            this.bytes = null;
            this.begin = offset;
            this.length = length;
        }
        public Span(T[] array, byte[] bytes, int offset, int length)
        {
            this.array = array;
            this.bytes = bytes;
            this.begin = offset;
            this.length = length;

        }
        public void CopyTo(Span<T> span)
        {
            for(int i = 0; i < this.length; i++)
            {
                span[i] = this[i];
            }
        }
        public void CopyTo(T[] array)
        {
            if (this.bytes != null)
            {
                Buffer.BlockCopy(bytes, begin * Marshal.SizeOf<T>(), array, 0, bytes.Length);
            }
            else if(this.array != null)
            {
                Array.Copy(this.array, this.begin, array, 0, length);
            }
        }

        public static implicit operator Span<T>(T[] arr)
        {
            return new Span<T>(arr, 0, arr.Length);
        }
        public bool SequenceEqual(Span<T> span)
        {
            if (this.length != span.length)
                return false;
            for(int i = 0; i < span.length; i++)
            {
                if (!this[i].Equals(span[i]))
                    return false;
            }
            return true;
        }

        public static bool operator == (Span<T> a, Span<T> b)
        {
            return a.Equals(b);
        }
        public static bool operator != (Span<T> a, Span<T> b)
        {
            return !a.Equals(b);
        }
        public override bool Equals(object obj)
        {
            if (!(obj is Span<T>))
                return false;
            var rhs = (Span<T>)obj;
            return array == rhs.array && begin == rhs.begin && length == rhs.length && bytes == rhs.bytes;
        }

        public int Length => length;

        public unsafe T this[int index]
        {
            get
            {
                if (array != null)
                    return array[begin + index];
                else if (bytes != null)
                {
                    fixed (byte* p = bytes)
                    {
                        return ((T*)p)[begin + index];
                    }
                }
                else
                    throw new NullReferenceException();
            }
            set
            {
                if(array != null)
                    array[begin + index] = value;
                else if(bytes != null)
                {
                    fixed(byte* p = bytes )
                    {
                        ((T*)p)[begin + index] = value;
                    }
                }
                else
                    throw new NullReferenceException();
            }
        }

        public Span<T> Slice(int offset)
        {
            return new Span<T>(array, bytes, this.begin + offset, length - offset);
        }
        public Span<T> Slice(int offset, int size)
        {
            return new Span<T>(array, bytes, this.begin + offset, size);
        }
    }
    internal class BitOperations
    {
        public static int LeadingZeroCount(ulong x) {

            // Do the smearing which turns (for example)
            // this: 0000 0101 0011
            // into: 0000 0111 1111
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            x |= x >> 32;

            // Count the ones
            x -= x >> 1 & 0x5555555555555555;
            x = (x >> 2 & 0x3333333333333333) + (x & 0x3333333333333333);
            x = (x >> 4) + x & 0x0f0f0f0f0f0f0f0f;
            x += x >> 8;
            x += x >> 16;
            x += x >> 32;

            const int numLongBits = sizeof(long) * 8; // compile time constant
            return (int)(numLongBits - (uint)(x & 0x0000007f)); // subtract # of 1s from 64
        }
    }
#endif
}
