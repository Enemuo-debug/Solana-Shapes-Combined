using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonMgtScript : MonoBehaviour
{
    private Button button;
    private PlayerScript playerScript;

    void Awake()
    {
        playerScript = FindObjectOfType<PlayerScript>();
    }

    void Start()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            playerScript.ChangeShape(gameObject.name);
        });
    }
}
