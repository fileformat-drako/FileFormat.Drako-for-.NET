using Openize.Drako.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Drako.Decoder
{
    class MeshEdgeBreakerTraversalDecoder : ITraversalDecoder
    {

        /// <summary>
        /// Buffer that contains the encoded data.
        /// </summary>
        protected DecoderBuffer buffer;

        protected DecoderBuffer symbol_buffer_ = new DecoderBuffer();
        protected DecoderBuffer startFaceBuffer;
        protected RAnsBitDecoder startFaceDecoder = new RAnsBitDecoder();
        protected RAnsBitDecoder[] attributeConnectivityDecoders;
        protected int numAttributeData;
        protected IMeshEdgeBreakerDecoderImpl decoderImpl;
        /// <summary>
        /// Returns true if there is an attribute seam for the next processed pair
        /// of visited faces.
        /// |attribute| is used to mark the id of the non-position attribute (in range
        /// of &lt; 0, numAttributes - 1&gt;).
        /// </summary>
        public bool DecodeAttributeSeam(int attribute)
        {
            return attributeConnectivityDecoders[attribute].DecodeNextBit();
        }

        protected int BitstreamVersion
        {
            get { return decoderImpl.GetDecoder().BitstreamVersion; }
        }

        public virtual void Init(IMeshEdgeBreakerDecoderImpl decoder)
        {
            decoderImpl = decoder;
            buffer = decoder.GetDecoder().Buffer.SubBuffer(0);
        }

        public virtual void SetNumEncodedVertices(int numVertices)
        {
        }

        public void SetNumAttributeData(int numData)
        {
            this.numAttributeData = numData;
        }

        public virtual bool Start(out DecoderBuffer outBuffer)
        {
            outBuffer = null;
            if (!DecodeTraversalSymbols())
                return false;
            if (!DecodeStartFaces())
                return false;
            if (!DecodeAttributeSeams())
                return false;
            outBuffer = buffer;
            return true;
        }


        public virtual void Done()
        {
            if (symbol_buffer_.BitDecoderActive)
                symbol_buffer_.EndBitDecoding();
            if (BitstreamVersion < 22)
            {
                startFaceBuffer.EndBitDecoding();
            }
            else
            {
                startFaceDecoder.EndDecoding();
            }
        }

        public virtual EdgeBreakerTopologyBitPattern DecodeSymbol()
        {
            uint s;
            symbol_buffer_.DecodeLeastSignificantBits32(1, out s);
            EdgeBreakerTopologyBitPattern symbol = (EdgeBreakerTopologyBitPattern) s;
            if (symbol == EdgeBreakerTopologyBitPattern.C)
            {
                return symbol;
            }
            // Else decode two additional bits.
            uint symbolSuffix;
            symbol_buffer_.DecodeLeastSignificantBits32(2, out symbolSuffix);
            s |= (symbolSuffix << 1);
            return (EdgeBreakerTopologyBitPattern)s;
        }

        public virtual void MergeVertices(int dest, int source)
        {
        }

        public virtual void NewActiveCornerReached(int corner)
        {
        }

        public virtual bool DecodeStartFaceConfiguration()
        {
            if (BitstreamVersion < 22)
            {
                uint face_configuration;
                startFaceBuffer.DecodeLeastSignificantBits32(1, out face_configuration);
                return face_configuration != 0;
            }
            else
            {
                bool ret = startFaceDecoder.DecodeNextBit();
                return ret;
            }
        }

        protected bool DecodeTraversalSymbols()
        {
            long traversalSize;
            symbol_buffer_ = buffer.Clone();
            if (!symbol_buffer_.StartBitDecoding(true, out traversalSize))
                return DracoUtils.Failed();
            buffer = symbol_buffer_.Clone();
            if (traversalSize > buffer.RemainingSize)
                return DracoUtils.Failed();
            buffer.Advance((int)traversalSize);
            return true;
        }

        protected bool DecodeStartFaces()
        {
            // Create a decoder that is set to the end of the encoded traversal data.
            if (BitstreamVersion  < 22)
            {
                startFaceBuffer = buffer.Clone();
                long traversalSize;
                if (!startFaceBuffer.StartBitDecoding(true, out traversalSize))
                    return DracoUtils.Failed();
                buffer = startFaceBuffer.Clone();
                if (traversalSize > buffer.RemainingSize)
                    return DracoUtils.Failed();
                buffer.Advance((int)traversalSize);
                return true;
            }
            return startFaceDecoder.StartDecoding(buffer);
        }

        protected bool DecodeAttributeSeams()
        {
            // Prepare attribute decoding.
            if (numAttributeData > 0)
            {
                attributeConnectivityDecoders = new RAnsBitDecoder[numAttributeData];
                for (int i = 0; i < numAttributeData; ++i)
                {
                    attributeConnectivityDecoders[i] = new RAnsBitDecoder();
                    if (!attributeConnectivityDecoders[i].StartDecoding(buffer))
                        return DracoUtils.Failed();
                }
            }

            return true;
        }
    }

}
