using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.GlobalIllumination;

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

    private float cooldown;
    private float maxCooldown = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(TryGetComponent<NavMeshAgent>(out NavMeshAgent navAgent))
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
        if(cooldown <= 0)
        {
            if (SightCheck(transform.position) == true)
            {
                cooldown = maxCooldown;
                
                Debug.Log(GatherSamples().Count);
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

            return true;
        }
        else
        {
            return false;
        }
    }


    //Gather Samples
    private List<Vector3> GatherSamples()
    {
        var sampler = new PoissonDiscSampler(transform.position, range, range, radius); //Create a PoissonDiscSampler
        List<Vector3> pointList= new List<Vector3>(); //Create a collection to store candidate hiding spots X
        foreach (var point in sampler.Samples())
        {
            if (SightCheck(point)) //Foreach point in the PoissonDiscSampler, can the avoidee see it? (check visibility to point)
            {
                //if (useGizmos) //Foreach point visualize a line to it
                //{
                //    Gizmos.color = Color.red;
                //    Gizmos.DrawLine(transform.position, point);
                //}
                //Yes: ignore that point
            }
            else
            {
                //if (useGizmos) //Foreach point visualize a line to it
                //{
                //    Gizmos.color = Color.green;
                //    Gizmos.DrawLine(transform.position, point);
                //}
                pointList.Add(point);
                //No: add the point to the candidate list
            }
        }
        
        return pointList;

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        var sampler = new PoissonDiscSampler(transform.position, range, range, radius); //Create a PoissonDiscSampler
        List<Vector2> pointList = new List<Vector2>(); //Create a collection to store candidate hiding spots X
        foreach (Vector2 point in sampler.Samples())
        {
            Gizmos.DrawLine(transform.position, point);
            if (useGizmos) //Foreach point visualize a line to it
            {
            }
        }
    }



    //Poisson Disc Sampling Stuff
}
