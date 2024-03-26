using UnityEngine;
using System.Collections.Generic;
using Dreamteck.Splines;
using System.Collections;
using DG.Tweening;

public class SegmentManager : MonoBehaviour
{
    GameManager gameManager;
    AudioManager audioManager;
    BallSpawner ballSpawner;

    [HideInInspector] public GameObject newBall = null;
    [HideInInspector] public GameObject contactedBall = null;
    [HideInInspector] public GameObject helper;
    private GameObject ballToFollow;

    SplineComputer spline;

    private PlayerBall playerBallScript; // of newball

    [SerializeField] private int contactedballIndex;
    [SerializeField] private int contactedSegmentIndex;

    // newball index after reposition
    [SerializeField] private int newBallIndex;

    public float normalSpeed;
    private float splineLength;
    public float angle;
    private float fastSpeed = 12f;
    private float diameter;

    public bool RPowerUpEnabled;

    public List<Segment> segments;
    private Segment contactedSegment;


    private void Start()
    {
        gameManager = GameManager.instance;
        audioManager = AudioManager.instance;
        ballSpawner = gameManager.ballSpawner;
        diameter = gameManager.diameter;
        spline = gameManager.spline;
        splineLength = gameManager.splineLength;
        ballToFollow = null;
        RPowerUpEnabled = false;
        contactedballIndex = -1;
    }


    private void Update()
    {
        // segment0 speed speedup if its too near to gate
        ArrangeSpeedOnEdges();

        // insert new ball
        if (newBall != null && contactedBall != null)
        {
            InsertNewBall();
        }

        // connections between segments
        if (segments.Count > 1)
        {
            CheckAndConnectAllSegments();
        }
    }

    private void ArrangeSpeedOnEdges()
    {
        if (segments.Count == 1)
        {
            Segment segment = segments[0];
            GameObject targetBall = segment.segment[^1];
            double position = targetBall.GetComponent<ZumaBallRotator>().position;
            float speed = targetBall.GetComponent<ZumaBall>().speed;
            if (position < 130 && speed == ballSpawner.startSpeed)
            {
                AddNewBallsToSegmentZero();

                foreach (GameObject ball in segment.segment)
                {
                    ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
                    zumaBall.speed = fastSpeed - 1;
                }
                gameManager.player.DisablePlayer();
            }

            else if (position > 160 && speed == fastSpeed - 1 )
            {
                AddNewBallsToSegmentZero();

                foreach (GameObject ball in segment.segment)
                {
                    ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
                    zumaBall.speed = ballSpawner.startSpeed;
                }
                gameManager.player.EnablePlayer();
            }
        }
        else if (segments.Count == 0)
        {
            Segment segment = ballSpawner.segmentForNewBalls;
            GameObject targetBall = segment.segment[^1];
            double position = targetBall.GetComponent<ZumaBallRotator>().position;
            float speed = targetBall.GetComponent<ZumaBall>().speed;
            if (position > 121 && position < 122 && speed == ballSpawner.startSpeed)
            {
                foreach (GameObject ball in segment.segment)
                {
                    ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
                    zumaBall.speed = fastSpeed - 1;
                }
            }

            else if (position > 160 && position < 161 && speed == fastSpeed - 1)
            {
                foreach (GameObject ball in segment.segment)
                {
                    ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
                    zumaBall.speed = ballSpawner.startSpeed;
                }
                gameManager.player.EnablePlayer();
            }
        }
    }

    void CheckAndConnectAllSegments()
    {
        for (int i = 1; i < segments.Count; i++)
        {
            // target segments
            Segment frontSegment = segments[i];
            Segment backSegment = segments[i - 1];

            // target balls of each segment
            GameObject frontSegmentFirstBall = frontSegment.segment[0];
            GameObject backSegmentLastBall = backSegment.segment[^1];

            // id of each ball
            int frontId = frontSegmentFirstBall.GetComponent<ZumaBall>().id;
            int backId = backSegmentLastBall.GetComponent<ZumaBall>().id;

            // pull other segment
            if (frontId == backId)
            {
                StartCoroutine(PullFrontSegmentBack(frontSegment));
            }

            // distance between 2 balls
            double distance = frontSegmentFirstBall.GetComponent<ZumaBallRotator>().position - backSegmentLastBall.GetComponent<ZumaBallRotator>().position;
            if (distance <= 2 && distance > 0)
            {
                // Merge
                bool merged = MergeBallsOnSpline(frontSegment, backSegment, frontSegmentFirstBall, backSegmentLastBall, frontId, backId);

                // Not merge
                if (!merged && !RPowerUpEnabled)
                {
                    float speed = backSegmentLastBall.GetComponent<ZumaBall>().speed;
                    double position = backSegmentLastBall.GetComponent<ZumaBallRotator>().position;
                    while (frontSegment.segment.Count > 0)
                    {
                        // target ball
                        GameObject ball = frontSegment.segment[0];
                        // change segment
                        frontSegment.segment.Remove(ball);
                        backSegment.segment.Add(ball);
                        // set speed
                        ball.GetComponent<ZumaBall>().speed = speed;
                        // reset position on spline
                        position += diameter;
                        SplinePositioner positioner = ball.GetComponent<SplinePositioner>();
                        positioner.SetPercent(position / splineLength);
                        positioner.RebuildImmediate();
                    }

                    // delete empty front segment
                    segments.Remove(frontSegment);

                    // reset some variables. needed when inserting of newball happens at the same time as connection of segments
                    if (newBall != null && contactedBall != null)
                    {
                        float oldNewBallIndex = newBallIndex;
                        GetIndexesOnSegment(contactedBall);
                        if (angle > 90)
                            newBallIndex = contactedballIndex - 1;
                        else
                            newBallIndex = contactedballIndex + 1;
                        // if newball was on the front segment
                        if (oldNewBallIndex < newBallIndex)
                        {
                            normalSpeed = speed;
                        }
                    }
                }
                else if (!merged && RPowerUpEnabled)
                {
                    float speed = frontSegmentFirstBall.GetComponent<ZumaBall>().speed;
                    double position = frontSegmentFirstBall.GetComponent<ZumaBallRotator>().position;
                    while (backSegment.segment.Count > 0)
                    {
                        // target ball
                        GameObject ball = backSegment.segment[^1];
                        // change segment
                        backSegment.segment.Remove(ball);
                        frontSegment.segment.Insert(0, ball);
                        // set speed
                        ball.GetComponent<ZumaBall>().speed = speed;
                        // reset position on spline
                        position -= diameter;
                        SplinePositioner positioner = ball.GetComponent<SplinePositioner>();
                        positioner.SetPercent(position / splineLength);
                        positioner.RebuildImmediate();
                    }

                    // delete empty back segment
                    segments.Remove(backSegment);
                }
            }
        }
    }

    IEnumerator PullFrontSegmentBack(Segment frontSegment)
    {
        yield return new WaitForSeconds(0.2f);
        foreach (GameObject ball in frontSegment.segment)
        {
            ball.GetComponent<ZumaBall>().speed = -fastSpeed * 2;
        }
    }

    void InsertNewBall()
    {
        // get indexes of interacting balls | plays once
        if (contactedballIndex < 0)
            GetIndexesOnSegment(contactedBall);

        // get angle | plays once
        GetAngle();

        // prepare new ball | plays once
        PrepareNewComingBall();

        // smooth integration of all balls | plays every frame
        SmoothIntegration();
    }

    void GetIndexesOnSegment(GameObject contactedBall)
    {
        for (int j = 0; j < segments.Count; j++)
        {
            // target segment
            Segment segment = segments[j];

            // iterate over balls on that segment
            for (int i = 0; i < segment.segment.Count; i++)
            {
                if (segment.segment[i] == contactedBall)
                {
                    contactedballIndex = i;
                    contactedSegmentIndex = j;
                    contactedSegment = segments[j];
                    return;
                }
            }
        }
    }

    void GetAngle()
    {
        if (helper == null)
        {
            Vector3 dir = (newBall.transform.position - contactedBall.transform.position).normalized;

            helper = new GameObject();
            SplinePositioner helperPositioner = helper.AddComponent<SplinePositioner>();
            helperPositioner.spline = spline;
            ZumaBallRotator rotator = contactedBall.GetComponent<ZumaBallRotator>();
            double helperPos = (rotator.position + 1) / splineLength;
            helperPositioner.SetPercent(helperPos);
            helperPositioner.RebuildImmediate();
            Vector3 dir2 = (helper.transform.position - contactedBall.transform.position).normalized;

            angle = Vector3.Angle(dir, dir2);

            // insert newball to contacted segment and define its index on it
            if (angle > 90)
            {
                newBallIndex = contactedballIndex;
                contactedSegment.segment.Insert(newBallIndex, newBall);
            }
            else
            {
                newBallIndex = contactedballIndex + 1;
                contactedSegment.segment.Insert(newBallIndex, newBall);
            }
        }
    }

    void PrepareNewComingBall()
    {
        if (newBall.transform.parent != null)
        {
            // untag
            newBall.tag = "Untagged";

            // unparent
            GameObject parent = newBall.transform.parent.gameObject;
            newBall.transform.SetParent(null);
            Destroy(parent);

            // add needed components
            newBall.GetComponent<SphereCollider>().isTrigger = true;
            newBall.AddComponent<Rigidbody>().useGravity = false;

            // take reference to newball.playerball script
            playerBallScript = newBall.GetComponent<PlayerBall>();
        }
    }

    void SmoothIntegration()
    {
        // smoothen reposition of balls
        if (angle > 90)
        {
            // plays once
            if (ballToFollow == null && contactedballIndex > 0)
            {
                ballToFollow = contactedSegment.segment[contactedballIndex - 1];
                normalSpeed = ballToFollow.GetComponent<ZumaBall>().speed;

                // other balls | speed them a little bit
                for (int i = contactedballIndex; i < contactedSegment.segment.Count; i++)
                {
                    contactedSegment.segment[i].GetComponent<ZumaBall>().speed = fastSpeed;
                }
            }
            // plays every frame
            IntegrateNewBall();
        }
        else // if angle <= 90
        {
            // plays once
            if (ballToFollow == null)
            {
                ballToFollow = contactedBall;
                normalSpeed = ballToFollow.GetComponent<ZumaBall>().speed;

                // other balls | speed them a little bit
                for (int i = contactedballIndex + 1; i < contactedSegment.segment.Count; i++)
                {
                    contactedSegment.segment[i].GetComponent<ZumaBall>().speed = fastSpeed;
                }
            }
            // plays every frame
            IntegrateNewBall();
        }
    }

    void IntegrateNewBall()
    {
        if (contactedballIndex == 0 && angle > 90)
        {
            // assign ball to follow
            if (ballToFollow == null)
            {
                ballToFollow = contactedBall;
                normalSpeed = ballToFollow.GetComponent<ZumaBall>().speed;;
            }
            // position of helper 
            ZumaBallRotator rotator = ballToFollow.GetComponent<ZumaBallRotator>();
            double newHelperPos = (rotator.position - diameter) / splineLength;
            SplinePositioner helperPositioner = helper.GetComponent<SplinePositioner>();
            helperPositioner.SetPercent(newHelperPos);
            helperPositioner.RebuildImmediate();

            // extreme situation lol
            if (contactedSegmentIndex > 0 && ballToFollow.GetComponent<ZumaBallRotator>().position - segments[contactedSegmentIndex - 1].segment[^1].GetComponent<ZumaBallRotator>().position < 4)
            {
                foreach (GameObject ball in contactedSegment.segment)
                {
                    SplinePositioner positioner = ball.GetComponent<SplinePositioner>();
                    float moveAmount = fastSpeed / 2 * Time.deltaTime;
                    float percent = moveAmount / splineLength;
                    percent += (float)positioner.GetPercent();
                    positioner.SetPercent(percent);
                }
            }
        }
        else // if contactedball index != 0 or angle < 90
        {
            // if index != 0  position of helper
            ZumaBallRotator rotator = ballToFollow.GetComponent<ZumaBallRotator>();
            double newHelperPos = (rotator.position + diameter) / splineLength;
            SplinePositioner helperPositioner = helper.GetComponent<SplinePositioner>();
            helperPositioner.SetPercent(newHelperPos);
            helperPositioner.RebuildImmediate();
            // extreme situation lol
            if (contactedSegmentIndex < (segments.Count - 1) && newBall == contactedSegment.segment[^1] && segments[contactedSegmentIndex + 1].segment[0].GetComponent<ZumaBallRotator>().position - ballToFollow.GetComponent<ZumaBallRotator>().position < 4)
            {
                foreach (GameObject ball in segments[contactedSegmentIndex + 1].segment)
                {
                    SplinePositioner positioner = ball.GetComponent<SplinePositioner>();
                    float moveAmount = fastSpeed / 2 * Time.deltaTime;
                    float percent = moveAmount / splineLength;
                    percent += (float)positioner.GetPercent();
                    positioner.SetPercent(percent);
                }
            }
        }

        // plays once for all angle and index conditions
        if (playerBallScript.target == null)
        {
            playerBallScript.target = helper.transform;
            playerBallScript.state = PlayerBall.State.Integrate;
        }
    }

    public void Reposition()
    {
        // enable newball spline positioner
        SplinePositioner newBallPositioner = newBall.GetComponent<SplinePositioner>();
        newBallPositioner.spline = spline;
        newBallPositioner.enabled = true;

        double position = 0;
        if (angle > 90)
        {
            if (contactedballIndex == 0)
            {
                position = contactedBall.GetComponent<SplinePositioner>().GetPercent() * splineLength;

                // target ball is newball

                // target ball's new position
                double newBallPos = position - diameter;
                newBallPositioner.SetPercent(newBallPos / splineLength);
                newBallPositioner.RebuildImmediate();
            }
            else
            {
                for (int i = 0; i < contactedSegment.segment.Count; i++)
                {
                    // 0 index ball
                    if (i == 0)
                    {
                        // target ball
                        GameObject ball = contactedSegment.segment[i];

                        // change position variable
                        position = ball.GetComponent<SplinePositioner>().GetPercent() * splineLength;
                    }
                    else // index more than 0
                    {
                        // target ball
                        GameObject ball = contactedSegment.segment[i];

                        // target ball's new position
                        SplinePositioner positioner = ball.GetComponent<SplinePositioner>();
                        double newPos = position + diameter;
                        positioner.SetPercent(newPos / splineLength);
                        positioner.RebuildImmediate();
                        
                        // change speed to normal
                        ball.GetComponent<ZumaBall>().speed = normalSpeed;
                        
                        // change position variable
                        position = newPos;
                    }
                }
            }
            
        }
        else // angle <= 90
        {
            if (contactedballIndex + 1 == contactedSegment.segment.Count)
            {
                position = contactedBall.GetComponent<SplinePositioner>().GetPercent() * splineLength;
                // target ball is newball

                // target ball's new position
                double newBallPos = position + diameter;
                newBallPositioner.SetPercent(newBallPos / splineLength);
                newBallPositioner.RebuildImmediate();
            }
            else
            {
                for (int i = 0; i < contactedSegment.segment.Count; i++)
                {
                    // 0 index ball
                    if (i == 0)
                    {
                        // target ball
                        GameObject ball = contactedSegment.segment[i];
                        
                        // change position variable
                        position = ball.GetComponent<SplinePositioner>().GetPercent() * splineLength;
                    }
                    else // index more than 0
                    {
                        // target ball
                        GameObject ball = contactedSegment.segment[i];
                        
                        // target ball's new position
                        SplinePositioner positioner = ball.GetComponent<SplinePositioner>();
                        double newPos = position + diameter;
                        positioner.SetPercent(newPos / splineLength);
                        positioner.RebuildImmediate();
                        
                        // change speed to normal
                        ball.GetComponent<ZumaBall>().speed = normalSpeed;
                        
                        // change position variable
                        position = newPos;
                    }
                }
            }
        }
        // merge
        MergeBallsOnShoot();

        // reset values of segment manager
        ResetValues();
    }

    void ResetValues()
    {
        newBall.tag = "Ball";
        Destroy(helper);
        newBall = null;
        contactedBall = null;
        contactedSegment = null;
        contactedballIndex = -1;
        ballToFollow = null;
        playerBallScript = null;
    }

    private void MergeBallsOnShoot()
    {
        // variables
        int idToScan = newBall.GetComponent<ZumaBall>().id;
        List<GameObject> ballsToMerge = new();
        ballsToMerge.Add(newBall);
        int indexForBallOfChange = newBallIndex;

        // scan balls in front of newball
        for (int i = newBallIndex + 1; i < contactedSegment.segment.Count; i++)
        {
            GameObject ball = contactedSegment.segment[i];
            int ballId = ball.GetComponent<ZumaBall>().id;
            if (ballId == idToScan)
            {
                ballsToMerge.Add(ball);
            }
            else
            {
                break;
            }
        }

        // scan balls at back of newball
        for (int i = newBallIndex - 1; i >= 0; i--)
        {
            GameObject ball = contactedSegment.segment[i];
            int ballId = ball.GetComponent<ZumaBall>().id;
            if (ballId == idToScan)
            {
                ballsToMerge.Insert(0, ball);
                indexForBallOfChange = i;
            }
            else
                break;
        }

        // merge if conditions are available
        if (ballsToMerge.Count >= 3)
        {
            // ball to change
            GameObject ballToChange = ballsToMerge[0];
            ballToChange.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
            int newId = idToScan + 1;
            gameManager.skinManager.ChangeSkin(ballToChange, newId);
            audioManager.Play("Merge");
            ballToChange.transform.DOScale(1, 0.3f).OnComplete(()=> {
                CheckMerge(ballToChange, newId);
            }
            );
            // other balls
            for (int i = 1; i < ballsToMerge.Count; i++)
            {
                // ball
                GameObject ball = ballsToMerge[i];
                // segmental changes
                contactedSegment.segment.Remove(ball);
                // animation
                ball.GetComponent<ZumaBall>().Merge(ballToChange);
            }

            // form new segment for front balls
            Segment newSegment = new();
            segments.Insert(contactedSegmentIndex + 1, newSegment);
            for (int i = indexForBallOfChange + 1; i < contactedSegment.segment.Count; i += 0)
            {
                // target ball
                GameObject ball = contactedSegment.segment[i];

                // actions
                contactedSegment.segment.Remove(ball);
                newSegment.segment.Add(ball);
                ball.GetComponent<ZumaBall>().speed = 0;
            }
            
            // delete segment if empty
            if (newSegment.segment.Count == 0)
                segments.Remove(newSegment);
        }
    }

    void CheckMerge(GameObject ball, int idToScan)
    {
        // variables
        int ballIndex = 0;
        int segmentIndex = 0;
        Segment segment = null;
        // find indexes
        for (int j = 0; j < segments.Count; j++)
        {
            // target segment
             Segment segment_ = segments[j];

            // iterate over balls on that segment
            for (int i = 0; i < segment_.segment.Count; i++)
            {
                if (segment_.segment[i] == ball)
                {
                    ballIndex = i;
                    segmentIndex = j;
                    segment = segment_;
                }
            }
        }

        // check conditions
        if (ballIndex > 1)
        {
            if (segmentIndex == segments.Count - 1 || segments[segmentIndex + 1].segment[0].GetComponent<ZumaBall>().id != idToScan)
            {
                List<GameObject> ballsToMerge = new();
                ballsToMerge.Add(ball);

                // scan balls at back
                for (int i = ballIndex - 1; i >= 0; i--)
                {
                    GameObject ballBack = segment.segment[i];
                    int ballId = ballBack.GetComponent<ZumaBall>().id;
                    if (ballId == idToScan)
                    {
                        ballsToMerge.Insert(0, ballBack);
                    }
                    else
                        break;
                }

                // merge if conditions are available
                if (ballsToMerge.Count >= 3)
                {
                    // ball to change
                    GameObject ballToChange = ballsToMerge[0];
                    ballToChange.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
                    int newId = idToScan + 1;
                    gameManager.skinManager.ChangeSkin(ballToChange, newId);
                    audioManager.Play("Merge");
                    ballToChange.transform.DOScale(1, 0.3f).OnComplete(() =>
                    {
                        CheckMerge(ballToChange, newId);
                    }
                    );

                    // other balls
                    for (int i = 1; i < ballsToMerge.Count; i++)
                    {
                        // ball
                        GameObject otherBall = ballsToMerge[i];
                        // segmental changes
                        segment.segment.Remove(otherBall);
                        // animation
                        otherBall.GetComponent<ZumaBall>().Merge(ballToChange);
                    }
                }
            }
        }

    }

    bool MergeBallsOnSpline(Segment frontSegment, Segment backSegment, GameObject frontSegmentFirstBall, GameObject backSegmentLastBall, int frontId, int backId)
    {
        bool merged = false;
        if (frontId == backId)
        {
            int idToScan = backId;
            List<GameObject> ballsToMerge = new();
            ballsToMerge.Add(backSegmentLastBall);
            ballsToMerge.Add(frontSegmentFirstBall);

            // scan balls in front segment
            for (int j = 1; j < frontSegment.segment.Count; j++)
            {
                GameObject ball = frontSegment.segment[j];
                int ballId = ball.GetComponent<ZumaBall>().id;
                if (ballId == idToScan)
                {
                    ballsToMerge.Add(ball);
                }
                else
                {
                    break;
                }
            }

            // scan balls in back segment
            for (int j = backSegment.segment.Count - 2; j >= 0; j--)
            {
                GameObject ball = backSegment.segment[j];
                int ballId = ball.GetComponent<ZumaBall>().id;
                if (ballId == idToScan)
                {
                    ballsToMerge.Insert(0, ball);
                }
                else
                    break;
            }

            // merge if conditions are available
            if (ballsToMerge.Count >= 3)
            {
                // ball to change
                GameObject ballToChange = ballsToMerge[0];
                ballToChange.transform.localScale = new Vector3(0.0f, 0.0f, 0.0f);
                int newId = idToScan + 1;
                gameManager.skinManager.ChangeSkin(ballToChange, newId);
                audioManager.Play("Merge");
                ballToChange.transform.DOScale(1, 0.3f).OnComplete(() => {
                    CheckMerge(ballToChange, newId);
                }
            );
                // other balls
                for (int j = 1; j < ballsToMerge.Count; j++)
                {
                    // ball 
                    GameObject ball = ballsToMerge[j];
                    // segmental changes
                    backSegment.segment.Remove(ball);
                    frontSegment.segment.Remove(ball);
                    // animation
                    ball.GetComponent<ZumaBall>().Merge(ballToChange);
                }

                StopAllCoroutines();

                // set front segment speed to zero
                if (frontSegment.segment.Count > 0)
                {
                    foreach (GameObject ball in frontSegment.segment)
                    {
                        ball.GetComponent<ZumaBall>().speed = 0;
                    }
                }
                else
                    segments.Remove(frontSegment);

                // set boolian
                merged = true;
            }
        }
        return merged;
    }

    // used in bomb powerup
    public void SplitSegment(GameObject contactedball)
    {
        GetIndexesOnSegment(contactedball);
        float speed = contactedball.GetComponent<ZumaBall>().speed;

        if (contactedball == contactedSegment.segment[0] | contactedball == contactedSegment.segment[^1])
        {
            // get rid of contacted ball
            contactedSegment.segment.Remove(contactedball);
            Destroy(contactedball);

            // if contacted segment is empty
            if (contactedSegment.segment.Count == 0)
            {
                if (contactedSegment == segments[0])
                {
                    segments.Remove(contactedSegment);
                    foreach (GameObject ball in segments[0].segment)
                    {
                        ball.GetComponent<ZumaBall>().speed = speed;
                    }
                }
                else
                    segments.Remove(contactedSegment);
            }
        }
        else
        {
            // get rid of contacted ball
            contactedSegment.segment.Remove(contactedball);
            Destroy(contactedball);
            
            // split segments
            Segment newSegment = new();
            segments.Insert(contactedSegmentIndex + 1, newSegment);
            for (int i = contactedballIndex; i < contactedSegment.segment.Count; i += 0)
            {
                // target ball
                GameObject ball = contactedSegment.segment[i];

                // actions
                contactedSegment.segment.Remove(ball);
                newSegment.segment.Add(ball);
                ball.GetComponent<ZumaBall>().speed = 0;
            }
        }
        
        // reset values
        contactedBall = null;
        contactedSegment = null;
        contactedballIndex = -1;
    }

    public void AddNewBallsToSegmentZero()
    {
        // if no segment, create one
        if (segments.Count == 0)
        {
            Segment newSegment = new();
            segments.Add(newSegment);
        }

        Segment pastSegment = ballSpawner.segmentForNewBalls;
        Segment postSegment = segments[0];
        while (pastSegment.segment.Count > 0)
        {
            GameObject ball = pastSegment.segment[^1];
            pastSegment.segment.Remove(ball);
            postSegment.segment.Insert(0, ball);
        }
    }
}