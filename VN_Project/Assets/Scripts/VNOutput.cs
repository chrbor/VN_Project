﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static VNProcessing;
using static VNInput;
using static VNEffects;

[RequireComponent(typeof(VNProcessing), typeof(VNEffects))]
public class VNOutput : MonoBehaviour
{
    public static VNOutput vnOutput;

    public RectTransform textField;
    private TextMeshProUGUI dialogText, nameText;

    char[] sepTag = new char[] { ' ', ',', '=', ':', ';', '|'};
    float ptr_limit;
    int ptr;

    void Awake()
    {
        vnOutput = this;

        nameText = textField.GetChild(0).GetComponent<TextMeshProUGUI>();
        dialogText = textField.GetChild(1).GetComponent<TextMeshProUGUI>();
    }

    public IEnumerator PlayText(OutputSettings output)
    {
        dialogText.text = "";
        output.speed = speechSpeed;
        output.text = output.text.Remove(output.text.Length - 1, 1);
        yield return new WaitForFixedUpdate();

        Debug.Log("dialog: " + dialogText.text.Length + ", output: " + output.text.Length);

        //laufe alle buchstaben ab und schreibe sie in das dialog-Feld:
        ptr = 0;
        ptr_limit = 0;
        while (dialogText.text.Length < output.text.Length)
        {
            if (output.speed > 0)
            {
                while(Mathf.FloorToInt(ptr_limit) < ptr && !Input.anyKey && Input.touchCount == 0)
                {
                    ptr_limit += output.speed;
                    yield return new WaitForFixedUpdate();
                }
            }

            //Teste auf Tags:
            CheckTag(output);
            if(output.pause > 0)
            {
                yield return new WaitForSeconds(output.pause);
                output.pause = 0;
            }


            dialogText.text += output.text[ptr++];

            if (Input.anyKey || Input.touchCount != 0) break;
        }

        if(dialogText.text.Length < output.text.Length)
        {
            //noch inkorrekt:
            dialogText.text += output.text.Substring(dialogText.text.Length);
        }

        //yield return new WaitUntil(() => !Input.anyKey && Input.touchCount == 0);
        yield return new WaitUntil(() => Input.anyKey || Input.touchCount != 0);
        yield return new WaitUntil(() => !Input.anyKey && Input.touchCount == 0);


        vnEffects.ClearFX();
        vnInput.ReadNextPart();
        yield break;
    }

    /// <summary>
    /// Liest alle Tags aus dem output-Text raus und wandelt sie in die entsprechende Ausgabe um
    /// </summary>
    /// <param name="output"></param>
    void CheckTag(OutputSettings output)
    {
        char nxtChar = output.text[dialogText.text.Length];
        if (nxtChar != '<') return;

        int tagLength = output.text.IndexOf('>', dialogText.text.Length) - dialogText.text.Length;
        if (tagLength < 0 || tagLength > 50) return;
        //Debug.Log("tagLength: " + tagLength + ", dialogLength: " + dialogText.text.Length + ", outputText: " + output.text.Length);

        string tagString = output.text.Substring(dialogText.text.Length+1, tagLength-1);
        string[] tagStrings = tagString.Split(sepTag, System.StringSplitOptions.RemoveEmptyEntries);


        bool isEnd = tagStrings[0][0] == '/';
        if (isEnd) tagStrings[0] = tagStrings[0].Remove(0, 1);

        //Ermittle Tag und [de]aktiviere ihn:
        bool skipRemove = false;
        switch (tagStrings[0])
        {
            case "speed":
                if (tagStrings.Length == 1) break;
                if (isEnd) output.speed = speechSpeed;
                else if (!float.TryParse(tagStrings[1], style, culture, out output.speed)) break;
                break;

            case "pause":
                if (tagStrings.Length == 1) break;
                else if (!float.TryParse(tagStrings[1], style, culture, out output.pause)) break;
                break;

            case "play":
                //alle oder selektiv?:
                if (tagStrings.Length == 1)//unbestimmt -> alle
                {

                }
                break;

            default://FX
                MethodInfo method = vnEffects.GetType().GetMethod("_" + tagStrings[0]);

                if(method == null)
                {
                    Debug.Log("überspringe tag: " + tagStrings[0] + " um " + tagLength);

                    //Falls nicht FX:
                    skipRemove = true;
                    break;
                }
                if (!isEnd) vnEffects.StartFX(tagStrings, method, ptr);//method.Invoke(vnEffects, new object[] { tagStrings, ptr });
                break;
        }

        //Remove tag:
        if (!skipRemove) { output.text = output.text.Remove(dialogText.text.Length, tagLength + 1); Debug.Log("shortened output:\n" + output.text); CheckTag(output); }
        else { ptr_limit += tagLength; }//überspringe tag
    }








    public IEnumerator PlayTansition(TransitionSettings transition)
    {
        yield return new WaitForFixedUpdate();

        vnInput.ReadNextPart();
        yield break;
    }

    public IEnumerator PlayCharTransition(CharTransitionSettings charTransition)
    {
        yield return new WaitForFixedUpdate();

        /*
        foreach (var character in selectedChars)
        {
            StartCoroutine(PlaySingleCharTransition(character, charTransition));
        }
        //*/
        vnInput.ReadNextPart();
        yield break;
    }

    public IEnumerator PlayEffect(EffectSettings effect)
    {
        yield return new WaitForFixedUpdate();

        vnInput.ReadNextPart();
        yield break;
    }

    private IEnumerator PlaySingleCharTransition(string character, CharTransitionSettings charTransition)
    {
        //Erstelle neues Object, wenn noch nicht vorhanden:
        GameObject newChar = Resources.Load<GameObject>("Dialog/Chars/" + character + "/" + character);
        dict_chars.Add(character, newChar.GetComponent<RectTransform>());

        //Führe Transition aus:
        if(charTransition.transitionName == "default")
        {
            if (charTransition.hide) Destroy(dict_chars[character].gameObject);
            yield break;
        }
        if (charTransition.time_transition <= .1f) charTransition.time_transition = .1f;

        float timeStep = Time.fixedDeltaTime / charTransition.time_transition;
        float count = 0;
        for(; count < 1; count += timeStep)
        {
            //Setze Transition:
            switch (charTransition.transitionName)
            {
                case "default"://erscheint sofort

                    break;
                case "fade"://ändert die Tranzparenz

                    break;
                default://alle shader-transitionen

                    break;
            }
            yield return new WaitForFixedUpdate();
        }

        if(Mathf.Round(count) == 0) { Destroy(dict_chars[character].gameObject); yield break; }
        

        //setze den Wert exakt:
        switch (charTransition.transitionName)
        {
            case "fade"://ändert die Tranzparenz:
                
                break;
            default://alle shader- transitionen

                break;
        }

        yield break;
    }
}
