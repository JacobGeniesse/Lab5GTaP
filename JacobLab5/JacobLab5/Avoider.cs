using System;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace JacobLab5
{
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

            if (avoidee == null)
            {
                Debug.LogError("Nothing to avoid!"); //Give an error if the avoidee transform is not assigned
            }

            sampleList = GatherSamples(); //Initial sample gathering for line drawing
        }

        void Update()
        {
            bool resetCooldown = true; //Should the cooldown be reset?
            if (fullList.Count > 0)
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
                        resetCooldown = false; //If there's nowhere to run, skip the cooldown and instantly try again
                    }
                }
                if (resetCooldown == true)
                {
                    cooldown = maxCooldown; //Start cooldown again
                }
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
                if (hit.collider.tag == "Player")
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


                /*Classmate's Code*/
                Vector3 pointTranslation = new Vector3(point.x + transform.position.x - range / 2, transform.position.y, point.y + transform.position.z - range / 2); /*Classmate's Code*/

                //My code takes over again
                //Debug.Log(pointTranslation);

                if (SightCheck(pointTranslation) == false) //Foreach point in the PoissonDiscSampler, can the avoidee see it? (check visibility to point)
                {

                    pointList.Add(pointTranslation); //Add the point to the list
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
                float closeness = 0; //Set closeness score to 0

                NavMeshPath testPath = new NavMeshPath(); //Create an empty path
                agent.CalculatePath(pos, testPath); //Calculate a path to the desired point
                agent.SetPath(testPath); //set the path

                //If the path is calculated
                if (!agent.pathPending)
                {
                    //If there are more corners than 0 on a path
                    if (testPath.corners.Length > 0)
                    {
                        //Calc the sqrmagnitude distance between the avoider and first corner
                        Vector3 pointOffset = transform.position - testPath.corners[0];
                        closeness += Vector3.SqrMagnitude(pointOffset);

                        //For each additional corner add on the sqrmagnitude to get a more accurate distance calc
                        for (int i = 1; i < testPath.corners.Length; i++)
                        {
                            pointOffset = testPath.corners[i - 1] - testPath.corners[i];
                            closeness += Vector3.SqrMagnitude(pointOffset);
                        }
                    }
                    else
                    {
                        //If there are no corners default to the distance between the avoider and the pos
                        Vector3 pointOffset = transform.position - pos;
                        closeness += Vector3.SqrMagnitude(pointOffset);
                    }
                }

                //If the pos is the avoidee's position, ignore it
                if (pos == avoidee.position)
                {
                    closeness = 9999;
                }

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
            if (agent != null)
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
                    if (SightCheck(pos) == false && pos != avoidee.position) //Foreach point in the PoissonDiscSampler, can the avoidee see it? (check visibility to point)
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


    //Poisson disc sampler code written by Gregory Schlomoff and obtained via the link provided in the assignment description.
    public class PoissonDiscSampler
    {
        private const int k = 30;  // Maximum number of attempts before marking a sample as inactive.

        private readonly Rect rect;
        private readonly float radius2;  // radius squared
        private readonly float cellSize;
        private Vector2[,] grid;
        private List<Vector2> activeSamples = new List<Vector2>();

        /// Create a sampler with the following parameters:
        ///
        /// width:  each sample's x coordinate will be between [0, width]
        /// height: each sample's y coordinate will be between [0, height]
        /// radius: each sample will be at least `radius` units away from any other sample, and at most 2 * `radius`.
        public PoissonDiscSampler(float width, float height, float radius)
        {
            rect = new Rect(0, 0, width, height);
            radius2 = radius * radius;
            cellSize = radius / Mathf.Sqrt(2);
            grid = new Vector2[Mathf.CeilToInt(width / cellSize),
                               Mathf.CeilToInt(height / cellSize)];
        }

        /// Return a lazy sequence of samples. You typically want to call this in a foreach loop, like so:
        ///   foreach (Vector2 sample in sampler.Samples()) { ... }
        public IEnumerable<Vector2> Samples()
        {
            // First sample is choosen randomly
            yield return AddSample(new Vector2(UnityEngine.Random.value * rect.width, UnityEngine.Random.value * rect.height));

            while (activeSamples.Count > 0)
            {

                // Pick a random active sample
                int i = (int)UnityEngine.Random.value * activeSamples.Count;
                Vector2 sample = activeSamples[i];

                // Try `k` random candidates between [radius, 2 * radius] from that sample.
                bool found = false;
                for (int j = 0; j < k; ++j)
                {

                    float angle = 2 * Mathf.PI * UnityEngine.Random.value;
                    float r = Mathf.Sqrt(UnityEngine.Random.value * 3 * radius2 + radius2); // See: http://stackoverflow.com/questions/9048095/create-random-number-within-an-annulus/9048443#9048443
                    Vector2 candidate = sample + r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                    // Accept candidates if it's inside the rect and farther than 2 * radius to any existing sample.
                    if (rect.Contains(candidate) && IsFarEnough(candidate))
                    {
                        found = true;
                        yield return AddSample(candidate);
                        break;
                    }
                }

                // If we couldn't find a valid candidate after k attempts, remove this sample from the active samples queue
                if (!found)
                {
                    activeSamples[i] = activeSamples[activeSamples.Count - 1];
                    activeSamples.RemoveAt(activeSamples.Count - 1);
                }
            }
        }

        private bool IsFarEnough(Vector2 sample)
        {
            GridPos pos = new GridPos(sample, cellSize);

            int xmin = Mathf.Max(pos.x - 2, 0);
            int ymin = Mathf.Max(pos.y - 2, 0);
            int xmax = Mathf.Min(pos.x + 2, grid.GetLength(0) - 1);
            int ymax = Mathf.Min(pos.y + 2, grid.GetLength(1) - 1);

            for (int y = ymin; y <= ymax; y++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    Vector2 s = grid[x, y];
                    if (s != Vector2.zero)
                    {
                        Vector2 d = s - sample;
                        if (d.x * d.x + d.y * d.y < radius2) return false;
                    }
                }
            }

            return true;

            // Note: we use the zero vector to denote an unfilled cell in the grid. This means that if we were
            // to randomly pick (0, 0) as a sample, it would be ignored for the purposes of proximity-testing
            // and we might end up with another sample too close from (0, 0). This is a very minor issue.
        }

        /// Adds the sample to the active samples queue and the grid before returning it
        private Vector2 AddSample(Vector2 sample)
        {
            activeSamples.Add(sample);
            GridPos pos = new GridPos(sample, cellSize);
            grid[pos.x, pos.y] = sample;
            return sample;
        }

        /// Helper struct to calculate the x and y indices of a sample in the grid
        private struct GridPos
        {
            public int x;
            public int y;

            public GridPos(Vector2 sample, float cellSize)
            {
                x = (int)(sample.x / cellSize);
                y = (int)(sample.y / cellSize);
            }
        }
    }

}
