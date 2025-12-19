using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Shapes
{
    BLOCK = 1,
    CROSS = 2,
    STEP = 3,
    RECTANGLE = 4
}

public class PlayerScript : MonoBehaviour
{
    [SerializeField] private List<GameObject> playerForms;
    [SerializeField] private Shapes currentShape = Shapes.BLOCK;
    [SerializeField] private RawImage redIndicatorImage;
    [SerializeField] private RawImage blueIndicatorImage;
    [SerializeField] private RawImage greenIndicatorImage;

    private Vector2 startPos;
    private DateTime startTime;
    public float minSwipeDistance = 50f;
    public float maxSwipeTime = 1f;

    void Start()
    {
        ChangeShape(currentShape);
    }

    public void ChangeShape(Shapes shape)
    {
        currentShape = shape;
        if (playerForms == null || playerForms.Count == 0)
        {
            Debug.LogWarning("playerForms is empty or null.");
            return;
        }

        for (int i = 0; i < playerForms.Count; i++)
        {
            bool shouldBeActive = (i == (int)currentShape - 1);
            if (playerForms[i] != null)
                playerForms[i].SetActive(shouldBeActive);
        }

        int activeIndex = (int)currentShape - 1;
        if (activeIndex >= 0 && activeIndex < playerForms.Count && playerForms[activeIndex] != null)
            ReColorShape(playerForms[activeIndex]);
    }

    void SetIndicatorColors(Color color)
    {
        if (color == Color.red && redIndicatorImage != null){
            redIndicatorImage.gameObject.SetActive(true);
            blueIndicatorImage.gameObject.SetActive(false);
            greenIndicatorImage.gameObject.SetActive(false);
        }
        if (color == Color.blue && redIndicatorImage != null) {
            redIndicatorImage.gameObject.SetActive(false);
            blueIndicatorImage.gameObject.SetActive(true);
            greenIndicatorImage.gameObject.SetActive(false);
        }
        if (color == Color.green && greenIndicatorImage != null)
        {
            redIndicatorImage.gameObject.SetActive(false);
            blueIndicatorImage.gameObject.SetActive(false);
            greenIndicatorImage.gameObject.SetActive(true);
        }
    }

    public void ChangeShape(string shapeName)
    {
        if (Enum.TryParse(shapeName.ToUpper(), out Shapes shape))
        {
            ChangeShape(shape);
        }
        else
        {
            Debug.LogWarning("Shape " + shapeName + " not recognized.");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startPos = Input.mousePosition;
            startTime = DateTime.Now;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Vector2 endPos = Input.mousePosition;
            TimeSpan duration = DateTime.Now - startTime;

            if (duration.TotalSeconds > maxSwipeTime)
                return;

            Vector2 swipe = endPos - startPos;

            if (swipe.magnitude < minSwipeDistance)
                return;

            swipe.Normalize();

            if (Mathf.Abs(swipe.x) > Mathf.Abs(swipe.y))
            {
                return;
            }
            else
            {
                bool moved = false;
                if (swipe.y > 0)
                {
                    if (transform.position.y <= 0f)
                    {
                        transform.position += Vector3.up * 1f;
                        moved = true;
                    }
                }
                else
                {
                    if (transform.position.y >= 0f)
                    {
                        transform.position += Vector3.down * 1f;
                        moved = true;
                    }
                }

                if (moved)
                    RecolorActiveForm();
            }
        }
    }
    
    private void RecolorActiveForm()
    {
        int activeIndex = (int)currentShape - 1;
        if (playerForms == null || activeIndex < 0 || activeIndex >= playerForms.Count)
            return;

        GameObject active = playerForms[activeIndex];
        if (active != null) {
            ReColorShape(active);
        }
    }

    void ReColorShape(GameObject obj)
    {
        if (obj == null) return;
        int laneIndex = Mathf.RoundToInt(transform.position.y);
        Color newColor = GameManager.ColorFromYIndex(laneIndex);
        Renderer[] rends = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in rends)
        {
            if (r == null) continue;
            try
            {
                r.material.color = newColor;
                SetIndicatorColors(newColor);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to recolor renderer on {r.gameObject.name}: {ex.Message}");
            }
        }
    }
}
