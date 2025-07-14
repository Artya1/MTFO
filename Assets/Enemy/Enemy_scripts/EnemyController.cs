using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{   
    
    private NavMeshAgent navmeshagent;

    void Awake()
    {
        navmeshagent = GetComponent<NavMeshAgent>();
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
