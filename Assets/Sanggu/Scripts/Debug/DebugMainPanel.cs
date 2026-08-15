using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public class DebugMainPanel: MonoBehaviour
{
    [SerializeField] private GameObject panel;

    [SerializeField] private GameObject categoryPrefab;
    [SerializeField] private GameObject inputPanelPrefab;
    [SerializeField] private GameObject contents;
    
    public MonoBehaviour[] targets;
    
    private void OnEnable()
    {
        Init();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    private void Init()
    {
        var chlids = new List<GameObject>();
        foreach (Transform chlid in contents.transform)
        {
            chlids.Add(chlid.gameObject);
        }

        foreach (var chlid in chlids.ToList())
        {
            Destroy(chlid);
        }

        foreach (var target in targets)
        {
            var debugEntries = Collect(target);
            var category = Instantiate(categoryPrefab, contents.transform).GetComponent<DebugCategory>();
            category.categoryName.text = target.name;
            foreach (var debugEntry in debugEntries) 
            {
               var go = Instantiate(inputPanelPrefab, contents.transform);
               var input = go.GetComponent<DebugInputPanel>();
               category.contents.Add(go);
               input.DebugEntry = debugEntry;
               input.Init(); 
            }
        }
        
    }
    
    private List<DebugEntry> Collect(MonoBehaviour target)
    {
        List<DebugEntry> entries = new();
        //if(!target.didAwake) continue;
        var type = target.GetType();

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.GetCustomAttribute<DebugFieldAttribute>() != null)
            {
                entries.Add(new DebugEntry
                {
                    Target = target,
                    Field = field,
                    FieldName = field.GetCustomAttribute<DebugFieldAttribute>().FieldName,
                });
            }
        }

        MethodInfo[] methods = type.GetMethods(
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        foreach (var method in methods)
        {
            if (method.GetCustomAttribute<DebugButtonAttribute>() != null)
            {
                entries.Add(new DebugEntry
                {
                    Target = target,
                    Method = method,
                    FieldName = method.GetCustomAttribute<DebugButtonAttribute>().MethodName
                });
            }
        }

        return entries;
    }
}