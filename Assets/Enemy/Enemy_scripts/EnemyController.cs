using UnityEngine;
using UnityEngine.AI;
using Unity.Behavior;
using System;
public class EnemyController : MonoBehaviour
{   
    public  BehaviorGraph behaviorTree; 
    private NavMeshAgent navmeshagent;
    public BlackboardVariable istunned;
    public BlackboardVariable StunDuration;

    void Awake()
    {
        navmeshagent = GetComponent<NavMeshAgent>();
        
        
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Medium")
        {
            if (behaviorTree.BlackboardReference.GetVariable("IsStunned", out istunned) && behaviorTree.BlackboardReference.GetVariable("StunDuration", out StunDuration))
            {
                istunned.ObjectValue = true;
                StunDuration.ObjectValue = 5;
                Debug.Log("AI has been stunned!");
            }   

        }
    }

  
    public void Stunned(float duration)
    {
        Debug.Log($"has been stunned for {duration} seconds!");
        navmeshagent.isStopped = true;

        Invoke(nameof(RecoverFromStun), duration);
    }

    private void RecoverFromStun()
    {
        Debug.Log("AI has recovered from stun.");
        navmeshagent.isStopped = false;
    }


}
