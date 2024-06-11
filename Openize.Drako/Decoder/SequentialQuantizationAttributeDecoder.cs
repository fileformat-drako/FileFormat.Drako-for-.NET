using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Utils;

namespace Openize.Draco.Decoder
{
    class SequentialQuantizationAttributeDecoder : SequentialIntegerAttributeDecoder
    {
        /// <summary>
        /// Max number of quantization bits used to encode each component of the
        /// attribute.
        /// </summary>
        private int quantizationBits;
        private float[] minValue;
        private float maxValueDif;

        public override bool Initialize(PointCloudDecoder decoder, int attributeId)
        {
            if (!base.Initialize(decoder, attributeId))
                return DracoUtils.Failed();
            PointAttribute attribute = decoder.PointCloud.Attribute(attributeId);
            // Currently we can quantize only floating point arguments.
            if (attribute.DataType != DataType.FLOAT32)
                return DracoUtils.Failed();
            return true;
        }

        public override bool DecodeIntegerValues(int[] pointIds, DecoderBuffer inBuffer)
        {
            if(Decoder.BitstreamVersion < 20 && !DecodeQuantizedDataInfo())
                return DracoUtils.Failed();
            return base.DecodeIntegerValues(pointIds, inBuffer);
        }

        protected override bool StoreValues(int numValues)
        {
            return DequantizeValues(numValues);
        }

        public override bool DecodeDataNeededByPortableTransform(int[] pointIds, DecoderBuffer in_buffer)
        {
            if (Decoder.BitstreamVersion >= 20)
            {
                // Decode quantization data here only for files with bitstream version 2.0+
                if (!DecodeQuantizedDataInfo())
                    return DracoUtils.Failed();
            }
            // Store the decoded transform data in portable attribute;
            var transform = new AttributeQuantizationTransform();
            transform.SetParameters(quantizationBits, minValue, attribute.ComponentsCount, maxValueDif);
            return transform.TransferToAttribute(PortableAttribute);
        }

        private bool DecodeQuantizedDataInfo()
        {
            int numComponents = Attribute.ComponentsCount;
            minValue = new float[numComponents];
            if (!Decoder.Buffer.Decode(minValue))
                return DracoUtils.Failed();
            if (!Decoder.Buffer.Decode(out maxValueDif))
                return DracoUtils.Failed();
            byte quantizationBits;
            if (!Decoder.Buffer.Decode(out quantizationBits) ||
                quantizationBits > 31)
                return DracoUtils.Failed();
            this.quantizationBits = quantizationBits;
            return true;
        }

        private bool DequantizeValues(int numValues)
        {
            // Convert all quantized values back to floats.
            int maxQuantizedValue = (1 << (quantizationBits)) - 1;
            int numComponents = Attribute.ComponentsCount;
            int entrySize = sizeof(float) * numComponents;
            float[] attVal = new float[numComponents];
            int quantValId = 0;
            int outBytePos = 0;
            Dequantizer dequantizer = new Dequantizer(maxValueDif, maxQuantizedValue);
            IntArray values = IntArray.Wrap(PortableAttribute.Buffer.GetBuffer(), 0, numValues * numComponents);
            for (uint i = 0; i < numValues; ++i)
            {
                for (int c = 0; c < numComponents; ++c)
                {
                    float value = dequantizer.DequantizeFloat(values[quantValId++]);
                    value = value + minValue[c];
                    attVal[c] = value;
                }
                // Store the floating point value into the attribute buffer.
                Attribute.Buffer.Write(outBytePos, attVal);
                outBytePos += entrySize;
            }
            return true;
        }
    }
}
