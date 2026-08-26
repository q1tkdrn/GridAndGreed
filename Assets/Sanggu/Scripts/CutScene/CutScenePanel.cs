using System;
using System.Collections;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CutScenePanel: MonoBehaviour
{
    public CutScene[] cutscenes;
    [HideInInspector] public CutScene currentCutScene;
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private Slider slider;

    [SerializeField] private float wordDelay = 0.1f;
    private float _wordDelay;
    [SerializeField] private float textDelay = 0.5f;
    [SerializeField] private float timeForSkip = 2f;
    private float _holdTime = 0f;
    private bool _isSkip = false;

    public void SetCutScene(string cutsceneName)
    {
        slider.value = 0f;
        currentCutScene = null;
        currentCutScene = cutscenes.FirstOrDefault(x => x.cutSceneName == cutsceneName);
        if (currentCutScene == null)
        {
            gameObject.SetActive(false);
            return;
        }
        
        slider.gameObject.SetActive(currentCutScene.skippable);
        _holdTime = 0f;
        _isSkip = false;
        StartCoroutine(PlayCutScene());
    }

    IEnumerator PlayCutScene()
    {
        foreach (var cut in currentCutScene.cuts)
        {
            image.sprite = cut.image;
            foreach (var t in cut.texts)
            {
                text.text = "";
                _wordDelay = wordDelay;
                foreach (var c in t)
                {
                    text.text += c;
                    yield return new WaitForSeconds(_wordDelay);
                }
                yield return new WaitForSeconds(textDelay);
            }
        }
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _wordDelay = 0f;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _holdTime = 0f;
            slider.value = _holdTime/timeForSkip;
        }
        
        if(!currentCutScene.skippable) return;
        
        if (!Input.GetKey(KeyCode.Space)) return;
        
        if (_isSkip) return;
        
        _holdTime += Time.deltaTime;
        slider.value = _holdTime/timeForSkip;
        
        if (!(_holdTime >= timeForSkip)) return;
        
        _isSkip = true;
        StopCoroutine(PlayCutScene());
        gameObject.SetActive(false);
    }
}
