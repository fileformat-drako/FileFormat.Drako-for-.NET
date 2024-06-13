﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Openize.Drako
{
    interface ICornerTableTraverser<TCornerTable> where TCornerTable : ICornerTable
    {
        TCornerTable CornerTable{ get; }

        //CornerTableTraversalProcessor<TCornerTable> TraversalProcessor{ get; }
        bool TraverseFromCorner(int cornerId);
        void OnTraversalStart();
        void OnTraversalEnd();
    }
}
