using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "Line of Sight Check", story: "Check [Target] With [LOS]", category: "Conditions", id: "efbfccb6e5b502a36d706a99e1fdf284")]
public partial class LineOfSightCheckCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<LineOfSightDetector> LOS;

    public override bool IsTrue()
    {
        Debug.Log(LOS.Value.PerformDetection(Target.Value) != null);
        return LOS.Value.PerformDetection(Target.Value) != null;
    }

}
