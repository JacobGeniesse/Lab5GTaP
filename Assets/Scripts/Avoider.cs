using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Experimental.GlobalIllumination;

//This is a copy of the code in the plugin that controls the avoider.

[RequireComponent(typeof(NavMeshAgent))]
public class Avoider : MonoBehaviour
{
    [Header("Nav Mesh Variables")]
    private NavMeshAgent agent; //nav mesh agent ref
    [SerializeField] private Transform avoidee; //object to avoid

    [Header("Sampling Vars")]
    [SerializeField] private float range = 10; //Range of the cast (width and height)
    [SerializeField] private float radius = 2; //Radius of the disc sampling
    [SerializeField] private float speed = 5; //Speed of the nav mesh agent
    [SerializeField] private bool useGizmos = true; //Should the debug lines for sampling be drawn?
    private float cooldown; //Cooldown between casts
    private float maxCooldown = 1; //Max cooldown between casts

    private List<Vector3> sampleList = new List<Vector3>(); //List of usable samples
    private List<Vector3> fullList = new List<Vector3>(); //List of samples for drawing
    
    void Start()
    {
        //Get the nav mesh agent
        if (TryGetComponent<NavMeshAgent>(out NavMeshAgent navAgent))
        {
            agent = navAgent; //assign agent component
            agent.speed = speed; //Set the speed
        }
        else
        {
            Debug.LogError("No nav mesh agent found!"); //Give an error if there is no nav mesh agent
        }

        if(avoidee == null)
        {
            Debug.LogError("Nothing to avoid!"); //Give an error if the avoidee transform is not assigned
        }

        sampleList = GatherSamples();
    }

    void Update()
    {
        if(fullList.Count > 0)
        {
            DrawSamples(fullList); //Draw the full list of points every frame to indicate position
        }
        if (cooldown <= 0) 
        {
            sampleList = GatherSamples();
            //If the cooldown is less than 0 see if the avoidee can see the avoider
            if (SightCheck(transform.position) == true)
            {
                if (sampleList.Count > 0)
                {
                    //If the avoidee can see the avoider finish the chain to move away from avoidee
                    Vector3 chosenPoint = ChoosePoint(sampleList);
                    MoveAway(chosenPoint);
                }
                else
                {
                    Debug.Log("Nowhere to run!");
                }
            }
            cooldown = maxCooldown; //Start cooldown again
        }
        else
        {
            cooldown -= Time.deltaTime; //Reduce cooldown
        }
    }

    //Helper func for checking sightlines
    private bool SightCheck(Vector3 originPoint)
    {
        if (Physics.Linecast(originPoint, avoidee.position, out RaycastHit hit)) //Check if a line can be drawn from the origin to the avoidee
        {
            if(hit.collider.tag == "Player")
            {
                return true; //If the player is hit return true
            }
            else
            {
                return false; //If not return false
            }
        }
        return false; //If the cast fails return false
    }


    //Helper func for gathering samples
    private List<Vector3> GatherSamples()
    {
        fullList.Clear(); //Clear the full list of points
        var sampler = new PoissonDiscSampler(range, range, radius); //Create a PoissonDiscSampler
        List<Vector3> pointList = new List<Vector3>(); //Create a collection to store candidate hiding spots X
        foreach (var point in sampler.Samples())
        {
            /* I recieved help from Xavier Peterson (classmate) on figuring out how to translate the points from the Poisson Disc Sampler as after hours of trying I could not make it work alone.
             * The rest of the code is mine, specifically the translation was recieved from him with that then being derived from you if he is to be believed.
             * 
             * I, Jacob Geniesse, swear that I tried to translate this several other ways, moreso than included here and none of them yielded the correct results. I have listed below my reasoning for why I 
             * think the formula given works, so that I prove understanding of the material I obtained from another person.
                > Shifting the point's y coord to the new listing's z coord takes it from a vertical point to a horizontal depth point, moving the rect from x,y to x,z.
                > Adding transform.position to the point moves the points from the grid's location of Vector3.zero to the location of the source object.
                > Subtracting by the range/2 moves the poisson disc sampler's originator from the corner of the cast to the center of the cast
            */
            
            
            /*Classmate's Code*/Vector3 pointTranslation = new Vector3(point.x + transform.position.x - range/2, transform.position.y, point.y + transform.position.z - range/2); /*Classmate's Code*/
            
            //My code takes over again
            //Debug.Log(pointTranslation);

            if (SightCheck(pointTranslation) == false) //Foreach point in the PoissonDiscSampler, can the avoidee see it? (check visibility to point)
            {
                if(useGizmos == true) //If using gizmos draw a green line to indicate that the desired location is a safe place to travel to
                {
                    Debug.DrawLine(transform.position, pointTranslation, Color.green);
                }
                pointList.Add(pointTranslation); //Add the point to the list
            }
            else
            {
                if(useGizmos == true) //If using gizmos draw a red line to indicate that the desired location is not a safe place to travel to
                {
                    Debug.DrawLine(transform.position, pointTranslation, Color.red);
                }
            }
            fullList.Add(pointTranslation); //Add point to the full list for drawing
        }

        return pointList; //Return a list of safe points
    }

    //Helper func for choosing a point to travel to
    private Vector3 ChoosePoint(List<Vector3> points)
    {
        Vector3 closestPos = Vector3.zero; //Set the closestPos to zero
        if (points.Count > 0)
        {
            closestPos = points[0]; //If the list is larger than zero, add the first point from that list as the closest point
        }
        float closenessScore = 99999; //Set the closeness score large enough that the first search will be garunteed to be smaller
        foreach (Vector3 pos in points)
        {
            //Calc the sqrmagnitude distance between the points
            Vector3 pointOffset = transform.position - pos;
            float closeness = Vector3.SqrMagnitude(pointOffset);

            //If the this point is closer than the reigning closeness score
            if (closeness < closenessScore)
            {
                closestPos = pos; //Set the position
                closenessScore = closeness; //Update the closeness score
            }
        }

        return closestPos; //Return the closest position
    }

    //Helper func for setting the nav mesh target
    private void MoveAway(Vector3 targetPos)
    {
        if(agent != null)
        {
            agent.SetDestination(targetPos); //Set the agent's target destination
        }
    }


    //Helper func for drawing lines of samples every frame without needing to calculate all of the sample stuff again
    private void DrawSamples(List<Vector3> points)
    {
        if (useGizmos)
        {
            foreach (Vector3 pos in points)
            {
                if (SightCheck(pos) == false) //Foreach point in the PoissonDiscSampler, can the avoidee see it? (check visibility to point)
                {
                    //If using gizmos draw a green line to indicate that the desired location is a safe place to travel to
                    Debug.DrawLine(transform.position, pos, Color.green);
                }
                else
                {
                     //If using gizmos draw a red line to indicate that the desired location is not a safe place to travel to
                     Debug.DrawLine(transform.position, pos, Color.red);
                }
            }
        }
    }
}
