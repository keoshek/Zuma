using UnityEngine;

public class SkinManager : MonoBehaviour
{
    private GameManager gameManager;
    private BallSpawner spawner;
    SegmentManager segmentManager;

    private int indexToCheck;

    private void Start()
    {
        gameManager = GameManager.instance;
        spawner = gameManager.ballSpawner;
        segmentManager = gameManager.segmentManager;

        indexToCheck = 7;
    }

    public void ChangeSkin(GameObject ball, int index)
    {
        // change ball material
        Renderer renderer = ball.GetComponent<MeshRenderer>();
        renderer.material = GetMaterial(index);
        // change ball index
        ball.GetComponent<ZumaBall>().id = index;
        // change range of coming balls
        if (index == indexToCheck)
        {
            UpgradeMinSkin(spawner.minSkinIndex);
            spawner.minSkinIndex++;
            indexToCheck += 1;
        }
    }

    void UpgradeMinSkin(int idToUpgrade)
    {
        // upgrade skins on segments
        foreach (Segment segment in segmentManager.segments)
        {
            foreach (GameObject ball in segment.segment)
            {
                ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
                if (zumaBall.id == idToUpgrade)
                {
                    ChangeSkin(ball, idToUpgrade + 1);
                }
            }
        }

        // upgrade skins on spawner.segmentForNewBalls
        foreach (GameObject ball in spawner.segmentForNewBalls.segment)
        {
            ZumaBall zumaBall = ball.GetComponent<ZumaBall>();
            if (zumaBall.id == idToUpgrade)
            {
                ChangeSkin(ball, idToUpgrade + 1);
            }
        }
    }

    /*if (index == 7 && gameManager.ballSpawner.minSkinIndex == 0) // 256
        {
            gameManager.ballSpawner.minSkinIndex++;
            Debug.Log(gameManager.ballSpawner.minSkinIndex);// now ball range is between 4-128
            return;
        }

        if (index == 9 && gameManager.ballSpawner.minSkinIndex == 1) // 1028
        {
            gameManager.ballSpawner.minSkinIndex++;
            Debug.Log(gameManager.ballSpawner.minSkinIndex);// now ball range is between 8-256
            return;
        }

        if (index == 11 && gameManager.ballSpawner.minSkinIndex == 2) // 4096
        {
            gameManager.ballSpawner.minSkinIndex++;
            Debug.Log(gameManager.ballSpawner.minSkinIndex);// now ball range is between 16-512
            return;
        }

        if (index == 13 && gameManager.ballSpawner.minSkinIndex == 3) // 16K
        {
            gameManager.ballSpawner.minSkinIndex++;
            Debug.Log(gameManager.ballSpawner.minSkinIndex);// now ball range is between 32-1024
            return;
        }

        if (index == 15 && gameManager.ballSpawner.minSkinIndex == 4) // 65K
        {
            gameManager.ballSpawner.minSkinIndex++;
            Debug.Log(gameManager.ballSpawner.minSkinIndex);// now ball range is between 64-2048
            return;
        }*/

    public Material GetMaterial(int index)
    {
        Ball[] types = gameManager.ballManager.types;

        for (int i = 0; i < types.Length; i++)
            if (i == index)
                return types[i].material;

        return null;
    }
}
