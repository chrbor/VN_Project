using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using static VNProcessing;

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
        public bool uniform;
        public string sufix;

        public FX_Data(int startptr, string sufix_default = "", bool _uniform = false)
        {
            tag = new List<string[]>();
            range = new Vector2Int(startptr, 0);
            uniform = _uniform;

            sufix = sufix_default;
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
        Debug.Log("start " + tagStrings[0] + " at " + startptr);

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
        int id = fx_names_open.LastIndexOf(fx_name);
        if (id == -1) return;

        fx_data[id].range += Vector2Int.up * endptr;
        fx_names_open[id] = "";

        UpdateRange(id);
    }

    /// <summary>
    /// Nimmt den Text und fügt die Effekte hinzu, die in den Listen aufgelistet sind
    /// </summary>
    /// <param name="text">Text, dem die Effekte hinzugefügt werden sollen</param>
    /// <returns>Text, dem die Effekte hinzugefügt wurden</returns>
    public string UpdateTextFX(string text)
    {
        textLength = text.Length;

        //im fall das keine fx-tags vorhanden sind breche ab:
        if (fx_data.Count == 0) return text;


        string text_out = "";
        int startptr = 0, endptr = 0;//start- und endpunkt der liste der aktiven tags für den jeweiligen buchstaben

        //Laufe die Buchstaben durch und füge die Tags hinzu:
        for (int i = 0; i < text.Length; i++)
        {
            //umgehe tags im text:
            if (text[i] == '<')
            {
                int i_end = text.IndexOf('>', i);
                if (i_end > 0 || i_end < text.IndexOf('<', i + 1))//falls innerhalb von <...>, dann überspringe diesen Bereich
                {
                    text_out += text.Substring(i, i_end + 1 - i);
                    i = i_end;
                    continue;
                }
            }

            //aktualisiere start- und endpunkt der tags:
            //startpunkt:
            for(; startptr < fx_data.Count; startptr++)
            {
                Vector2Int range = fx_data[startptr].range;
                if (range.y == 0) range.y = range.x + fx_data[startptr].tag.Count;
                if (range.y > i) break;
            }
            //endpunkt:
            for (; endptr < fx_data.Count; endptr++)
            {
                Vector2Int range = fx_data[endptr].range;
                if (range.x > i || (range.y < i && range.y != 0)) break;
            }

            //Schreibe Text:
            //prefixe:
            for (int j = startptr; j < endptr; j++)
            {
                if (j == fx_data.Count || fx_data[j].uniform && i != fx_data[j].range.x) continue;
                //if (fx_data[j].tag.Count > 0) Debug.Log("prefix: " + fx_data[j].tag[0][1]);

                text_out += fx_data[j].tag[i - fx_data[j].range.x][0];
            }

            //Buchstabe:
            text_out += text[i];

            //sufixe:
            for (int j = startptr; j < endptr; j++)
            {
                if (fx_data[j].uniform)
                {
                    if (i != fx_data[j].range.y - 1) continue;
                    text_out += fx_data[j].tag[0][1];
                }
                else
                    text_out += fx_data[j].tag[i - fx_data[j].range.x][1];
            }
        }

        return text_out;
    }


    /// <summary>
    /// lässt die buchstaben springen
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _jump(string[] tagStrings, int id)
    {
        //Laufe andere params ab:
        float frq = 1;
        float step = .1f;
        float amp = 1;
        for (int ptr = 1; ptr < tagStrings.Length; ptr++)
        {
            switch (tagStrings[ptr])
            {
                case "frq":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out frq);
                    break;
                case "amp":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out amp);
                    break;
                case "step":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out step);
                    fx_data[id].uniform = step == 0;
                    break;
            }
        }

        //sufix ist eine Konstante:
        fx_data[id].sufix = "</voffset>";
        StartCoroutine(vMoveSine(id, amp, frq, step, true));
    }
    /// <summary>
    /// lässt die buchstaben sich in einer welle bewegen
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _wave(string[] tagStrings, int id)
    {
        //Laufe andere params ab:
        float frq = 1;
        float step = .1f;
        float amp = 1;
        for (int ptr = 1; ptr < tagStrings.Length; ptr++)
        {
            switch (tagStrings[ptr])
            {
                case "frq":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out frq);
                    break;
                case "amp":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out amp);
                    break;
                case "step":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out step);
                    fx_data[id].uniform = step == 0;
                    break;
            }
        }

        //sufix ist eine Konstante:
        fx_data[id].sufix = "</voffset>";
        StartCoroutine(vMoveSine(id, amp, frq, step, false));
    }
    IEnumerator vMoveSine(int id, float amp, float frq, float step, bool jump)
    {
        while (!clearEffects)
        {
            int endRange = UpdateRange(id);

            //Aktualisiere die Tags:
            for (int i = 0; i < endRange; i++)
            {
                //prefix:
                float sinVal = Mathf.Sin((Time.time * frq + i * step) * Mathf.PI) * amp;
                fx_data[id].tag[i][0] = "<voffset=" + (jump ? Mathf.Abs(sinVal) : sinVal).ToString("F", culture) + "em>";
                //sufix:
                //fx_data[id].tag[i][1] = "</voffset>";
            }

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    /// <summary>
    /// lässt die buchstaben sich dehnen und stauchen
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _stretch(string[] tagStrings, int id)
    {
        //Laufe andere params ab:
        float frq = 1;
        float amp = .1f;
        for (int ptr = 1; ptr < tagStrings.Length; ptr++)
        {
            switch (tagStrings[ptr])
            {
                case "frq":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out frq);
                    break;
                case "amp":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out amp);
                    break;
            }
        }

        //sufix ist eine Konstante:
        //fx_data[id].sufix = "</cspace>";
        fx_data[id].uniform = true;
        StartCoroutine(Stretch(id, amp, frq));
    }
    IEnumerator Stretch(int id, float amp, float frq)
    {
        //Update of fx:
        FX_Data data = fx_data[id];

        while (!clearEffects)
        {
            if (data.tag.Count < 1)
                data.tag.Add(new string[2] { "", data.sufix });

            //Aktualisiere Tag:
            //prefix:
            int range = data.range.y > 0 ? data.range.y - data.range.x : textLength - data.range.x;
            float dist = Mathf.Sin((Time.time * frq) * Mathf.PI) * amp + .8f;

            //float div = 2 + 0f / range;

            data.tag[0][0] = "<mspace=" + (dist).ToString(culture) + "em>";
            data.tag[0][0] += "<space=" + ((.8f - dist) * range/2f).ToString(culture) + "em>";//positionsausgleich
            //sufix:
            data.tag[0][1] = "</mspace><space=" + ((.8f - dist) * range/2f).ToString(culture) + "em>";

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    /// <summary>
    /// lässt die buchstaben in ihrer Größe pulsieren
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _scale(string[] tagStrings, int id)
    {
        //Laufe andere params ab:
        float frq = 2;
        float amp = 2f;
        for (int ptr = 1; ptr < tagStrings.Length; ptr++)
        {
            switch (tagStrings[ptr])
            {
                case "frq":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out frq);
                    break;
                case "amp":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out amp);
                    break;
            }
        }

        fx_data[id].uniform = true;
        StartCoroutine(Scale(id, amp, frq));
    }
    IEnumerator Scale(int id, float amp, float frq)
    {
        //Update of fx:
        FX_Data data = fx_data[id];

        while (!clearEffects)
        {
            if (data.tag.Count < 1)
                data.tag.Add(new string[2] { "", data.sufix });

            //Aktualisiere Tag:
            //prefix:
            int range = data.range.y > 0 ? data.range.y - data.range.x : textLength - data.range.x;
            float size = (Mathf.Sin((Time.time * frq) * Mathf.PI)/2 +.5f) * (amp-1) + 1;
            float dist = size * .7f;
            data.tag[0][0] = "<mspace=" + (dist).ToString(culture) + "em>"
            + "<space=" + ((.8f - dist) * range / 2f).ToString(culture) + "em>"
            + "<size=" + (size).ToString(culture) + "em>";
            //sufix:
            data.tag[0][1] = "</size><space=" + ((.8f - dist) * range / 2f).ToString(culture) + "em></mspace>";

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    /// <summary>
    /// lässt die buchstaben in ihrer Größe pulsieren
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _swave(string[] tagStrings, int id)
    {
        //Laufe andere params ab:
        float frq = 2;
        float amp = 2f;
        for (int ptr = 1; ptr < tagStrings.Length; ptr++)
        {
            switch (tagStrings[ptr])
            {
                case "frq":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out frq);
                    break;
                case "amp":
                    if (++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out amp);
                    break;
            }
        }

        fx_data[id].uniform = true;
        StartCoroutine(SWave(id, amp, frq));
    }
    IEnumerator SWave(int id, float amp, float frq)
    {
        //Update of fx:
        FX_Data data = fx_data[id];

        while (!clearEffects)
        {

            if (data.tag.Count < 1)
                data.tag.Add(new string[2] { "", data.sufix });

            //Aktualisiere Tag:
            //prefix:
            int range = data.range.y > 0 ? data.range.y - data.range.x : textLength - data.range.x;
            float size = (Mathf.Sin((Time.time * frq) * Mathf.PI) / 2 + .5f) * (amp - 1) + 1;
            float dist = size * .7f;
            data.tag[0][0] = "<mspace=" + (dist).ToString(culture) + "em>"
            + "<space=" + ((.8f - dist) * range / 2f).ToString(culture) + "em>"
            + "<size=" + (size).ToString(culture) + "em>";
            //sufix:
            data.tag[0][1] = "</size><space=" + ((.8f - dist) * range / 2f).ToString(culture) + "em></mspace>";

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    /// <summary>
    /// lässt die buchstaben in Regenbogenfarben aufleuchten 
    /// </summary>
    /// <param name="tagStrings"></param>
    public void _rainbow(string[] tagStrings, int id)
    {
        if (tagStrings.Length < 2)
        {
            fx_data[id].tag.Add(new string[2]);
            fx_data[id].uniform = true;
            return;
        }
        //erstelle Liste der Farben, die durchlaufen werden sollen
        List<Color> colors = new List<Color>();
        for(int ptr = 0; ptr < tagStrings[1].Length; ptr++)
        {
            switch (tagStrings[1][ptr])
            {
                case '#':
                    //laufe index nach ziffern ab:
                    string cVal = "#";
                    ptr++;
                    for(; ptr < tagStrings[1].Length; ptr++)
                    {
                        if (tagStrings[1][ptr] < 48 || tagStrings[1][ptr] > 70 || (tagStrings[1][ptr] > 57 && tagStrings[1][ptr] < 65)) break;
                        cVal += tagStrings[1][ptr];
                    }
                    if(ptr != tagStrings[1].Length) --ptr;

                    Color cOut = Color.black; 
                    ColorUtility.TryParseHtmlString(cVal, out cOut);
                    colors.Add(cOut);
                    break;
                case 'r': colors.Add(Color.red); break;
                case 'g': colors.Add(Color.green); break;
                case 'b': colors.Add(Color.blue); break;
                case 'y': colors.Add(Color.yellow); break;
                case 'c': colors.Add(Color.cyan); break;
                case 'm': colors.Add(Color.magenta); break;
                case 'p': colors.Add(Color.magenta); break;
                case 'o': colors.Add(new Color(1, .5f, 0)); break;//orange
                case 'w': colors.Add(Color.white); break;
                case 's': colors.Add(new Color(.75f, .75f, .75f)); break;
                case 'd': colors.Add(Color.black); break;
            }
        }

        //Laufe andere params ab:
        float frq = 1;
        float step = .02f;
        for(int ptr = 2; ptr < tagStrings.Length; ptr++)
        {
            switch (tagStrings[ptr])
            {
                case "frq":
                    if(++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out frq);
                    break;
                case "step":
                    if(++ptr < tagStrings.Length) float.TryParse(tagStrings[ptr], style, culture, out step);
                    break;
            }
        }

        //sufix ist eine Konstante:
        fx_data[id].sufix = "</color>";
        StartCoroutine(Rainbow(id, colors.ToArray(), frq, step));
    }
    IEnumerator Rainbow(int id, Color[] colors, float frq, float step)
    {
        //Update of fx:
        FX_Data data = fx_data[id];

        while (!clearEffects)
        {
            //Jeder Buchstabe hat einen bestimmten Tag

            //Update die Range:
            int endRange = UpdateRange(id);

            //Aktualisiere die Tags:
            for (int i = 0; i < endRange; i++)
                data.tag[i][0] = "<color=#" + ColorUtility.ToHtmlStringRGBA(GetColorWheel(colors, frq, i * step)) + ">";

            yield return new WaitForEndOfFrame();
        }

        yield break;
    }

    Color GetColorWheel(Color[] colors, float frequency, float offset = 0)
    {

        //setze zeit:
        float time = Time.time * frequency + offset;

        //ermittle color-index:
        int cid1 = Mathf.FloorToInt(Mathf.Abs(time - Mathf.Floor(time)) * colors.Length);
        int cid2 = (cid1 + 1) % colors.Length;

        time *= colors.Length;
        time -= Mathf.Floor(time);

        //ermittle color-faktoren:
        //float cfac1 = Mathf.Clamp01(Mathf.Abs(time - Mathf.Floor(time) - .5f) * 2 * colors.Length - colors.Length + 2);
        //float cfac2 = Mathf.Clamp01(Mathf.Abs(displaced_time - Mathf.Floor(displaced_time) - .5f) * 2 * colors.Length - colors.Length + 2);
        float cfac1 = Mathf.Clamp01(2 - 2 * time);
        float cfac2 = Mathf.Clamp01(2 * time);

        return cfac1 * colors[cid1] + cfac2 * colors[cid2];
    }

    int UpdateRange(int id)
    {
        //Update of fx:
        FX_Data data = fx_data[id];

        int endRange = 1;
        if (data.uniform)
        {
            if(data.tag.Count == 0)
                data.tag.Add(new string[2] { "", data.sufix });
        }
        else
        {
            //Jeder Buchstabe hat einen bestimmten Tag

            //Update die Range:
            endRange = data.range.y != 0 ? data.range.y - data.range.x : textLength - data.range.x + 1;
            //if (endRange < 1) endRange = 1;
            if (data.tag.Count < endRange)
            {
                int diff = endRange - data.tag.Count;
                data.tag.AddRange(new string[diff][]);
                for (int i = 0; i < diff; i++)
                    data.tag[data.tag.Count - 1 - i] = new string[2] { "", data.sufix };
            }
        }

        return endRange;
    }
}
