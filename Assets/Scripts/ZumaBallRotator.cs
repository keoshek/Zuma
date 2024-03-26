using Dreamteck.Splines;
using UnityEngine;

public class ZumaBallRotator : MonoBehaviour
{
    public SplinePositioner positioner;

    public float diameter;
    private float splineLength;
    
    public double position;
    private double circumference;

    private void Start()
    {
        diameter = GameManager.instance.diameter;
        splineLength = positioner.spline.CalculateLength();
        circumference = diameter * 2;
    }

    public void Rotate()
    {
        // Calculate new rotation
        double pos = positioner.GetPercent() * splineLength;
        position = pos;
        double posInCircumference = pos % circumference;
        float rotationX = (float)((posInCircumference * 360) / circumference);

        // Apply rotation
        Vector3 newRotation = transform.rotation.eulerAngles;
        newRotation.x = rotationX;
        transform.rotation = Quaternion.Euler(newRotation);
    }
}
