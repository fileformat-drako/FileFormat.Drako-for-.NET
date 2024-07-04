using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Openize.Drako.Compression;
using Openize.Drako.Utils;

namespace Openize.Drako.Decoder
{
    class SequentialIntegerAttributeDecoder : SequentialAttributeDecoder
    {
        private PredictionScheme predictionScheme;


        protected override bool DecodeValues(int[] pointIds, DecoderBuffer inBuffer)
        {
            int numValues = pointIds.Length;
            // Decode prediction scheme.
            sbyte predictionSchemeMethod;
            inBuffer.Decode(out predictionSchemeMethod);
                
            if (predictionSchemeMethod != (sbyte)PredictionSchemeMethod.None)
            {
                sbyte predictionTransformType;
                inBuffer.Decode(out predictionTransformType);
                predictionScheme = CreateIntPredictionScheme( (PredictionSchemeMethod)(predictionSchemeMethod),
                    (PredictionSchemeTransformType)(predictionTransformType));
            }

            if (predictionScheme != null)
            {
                if (!InitPredictionScheme(predictionScheme))
                    return DracoUtils.Failed();
            }

            if (!DecodeIntegerValues(pointIds, inBuffer))
                return DracoUtils.Failed();

            if (Decoder != null && Decoder.BitstreamVersion < 20)
            {
                if (!StoreValues(numValues))
                    return DracoUtils.Failed();
            }

            return true;
        }

        protected virtual PredictionScheme CreateIntPredictionScheme(PredictionSchemeMethod method,
            PredictionSchemeTransformType transformType)
        {
            if (transformType != PredictionSchemeTransformType.Wrap)
                return null; // For now we support only wrap transform.
            return CreatePredictionSchemeForDecoder(method, AttributeId, Decoder);
        }

        PredictionScheme CreatePredictionSchemeForDecoder(PredictionSchemeMethod method, int attId,
            PointCloudDecoder decoder)
        {
            return CreatePredictionSchemeForDecoder(
                method, attId, decoder, new PredictionSchemeWrapTransform());
        }

        /// <summary>
        /// Creates a prediction scheme for a given decoder and given prediction method.
        /// The prediction schemes are automatically initialized with decoder specific
        /// data if needed.
        /// </summary>
        PredictionScheme CreatePredictionSchemeForDecoder(PredictionSchemeMethod method, int attId,
            PointCloudDecoder decoder,
            PredictionSchemeTransform transform)
        {
            PointAttribute att = decoder.PointCloud.Attribute(attId);
            if (decoder.GeometryType == EncodedGeometryType.TriangularMesh)
            {
                // Cast the decoder to mesh decoder. This is not necessarily safe if there
                // is some other decoder decides to use TRIANGULARMESH as the return type,
                // but unfortunately there is not nice work around for this without using
                // RTTI (double dispatch and similar conecepts will not work because of the
                // template nature of the prediction schemes).
                MeshDecoder meshDecoder = (MeshDecoder) decoder;
                var ret = PredictionScheme.Create(meshDecoder, method, attId, transform);
                if (ret != null)
                    return ret;
                // Otherwise try to create another prediction scheme.
            }
            return new PredictionSchemeDeltaDecoder(att, transform);
        }


        private void PreparePortableAttribute(int num_entries, int num_components)
        {
            var va = new PointAttribute();
            va.AttributeType = attribute.AttributeType;
            va.ComponentsCount = attribute.ComponentsCount;
            va.DataType = DataType.INT32;
            va.ByteStride = num_components * DracoUtils.DataTypeLength(DataType.INT32);
            va.IdentityMapping = true;
            va.Reset(num_entries);
            PortableAttribute = va;
        }

        protected virtual int GetNumValueComponents()
        {
            return attribute.ComponentsCount;
        }

        private Span<int> GetValues(int numEntries)
        {
            int numComponents = GetNumValueComponents();
            int numValues = numEntries * numComponents;
            if (numComponents <= 0)
                return null;
            PreparePortableAttribute(numEntries, numComponents);
            if (PortableAttribute.NumUniqueEntries == 0)
                return null;
            var buf = PortableAttribute.Buffer.GetBuffer();
            return MemoryMarshal.Cast<byte, int>(buf.AsSpan(0, numValues * 4));
        }

        public virtual bool DecodeIntegerValues(int[] pointIds, DecoderBuffer inBuffer)
        {
            int numComponents = GetNumValueComponents();
            int numEntries = pointIds.Length;
            int numValues = numEntries * numComponents;
            if (numComponents <= 0)
                return DracoUtils.Failed();
            Span<int> values = GetValues(numEntries);
            if(values == null)
                return DracoUtils.Failed();
            byte compressed;
            if (!inBuffer.Decode(out compressed))
                return DracoUtils.Failed();
            if (compressed > 0)
            {
                // Decode compressed values.
                if (!Decoding.DecodeSymbols(numValues, numComponents, inBuffer, values))
                    return DracoUtils.Failed();
            }
            else
            {
                // Decode the integer data directly.
                // Get the number of bytes for a given entry.
                byte numBytes;
                if (!inBuffer.Decode(out numBytes))
                    return DracoUtils.Failed();

                //if (numBytes == sizeof(int))
                //{
                //    if (!inBuffer.Decode(values, values.Count))
                //        return false;
                //}
                //else
                //{
                    for (int i = 0; i < values.Length; ++i)
                    {
                        int tmp;
                        inBuffer.Decode(out tmp);
                        values[i] = tmp;
                    }
                //}
            }

            if (predictionScheme == null ||
                !predictionScheme.AreCorrectionsPositive())
            {
                // Convert the values back to the original signed format.
                Decoding.ConvertSymbolsToSignedInts(values, values);
            }

            // If the data was encoded with a prediction scheme, we must revert it.
            if (predictionScheme != null)
            {
                if (!predictionScheme.DecodePredictionData(inBuffer))
                    return DracoUtils.Failed();

                if (!predictionScheme.ComputeOriginalValues(values, values, values.Length, numComponents, pointIds))
                {
                    return DracoUtils.Failed();
                }
            }
            return true;
        }

        public override bool TransformAttributeToOriginalFormat(int[] pointIds)
        {
            if (decoder != null && decoder.BitstreamVersion < 20)
                return true;
            return StoreValues(pointIds.Length);
        }

        protected virtual bool StoreValues(int numValues)
        {
            switch (Attribute.DataType)
            {
                case DataType.UINT8:
                case DataType.INT8:
                    Store8BitsValues(numValues);
                    break;
                case DataType.UINT16:
                case DataType.INT16:
                    Store16BitsValues(numValues);
                    break;
                case DataType.UINT32:
                case DataType.INT32:
                    Store32BitsValues(numValues);
                    break;
                default:
                    return DracoUtils.Failed();
            }
            return true;
        }

        /// <summary>
        /// Stores decoded values into the attribute with a data type AttributeTypeT.
        /// </summary>
        private void Store8BitsValues(int numValues)
        {
            int vals = Attribute.ComponentsCount * numValues;
            var values = GetValues(numValues);
            int outBytePos = 0;
            for (int i = 0; i < vals; ++i)
            {
                // Store the integer value into the attribute buffer.
                Attribute.Buffer.Write(outBytePos, (byte)values[i]);
                outBytePos++;
            }
        }
        private void Store16BitsValues(int numValues)
        {
            int vals = Attribute.ComponentsCount * numValues;
            var values = GetValues(numValues);
            int outBytePos = 0;
            for (int i = 0; i < vals; ++i)
            {
                // Store the integer value into the attribute buffer.
                Attribute.Buffer.Write(outBytePos, (ushort)values[i]);
                outBytePos += 2;
            }
        }
        private void Store32BitsValues(int numValues)
        {
            int vals = Attribute.ComponentsCount * numValues;
            var values = GetValues(numValues);
            int outBytePos = 0;
            for (int i = 0; i < vals; ++i)
            {
                // Store the integer value into the attribute buffer.
                Attribute.Buffer.Write(outBytePos, (uint)values[i]);
                outBytePos += 4;
            }
        }
    }
}
