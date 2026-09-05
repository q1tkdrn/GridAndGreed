using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class MemorialButton : MonoBehaviour
{
    public MemorialData memorialData;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    private void Start()
    {
        button.interactable = InventoryManager.Instance.HasMemorial(memorialData.id);
        nameText.text = memorialData.memorialName;
    }
}