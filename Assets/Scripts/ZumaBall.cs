using UnityEngine;
using System.Collections;
using Dreamteck.Splines;

public class ZumaBall : MonoBehaviour
{
    [SerializeField] ZumaBallMovement moveScript;
    [SerializeField] ZumaBallRotator rotateScript;

    SplinePositioner positioner;

    GameManager gameManager;

    SegmentManager segmentManager;

    AudioManager audioManager;

    private bool merging;

    private float mergeDuration = 0.05f;
    private float elapsedTime;
    public float speed = 2f;
    
    private double endPosition;
    private double startPosition;

    public int id;

    private void Start()
    {
        gameManager = GameManager.instance;
        segmentManager = gameManager.segmentManager;
        audioManager = AudioManager.instance;
        merging = false;
    }

    private void Update()
    {
        moveScript.Move();
        rotateScript.Rotate();
        if (merging)
        {
            elapsedTime += Time.deltaTime;
            float percentageComplete = elapsedTime / mergeDuration;

            positioner.SetPercent(Mathf.Lerp((float)startPosition, (float)endPosition, percentageComplete));

            if (positioner.GetPercent() == endPosition)
                Destroy(gameObject);
        }
    }

    public void Merge(GameObject targetBall)
    {
        // target splinepercent
        endPosition = targetBall.GetComponent<SplinePositioner>().GetPercent();
        // ball splinepercent at start
        positioner = GetComponent<SplinePositioner>();
        startPosition = positioner.GetPercent();
        StartCoroutine(Wait());
    }
    IEnumerator Wait()
    {
        yield return null;
        merging = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // collide with normal ball
        if (other.gameObject.CompareTag("Player"))
        {
            audioManager.Play("BallContact");
            if (segmentManager.newBall == null)
            {
                segmentManager.newBall = other.gameObject;
                segmentManager.contactedBall = gameObject;
            }
        }

        // collide with 2X ball
        if (other.gameObject.CompareTag("2X"))
        {
            // rainbow ball
            GameObject ball = other.gameObject;
            ball.tag = "Untagged";
            Destroy(ball);
            
            // this ball
            gameManager.skinManager.ChangeSkin(gameObject, id + 1);
        }

        // collide with bomb ball
        if (other.gameObject.CompareTag("Bomb"))
        {
            // bomb ball
            GameObject ball = other.gameObject;
            ball.tag = "Untagged";
            Destroy(ball);

            // this ball
            segmentManager.SplitSegment(gameObject);
        }
    }
}
