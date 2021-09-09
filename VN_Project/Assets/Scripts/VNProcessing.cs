using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static VNInput;
using static VNOutput;


[RequireComponent(typeof(VNInput), typeof(VNOutput))]
public class VNProcessing : MonoBehaviour
{
    public static VNProcessing vnProcessing;

    public enum VNMode { idle, outputDefinition, output, transition, char_transition, effect}
    [HideInInspector]
    public VNMode mode = VNMode.idle;

    public class OutputSettings
    {
        public bool overlay = false;
        public float time = -1;

        public float speed;
        public float pause;
        public List<string> fxName;

        public string charName = "";
        public string text = "";

        public OutputSettings()
        {
            fxName = new List<string>();
        }
    }

    public class EffectSettings
    {
        public string effectName = "";
        public bool stop = false;
    }

    public class TransitionSettings
    {
        public string transitionName = "";
        public Sprite background = null;
        public float time_transition = 1;
        public float time_pause = -1;

        public TransitionSettings()
        {

        }
    }

    public class CharTransitionSettings
    {
        public class CharData
        {
            /// <summary> Name, der mit dem in dict_chars gleicht</summary>
            public string char_name;
            /// <summary> Position des Chars in Prozent (Center ist der untere linke Bildschirmrand)</summary>
            public Vector2 char_position;
            /// <summary> Name der Animation des Chars, die bei der Transition abgespielt wird </summary>
            public string char_animation = "default";

            public CharData(string _name, Vector2 _position, string _animation)
            {
                char_name = _name;
                char_position = _position;
                char_animation = _animation;
            }
        }
        /// <summary> Name der Transition, die abgespielt werden soll </summary>
        public string transitionName = "";
        /// <summary> indivduelle Daten für jeden aktiven Charakter </summary>
        public CharData[] charData;
        /// <summary> Wenn wahr, dann wird der Char ausgeblendet und der aktive Char zerstört </summary>
        public bool hide = false;
        /// <summary> Gibt in Sekunden an, wie lange die Transition dauert </summary>
        public float time_transition = 1;
        /// <summary> Gibt in Sekunden an, ab wann das nächste Kommando automatisch abgespielt wird. Wenn time_pause kleiner 0, dann ist Autoplay deaktiviert</summary>
        public float time_pause = -1;

        public CharTransitionSettings()
        {

        }
    }

    private OutputSettings outputSettings;
    private EffectSettings effectSettings;
    private TransitionSettings transitionSettings;
    private CharTransitionSettings charSettings;


    /// <summary>Wenn wahr, dann wird jedes mal bei einer Textausgabe eines Chars dessen Mund animiert (default: false)</summary>
    public static bool useSpeechAnimation;
    /// <summary>Gibt an, wie viele character pro fixedUpdate geschrieben werden (default: .5f)</summary>
    public static float speechSpeed;
    /// <summary>Gibt an, auf welcher Höhe in Prozent die Sprites standardmäßig spawnen (default: 50)</summary>
    public static float y_default;


    int lineSkipCount = 0;
    int modCount = 0;
    bool firstLine = false;

    readonly char[] sepSpace = new char[] { ' ' };
    readonly char[] sepComma = new char[] { ' ', ',' };
    readonly char[] sepMod = new char[] {'[', ' ', ']' };
    readonly char[] sepSpaceMod = new char[] {'[', ' ', ',', ':', ']' };
    readonly char[] sepVector = new char[] {'(', ':', ';', '|', ')'};

    public static System.Globalization.NumberStyles style = System.Globalization.NumberStyles.Number;
    public static System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CreateSpecificCulture("en-GB");

    //Liste der aktuellen Characters, die sprechen oder in der Szene sind:
    /// <summary> Dict aller Chars, die auf dem Bildschirm zu sehen sind </summary>
    public static Dictionary<string, RectTransform> dict_chars;
    /// <summary> Liste aller Chars, die für das aktuelle Kommando aktiv sind </summary>
    public static List<string> selectedChars;


    private void Awake()
    {
        if (vnProcessing != null) { Destroy(gameObject); return; }
        vnProcessing = this;

        useSpeechAnimation = false;
        speechSpeed = .5f;
        y_default = 50;

        selectedChars = new List<string>();
    }

    /// <summary>
    /// Handelt das angegebene Kommando
    /// </summary>
    /// <param name="cmd"></param>
    public void VN_Cmd_Handler(string cmd)
    {
        //Debug.Log(cmd);
        foreach (string line in cmd.Split('\n')) { VN_Line_Handler(line); }
        vnInput.ReadNextPart();
    }

    /// <summary>
    /// Handelt die angegebene Textzeile
    /// </summary>
    /// <param name="line"></param>
    public void VN_Line_Handler(string line)
    {
        //Debug.Log("handling:\n" + line);

        //Leere output-line:
        if (line.Length == 1 && mode == VNMode.output)
        {
            lineSkipCount++;
            if(!firstLine) outputSettings.text += line + "<line-height=100%>" + '\n';
            vnInput.ReadNextPart();
        }

        //Modifikation
        else if (line[0] == '[' && line[line.Length - 2] == ']')
        {
            if (mode == VNMode.idle || mode == VNMode.output) return;
            DoModification(line.ToLower().Split(sepMod, System.StringSplitOptions.RemoveEmptyEntries));//ermöglicht folgende syntax: [...][...]...
            vnInput.ReadNextPart();
        }     

        //Kommando:     
        else if(line.Length > 1 && line[line.Length-2] == ':')
        {
            VNMode _mode = mode;

            //Führe den aktuellen Modus aus:
            DoCommand(_mode, line != "end:\n");

            //Ermittle den nächsten Modus:
            GetMode(line);


        }

        //Wandle Text um:
        else if(mode == VNMode.outputDefinition || mode == VNMode.output)
        {
            if (mode == VNMode.outputDefinition) outputSettings.text = "<line-height=.1>" + '\n';

            mode = VNMode.output;
            outputSettings.text += line + "<line-height=100%>" +'\n';
            firstLine = false;
            vnInput.ReadNextPart();
        }

        //Settings:
        else
        {
            string[] words = line.ToLower().Split(sepSpaceMod, System.StringSplitOptions.RemoveEmptyEntries);
            switch (words[0])
            {
                case "speechAnimation": useSpeechAnimation = words[1] == "on"; break;
                case "speechSpeed": float.TryParse(words[1], style, culture, out speechSpeed); break;
                case "y_default": float.TryParse(words[1], style, culture, out y_default); break;          
            }
        }
    }

    void DoModification(string[] modStrings)
    {
        foreach(string modString in modStrings)
        {
            //string modString = _modString.Trim();
            if (modString.Length == 0) continue;

            switch (mode)
            {
                case VNMode.transition:
                    if (transitionSettings == null) break;

                    if (modString.Contains("/"))
                        transitionSettings.background = Resources.Load<Sprite>("VisualNovel/Background/" + modString);
                    else if(modString[0] > 47 && modString[0] < 58)//Zahl
                    {
                        string[] timeStrings = modString.Split(sepComma, System.StringSplitOptions.RemoveEmptyEntries);

                        if (timeStrings[0].Contains("se"))
                            timeStrings[0].Remove(timeStrings[0].Length - 3);
                        float.TryParse(timeStrings[0], out transitionSettings.time_transition);

                        if (timeStrings.Length == 1) break;

                        if (timeStrings[1].Contains("se"))
                            timeStrings[1].Remove(timeStrings[1].Length - 3);
                        float.TryParse(timeStrings[1], out transitionSettings.time_pause);
                    }
                    else
                        transitionSettings.transitionName = modString;

                    break;



                case VNMode.char_transition:
                    Debug.Log("charTransMod");
                    if (charSettings == null || charSettings.charData == null) break;

                    string[] mods = modString.Split(sepComma, System.StringSplitOptions.RemoveEmptyEntries);

                    if (modCount == 0)
                    {
                        int i = 0;
                        string prevAnim = "";
                        foreach (var character in charSettings.charData)
                        {
                            character.char_animation = i >= mods.Length ? prevAnim : mods[i];
                            prevAnim = character.char_animation;
                            i++;
                        }  
                    }
                    else if (modString[0] == '(')
                    {
                        int i = 0;
                        string[] prevPos = new string[0];
                        string[] posString = new string[0];
                        float x_pos, y_pos;
                        foreach (var character in charSettings.charData)
                        {
                            posString = i < mods.Length ? mods[i].Split(sepVector, System.StringSplitOptions.None) : prevPos;
                            x_pos = -1;
                            y_pos = -1;
                            float.TryParse(posString[1], out x_pos);
                            float.TryParse(posString[posString.Length - 2], out y_pos);

                            character.char_position =  new Vector2(x_pos > 0 ? x_pos : (100 * i + 50) / charSettings.charData.Length, 
                                                                   y_pos > 0 ? y_pos : y_default);
                            prevPos = posString;
                            i++;
                        }
                    }
                    else if (modString == "hide") charSettings.hide = true;
                    else if (modString[0] > 47 && modString[0] < 58)//Zahl
                    {
                        string[] timeStrings = modString.Split(sepComma, System.StringSplitOptions.RemoveEmptyEntries);

                        if (timeStrings[0].Contains("se"))
                            timeStrings[0].Remove(timeStrings[0].Length - 3);
                        float.TryParse(timeStrings[0], out charSettings.time_transition);

                        if (timeStrings.Length == 1) break;

                        if (timeStrings[1].Contains("se"))
                            timeStrings[1].Remove(timeStrings[1].Length - 3);
                        float.TryParse(timeStrings[1], out charSettings.time_pause);
                    }
                    else charSettings.transitionName = modString;

                    break;



                case VNMode.outputDefinition:
                    if (outputSettings == null) break;

                    if(modString == "overlay") outputSettings.overlay = true;
                    else if (modString[0] > 47 && modString[0] < 58)//Zahl
                    {
                        if(modString.Contains("se"))
                            modString.Remove(modString.Length - 3);
                        float.TryParse(modString, out outputSettings.time);
                    }
                    break;
                case VNMode.effect:
                    if (effectSettings == null) break;

                    if (modString == "stop") effectSettings.stop |= true;
                    else effectSettings.effectName = modString;
                    break;
            }
            modCount++;
        }

    }

    void GetMode(string line)
    {
        modCount = 0;
        line = line.Remove(line.Length - 2);
        string[] words = line.Split(sepSpace, System.StringSplitOptions.RemoveEmptyEntries);

        if(words.Length == 0)
        {
            //Output ohne Char:
            selectedChars.Clear();
            mode = VNMode.outputDefinition;
            outputSettings = new OutputSettings();
            return;
        }

        switch(words[words.Length - 1].ToLower())
        {
            case "effect":
                mode = VNMode.effect;
                effectSettings = new EffectSettings();
                break;

            case "idle": mode = VNMode.idle; break;
            case "end": mode = VNMode.idle; break;

            default:
                if(words[0].ToLower() == "transition")
                {
                    mode = VNMode.transition;
                    transitionSettings = new TransitionSettings();
                    return;
                }

                //Name(n)
                //setze die aktiven Charaktere:
                selectedChars.Clear();
                selectedChars.AddRange(line.Split(sepComma, System.StringSplitOptions.RemoveEmptyEntries));

                if (selectedChars[selectedChars.Count - 1].ToLower() != "transition")
                {
                    mode = VNMode.outputDefinition;
                    outputSettings = new OutputSettings();
                    outputSettings.charName = line;
                    return;
                }
                selectedChars.RemoveAt(selectedChars.Count - 1);

                mode = VNMode.char_transition;
                charSettings = new CharTransitionSettings();
                break;
        }
    }

    void DoCommand(VNMode _mode, bool fetchNext = true)
    {
        Debug.Log("execute " + _mode);
        //*
        switch (_mode)
        {
            case VNMode.output:
                if (lineSkipCount > 0) outputSettings.text = outputSettings.text.Remove(outputSettings.text.Length - lineSkipCount * 20);//lösche alle letzten leeren Zeilen
                lineSkipCount = 0;
                firstLine = true;
                StartCoroutine(vnOutput.PlayText(outputSettings));
                break;
            case VNMode.transition: StartCoroutine(vnOutput.PlayTansition(transitionSettings)); break;
            case VNMode.char_transition: StartCoroutine(vnOutput.PlayCharTransition(charSettings)); break;
            case VNMode.effect: StartCoroutine(vnOutput.PlayEffect(effectSettings)); break;
            case VNMode.idle: if (fetchNext) StartCoroutine(FetchNext()); return;
        }
        //*/
    }

    IEnumerator FetchNext()
    {
        yield return new WaitForFixedUpdate();
        vnInput.ReadNextPart();
        yield break;
    }
}
