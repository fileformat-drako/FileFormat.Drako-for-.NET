using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Openize.Draco.Utils
{
    /// <summary>
    /// Wraps a byte array as int array
    /// </summary>
    sealed class IntArray : IEnumerable<int>
    {

        class IntArrayEnumerator : IEnumerator<int>
        {
            private int idx = 0;
            private int current;
            private IntArray array;
            public int Current => current;

            object IEnumerator.Current => current;

            public IntArrayEnumerator(IntArray array)
            {
                this.array = array;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (idx >= array.length)
                    return false;
                current = array[idx++];
                return true;
            }

            public void Reset()
            {
                idx = -1;
            }
        }

        private readonly int length;
        internal readonly byte[] data;
        private readonly int offset;
        public int Length => length;

        public static readonly IntArray Empty = IntArray.Wrap(new byte[0]);

        public static IntArray Array(int length)
        {
            return new IntArray(new byte[length * 4], 0, length);
        }

        public static IntArray Wrap(byte[] data)
        {
            return Wrap(data, 0, data.Length);
        }
        public static IntArray Wrap(byte[] data, int offset, int bytes)
        {
            return new IntArray(data, offset, bytes >> 2);
        }

        private IntArray(byte[] data, int offset, int length)
        {
            this.length = length;
            this.data = data;
            this.offset = 0;
        }

        public byte[] GetBuffer()
        {
            return data;
        }
        public int ByteOffset => offset * 4;

        public int this[int idx]
        {
            get
            {
                int pos = offset + idx * 4;
                return (int) Unsafe.GetLE32(data, pos);
            }
            set
            {
                int pos = offset + idx * 4;
                Unsafe.PutLE32(data, pos, (uint) value);

            }
        }

        public IEnumerator<int> GetEnumerator()
        {
            return new IntArrayEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public static IntArray operator +(IntArray array, int offset)
        {
            if (offset == 0)
                return array;
            return new IntArray(array.data, array.offset + offset, array.length - offset);
        }

        /// <summary>
        /// Copy the content to new array with specified size
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="elements"></param>
        public void CopyTo(IntArray dst, int elements)
        {
            System.Array.Copy(data, dst.data, elements * 4);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("[{0}]", Length);
            sb.Append("{");
            int valsToShow = Math.Min(length, 10);
            for (int i = 0; i < valsToShow; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(this[i]);
            }

            if (length > valsToShow)
                sb.Append("...");
            sb.Append("}");
            return sb.ToString();
        }

        public static IntArray ArrayWithValue(int size, int val)
        {
            IntArray ret = Array(size);
            for (int i = 0; i < size; i++)
                ret[i] = val;
            return ret;
        }

        public static IntArray Resize(IntArray arr, int newSize)
        {
            if (arr != null && arr.Length == newSize)
                return arr;//no need to resize
            Debug.Assert(newSize >= 0);
            IntArray newArr = IntArray.Array(newSize);
            if (newSize != 0 && arr != null && arr.Length > 0)
            {
                //Move old data
                arr.CopyTo(newArr, Math.Min(arr.Length, newSize));
            }
            return newArr;
        }
    }
}
