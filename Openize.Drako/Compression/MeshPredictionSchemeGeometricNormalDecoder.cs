﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Decoder;
using Openize.Draco.Utils;

namespace Openize.Draco.Compression
{

    partial class MeshPredictionSchemeGeometricNormal : MeshPredictionScheme
    {
        MeshPredictionSchemeGeometricNormalPredictorArea predictor_;
        RAnsBitDecoder flip_normal_bit_decoder_;
        OctahedronToolBox octahedron_tool_box_;
        public MeshPredictionSchemeGeometricNormal(PointAttribute attribute, PredictionSchemeTransform transform, MeshPredictionSchemeData meshData) : base(attribute, transform, meshData)
        {
            predictor_ = new MeshPredictionSchemeGeometricNormalPredictorArea(meshData);
            octahedron_tool_box_ = new OctahedronToolBox();
            flip_normal_bit_decoder_ = new RAnsBitDecoder();
        }

        public override int NumParentAttributes => 1;
        public override AttributeType GetParentAttributeType(int i)
        {
            return AttributeType.Position;
        }

        public override bool SetParentAttribute(PointAttribute att)
        {
            if (att.AttributeType != AttributeType.Position)
                return DracoUtils.Failed(); // Invalid attribute type.
            if (att.ComponentsCount != 3)
                return DracoUtils.Failed(); // Currently works only for 3 component positions.
            predictor_.pos_attribute_  = att;
            return true;
        }

        public override bool Initialized
        {
            get
            {

                if (!predictor_.IsInitialized)
                    return DracoUtils.Failed();
                //if (!meshData.Initialized)
                //    return DracoUtils.Failed();
                if (!octahedron_tool_box_.IsInitialized)
                    return DracoUtils.Failed();
                return true;
            }
        }

        public override bool ComputeOriginalValues(Span<int> inCorr, Span<int> outData, int size, int numComponents, int[] entryToPointIdMap)
        {
            octahedron_tool_box_.SetQuantizationBits(((PredictionSchemeNormalOctahedronTransformBase) transform_).QuantizationBits);
            predictor_.entry_to_point_id_map_ = entryToPointIdMap;

            // Expecting in_data in octahedral coordinates, i.e., portable attribute.

            int corner_map_size = this.meshData.dataToCornerMap.Count;

            int[] pred_normal_3d = new int[3];
            Span<int> pred_normal_oct = stackalloc int[2];

            for (int data_id = 0; data_id < corner_map_size; ++data_id)
            {
                int corner_id =
                    this.meshData.dataToCornerMap[data_id];
                predictor_.ComputePredictedValue(corner_id, pred_normal_3d);

                // Compute predicted octahedral coordinates.
                octahedron_tool_box_.CanonicalizeIntegerVector(pred_normal_3d);
                if (flip_normal_bit_decoder_.DecodeNextBit())
                {
                    pred_normal_3d[0] = -pred_normal_3d[0];
                    pred_normal_3d[1] = -pred_normal_3d[1];
                    pred_normal_3d[2] = -pred_normal_3d[2];
                }

                int s, t;
                octahedron_tool_box_.IntegerVectorToQuantizedOctahedralCoords(pred_normal_3d, out s, out t);
                pred_normal_oct[0] = s;
                pred_normal_oct[1] = t;

                int data_offset = data_id * 2;
                this.transform_.ComputeOriginalValue(pred_normal_oct, 0, inCorr, data_offset, outData, data_offset);
            }

            flip_normal_bit_decoder_.EndDecoding();
            return true;
        }

        public override bool DecodePredictionData(DecoderBuffer buffer)
        {
            if (!this.transform_.DecodeTransformData(buffer))
                return DracoUtils.Failed();

            if (buffer.BitstreamVersion < 22)
            {
                byte prediction_mode;
                buffer.Decode(out prediction_mode);

                if (!predictor_.SetNormalPredictionMode((NormalPredictionMode) prediction_mode))
                    return DracoUtils.Failed();
            }

            // Init normal flips.
            if (!flip_normal_bit_decoder_.StartDecoding(buffer))
                return DracoUtils.Failed();

            return true;
        }

        public override PredictionSchemeMethod PredictionMethod
        {
            get { return PredictionSchemeMethod.GeometricNormal; }
        }
    }
}
