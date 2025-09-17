using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEditor;

public class StringListSerchProvider : ScriptableObject , ISearchWindowProvider
// public class StringListSerchProvider
{

    public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
    {
        List<SearchTreeEntry> serchList = new List<SearchTreeEntry>();
        serchList.Add(new SearchTreeGroupEntry(new GUIContent("List"),0));
        List<string> listItemAfterSort = listItems.ToList();
        
        sortListItems(ref listItemAfterSort);
        
        List<string> groups = new List<string>();
        foreach (var item in listItemAfterSort)
        {
            string[] entryTitle = item.Split("/");
            string groupName = "";
            for (int i = 0; i < entryTitle.Length - 1; i++)
            {
                groupName += entryTitle[i];
                if (!groups.Contains(groupName))
                {
                    serchList.Add(new SearchTreeGroupEntry(new GUIContent(entryTitle[i]),i+1));
                }
                groupName += "/";
            }
        
            SearchTreeEntry entry = new SearchTreeEntry(new GUIContent(entryTitle.Last()));
            entry.level = entryTitle.Length;
            entry.userData = item;//这里是serch操作点击最后的返回值，是什么都可以的。
            // Debug.Log(entry.userData);
            serchList.Add(entry);
        }
        
        
        return serchList;

    }

    public bool OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
    {
        string data = (string)searchTreeEntry.userData;
        onSetIndexCallback?.Invoke(data);
        return true;
    }

    private string[] listItems;
    private Action<string> onSetIndexCallback;
    public StringListSerchProvider(string[] items, Action<string> callBack)
    {
        listItems = items;
        onSetIndexCallback = callBack;
    }

    public void Initialize(string[] items, Action<string> callBack)
    {
        listItems = items;
        onSetIndexCallback = callBack;
    }

    // private List<string> sortListItems;

    void sortListItems(ref List<string> list)
    {
        // sortListItems = listItems.ToList();
        list.Sort((a, b) =>
        {
            string[] splits1 = a.Split('/');
            string[] splits2 = b.Split('/');

            for (int i = 0; i < splits1.Length; i++)
            {
                if (i >= splits2.Length)
                {
                    return 1;
                }

                int value = splits1[i].CompareTo(splits2[i]);
                if (value != 0)
                {
                    if (splits1.Length != splits2.Length && (i == splits1.Length - 1 || i == splits2.Length - 1))
                    {
                        return splits1.Length < splits2.Length ? 1 : -1;
                    }

                    return value;
                }
            }

            return 0;
        });
    }
}
