﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Openize.Draco.Utils;

namespace Openize.Draco
{

    /// <summary>
    /// Class for generating a sequence of point ids that can be used to encode
    /// or decode attribute values in a specific order.
    /// See sequentialAttributeEncoders/decodersController.h for more details.
    /// </summary>
    abstract class PointsSequencer
    {
        protected IntList outPointIds = new IntList();

        /// <summary>
        /// Fills the |outPointIds| with the generated sequence of point ids.
        /// </summary>
        public bool GenerateSequence(out int[] outPointIds)
        {
            this.outPointIds.Clear();
            bool ret = GenerateSequenceInternal();
            outPointIds = this.outPointIds.ToArray();
            return ret;
        }

        /// <summary>
        /// Appends a point to the sequence.
        /// </summary>
        public void AddPointId(int pointId)
        {
            outPointIds.Add(pointId);
        }

        /// <summary>
        /// Sets the correct mapping between point ids and value ids. I.e., the inverse
        /// of the |outPointIds|. In general, |outPointIds| does not contain
        /// sufficient information to compute the inverse map, because not all point
        /// ids are necessarily contained within the map.
        /// Must be implemented for sequencers that are used by attribute decoders.
        /// </summary>
        public virtual bool UpdatePointToAttributeIndexMapping(PointAttribute attr)
        {
            return DracoUtils.Failed();
        }

        /// <summary>
        /// Method that needs to be implemented by the derived classes. The
        /// implementation is responsible for filling |outPointIds| with the valid
        /// sequence of point ids.
        /// </summary>
        protected abstract bool GenerateSequenceInternal();
    }
}
