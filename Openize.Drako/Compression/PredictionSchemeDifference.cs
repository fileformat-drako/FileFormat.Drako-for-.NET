using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Utils;

namespace Openize.Draco.Compression
{


    /// <summary>
    /// Basic prediction scheme based on computing backward differences between
    /// stored attribute values (also known as delta-coding). Usually works better
    /// than the reference point prediction scheme, because nearby values are often
    /// encoded next to each other.
    /// </summary>
    class PredictionSchemeDifference : PredictionScheme
    {

        public PredictionSchemeDifference(PointAttribute att, PredictionSchemeTransform transform_)
            :base(att, transform_)
        {
        }

        public override PredictionSchemeMethod PredictionMethod { get {return PredictionSchemeMethod.Difference;} }
        public override bool Initialized { get { return true; } }

        public override bool ComputeCorrectionValues(IntArray inData, IntArray outCorr, int size, int numComponents, int[] entryToPointIdMap)
        {

            transform_.InitializeEncoding(inData, numComponents);
            // Encode data from the back using D(i) = D(i) - D(i - 1).
            for (int i = size - numComponents; i > 0; i -= numComponents)
            {
                transform_.ComputeCorrection(
                    inData,  i, inData,  i - numComponents, outCorr, 0, i);
            }
            // Encode correction for the first element.
            IntArray zeroVals = IntArray.Array(numComponents);
            transform_.ComputeCorrection(inData, zeroVals, outCorr, 0);
            return true;
        }

        public override bool ComputeOriginalValues(IntArray inCorr, IntArray outData, int size, int numComponents, int[] entryToPointIdMap)
        {
            transform_.InitializeDecoding(numComponents);
            // Decode the original value for the first element.
            IntArray zeroVals = IntArray.Array(numComponents);
            transform_.ComputeOriginalValue(zeroVals, inCorr, outData);

            // Decode data from the front using D(i) = D(i) + D(i - 1).
            for (int i = numComponents; i < size; i += numComponents)
            {
                transform_.ComputeOriginalValue(outData, i - numComponents,
                    inCorr, i, outData, i);
            }
            return true;
        }
    }
}
