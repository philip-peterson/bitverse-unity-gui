﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;


/*
// Create a new instance of the MD5CryptoServiceProvider object.
MD5 md5Hasher = MD5.Create();

// Convert the input string to a byte array and compute the hash.
byte[] data = md5Hasher.ComputeHash(Encoding.Default.GetBytes(input));

//new Guid(data) 
 */

public class GuiAcessorGenerator
{
    private static BitControl _root;

    [MenuItem("Tools/GUI/Generate Gui Acessor from Selection")]
    public static void GenerateScreenBundles()
    {
        Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.TopLevel);

        StringBuilder builder = new StringBuilder();
        StringBuilder builderEnd = new StringBuilder();

        builder.AppendLine("// THIS IS AN AUTO GENERATED FILE");
        builder.AppendLine("// IT SIMPLIFIES THE USE OF THE GUI, ONLY USE IT DURING INITIALIZATION,");
        builder.AppendLine("// DONT USE IT AT UPDATE(), OR YOU WILL HAVE PERFORMANCE LOSS, USE IT AT START()");
        builder.AppendLine("using System;");
        builder.AppendLine("using UnityEngine;");
        builder.AppendLine("using Bitverse.Unity.Gui;");
        string className = "UNDEFINED";
        StringBuilder body = new StringBuilder();
        StringBuilder initData = new StringBuilder();
        StringBuilder cleanUp = new StringBuilder();
        Dictionary<string, int> nameCounter = new Dictionary<string, int>();
        Dictionary<string, string> nameCouterHierarchy = new Dictionary<string, string>();

        Dictionary<string, string> assignedVariables = new Dictionary<string, string>();
        foreach (Object o in selection)
        {
            if (o.GetType().IsAssignableFrom(typeof(GameObject)))
            {
                GameObject go = (GameObject)o;
                Component[] comps = go.GetComponentsInChildren(typeof(BitControl));
                for (int t = 0; t < comps.Length; t++)
                {
                    BitControl control = (BitControl)comps[t];

                    List<string> usedNamesInSameGroup = new List<string>();

                    if (control is BitContainer)
                    {
                        for (int y = 0; y < control.transform.childCount; y++)
                        {
                            GameObject tmpgo = control.transform.GetChild(y).gameObject;
                            string currentName = tmpgo.name;
                            if (usedNamesInSameGroup.Contains(currentName))
                            {
                                string pathtext = _root.name;
                                RecursiveTextPath(control, ref pathtext);
                                EditorUtility.DisplayDialog("ALERT", "YOU CANT HAVE COMPONENTS WITH THE SAME NAME IN THE SAME CONTAINER: " + pathtext, "ok");
                                return;
                            }
                            usedNamesInSameGroup.Add(currentName);
                        }
                    }

                    string bitClassName = comps[t].GetType().Name;
                    string usableName = ConvertToUsableName(control.name);
                    if (t == 0)
                    {
                        _root = control;
                        className = usableName;
                        builder.AppendLine("public class " + usableName + "GuiAcessor");
                        builder.AppendLine("{");
                        builder.AppendLine("    private " + bitClassName + " root;");
                        builder.AppendLine("    public " + usableName + "GuiAcessor(" + bitClassName + " root)");
                        builder.AppendLine("    {");
                        builder.AppendLine("               if (root==null)");
                        builder.AppendLine("                   throw new Exception(\"ROOT CANT BE NULL: " + usableName + "GuiAcessor\");");
                        builder.AppendLine("        this.root=root;");
                        builder.AppendLine("        Refresh();");
                        builder.AppendLine("    }");
                        builder.AppendLine("");
                        builder.AppendLine("    public void Refresh()");
                        builder.AppendLine("    {");
                        builderEnd.AppendLine("    }");
                        builderEnd.AppendLine("");
                        builderEnd.AppendLine("    public " + bitClassName + " " + usableName);
                        builderEnd.AppendLine("    {");
                        builderEnd.AppendLine("        get{");
                        builderEnd.AppendLine("            return root;");
                        builderEnd.AppendLine("        }");
                        builderEnd.AppendLine("    }");
                        builderEnd.AppendLine("");
                        if (nameCounter.ContainsKey(usableName))
                        {
                            nameCounter[usableName] = nameCounter[usableName] + 1;
                            string hierarchyConflict = GetHierarchy(control);
                            Debug.LogError("Two or more BitGUI components have the same name: " + usableName + "at level:    " + hierarchyConflict + "    and at level:   " + nameCouterHierarchy[usableName]);
                            //usableName = usableName + nameCounter[usableName];
                        }
                        else
                        {
                            nameCounter[usableName] = 0;
                            nameCouterHierarchy[usableName] = GetHierarchy(control);
                        }
                        continue;
                    }

                    if (nameCounter.ContainsKey(usableName))
                    {
                        nameCounter[usableName] = nameCounter[usableName] + 1;
                        string hierarchyConflict = GetHierarchy(control);
                        Debug.LogError("Two or more BitGUI components have the same name: " + usableName + ", at level:    " + hierarchyConflict + "    and at level:   " + nameCouterHierarchy[usableName]);
                        usableName = usableName + nameCounter[usableName];
                    }
                    else
                    {
                        nameCounter[usableName] = 0;
                        nameCouterHierarchy[usableName] = GetHierarchy(control);
                    }
                    body.AppendLine("   private " + bitClassName + " " + usableName + "Value;");
                    body.AppendLine("");
                    body.AppendLine("   public " + bitClassName + " " + usableName + "");
                    body.AppendLine("   {");
                    body.AppendLine("       get{");
                    body.AppendLine("           return " + usableName + "Value;");
                    string recursiveFind = "";
                    RecursiveFind(control, ref recursiveFind);
                    body.AppendLine("       }");
                    body.AppendLine("   }");
                    body.AppendLine("");

                    //use the previously assigned variables
                    bool replaced;
                    do
                    {
                        replaced = false;
                        foreach (KeyValuePair<string, string> pair in assignedVariables)
                        {
                            if (recursiveFind.Contains(pair.Key))
                            {
                                recursiveFind = recursiveFind.Replace(pair.Key, pair.Value);
                                replaced = true;
                            }
                        }
                    } while (replaced);
                    Debug.Log(recursiveFind + ", " + usableName + "Value");
                    assignedVariables[recursiveFind] = usableName + "Value";

                    initData.AppendLine("           " + usableName + "Value = " + recursiveFind + ";");
                    initData.AppendLine("           if (" + usableName + "Value==null)");
                    initData.AppendLine("               throw new Exception(\"COULD NOT FIND BITCONTROL WITH NAME " + control.name + " , PLEASE CHECK THE PREFAB AND REGENERATE GUI ACESSOR.\");");
                    cleanUp.AppendLine("            " + usableName + "Value = null;");
                }

            }
        }
        builder.AppendLine(initData.ToString());
        builder.AppendLine(builderEnd.ToString());
        builder.AppendLine(body.ToString());

        builder.AppendLine("    public bool IsLoaded");
        builder.AppendLine("    {");
        builder.AppendLine("        get");
        builder.AppendLine("        {");
        builder.AppendLine("            return root!=null;");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine("");
        builder.AppendLine("    public void Dispose()");
        builder.AppendLine("    {");
        builder.AppendLine(cleanUp.ToString());
        builder.AppendLine("    }");
        builder.AppendLine("}");
        Debug.Log(builder.ToString());

        string path = EditorUtility.SaveFilePanel("Save Generated Gui Acessor", "", className + "GuiAcessor.cs", "cs");
        TextWriter tw = new StreamWriter(path);
        tw.Write(builder.ToString());
        tw.Close();
    }

    private static string GetHierarchy(BitControl control)
    {
        string hierarchyConflict = control.name;
        BitControl bc = control.Parent;
        while (bc != null)
        {
            hierarchyConflict = bc.name + "->" + hierarchyConflict;
            bc = bc.Parent;
        }
        return hierarchyConflict;
    }

    private static void RecursiveFind(BitControl control, ref string text)
    {
        if (control == _root)
        {
            text += "root";
            return;
        }
        if (control.Parent != null)
            RecursiveFind(control.Parent, ref text);
        text += ".FindControl<" + control.GetType().Name + ">(\"" + control.name + "\")";
    }

    private static void RecursiveTextPath(BitControl control, ref string text)
    {
        if (control == _root)
        {
            return;
        }
        if (control.Parent != null)
            RecursiveTextPath(control.Parent, ref text);
        text += "." + control.name;
    }

    private static string ConvertToUsableName(string text)
    {
        string tmp = "";
        for (int t = 0; t < text.Length; t++)
        {
            if (t == 0)
            {
                if (Char.IsNumber(text[0]))
                {
                    tmp += "_" + text[0].ToString().ToUpper();
                }
                else
                {
                    tmp += text[0].ToString().ToUpper();
                }

            }
            else
            {
                if (text[t] == '_')
                {
                    t++;
                    tmp += text[t].ToString().ToUpper();
                }
                else
                {
                    tmp += text[t].ToString().ToLower();
                }
            }
        }
        return tmp;
    }

}
