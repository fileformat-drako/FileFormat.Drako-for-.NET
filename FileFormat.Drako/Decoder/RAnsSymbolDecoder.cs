using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Drako.Utils;

namespace Openize.Drako.Decoder
{
    class RAnsSymbolDecoder : RAnsBitCodec
    {
        private int maxSymbolBitLength;
        private int maxSymbols;
        private int ransPrecision;
        private uint[] probabilityTable;
        private int numSymbols;
        private RAnsDecoder ans;

        public RAnsSymbolDecoder(int maxSymbolBitLength)
        {
            this.maxSymbolBitLength = maxSymbolBitLength;
            this.maxSymbols = 1 << maxSymbolBitLength;
            int ransPrecisionBits = ComputeRAnsPrecisionFromMaxSymbolBitLength(maxSymbolBitLength);
            this.ransPrecision = 1 << ransPrecisionBits;
            ans= new RAnsDecoder(ransPrecisionBits);
        }

        public bool Create(DecoderBuffer buffer)
        {
            if (buffer.BitstreamVersion == 0)
                return DracoUtils.Failed();
            // Decode the number of alphabet symbols.
            if (buffer.BitstreamVersion < 20)
            {
                if (!buffer.Decode(out numSymbols))
                    return DracoUtils.Failed();
            }
            else
            {
                uint n;
                if (!Decoding.DecodeVarint(out n, buffer))
                    return DracoUtils.Failed();
                numSymbols = (int)n;
            }
            probabilityTable = new uint[numSymbols];
            if (numSymbols == 0)
                return true;
            // Decode the table.
            for (int i = 0; i < numSymbols; ++i)
            {
                uint prob = 0;
                byte byteProb = 0;
                // Decode the first byte and extract the number of extra bytes we need to
                // get.
                if (!buffer.Decode(out byteProb))
                    return DracoUtils.Failed();
                int token = byteProb & 3;
                if (token == 3)
                {
                    var offset = byteProb >> 2;
                    if (i + offset >= numSymbols)
                        return DracoUtils.Failed();
                    // Set zero probability for all symbols in the specified range.
                    for (int j = 0; j < offset + 1; ++j)
                    {
                        probabilityTable[i + j] = 0;
                    }

                    i += offset;
                }
                else
                {
                    int extraBytes = byteProb & 3;
                    prob = (uint) (byteProb >> 2);
                    for (int b = 0; b < extraBytes; ++b)
                    {
                        byte eb;
                        if (!buffer.Decode(out eb))
                            return DracoUtils.Failed();
                        // Shift 8 bits for each extra byte and subtract 2 for the two first bits.
                        prob |= (uint) (eb) << (8 * (b + 1) - 2);
                    }
                }
                probabilityTable[i] = prob;
            }
            if (!ans.BuildLookupTable(probabilityTable, numSymbols))
                return DracoUtils.Failed();
            return true;
        }

        public bool StartDecoding( DecoderBuffer buffer)
        {
            long bytesEncoded;
            // Decode the number of bytes encoded by the encoder.
            if (buffer.BitstreamVersion < 20)
            {
                if (!buffer.Decode(out bytesEncoded))
                    return DracoUtils.Failed();
            }
            else
            {
                ulong n;
                if (!Decoding.DecodeVarint(out n, buffer))
                    return DracoUtils.Failed();
                bytesEncoded = (long)n;
            }

            if (bytesEncoded > buffer.RemainingSize)
                return DracoUtils.Failed();
            BytePointer dataHead = buffer.Pointer + buffer.DecodedSize;
            // Advance the buffer past the rANS data.
            buffer.Advance((int)bytesEncoded);
            if (ans.readInit(dataHead, (int)bytesEncoded) != 0)
                return DracoUtils.Failed();
            return true;
        }

        public int NumSymbols
        {
            get { return numSymbols; }
        }

        public int DecodeSymbol()
        {
            return ans.Read();
        }

        public void EndDecoding()
        {
            ans.ReadEnd();
        }
    }
}
