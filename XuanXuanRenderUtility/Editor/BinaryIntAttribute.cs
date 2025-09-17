using UnityEditor;
using UnityEngine;
using System;
// using Unity.Properties;

public class BinaryIntAttribute : PropertyAttribute
{
    public int binaryBits;
    public bool showInputFiled;
    public int tabNums;


    public BinaryIntAttribute(int Bits = 32,bool showInput = false,int tab = 0)
    {
        binaryBits = Bits;
        showInputFiled = showInput;
        tabNums = tab;
    }
}

[CustomPropertyDrawer(typeof(BinaryIntAttribute))]
public class BinaryIntDrawer : PropertyDrawer
{
   
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        
        GUIStyle richTextStyle = EditorStyles.label;
        richTextStyle.richText = true;
        BinaryIntAttribute binaryIntAttribute = (BinaryIntAttribute)attribute; 
        // int value = property.intValue;
        // int largestBit = 0;
        // for (int i = 0; i < 32; i++)
        // {
        //     if ((~value & (1 << i)) == 0)
        //     {
        //         largestBit = i;
        //     }
        // }
        //
        // int addZeroCount = 0;
        // if (largestBit < binaryIntAttribute.binaryBits)
        // {
        //     addZeroCount = binaryIntAttribute.binaryBits - largestBit - 1;
        // }
        //
        // string addZeroString = "";
        // for (int i = 0; i < addZeroCount; i++)
        // {
        //     addZeroString += "0";
        // }
        // string binary = addZeroString+Convert.ToString(property.intValue, 2);
        string binary = DrawBinaryInt(property.intValue, binaryIntAttribute.binaryBits);

        string tabs = "";
        for (int i = 0; i < binaryIntAttribute.tabNums; i++)
        {
            tabs += "\t";
        }
  
        if (binaryIntAttribute.showInputFiled)
        {
            string labelText = property.displayName + tabs + "<mspace=1em>" + binary + "</mspace>";
            Rect intRect = EditorGUI.PrefixLabel(position,new GUIContent(labelText),richTextStyle);
            property.intValue = EditorGUI.IntField( intRect,property.intValue);
        }
        else
        {
            EditorGUILayout.LabelField(property.displayName + tabs);
            EditorGUILayout.LabelField(binary);
        }
    }

    public static string DrawBinaryInt(int value ,int binaryBits)
    {
        int largestBit = 0;
        for (int i = 0; i < 32; i++)
        {
            if ((~value & (1 << i)) == 0)
            {
                largestBit = i;
            }
        }

        int addZeroCount = 0;
        if (largestBit < binaryBits)
        {
            addZeroCount = binaryBits - largestBit - 1;
        }

        string addZeroString = "";
        for (int i = 0; i < addZeroCount; i++)
        {
            addZeroString += "0";
        }
        string binary = addZeroString+Convert.ToString(value, 2);
        return binary;
    }
}
    