using UnityEngine;
using System.Collections;
using Dreamteck.Splines;

public class BallSpawner : MonoBehaviour
{
    GameManager gameManager;

    SplineComputer spline;

    [SerializeField] GameObject ball;
    
    public Segment segmentForNewBalls;

    private float splineLength;
    private float diameter;
    private float positionForSpawn;
    public float startSpeed = 2.5f;

    private int range;
    private int downRange;
    private int upRange;
    private int skinIndex;
    public int minSkinIndex;

    void Start()
    {
        gameManager = GameManager.instance;

        diameter = gameManager.diameter;

        spline = gameManager.spline;
        splineLength = gameManager.splineLength;

        positionForSpawn = 118f;

        range = 0;
        downRange = 2;
        upRange = downRange + 3;

        minSkinIndex = 0; // ball range is between 2-64
        
        /*balls coming from spline*/
        SplinePositioner newBallPositioner = SpawnBallsOnSpline(startSpeed);
        StartCoroutine(WaitForBall(newBallPositioner));
    }

    // Spawn On Spline
    SplinePositioner SpawnBallsOnSpline(float speed_)
    {
        // spawn ball and set its position on spline
        GameObject newBall = Instantiate(ball, transform);

        // set speed
        newBall.GetComponent<ZumaBall>().speed = speed_;

        // set newball position on spline
        SplinePositioner positioner = newBall.GetComponent<SplinePositioner>();
        positioner.spline = spline;
        positioner.SetPercent(positionForSpawn / splineLength);
        positioner.RebuildImmediate();

        // add ball to segment
        segmentForNewBalls.segment.Insert(0, newBall);

        // generate range and material
        if (range == 0)
        {
            GenerateRange();
            int oldSkinIndex = skinIndex;
            while (skinIndex == oldSkinIndex)
                skinIndex = GenerateSkinIndex();
        }

        // set skin and id
        gameManager.skinManager.ChangeSkin(newBall, skinIndex);

        range--;

        return positioner;
    }

    IEnumerator WaitForBall(SplinePositioner positioner)
    {
        // track position
        float position = positionForSpawn;
        while (position < positionForSpawn + diameter)
        {
            yield return null;
            position = (float)positioner.GetPercent() * splineLength;
        }

        // prev ball's speed
        float speed = positioner.GetComponent<ZumaBall>().speed;

        // spawn another ball and track it
        SplinePositioner newBallPositioner = SpawnBallsOnSpline(speed);
        StartCoroutine(WaitForBall(newBallPositioner));
    }

    // generate range of balls
    void GenerateRange()
    {
        range = Random.Range(downRange, upRange);
    }

    public int GenerateSkinIndex()
    {
        int number = Random.Range(minSkinIndex, minSkinIndex + 6);
        return number;
    }

    /*void SpawnBallsOnSpline()
    {
        Segment newSegment = new();

        for (int i = 0; i < ballsRangeOnSpline; i += 0)
        {
            //generate skin type
            int skinIndex = Random.Range(0, GameManager.instance.ballManager.types.Length);

            //generate range of similar balls
            int range = Random.Range(3, 6);

            //iterate according to the given values
            for (int j = 0; j < range && i < ballsRangeOnSpline; j++)
            {
                GameObject newBall = Instantiate(ball, transform);
                SplinePositioner positioner = newBall.GetComponent<SplinePositioner>();
                positioner.spline = spline;
                float positionOnSpline = i * diameter / splineLength;
                positioner.SetPercent(positionOnSpline);
                positioner.RebuildImmediate();

                //change skin
                GameManager.instance.skinManager.ChangeSkin(newBall, skinIndex);

                // set id for ball
                newBall.GetComponent<ZumaBall>().id = skinIndex;

                // add ball to segment0
                newSegment.segment.Add(newBall);
                i++;
            }
        }

        // add newSegment to list of segments
        gameManager.segmentManager.segments.Add(newSegment);
    }*/
}
