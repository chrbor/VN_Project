using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

public class VNEffects : MonoBehaviour
{
    public static VNEffects vnEffects;
    public bool clearEffects;

    int textLength;

    List<string> fx_names_open = new List<string>();//liste an aktiven fx-tags im text, in welchem geschlossene tags mit einem leeren string versehen werden 
    List<string> fx_names = new List<string>();//aktive fx-tags im text
    List<FX_Data> fx_data = new List<FX_Data>();//startptr aktiver fx-tags im text

    public class FX_Data
    {
        public Vector2Int range;
        public List<string[]> tag;
        public bool isUniform;

        public FX_Data(int startptr, bool _uniform = false)
        {
            tag = new List<string[]>();
            range = new Vector2Int(startptr, 0);
            isUniform = _uniform;
        }
    }

    void Awake()
    {
        vnEffects = this;
    }

    /// <summary>
    /// Löscht alle aktiven Effekte im Text
    /// </summary>
    public void ClearFX()
    {
        fx_names_open.Clear();
        fx_names.Clear();
        fx_data.Clear();
        clearEffects = true;
    }

    public void StartFX(string[] tagStrings, MethodInfo method, int startptr)
    {
        Debug.Log("start " + tagStrings[0] + ": ");

        //Setup des FX:
        clearEffects = false;
        fx_names.Add(tagStrings[0]);
        fx_names_open.Add(tagStrings[0]);
        fx_data.Add(new FX_Data(startptr));

        method.Invoke(vnEffects, new object[] { tagStrings, fx_names.Count - 1 });
    }

    /// <summary>
    /// Setzt die Länge des Effektes fest
    /// </summary>
    /// <param name="fx_name"></param>
    public void EndFX(string fx_name, int endptr)
    {
        int lastIndex = fx_names_open.LastIndexOf(fx_name);
        fx_data[lastIndex].range += Vector2Int.up * endptr;
        fx_names_open[lastIndex] = "";
    }

    /// <summary>
    /// Nimmt den Text und fügt die Effekte hinzu, die in den Listen aufgelistet sind
    /// </summary>
    /// <param name="text">Text, dem die Effekte hinzugefügt werden sollen</param>
    /// <returns>Text, dem die Effekte hinzugefügt wurden</returns>
    public string UpdateTextFX(string text)
    {
        string text_out = "";
        int startptr = fx_data[0].range.x, endptr = 0;
        textLength = text.Length;

        //ermittle endpunkt in der liste der aktiven tags:
        for (int i = 0; i < fx_data.Count; i++)
            if (fx_data[i].range.y == 0) { endptr = textLength; break; }
            else if (endptr < fx_data[i].range.y) endptr = fx_data[i].range.y;

        //Laufe die Buchstaben durch und füge die Tags hinzu:
        for (int i = 0; i < text.Length; i++)
        {
            //umgehe tags im text:
            if (text[i] == '<')
            {
                int i_end = text.IndexOf('>', i);
                if (i_end > 0 || i_end < text.IndexOf('<', i + 1))//falls innerhalb von <...>, dann überspringe diesen Bereich
                {
                    i = i_end;
                    continue;
                }
            }

            //aktualisiere start- und endpunkt der tags:
            //startpunkt:
            for(int j = startptr; j < fx_data.Count; j++)
            {
                Vector2Int range = fx_data[j].range;
                if (range.x + range.y > i || range.y == 0) { startptr = j;  break; }
            }
            //endpunkt:
                for (int j = endptr; j < fx_data.Count; j++)
                    if (fx_data[j].range.x > i) { endptr = j; break; }

            //Schreibe Text:
            //prefixe:
            for (int j = startptr; j < endptr; j++)
                text_out += fx_data[j].tag[0];

            //Buchstabe:
            text_out += text[i];

            //sufixe:
            for (int j = startptr; j < endptr; j++)
                text_out += fx_data[j].tag[1];
        }

        return text_out;
    }

    /*
    /// <summary>
    /// Lässt die Buchstaben zittern
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _tremble(string[] tagStrings)
    {
        Debug.Log("start tremble");



        StartCoroutine(Tremble());
    }
    IEnumerator Tremble()
    {

        yield break;
    }
    //*/


    /// <summary>
    /// lässt die buchstaben in Regenbogenfarben aufleuchten 
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _rainbow(string[] tagStrings, int id)
    {
        if (tagStrings.Length < 2) return;

        //erstelle Liste der Farben, die durchlaufen werden sollen
        Color[] colors = new Color[tagStrings[1].Length];
        for(int i = 0; i < colors.Length; i++)
        {
            switch (tagStrings[1][i])
            {
                case 'r': colors[i] = Color.red; break;
                case 'g': colors[i] = Color.green; break;
                case 'b': colors[i] = Color.blue; break;
                case 'y': colors[i] = Color.yellow; break;
                case 'c': colors[i] = Color.cyan; break;
                case 'm': colors[i] = Color.magenta; break;
                case 'p': colors[i] = Color.magenta; break;
                case 'o': colors[i] = new Color(1, .5f, 0); break;//orange
                case 'w': colors[i] = Color.white; break;
                case 'd': colors[i] = Color.black; break;
            }
        }


        StartCoroutine(Rainbow(id, colors));
    }
    IEnumerator Rainbow(int id, Color[] colors)
    {
        while (!clearEffects)
        {
            //Update of fx:
            FX_Data data = fx_data[id];

            if (data.isUniform)
            {
                //Alle Buchstaben teilen sich einen Tag

            }
            else
            {
                //Jeder Buchstabe hat einen bestimmten Tag

                //Update die Range:
                int endRange = fx_data[id].range.y != 0 ? fx_data[id].range.y - fx_data[id].range.x : 2;
                if (fx_data[id].tag.Count < endRange)
                {
                    int diff = endRange - fx_data[id].tag.Count;
                    fx_data[id].tag.AddRange(new string[diff][]);
                    for(int i = 0; i < diff; i++)
                        fx_data[id].tag[fx_data[id].tag.Count - 1 - i] = new string[2] { "", "</color>"};
                }

                //Aktualisiere die Tags:
                for (int i = 0; i < endRange; i++)
                {
                    //prefix:
                    fx_data[id].tag[i][0] = "<color=#" + ColorUtility.ToHtmlStringRGBA(GetColorWheel(colors, i * .1f)) + ">";
                    //sufix:
                    //fx_data[id].tag[i][1] = "</color>";
                }

            }


            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    Color GetColorWheel(Color[] colors, float offset = 0)
    {
        //setze zeit:
        float time = Time.time + offset;

        //ermittle color-index:
        int cid1 = Mathf.FloorToInt(Mathf.Abs(time - Mathf.Floor(time)) * colors.Length);
        int cid2 = (cid1 + 1) % colors.Length;
        
        //ermittle color-faktoren:
        float displacement = 1 / colors.Length;
        float cfac1 = Mathf.Clamp01(Mathf.Abs(time - Mathf.Floor(time) - .5f) * colors.Length - colors.Length/2 + 2);
        float cfac2 = Mathf.Clamp01(Mathf.Abs(time + displacement - Mathf.Floor(time + displacement) - .5f) * colors.Length - colors.Length/2 + 2);

        return cfac1 * colors[cid1] + cfac2 * colors[cid2];
    }

}
