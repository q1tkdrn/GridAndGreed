using UnityEngine;
using UnityEngine.UI;

public class MemorialButton : MonoBehaviour
{
    [SerializeField] private string memorialId;
    [SerializeField] private Button button;

    private void Start()
    {
        button.interactable = InventoryManager.Instance.HasMemorial(memorialId);
    }
}