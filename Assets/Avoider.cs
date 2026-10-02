using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public class Avoider : MonoBehaviour
{
    //nav mesh values
    private NavMeshAgent agent;
    [SerializeField] private Transform avoidee;

    //Sample Values
    [SerializeField] private float range = 3;
    [SerializeField] private float radius = 3;
    [SerializeField] private float speed;
    [SerializeField] private bool useGizmos;

    public int count;

    private float cooldown;
    private float maxCooldown = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        if (TryGetComponent<NavMeshAgent>(out NavMeshAgent navAgent))
        {
            agent = navAgent;
            agent.speed = speed;
        }
        else
        {
            Debug.LogWarning("No nav mesh agent found!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (cooldown <= 0)
        {
            if (SightCheck(transform.position) == true)
            {
                MoveAway(ChosePoint(GatherSamples()));
                cooldown = maxCooldown;
            }
        }
        else
        {
            cooldown -= Time.deltaTime;
        }
    }

    private bool SightCheck(Vector3 originPoint)
    {
        if (Physics.Linecast(originPoint, avoidee.position, out RaycastHit hit))
        {
            if(hit.collider.tag == "Player")
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }
        return false;
    }


    //Gather Samples
    private List<Vector3> GatherSamples()
    {
        var sampler = new PoissonDiscSampler(range, range, radius); //Create a PoissonDiscSampler
        List<Vector3> pointList = new List<Vector3>(); //Create a collection to store candidate hiding spots X
        foreach (var point in sampler.Samples())
        {
            /*
             * Full disclosure, the sample pos solution did not originate with me. I asked Xavier Peterson (Classmate) for help
             * after struggling for most of class wednesday and for hours on friday trying to get my attempted solution to work.
             * He told me that you ended up giving him help with this since he and his team were also having trouble. The rest of this script
             * is my code, specifically the samplePos solution is not however
             */
            Vector3 samplePos = new Vector3(transform.position.x + point.x - range / 2, transform.position.y, transform.position.z + point.y - range / 2);
            if (SightCheck(samplePos) == false) //Foreach point in the PoissonDiscSampler, can the avoidee see it? (check visibility to point)
            {
                if(useGizmos == true)
                {
                    Debug.DrawLine(transform.position, samplePos, Color.green);
                }
                pointList.Add(samplePos);
            }
            else
            {
                if(useGizmos == true)
                {
                    Debug.DrawLine(transform.position, samplePos, Color.red);
                }
            }
        }

        return pointList;
    }

    private Vector3 ChosePoint(List<Vector3> points)
    {
        Vector3 closestPos = Vector3.zero;
        float closenessScore = 99999;
        foreach (Vector3 pos in points)
        {
            Vector3 pointOffset = transform.position - pos;
            float closeness = Vector3.SqrMagnitude(pointOffset);

            if (closeness < closenessScore)
            {
                closestPos = pos;
                closenessScore = closeness;
            }
        }

        return closestPos;
    }

    private void MoveAway(Vector3 targetPos)
    {
        if(agent != null)
        {
            agent.SetDestination(targetPos);
        }
    }

}
