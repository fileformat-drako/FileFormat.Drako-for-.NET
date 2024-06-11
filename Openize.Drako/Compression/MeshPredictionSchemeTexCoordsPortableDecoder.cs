using Openize.Draco.Decoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Utils;
using Openize.Draco.Encoder;

namespace Openize.Draco.Compression
{
    class MeshPredictionSchemeTexCoordsPortableDecoder : MeshPredictionScheme
    {

        MeshPredictionSchemeTexCoordsPortablePredictor predictor_;
        public MeshPredictionSchemeTexCoordsPortableDecoder(PointAttribute attribute, PredictionSchemeTransform transform, MeshPredictionSchemeData meshData) : base(attribute, transform, meshData)
        {
            predictor_ = new MeshPredictionSchemeTexCoordsPortablePredictor(meshData);
        }

        public override bool ComputeCorrectionValues(IntArray in_data, IntArray out_corr, int size, int num_components,
            int[] entry_to_point_id_map)
        {
            predictor_.entry_to_point_id_map_ = entry_to_point_id_map;
            this.transform_.InitializeEncoding(in_data, num_components);
            // We start processing from the end because this prediction uses data from
            // previous entries that could be overwritten when an entry is processed.
            for (int p = this.meshData.dataToCornerMap.Count - 1;
                p >= 0;
                --p)
            {
                int corner_id = this.meshData.dataToCornerMap[p];
                predictor_.ComputePredictedValue(true, corner_id, in_data, p);

                int dst_offset = p * num_components;
                this.transform_.ComputeCorrection(in_data, dst_offset,
                    predictor_.predicted_value_, 0,
                    out_corr, dst_offset, 0);
            }

            return true;
        }

        public override bool EncodePredictionData(EncoderBuffer buffer)
        {

            // Encode the delta-coded orientations using arithmetic coding.
            int num_orientations = predictor_.num_orientations();
            buffer.Encode(num_orientations);
            bool last_orientation = true;
            RAnsBitEncoder encoder = new RAnsBitEncoder();
            encoder.StartEncoding();
            for (int i = 0; i < num_orientations; ++i)
            {
                bool orientation = predictor_.orientation(i);
                encoder.EncodeBit(orientation == last_orientation);
                last_orientation = orientation;
            }

            encoder.EndEncoding(buffer);
            return base.EncodePredictionData(buffer);
        }


        public override bool SetParentAttribute(PointAttribute att)
        {
            if (att == null || att.AttributeType != AttributeType.Position)
                return DracoUtils.Failed();  // Invalid attribute type.
            if (att.ComponentsCount != 3)
                return DracoUtils.Failed();  // Currently works only for 3 component positions.
            predictor_.pos_attribute_ = att;
            return true;
        }

        public override bool ComputeOriginalValues(IntArray inCorr, IntArray outData, int size, int numComponents, int[] entryToPointIdMap)
        {
            predictor_.entry_to_point_id_map_ = entryToPointIdMap;
            this.transform_.InitializeDecoding(numComponents);

            int corner_map_size = this.meshData.dataToCornerMap.Count;
            for (int p = 0; p < corner_map_size; ++p) {
                int corner_id = this.meshData.dataToCornerMap[p];
                if (!predictor_.ComputePredictedValue(false, corner_id, outData, p))
                    return DracoUtils.Failed();

                int dst_offset = p * numComponents;
                this.transform_.ComputeOriginalValue(predictor_.predicted_value_, 0,
                                                       inCorr, dst_offset,
                                                       outData, dst_offset);
            }
            return true;
        }
        public override bool DecodePredictionData(DecoderBuffer buffer) {
            // Decode the delta coded orientations.
            int num_orientations = 0;
            if (!buffer.Decode(out num_orientations) || num_orientations < 0)
                return DracoUtils.Failed();
            predictor_.ResizeOrientations(num_orientations);
            bool last_orientation = true;
            RAnsBitDecoder decoder = new RAnsBitDecoder();
            if (!decoder.StartDecoding(buffer))
                return DracoUtils.Failed();
            for (int i = 0; i < num_orientations; ++i) {
                if (!decoder.DecodeNextBit())
                    last_orientation = !last_orientation;
                predictor_.set_orientation(i, last_orientation);
            }
            decoder.EndDecoding();
            return base.DecodePredictionData(buffer);
        }
        public override AttributeType GetParentAttributeType(int i)
        {
            return AttributeType.Position;
        }
        public override int NumParentAttributes => 1;

        public override PredictionSchemeMethod PredictionMethod
        {
            get { return PredictionSchemeMethod.TexCoordsPortable; }
        }
    }
}
