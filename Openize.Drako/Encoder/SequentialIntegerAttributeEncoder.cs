using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Openize.Draco.Compression;
using Openize.Draco.Utils;

namespace Openize.Draco.Encoder
{

    /// <summary>
    /// Attribute encoder designed for lossless encoding of integer attributes. The
    /// attribute values can be pre-processed by a prediction scheme and compressed
    /// with a built-in entropy coder.
    /// </summary>
    class SequentialIntegerAttributeEncoder : SequentialAttributeEncoder
    {

        /// <summary>
        /// Optional prediction scheme can be used to modify the integer values in
        /// order to make them easier to compress.
        /// </summary>
        private PredictionScheme predictionScheme;
        public SequentialIntegerAttributeEncoder()
        {
        }

        public override SequentialAttributeEncoderType GetUniqueId()
        {
            return SequentialAttributeEncoderType.Integer;
        }

        public override bool Initialize(PointCloudEncoder encoder, int attributeId)
        {
            if (!base.Initialize(encoder, attributeId))
                return false;

            // When encoding integers, this encoder currently works only for integer
            // attributes up to 32 bits.

            if (GetUniqueId() == SequentialAttributeEncoderType.Integer &&
                !DracoUtils.IsIntegerType(Attribute.DataType))
                return false;
            // Init prediction scheme.
            PredictionSchemeMethod predictionSchemeMethod = encoder.Options.GetAttributePredictionScheme(Attribute);

            predictionScheme = CreateIntPredictionScheme(predictionSchemeMethod);

            if (predictionScheme != null && !InitPredictionScheme(predictionScheme))
                predictionScheme = null;

            return true;
        }


        public override bool TransformAttributeToPortableFormat(int[] point_ids)
        {
            if (Encoder != null)
            {
                if (!PrepareValues(point_ids, Encoder.PointCloud.NumPoints))
                    return false;
            }
            else
            {
                if (!PrepareValues(point_ids, 0))
                    return false;
            }

            // Update point to attribute mapping with the portable attribute if the
            // attribute is a parent attribute (for now, we can skip it otherwise).
            if (IsParentEncoder())
            {
                // First create map between original attribute value indices and new ones
                // (determined by the encoding order).
                PointAttribute orig_att = Attribute;
                PointAttribute portable_att = portableAttribute;
                IntArray value_to_value_map = IntArray.Array(orig_att.NumUniqueEntries);
                for (int i = 0; i < point_ids.Length; ++i)
                {
                    value_to_value_map[orig_att.MappedIndex(point_ids[i])] =
                        i;
                }

                // Go over all points of the original attribute and update the mapping in
                // the portable attribute.
                for (int i = 0; i < Encoder.PointCloud.NumPoints; ++i)
                {
                    portable_att.SetPointMapEntry(
                        i, value_to_value_map[orig_att.MappedIndex(i)]);
                }
            }

            return true;
        }

        protected override bool EncodeValues(int[] pointIds, EncoderBuffer outBuffer)
        {

            // Initialize general quantization data. 
            PointAttribute attrib = Attribute;
            if (attrib.NumUniqueEntries == 0)
                return true;

            sbyte prediction_scheme_method = (sbyte) PredictionSchemeMethod.None;
            if (predictionScheme != null)
            {
                if (!SetPredictionSchemeParentAttributes(predictionScheme))
                {
                    return false;
                }

                prediction_scheme_method = (sbyte) (predictionScheme.PredictionMethod);
            }

            outBuffer.Encode((byte)prediction_scheme_method);
            if (predictionScheme != null)
            {
                outBuffer.Encode((byte) (predictionScheme.TransformType));
            }

            int num_components = portableAttribute.ComponentsCount;
            int num_values = num_components * portableAttribute.NumUniqueEntries;
            IntArray portable_attribute_data = GetPortableAttributeData();

            // We need to keep the portable data intact, but several encoding steps can
            // result in changes of this data, e.g., by applying prediction schemes that
            // change the data in place. To preserve the portable data we store and
            // process all encoded data in a separate array.
            IntArray encoded_data = IntArray.Array(num_values);

            // All integer values are initialized. Process them using the prediction
            // scheme if we have one.
            if (predictionScheme != null)
            {
                predictionScheme.ComputeCorrectionValues(
                    portable_attribute_data, encoded_data, num_values, num_components,
                    pointIds);
            }

            if (predictionScheme == null ||
                !predictionScheme.AreCorrectionsPositive())
            {
                IntArray input =
                    predictionScheme != null ? encoded_data : portable_attribute_data;
                Encoding.ConvertSignedIntsToSymbols(input, num_values, encoded_data);
            }

            if (Encoder == null || Encoder.Options.UseBuiltinAttributeCompression)
            {
                outBuffer.Encode((byte) 1);
                DracoEncodeOptions symbol_encoding_options = new DracoEncodeOptions();
                if (Encoder != null)
                {
                    symbol_encoding_options.CompressionLevel = Encoder.Options.CompressionLevel;
                    //SetSymbolEncodingCompressionLevel(&symbol_encoding_options, 10 - encoder().options().GetSpeed());
                }


                if (!Encoding.EncodeSymbols(encoded_data, pointIds.Length * num_components, num_components,
                    symbol_encoding_options, outBuffer))
                {
                    return false;
                }
            }
            else
            {
                // No compression. Just store the raw integer values, using the number of
                // bytes as needed.

                // To compute the maximum bit-length, first OR all values.
                uint masked_value = 0;
                for (int i = 0; i < num_values; ++i)
                {
                    masked_value |= (uint)encoded_data[i];
                }

                // Compute the msb of the ORed value.
                int value_msb_pos = 0;
                if (masked_value != 0)
                {
                    value_msb_pos = DracoUtils.MostSignificantBit(masked_value);
                }

                int num_bytes = 1 + value_msb_pos / 8;

                outBuffer.Encode((byte) 0);
                outBuffer.Encode((byte) num_bytes);

                if (num_bytes == DracoUtils.DataTypeLength(DataType.INT32))
                {
                    outBuffer.Encode(encoded_data, 4 * num_values);
                }
                else
                {
                    for (int i = 0; i < num_values; ++i)
                    {
                        outBuffer.Encode(encoded_data, i * 4, num_bytes);
                    }
                }
            }

            if (predictionScheme != null)
            {
                predictionScheme.EncodePredictionData(outBuffer);
            }

            return true;
        }

        private IntArray GetPortableAttributeData()
        {
            int num_components = portableAttribute.ComponentsCount;
            int num_values = num_components * portableAttribute.NumUniqueEntries;
            IntArray array = IntArray.Wrap(portableAttribute.Buffer.GetBuffer(), portableAttribute.ByteOffset, num_values * 4) ;
            return array;
        }

        /// <summary>
        /// Returns a prediction scheme that should be used for encoding of the
        /// integer values.
        /// </summary>
        protected virtual PredictionScheme CreateIntPredictionScheme(PredictionSchemeMethod method)
        {
            //AttributeId, Encoder;
            //PredictionSchemeWrapTransform

            return PredictionScheme.Create(Encoder, method, AttributeId, new PredictionSchemeWrapTransform());
        }

        /// <summary>
        /// Prepares the integer values that are going to be encoded.
        /// </summary>
        protected virtual bool PrepareValues(int[] pointIds, int numPoints)
        {
            // Convert all values to int32T format.
            PointAttribute attrib = Attribute;
            int numComponents = attrib.ComponentsCount;
            int numEntries = pointIds.Length;
            PreparePortableAttribute(numEntries, numComponents, numPoints);
            int dstIndex = 0;
            var portable_attribute_data = GetPortableAttributeData();
            for (int i = 0; i < numEntries; ++i)
            {
                int attId = attrib.MappedIndex(pointIds[i]);
                int tmp;
                attrib.ConvertValue(attId, out tmp);
                portable_attribute_data[dstIndex] = tmp;
                dstIndex += numComponents;
            }
            return true;
        }

        private void PreparePortableAttribute(int num_entries, int num_components, int num_points)
        {
            var va = new PointAttribute();
            va.AttributeType = attribute.AttributeType;
            va.ComponentsCount = attribute.ComponentsCount;
            va.DataType = DataType.INT32;
            va.Normalized = false;
            va.ByteStride = num_components * DracoUtils.DataTypeLength(DataType.INT32);
            va.Reset(num_entries);
            this.portableAttribute = va;
            if (num_points != 0)
            {
                portableAttribute.SetExplicitMapping(num_points);
            }
        }
    }
}
