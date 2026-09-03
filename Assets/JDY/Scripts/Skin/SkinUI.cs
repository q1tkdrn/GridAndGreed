using UnityEngine;
using UnityEngine.UI;
public class SkinUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button[] SkinButton;
    [SerializeField] private SkinDialog skinDialog;

    void Start()
    {
        for(int i = 0; i < SkinButton.Length; i++) 
        { 
            SkinButton[i].interactable = false; 
        }
        if(skinDialog.currentPhase>=0)//°ïÃæ
            SkinButton[0].interactable = true;
        if (skinDialog.currentPhase >= 1)//¿Õ±¹
            SkinButton[1].interactable = true;
        if (skinDialog.currentPhase >= 2)//»ç½Å
            SkinButton[2].interactable = true;
    }
}
