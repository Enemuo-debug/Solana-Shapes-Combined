using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShapes : MonoBehaviour
{
    public GameManager gameManager;
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
    }
    void OnTriggerEnter(Collider other)
    {
        if (gameManager.Match(gameObject, other.gameObject))
        {
            Debug.Log("Matched!");
        }
        else
        {
            if (gameManager.GetMissCount() == 0)
            {
                gameManager.GameOver();
                return;
            }
            gameManager.RegisterMiss();
        }
        Destroy(other.gameObject);
    }
}
