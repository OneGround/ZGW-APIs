using System;

namespace Roxit.ZGW.Common.DataModel;

public interface IConceptEntity
{
    bool Concept { get; set; }
    DateOnly BeginGeldigheid { get; set; }
    DateOnly? EindeGeldigheid { get; set; }
}
