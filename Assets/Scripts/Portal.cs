using UnityEngine;

public class Portal : MonoBehaviour
{
    [SerializeField] private string sceneToLoad;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private Transform visual;

    private bool used;

    public string SceneToLoad
    {
        get => sceneToLoad;
        set => sceneToLoad = value;
    }

    public void Enter()
    {
        if (used || string.IsNullOrEmpty(sceneToLoad)) return;
        used = true;
        AudioManager.Instance.PlaySfx(AudioManager.Instance.Portal);
        SceneLoader.Load(sceneToLoad);
    }

    public void GoToScene()
    {
        Enter();
    }

    private void Update()
    {
        if (visual != null) visual.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }
}
