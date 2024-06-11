﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Draco
{

    /// <summary>
    /// A simple sequencer that generates a linear sequence [0, numPoints - 1].
    /// I.e., the order of the points is preserved for the input data.
    /// </summary>
    class LinearSequencer : PointsSequencer
    {
        private int numPoints;

        public LinearSequencer(int numPoints)
        {
            this.numPoints = numPoints;
        }

        public override bool UpdatePointToAttributeIndexMapping(PointAttribute attribute)
        {
            attribute.IdentityMapping = true;
            return true;
        }

        protected override bool GenerateSequenceInternal()
        {
            outPointIds.Capacity = numPoints;
            for (int i = 0; i < numPoints; ++i)
            {
                outPointIds.Add(i);
            }
            return true;
        }

    }
}
