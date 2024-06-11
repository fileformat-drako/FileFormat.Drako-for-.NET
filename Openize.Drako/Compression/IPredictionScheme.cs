using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Decoder;
using Openize.Draco.Encoder;
using Openize.Draco.Utils;

namespace Openize.Draco.Compression
{
    abstract partial class PredictionScheme
    {
        protected PointAttribute attribute_;
        protected PredictionSchemeTransform transform_;

        protected PredictionScheme(PointAttribute attribute, PredictionSchemeTransform transform)
        {
            this.attribute_ = attribute;
            this.transform_ = transform;
        }


        /// <summary>
        /// Returns the encoded attribute.
        /// </summary>
        public PointAttribute Attribute => attribute_;

        public abstract bool Initialized { get; }

        /// <summary>
        /// Returns the number of parent attributes that are needed for the prediction.
        /// </summary>
        public virtual int NumParentAttributes
        {
            get { return 0; }
        }

        public virtual AttributeType GetParentAttributeType(int i)
        {
            return AttributeType.Invalid;
        }


      // Sets the required parent attribute.
        public virtual bool SetParentAttribute(PointAttribute att)
        {
            return false;
        }

        public virtual bool AreCorrectionsPositive()
        {
            return transform_.AreCorrectionsPositive();
        }

        public virtual PredictionSchemeTransformType TransformType
        {
            get { return transform_.Type; }
        }
        public abstract PredictionSchemeMethod PredictionMethod { get; }


        public virtual bool EncodePredictionData(EncoderBuffer buffer)
        {
            if (!transform_.EncodeTransformData(buffer))
                return false;
            return true;
        }

        // Method that can be used to decode any prediction scheme specific data
        // from the input buffer.
        public virtual bool DecodePredictionData(DecoderBuffer buffer)
        {
            if (!transform_.DecodeTransformData(buffer))
                return false;
            return true;
        }

        public abstract bool ComputeCorrectionValues(Span<int> in_data, Span<int> out_corr, int size, int num_components,
            int[] entry_to_point_id_map);

        // Reverts changes made by the prediction scheme during encoding.
        public abstract bool ComputeOriginalValues(Span<int> in_corr, Span<int> out_data, int size, int num_components,
            int[] entry_to_point_id_map);

    }
}
