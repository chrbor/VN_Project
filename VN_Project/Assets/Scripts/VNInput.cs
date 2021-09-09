using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using static VNProcessing;

//ref: https://support.unity.com/hc/en-us/articles/115000341143-How-do-I-read-and-write-data-from-a-text-file-
//ref: http://digitalnativestudios.com/textmeshpro/docs/rich-text/

[RequireComponent(typeof(VNProcessing))]
public class VNInput : MonoBehaviour
{
    public static VNInput vnInput;

    public TextAsset VN_start;
    TextAsset VN;
    StreamReader reader;


    private int ptr;//Position des lese-pointers
    private int ptr_linebreak;//position des letzten zeilenumbruchs

    private void Awake()
    {
        vnInput = this;
    }

    private void Start()
    {
        StartTXT(VN_start);
    }

    public void StartTXT(TextAsset text_VN)
    {
        if (text_VN == null) return;
        VN = text_VN;

        //Setup:
        ptr = 0;

        //sDebug.Log(VN.text);

        ReadNextPart();
    }

    public void CloseTXT()
    {

    }

    public void ReadNextPart(string overflow = "")
    {
        //Führe das letzte Kommando aus und gehe in idle- zustand, wenn das Ende des Textdokuments erreicht wird:
        if (ptr >= VN.text.Length - 2)
        {
            vnProcessing.VN_Line_Handler("end:\n");
            return;
        }
        //else Debug.Log(ptr + " < " + VN.text.Length);

        //suche nach nächstem Befehl:
        string cmd = overflow;
        ptr_linebreak = ptr;

        char _char;
        int i = ptr;
        for(; i < ptr + 1000 || i >= VN.text.Length; i++)
        {
            _char = VN.text[i];
            if (_char == '\n' || (_char == '/' && (VN.text[i + 1] == '/' || VN.text[i + 1] == '*')))
                break;
            cmd += _char;
        }
        if(i == ptr+1000) { Debug.Log("Error: 1000 chars without return!"); return; }

        ptr = i + 1;

        if (VN.text[ptr - 1] == '/')
        {
            if(VN.text[ptr] == '/')//skip line comment
            {
                int jmp = VN.text.IndexOf('\n', ptr);
                ptr = jmp < 0 ? VN.text.Length : jmp + 1; cmd += '\n';
            }
            else//skip comment
            {
                int jmp = VN.text.IndexOf("*/", ptr);
                ptr = jmp < 0 ? VN.text.Length : jmp + 2;
                ReadNextPart(cmd);
                return;
            }
        }


        if (cmd.Length < 2 && vnProcessing.mode != VNMode.output) { ReadNextPart(); return; }//überspringe leeren string

        vnProcessing.VN_Line_Handler(cmd);
        //ReadNextPart();
    }




}
