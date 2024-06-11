﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Decoder;
using Openize.Draco.Utils;

namespace Openize.Draco
{

    /// <summary>
    /// Class for decoding a sequence of bits that were encoded with RAnsBitEncoder.
    /// </summary>
    class RAnsBitDecoder : RAnsBitCodec, IBitDecoder
    {
        private BytePointer buf;
        private int offset;
        private uint state;
        private byte probZero;


        /// <summary>
        /// Sets |sourceBuffer| as the buffer to decode bits from.
        /// Returns false when the data is invalid.
        /// </summary>
        public bool StartDecoding(DecoderBuffer sourceBuffer)
        {
            Clear();

            if (!sourceBuffer.Decode(out probZero))
                return DracoUtils.Failed();

            uint sizeInBytes;
            if (sourceBuffer.BitstreamVersion < 22)
            {
                if (!sourceBuffer.Decode(out sizeInBytes))
                    return DracoUtils.Failed();
            }
            else
            {
                if (!Decoding.DecodeVarint(out sizeInBytes, sourceBuffer))
                    return DracoUtils.Failed();
            }

            if (sizeInBytes > sourceBuffer.RemainingSize)
                return DracoUtils.Failed();

            if (ANSReadInit(sourceBuffer.Pointer + sourceBuffer.DecodedSize, (int)sizeInBytes) != 0)
                return DracoUtils.Failed();
            sourceBuffer.Advance((int)sizeInBytes);
            return true;

        }

        private int ANSReadInit(BytePointer buf, int offset)
        {
            int x;
            if (offset < 1)
                return 1;
            this.buf = buf;
            x = buf[offset - 1] >> 6;
            if (x == 0)
            {
                this.offset = offset - 1;
                state = (uint) (buf[this.offset - 1] & 0x3F);
            }
            else if (x == 1)
            {
                if (offset < 2)
                    return 1;
                this.offset = offset - 2;
                state = (uint)buf.ToUInt16LE(this.offset) & 0x3FFF;
            }
            else if (x == 2)
            {
                if (offset < 3)
                    return 1;
                this.offset = offset - 3;
                state = buf.ToUInt24LE(this.offset) & 0x3FFFFF;
            }
            else
            {
                // x == 3 implies this byte is a superframe marker
                return 1;
            }
            state += lBase;
            if (state >= lBase*ioBase)
                return 1;
            return 0;
        }

        bool RabsRead(int p0)
        {
            bool val;
            uint quot, rem, x, xn;
            int p = ansP8Precision - p0;
            if (state < lBase)
            {
                state = state*ioBase + buf[--offset];
            }
            x = state;
            quot = x/ansP8Precision;
            rem = x%ansP8Precision;
            xn = (uint) (quot*p);
            val = (rem < p);
            if (val)
            {
                state = xn + rem;
            }
            else
            {
                // ans->state = quot * p0 + rem - p;
                state = (uint) (x - xn - p);
            }
            return val;
        }


        /// <summary>
        /// Decode one bit. Returns true if the bit is a 1, otherwsie false.
        /// </summary>
        public bool DecodeNextBit()
        {
            return RabsRead(probZero);
        }

        /// <summary>
        /// Decode the next |nbits| and return the sequence in |value|. |nbits| must be
        /// &gt; 0 and &lt;= 32.
        /// </summary>
        public uint DecodeLeastSignificantBits32(int nbits)
        {
            Debug.Assert(nbits <= 32);
            Debug.Assert(nbits > 0);

            uint result = 0;
            while (nbits > 0)
            {
                result = (uint) ((result << 1) + (DecodeNextBit() ? 1 : 0));
                --nbits;
            }
            return result;
        }

        public void EndDecoding()
        {
        }

        public void Clear()
        {
            state = lBase;
        }
    }
}
