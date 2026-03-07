using RimWorld;
using System.Collections.Generic;
using Verse;

namespace SimpleSlaveryCollars
{
    public abstract class SlaveApparel : Apparel
    {
        public abstract IEnumerable<Gizmo> SlaveGizmos();
    }
}