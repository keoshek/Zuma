using UnityEngine;

public class PlayerBall : MonoBehaviour
{
    GameManager gameManager;
    SegmentManager segmentManager;
    SkinManager skinManager;
    BallSpawner spawner;

    private Transform parentGO;
    public Transform target;

    public float rotateRate = 1f;
    public float moveSpeed = 30f;
    private float xBound = 30f;
    private float zBound = 40f;
    private float integrateSpeedUnit = 7f;
    private float integrateSpeed;

    public State state;
    public enum State
    {
        Normal,
        Shooted,
        Integrate
    }

    void Start()
    {
        gameManager = GameManager.instance;
        segmentManager = gameManager.segmentManager;
        skinManager = gameManager.skinManager;
        spawner = gameManager.ballSpawner;
        state = State.Normal;
        parentGO = transform.parent;
        SetMaterial();
    }

    void Update()
    {
        if (state == State.Normal)
        {
            Rotate();
        }

        if (state == State.Shooted)
        {
            Rotate();
            MoveForward();
            DestroyOutOfBound();
        }

        // integration into segment
        if (state == State.Integrate && target != null)
        {
            Integrate();
        }
    }

    private void Integrate()
    {
        // integrate speed
        float speed = segmentManager.normalSpeed;
        if (speed == 0)
            integrateSpeed = 15;
        else
            integrateSpeed = speed * integrateSpeedUnit;
        // movetoward positions
        Vector3 a = transform.position;
        Vector3 b = target.position;
        transform.position = Vector3.MoveTowards(a, b, integrateSpeed * Time.deltaTime);
        // when movetoward ends
        if (Vector3.Distance(a, b) <= 0.1f)
        {
            ZumaBall zumaBall = gameObject.GetComponent<ZumaBall>();
            zumaBall.enabled = true;
            zumaBall.speed = speed;
            gameObject.GetComponent<ZumaBallRotator>().enabled = true;
            gameObject.GetComponent<ZumaBallMovement>().enabled = true;
            // reposition all balls
            segmentManager.Reposition();
            // switch off current script
            this.enabled = false;
        }
    }

    private void Rotate()
    {
        transform.Rotate(transform.right, rotateRate, Space.World);
    }

    // move the parent gameobject forward
    private void MoveForward()
    {
        parentGO.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    private void SetMaterial()
    {
        // generate skin type
        int skinIndex = spawner.GenerateSkinIndex();

        // change skin and id
        skinManager.ChangeSkin(gameObject, skinIndex);
    }

    private void DestroyOutOfBound()
    {
        if (parentGO.position.z < -zBound | parentGO.position.z > zBound | parentGO.position.x < -xBound | parentGO.position.x > xBound)
            Destroy(parentGO.gameObject);
    }
}
