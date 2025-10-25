using UnityEngine;

public class HouseAssemblyManager : MonoBehaviour
{
    public static HouseAssemblyManager Instance;

    public int totalParts = 5;
    private int placedParts = 0;
    private GameTimer timer;

    void Awake()
    {
        Instance = this;
        timer = GetComponent<GameTimer>();
    }

    public void AddPlacedPart()
    {
        placedParts++;
        if (placedParts >= totalParts)
        {
            timer.WinGame();
        }
    }

    public void StartTimerOnce()
    {
        timer.StartTimer();
    }
}
