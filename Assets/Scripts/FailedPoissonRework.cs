using System.Collections.Generic;
using UnityEngine;


/*This is a failed rework of the Poisson Disc Sampler code to make it function in 3D.
  The code here is not an exhaustive list of everything that was tried but after making
  my Unity freeze for the 6th time I give up trying to rework this in this sense. This is
  being put here as proof of effort. 

  Other things tried that are no longer in this script:
        > Replacing the grid system with a distance check using Vector3.SqrMagnitude
          Scrapped due to consistantly freezing the software.
        > Getting an actual sample point based on the user's position.
          Scrapped due to not playing nice with the grid system
          and freezing the software in several other cases.
        > Probably more I am forgetting about
 */

public class FailedPoissonRework : MonoBehaviour
{
    private const int k = 30;  // Maximum number of attempts before marking a sample as inactive.

    private readonly Vector3 origin;
    private readonly Bounds bounds;
    private readonly float radius3;  // radius squared
    private readonly float cellSize;
    private Vector3[,,] grid;
    private List<Vector2> activeSamples = new List<Vector2>();

    /// Create a sampler with the following parameters:
    ///
    /// width:  each sample's x coordinate will be between [0, width]
    /// height: each sample's y coordinate will be between [0, height]
    /// radius: each sample will be at least `radius` units away from any other sample, and at most 2 * `radius`.
    public FailedPoissonRework(Vector3 pos, float width, float height, float radius)
    {
        origin = pos;
        bounds = new Bounds(Vector3.zero, new Vector3(width, height, width));
        radius3 = radius * radius * radius;
        cellSize = radius / Mathf.Sqrt(3);
        grid = new Vector3[Mathf.CeilToInt(width / cellSize),
                           Mathf.CeilToInt(height / cellSize),
                           Mathf.CeilToInt(width / cellSize)];
    }

    /// Return a lazy sequence of samples. You typically want to call this in a foreach loop, like so:
    ///   foreach (Vector2 sample in sampler.Samples()) { ... }
    public IEnumerable<Vector3> Samples()
    {
        // First sample is choosen randomly
        yield return AddSample(new Vector3(UnityEngine.Random.value * bounds.max.x, UnityEngine.Random.value * bounds.max.y, UnityEngine.Random.value * bounds.max.z));

        while (activeSamples.Count > 0)
        {

            // Pick a random active sample
            int i = (int)UnityEngine.Random.value * activeSamples.Count;
            Vector2 sample = activeSamples[i];

            // Try `k` random candidates between [radius, 2 * radius] from that sample.
            bool found = false;
            for (int j = 0; j < k; j++)
            {

                float angle = 2 * Mathf.PI * UnityEngine.Random.value;
                float r = Mathf.Sqrt(UnityEngine.Random.value * 3 * radius3 + radius3 + radius3); // See: http://stackoverflow.com/questions/9048095/create-random-number-within-an-annulus/9048443#9048443
                Vector2 candidate = sample + r * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // Accept candidates if it's inside the rect and farther than 2 * radius to any existing sample.
                if (bounds.Contains(candidate) && IsFarEnough(candidate))
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

    private bool IsFarEnough(Vector3 sample)
    {
        GridPos pos = new GridPos(sample, cellSize);

        int xmin = Mathf.Max(pos.x - 2, 0);
        int ymin = Mathf.Max(pos.y - 2, 0);
        int xmax = Mathf.Min(pos.x + 2, grid.GetLength(0) - 1);
        int ymax = Mathf.Min(pos.y + 2, grid.GetLength(1) - 1);
        int zmax = Mathf.Max(pos.z - 2, 0);
        int zmin = Mathf.Min(pos.z + 2, grid.GetLength(2) - 1);

        for (int z = zmin; z <= zmax; z++)
        {
            for (int y = ymin; y <= ymax; y++)
            {
                for (int x = xmin; x <= xmax; x++)
                {
                    Vector3 s = grid[x, y, z];
                    if (s != Vector3.zero)
                    {
                        Vector3 d = s - sample;
                        if (d.x * d.x + d.y * d.y + d.z * d.z < radius3)
                        {
                            return false;
                        }
                    }
                }
            }
        }
        return true;

        // Note: we use the zero vector to denote an unfilled cell in the grid. This means that if we were
        // to randomly pick (0, 0) as a sample, it would be ignored for the purposes of proximity-testing
        // and we might end up with another sample too close from (0, 0). This is a very minor issue.
    }

    /// Adds the sample to the active samples queue and the grid before returning it
    private Vector3 AddSample(Vector3 sample)
    {
        activeSamples.Add(sample);
        GridPos pos = new GridPos(sample, cellSize);
        //Debug.Log(cellSize);
        Debug.Log(sample.x + ", " + sample.y + ", " + sample.z);
        grid[pos.x, pos.y, pos.z] = sample;
        return sample;
    }

    /// Helper struct to calculate the x and y indices of a sample in the grid
    private struct GridPos
    {
        public int x;
        public int y;
        public int z;

        public GridPos(Vector3 sample, float cellSize)
        {
            x = (int)(sample.x / cellSize);
            y = (int)(sample.y / cellSize);
            z = (int)(sample.z / cellSize);
        }
    }
}
