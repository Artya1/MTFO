using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEditor.UI;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "StunAI", story: "Stun [Self] For [Duration]", category: "Action", id: "9f185ec8ad52331ef2b165e57a6ed940")]
public partial class StunAiAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyController> Self;
    [SerializeReference] public BlackboardVariable<float> Duration;
    
    protected override Status OnStart()
    {
        
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Self.Value == null)
        {
            Debug.LogError("AI_Controller script not found!");
            return Status.Failure; 
        }

        Self.Value.Stunned(Duration.Value);

        return Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

