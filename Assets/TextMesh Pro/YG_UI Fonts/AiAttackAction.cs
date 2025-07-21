using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "AI Attack", story: "[EnemyController] Attacks [Target]", category: "Action", id: "b741246f486e1f25e8780f07e3b0c249")]
public partial class AiAttackAction : Action
{
    [SerializeReference] public BlackboardVariable<EnemyController> EnemyController;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    
    public PlayerStats playerStats;

    protected override Status OnStart()
    {
        if (EnemyController.Value == null || Target.Value == null)
        {
            playerStats = Target.Value.GetComponent<PlayerStats>();
        }
        return Status.Running;

    }

    protected override Status OnUpdate()
    {   
        playerStats.dmgTaken(35);
        return Status.Success;
    }

    protected override void OnEnd()
    {
        
    }
}

