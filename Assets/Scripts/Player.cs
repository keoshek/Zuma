using UnityEngine;
using System.Collections;
using DG.Tweening;

public class Player : MonoBehaviour
{
    AudioManager audioManager;
    GameManager gameManager;
    SegmentManager segmentManager;

    private PlayerBall playerBall;
    private GameObject nextBall;

    public GameObject ballPrefab;
    public GameObject nextBallPrefab;
    [SerializeField] Transform playerBallPos;
    [SerializeField] Transform nextBallPos;

    public Camera mainCamera;

    public LayerMask layerMask;

    public enum State
    {
        Aim,
        NoAim,
        PlayerDisabled
    }

    public State state;

    private void Start()
    {
        audioManager = AudioManager.instance;
        gameManager = GameManager.instance;
        segmentManager = gameManager.segmentManager;

        // spawn playerBall
        playerBall = SpawnPlayerBall();
        // spawn nextBall
        nextBall = SpawnNextBall();
    }

    void Update()
    {
        if (state == State.NoAim)
            Aim();

        if (state == State.Aim)
        {
            RotateZuma();
        }
    }

    void RotateZuma()
    {
        Vector3 mousePosition = Input.mousePosition;
        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, float.MaxValue, layerMask))
        {
            Vector3 hitPoint = raycastHit.point;
            hitPoint.y = transform.position.y;
            Vector3 aimDirection = (hitPoint - transform.position);
            transform.forward = aimDirection;
        }

        if (Input.GetMouseButtonUp(0))
        {
            // add balls of BallSpawner.segmentForNewBalls to zero segment of segment manager
            segmentManager.AddNewBallsToSegmentZero();
            
            //unparent shooted ball
            playerBall.gameObject.transform.parent.gameObject.transform.SetParent(null);

            // make ball move forward
            playerBall.state = PlayerBall.State.Shooted;

            // sound effect
            audioManager.Play("ShootBall");

            // spawn new ball
            StartCoroutine(EnablePlayerr());

            // disable player
            DisablePlayer();
        }
    }

    void Aim()
    {
        if (Input.GetMouseButtonDown(0))
        {
            state = State.Aim;
        }
    }

    private PlayerBall SpawnPlayerBall()
    {
        GameObject newBall = Instantiate(ballPrefab, playerBallPos.position, transform.rotation, transform);
        newBall.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        PlayerBall playerBall = newBall.transform.GetChild(0).gameObject.GetComponent<PlayerBall>();
        
        newBall.transform.DOScale(1, 0.2f);

        return playerBall;
    }

    private GameObject SpawnNextBall()
    {
        // instantiate nextBall
        GameObject ball = Instantiate(nextBallPrefab, nextBallPos.position, nextBallPrefab.transform.rotation, nextBallPos);
        return ball;
    }

    // change state | needed not to shoot a ball while interfering with ui
    public void DisablePlayer()
    {
        state = State.PlayerDisabled;
    }

    public void EnablePlayer()
    {
        state = State.NoAim;
    }

    // wait a bit after throwing prev ball
    IEnumerator EnablePlayerr()
    {
        yield return new WaitForSeconds(0.3f);
        playerBall = SpawnPlayerBall();
        EnablePlayer();
    }

    // POWERUPS
    public void TurnBallTo2XPowerup()
    {
        // target ball
        GameObject ball = playerBall.gameObject;
        // change material
        ball.GetComponent<MeshRenderer>().material = null;
        // change tag
        ball.tag = "2X";
        // return to normal state of player
        EnablePlayer();
    }

    public void TurnBallToBombPowerup()
    {
        // target ball
        GameObject ball = playerBall.gameObject;
        // change material
        ball.GetComponent<MeshRenderer>().material = null;
        // change tag
        ball.tag = "Bomb";
        // return to normal state of player
        EnablePlayer();
    }

    public void FreezePowerup()
    {
        segmentManager.AddNewBallsToSegmentZero();

        // target segment
        Segment segment = segmentManager.segments[0];
        float oldSpeed = segment.segment[0].GetComponent<ZumaBall>().speed;
        foreach (GameObject ball in segment.segment)
        {
            ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
            zumaBall.speed = 0;
        }
        StartCoroutine(DisableFreezePowerup(oldSpeed, 7));
        // return to normal state of player
        EnablePlayer();
    }

    IEnumerator DisableFreezePowerup(float speed, float period)
    {
        yield return new WaitForSeconds(period);
        Segment segment = segmentManager.segments[0];
        foreach (GameObject ball in segment.segment)
        {
            ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
            zumaBall.speed = speed;
        }
    }

    public void RPowerup()
    {
        segmentManager.AddNewBallsToSegmentZero();

        // freeze zero segment
        Segment segmentZero = segmentManager.segments[0];
        foreach (GameObject ball in segmentZero.segment)
        {
            ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
            zumaBall.speed = 0;
        }

        // R last segment
        Segment segmentLast = segmentManager.segments[^1];
        foreach (GameObject ball in segmentLast.segment)
        {
            ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
            zumaBall.speed = -20;
        }

        //enable bool in segmentmanager
        segmentManager.RPowerUpEnabled = true;

        StartCoroutine(DisableRPowerup(3));
    }

    IEnumerator DisableRPowerup(float period)
    {
        yield return new WaitForSeconds(period);
        Segment segment = segmentManager.segments[0];
        foreach (GameObject ball in segment.segment)
        {
            ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
            zumaBall.speed = gameManager.ballSpawner.startSpeed;
        }

        //disable bool in segmentmanager
        segmentManager.RPowerUpEnabled = false;

        // return to normal state of player
        EnablePlayer();
    }
}
