using System;
using System.Collections.Generic;
using Verse;

namespace SK_Enviro.AI
{
    public class BrokenStateDef : Verse.BrokenStateDef
    {
        public override IEnumerable<string> ConfigErrors()
        {
            yield break;
        }
    }
}
