using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Drako.Utils;

namespace Openize.Drako.Decoder
{
    sealed class Decoding
    {

        internal static bool DecodeSymbols(int numValues, int numComponents,
            DecoderBuffer srcBuffer, Span<int> outValues)
        {
            if (numValues < 0)
                return DracoUtils.Failed();
            if (numValues == 0)
                return true;
            // Decode which scheme to use.
            byte scheme;
            if (!srcBuffer.Decode(out scheme))
                return DracoUtils.Failed();
            if (scheme == 0)
            {
                return DecodeTaggedSymbols(numValues, numComponents,
                    srcBuffer, outValues);
            }
            else if (scheme == 1)
            {
                return DecodeRawSymbols(numValues, srcBuffer,
                    outValues);
            }
            return DracoUtils.Failed();
        }

        static bool DecodeTaggedSymbols(int numValues, int numComponents,
            DecoderBuffer srcBuffer, Span<int> outValues)
        {
            // Decode the encoded data.
            RAnsSymbolDecoder tagDecoder = new RAnsSymbolDecoder(5);
            if (!tagDecoder.Create(srcBuffer))
                return DracoUtils.Failed();

            if (!tagDecoder.StartDecoding(srcBuffer))
                return DracoUtils.Failed();

            if (numValues > 0 && tagDecoder.NumSymbols == 0)
                return DracoUtils.Failed(); // Wrong number of symbols.

            // srcBuffer now points behind the encoded tag data (to the place where the
            // values are encoded).
            long tmp;
            srcBuffer.StartBitDecoding(false, out tmp);
            int valueId = 0;
            for (int i = 0; i < numValues; i += numComponents)
            {
                // Decode the tag.
                int bitLength = tagDecoder.DecodeSymbol();
                // Decode the actual value.
                for (int j = 0; j < numComponents; ++j)
                {
                    uint val;
                    if (!srcBuffer.DecodeLeastSignificantBits32(bitLength, out val))
                        return DracoUtils.Failed();
                    outValues[valueId++] = (int) val;
                }
            }
            tagDecoder.EndDecoding();
            srcBuffer.EndBitDecoding();
            return true;
        }

        static bool DecodeRawSymbols(int numValues, DecoderBuffer srcBuffer,
            Span<int> outValues)
        {
            byte maxBitLength;
            if (!srcBuffer.Decode(out maxBitLength))
                return DracoUtils.Failed();

            RAnsSymbolDecoder decoder = new RAnsSymbolDecoder(maxBitLength);
            if (!decoder.Create(srcBuffer))
                return DracoUtils.Failed();

            if (numValues > 0 && decoder.NumSymbols == 0)
                return DracoUtils.Failed(); // Wrong number of symbols.

            if (!decoder.StartDecoding(srcBuffer))
                return DracoUtils.Failed();
            for (int i = 0; i < numValues; ++i)
            {
                // Decode a symbol into the value.
                int value = decoder.DecodeSymbol();
                outValues[i] = value;
            }
            decoder.EndDecoding();
            return true;
        }

        public static void ConvertSymbolsToSignedInts(Span<int> symbols, Span<int> result)
        {
            for (int i = 0; i < symbols.Length; ++i)
            {
                int val = symbols[i];
                bool isNegative = (val & 1) != 0;//lowest bit is sign bit
                val >>= 1;
                int ret = val;
                if (isNegative)
                    ret = -ret - 1;
                result[i] = ret;
            }
        }

        /// <summary>
        /// Decodes a specified integer as varint. Note that the IntTypeT must be the
        /// same as the one used in the corresponding EncodeVarint() call.
        /// </summary>
        /// <param name="out_val"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static bool DecodeVarint(out uint out_val, DecoderBuffer buffer)
        {
            // Coding of unsigned values.
            // 0-6 bit - data
            // 7 bit - next byte?
            byte in_;
            if (!buffer.Decode(out in_))
            {
                out_val = 0;
                return DracoUtils.Failed();
            }

            if ((in_ & (1 << 7)) != 0)
            {
                // Next byte is available, decode it first.
                if (!DecodeVarint(out out_val, buffer))
                    return DracoUtils.Failed();
                // Append decoded info from this byte.
                out_val <<= 7;
                out_val |= (uint)(in_ & ((1 << 7) - 1));
            }
            else
            {
                // Last byte reached
                out_val = in_;
            }
            return true;
        }
        public static bool DecodeVarint(out ushort out_val, DecoderBuffer buffer)
        {
            // Coding of unsigned values.
            // 0-6 bit - data
            // 7 bit - next byte?
            byte in_;
            if (!buffer.Decode(out in_))
            {
                out_val = 0;
                return DracoUtils.Failed();
            }

            if ((in_ & (1 << 7)) != 0)
            {
                // Next byte is available, decode it first.
                if (!DecodeVarint(out out_val, buffer))
                    return DracoUtils.Failed();
                // Append decoded info from this byte.
                out_val <<= 7;
                out_val |= (ushort)(in_ & ((1 << 7) - 1));
            }
            else
            {
                // Last byte reached
                out_val = in_;
            }
            return true;
        }
        public static bool DecodeVarint(out ulong out_val, DecoderBuffer buffer)
        {
            // Coding of unsigned values.
            // 0-6 bit - data
            // 7 bit - next byte?
            byte in_;
            if (!buffer.Decode(out in_))
            {
                out_val = 0;
                return DracoUtils.Failed();
            }

            if ((in_ & (1 << 7)) != 0)
            {
                // Next byte is available, decode it first.
                if (!DecodeVarint(out out_val, buffer))
                    return DracoUtils.Failed();
                // Append decoded info from this byte.
                out_val <<= 7;
                out_val |= (uint)(in_ & ((1 << 7) - 1));
            }
            else
            {
                // Last byte reached
                out_val = in_;
            }
            return true;
        }
    }
}
