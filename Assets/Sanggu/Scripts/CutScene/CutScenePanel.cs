using System;
using System.Collections;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    [SerializeField] private float cutDelay = 0.5f;
    [SerializeField] private float timeForSkip = 2f;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextFadeEffect textFadeEffect;
    private float _holdTime = 0f;
    private bool _isSkip = false;

    private int _currentCut = 0;
    private int _currentText = -1;

    public void SetCutScene(string cutsceneName)
    {
        _currentText = -1;
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
        //StartCoroutine(PlayCutScene());
        NextText();
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
            yield return new WaitForSeconds(cutDelay);
        }

        if (currentCutScene.name is "Ending1" or "Ending2")
        {
            SceneManager.LoadScene("Main");
        }
        gameObject.SetActive(false);
    }

    public void NextText()
    {
        nextButton.gameObject.SetActive(false);
        _currentText++;
        if (currentCutScene.cuts[_currentCut].texts.Length <= _currentText)
        {
            _currentCut++;
            _currentText = 0;
        }

        if (currentCutScene.cuts.Length <= _currentCut)
        {
            if (currentCutScene.name is "Ending1" or "Ending2")
            {
                SceneManager.LoadScene("Main");
            }
            gameObject.SetActive(false);
            return;
        }
        
        image.sprite = currentCutScene.cuts[_currentCut].image;
        
        text.text = currentCutScene.cuts[_currentCut].texts[_currentText];
        textFadeEffect.FadeInText(text, () => nextButton.gameObject.SetActive(true));
    }

    public void ClickButton()
    {
        textFadeEffect.FadeOutText(text, () => NextText());
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
        //StopCoroutine(PlayCutScene());
        if (currentCutScene.name is "Ending1" or "Ending2")
        {
            SceneManager.LoadScene("Main");
        }
        gameObject.SetActive(false);
    }
}
