using UnityEngine;

public class BallManager : MonoBehaviour
{
    public Ball[] types;
}

[System.Serializable]
public class Ball
{
    public string name;
    public Material material;
}
