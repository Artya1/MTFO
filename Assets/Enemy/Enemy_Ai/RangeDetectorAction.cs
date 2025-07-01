using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RangeDetector", story: "Find The [Range] between player and asign the [Target]", category: "Action", id: "b39295b206da86458bb108996672eb29")]
public partial class RangeDetectorAction : Action
{
    [SerializeReference] public BlackboardVariable<RangeDetector> Range;
    [SerializeReference] public BlackboardVariable<GameObject> Target;



    protected override Status OnUpdate()
    {
        Target.Value = Range.Value.UpdateDetector();
        return Range.Value.UpdateDetector() == null ? Status.Failure : Status.Success;
    }

}

