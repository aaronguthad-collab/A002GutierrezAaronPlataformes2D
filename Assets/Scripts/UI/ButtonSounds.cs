using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonSounds : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IDeselectHandler
{
    private bool selected;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => AudioManager.Instance.PlaySfx(AudioManager.Instance.ButtonClick));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        selected = true;
        AudioManager.Instance.PlaySfx(AudioManager.Instance.ButtonHover, 0.6f);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        selected = false;
    }

    private void Update()
    {
        float target = selected ? 1.08f : 1f;
        transform.localScale = Vector3.one * Mathf.MoveTowards(transform.localScale.x, target, Time.unscaledDeltaTime * 1.5f);
    }
}
