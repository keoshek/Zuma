using UnityEngine;

public class NextBall : MonoBehaviour
{
    GameManager gameManager;
    BallSpawner spawner;
    SkinManager skinManager;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.instance;
        skinManager = gameManager.skinManager;
        spawner = gameManager.ballSpawner;

        SetMaterial();
    }

    private void SetMaterial()
    {
        // generate skin type
        int skinIndex = spawner.GenerateSkinIndex();

        // change skin and id
        skinManager.ChangeSkin(gameObject, skinIndex);
    }
}
