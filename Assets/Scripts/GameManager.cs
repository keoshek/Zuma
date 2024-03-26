using UnityEngine;
using Dreamteck.Splines;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public BallManager ballManager;
    public SkinManager skinManager;
    public SegmentManager segmentManager;
    public BallSpawner ballSpawner;
    public Player player;

    public SplineComputer spline;

    public GameObject ballPrefab;
    public float diameter;
    public float splineLength;

    private void Awake()
    {
        if (instance == null)
            instance = this;

        diameter = ballPrefab.GetComponent<SphereCollider>().radius * 2;

        splineLength = spline.CalculateLength();
    }

    private void Start()
    {
        player.DisablePlayer();
    }
}
