using UnityEngine;
using Dreamteck.Splines;

public class ZumaBallMovement : MonoBehaviour
{
    public ZumaBall ball;

    public SplinePositioner positioner;

    private float splineLength;

    void Start()
    {
        splineLength = positioner.spline.CalculateLength();
    }

    public void Move()
    {
        float moveAmount = ball.speed * Time.deltaTime;
        float percent = moveAmount / splineLength;
        percent += (float)positioner.GetPercent();
        positioner.SetPercent(percent);
    }
}
