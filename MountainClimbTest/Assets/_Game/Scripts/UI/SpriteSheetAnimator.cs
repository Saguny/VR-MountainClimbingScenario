using UnityEngine;
using UnityEngine.UI;

public class SpriteSheetAnimator : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 12f;
    [SerializeField] private bool loop = true;

    private Image image;
    private int currentFrame;
    private float timer;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        currentFrame = 0;
        timer = 0;
        if (frames != null && frames.Length > 0)
            image.sprite = frames[0];
    }

    private void Update()
    {
        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;

        if (timer >= 1f / framesPerSecond)
        {
            timer = 0;
            currentFrame++;

            if (currentFrame >= frames.Length)
            {
                if (loop) currentFrame = 0;
                else return;
            }

            image.sprite = frames[currentFrame];
        }
    }
}