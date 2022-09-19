using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;
using System;

public class SimonsSwarmScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMColorblindMode colorblind;
    public KMSelectable[] buttons;
    public Light[] lights;
    public SpriteRenderer[] spriteRends;
    public Sprite[] sprites;
    public TextMesh[] cbTexts;

    private readonly string[] beeNames = { "zBBBzBz", "zzzBBBBz", "BzzBBz", "zzzBzBz", "zBBzBB", "zzBBzB", "zzBBzzzB", "BzBzzzzB", "BBBzBzz", "BBBzzzz", "BzzzzBzzz", "BzzzzBzB", "zBBzzBB", "BBBzzzzB", "BzBzzzB", "zzzBzB", "zBzBzzz", "BzzzBzzzB", "zBzzBzzzB", "zzBzBzB", "zBzzzz", "BBzzzBB", "zzBzzBz", "zBBBzzB", "BzBBBzzz", "BBBBzzz", "zzBzzBBzB", "zBBzBzB", "BzzzBzzz", "zzzBBzB", "zBzzBB", "BBzzBB", "zzBzzzBB", "zzzBzzB", "zBzBBz", "zBzBBz", "BzzzBz", "zzBzzzzBB", "zzzzBBz", "zBzzzB", "zBBBzzzB", "BzzBzzB" };
    private readonly string[] typeNames = { "Honeybee", "Carpenter Bee", "Bumblebee", "Mason Bee", "Leafcutter Bee", "Squash Bee", "Blueberry Bee" };
    private readonly string[] colorNames = { "Red", "Green", "Blue", "Yellow", "Cyan", "Magenta" };
    private readonly Color[] colors = { new Color(1, 0, 0), new Color(0, 1, 0), new Color(0, 0, 1), new Color(1, 1, 0), new Color(0, 1, 1), new Color(1, 0, 1) };
    private int[] chosenColors = new int[5];
    private int[] chosenTypes = new int[5];
    private List<int> flashes = new List<int>();
    private List<int> answer = new List<int>();
    private string bzz;
    private int pressIndex;
    private int stage;
    private bool firstPress;
    private bool activated;
    private bool cbEnabled;
    private bool addedB;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += Activate;
    }

    void Start()
    {
        if (colorblind.ColorblindModeActive)
            cbEnabled = true;
        Debug.LogFormat("[Simon's Swarm #{0}] Bees going clockwise from north:", moduleId);
        float scalar = transform.lossyScale.x;
        for (int i = 0; i < 5; i++)
        {
            chosenColors[i] = UnityEngine.Random.Range(0, colorNames.Length);
            chosenTypes[i] = UnityEngine.Random.Range(0, typeNames.Length);
            Debug.LogFormat("[Simon's Swarm #{0}] Type: {1}, Color: {2}, Name: {3}", moduleId, typeNames[chosenTypes[i]], colorNames[chosenColors[i]], beeNames[chosenTypes[i] * 6 + chosenColors[i]]);
            spriteRends[i].sprite = sprites[chosenTypes[i] * 6 + chosenColors[i]];
            lights[i].color = colors[chosenColors[i]];
            lights[i].range *= scalar;
            if (cbEnabled)
                cbTexts[i].text = colorNames[chosenColors[i]][0].ToString();
            else
                cbTexts[i].text = "";
        }
        LightsOff();
        for (int i = 0; i < 3; i++)
            flashes.Add(UnityEngine.Random.Range(0, 5));
        Debug.LogFormat("[Simon's Swarm #{0}] Flashes going clockwise from north: {1}", moduleId, flashes.Join(", "));
        for (int i = 0; i < 3; i++)
            AddToString(i);
        if (bzz.EndsWith("z"))
        {
            addedB = true;
            bzz += "B";
        }
        Debug.LogFormat("[Simon's Swarm #{0}] String Bzz for stage {1} is: {2}", moduleId, stage + 1, bzz);
        GetPresses();
        Debug.LogFormat("[Simon's Swarm #{0}] Bees to press for stage {1} going clockwise from north: {2}", moduleId, stage + 1, answer.Join(", "));
    }

    void Activate()
    {
        StartCoroutine(FlashSequence());
        activated = true;
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true && activated != false)
        {
            pressed.AddInteractionPunch(.75f);
            audio.PlaySoundAtTransform("buzz" + UnityEngine.Random.Range(1, 6), pressed.transform);
            if (!firstPress)
                firstPress = true;
            StopAllCoroutines();
            LightsOff();
            int index = Array.IndexOf(buttons, pressed);
            StartCoroutine(FlashBee(index));
            Debug.LogFormat("[Simon's Swarm #{0}] Pressed bee {1}", moduleId, index);
            if (answer[pressIndex] == index)
            {
                pressIndex++;
                if (answer.Count == pressIndex)
                {
                    pressIndex = 0;
                    answer.Clear();
                    stage++;
                    if (stage == 3)
                    {
                        Debug.LogFormat("[Simon's Swarm #{0}] Module solved!", moduleId);
                        moduleSolved = true;
                        GetComponent<KMBombModule>().HandlePass();
                        StartCoroutine(SolveAnim());
                        return;
                    }
                    flashes.Add(UnityEngine.Random.Range(0, 5));
                    Debug.LogFormat("[Simon's Swarm #{0}] New flash: {1}", moduleId, flashes.Last());
                    if (addedB)
                    {
                        bzz = bzz.Substring(0, bzz.Length - 1);
                        addedB = false;
                    }
                    AddToString(2 + stage);
                    if (bzz.EndsWith("z"))
                    {
                        bzz += "B";
                        addedB = true;
                    }
                    Debug.LogFormat("[Simon's Swarm #{0}] String Bzz for stage {1} is: {2}", moduleId, stage + 1, bzz);
                    GetPresses();
                    Debug.LogFormat("[Simon's Swarm #{0}] Bees to press for stage {1} going clockwise from north: {2}", moduleId, stage + 1, answer.Join(", "));
                    StartCoroutine(DelaySequence());
                }
            }
            else
            {
                Debug.LogFormat("[Simon's Swarm #{0}] Incorrect, strike!", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
                pressIndex = 0;
                StartCoroutine(DelaySequence());
            }
        }
    }

    void AddToString(int index)
    {
        bool hasHex = bomb.GetModuleNames().Any(x => x.ToUpper().Contains("HEX"));
        int beeIndex = chosenTypes[flashes[index]] * 6 + chosenColors[flashes[index]];
        if (index == 0)
            bzz = beeNames[beeIndex];
        else if (flashes[index] == flashes[index - 1] && hasHex)
            bzz += beeNames[beeIndex];
        else if (flashes[index] == flashes[index - 1] && !hasHex)
            bzz = beeNames[beeIndex] + bzz;
        else
        {
            int oneCounter = flashes[index - 1] - 1;
            if (oneCounter < 0) oneCounter += 5;
            int twoCounter = flashes[index - 1] - 2;
            if (twoCounter < 0) twoCounter += 5;
            if (flashes[index] == oneCounter || flashes[index] == twoCounter)
                bzz = beeNames[beeIndex] + bzz;
            else
                bzz += beeNames[beeIndex];
        }
    }

    void GetPresses()
    {
        int pointer = flashes[0];
        bool haveBuzz = bomb.GetModuleNames().Any(x => x.ToUpper().Contains("BUZZ"));
        for (int i = 0; i < bzz.Length; i++)
        {
            if (bzz[i] == 'B')
                answer.Add(pointer);
            else if (haveBuzz && bzz[i] == 'z')
            {
                pointer++;
                if (pointer > 4)
                    pointer = 0;
            }
            else
            {
                pointer--;
                if (pointer < 0)
                    pointer = 4;
            }
        }
    }

    void LightsOff()
    {
        for (int i = 0; i < 5; i++)
            lights[i].enabled = false;
    }

    IEnumerator DelaySequence()
    {
        yield return new WaitForSeconds(2f);
        StartCoroutine(FlashSequence());
    }

    IEnumerator FlashSequence()
    {
        while (true)
        {
            for (int i = 0; i < flashes.Count; i++)
            {
                if (firstPress)
                    audio.PlaySoundAtTransform("buzz" + UnityEngine.Random.Range(1, 6), buttons[flashes[i]].transform);
                lights[flashes[i]].enabled = true;
                yield return new WaitForSeconds(.6f);
                lights[flashes[i]].enabled = false;
                yield return new WaitForSeconds(.3f);
            }
            yield return new WaitForSeconds(1.5f);
        }
    }

    IEnumerator FlashBee(int index)
    {
        lights[index].enabled = true;
        yield return new WaitForSeconds(.6f);
        lights[index].enabled = false;
    }

    IEnumerator SolveAnim()
    {
        yield return new WaitForSeconds(.8f);
        for (int i = 0; i < 5; i++)
        {
            lights[i].enabled = true;
            audio.PlaySoundAtTransform("buzz" + UnityEngine.Random.Range(1, 6), buttons[i].transform);
            yield return new WaitForSeconds(.2f);
        }
        for (int j = 0; j < 2; j++)
        {
            LightsOff();
            yield return new WaitForSeconds(.2f);
            for (int i = 0; i < 5; i++)
            {
                lights[i].enabled = true;
                audio.PlaySoundAtTransform("buzz" + UnityEngine.Random.Range(1, 6), buttons[i].transform);
            }
            yield return new WaitForSeconds(.2f);
        }
        LightsOff();
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} press <p1> (p2) [Presses the bee in the specified position (optionally include multiple positions)] | !{0} colorblind [Toggles colorblind mode] | Valid positions are 0-4 going clockwise from north";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind|colourblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            cbEnabled = !cbEnabled;
            for (int i = 0; i < 5; i++)
            {
                if (cbEnabled)
                    cbTexts[i].text = colorNames[chosenColors[i]][0].ToString();
                else
                    cbTexts[i].text = "";
            }
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*press\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (parameters.Length == 1)
                yield return "sendtochaterror Please specify at least one position!";
            else
            {
                for (int i = 1; i < parameters.Length; i++)
                {
                    int temp = -1;
                    if (!int.TryParse(parameters[i], out temp))
                    {
                        yield return "sendtochaterror!f The specified position '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                    if (temp < 0 || temp > 4)
                    {
                        yield return "sendtochaterror The specified position '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                }
                yield return null;
                for (int i = 1; i < parameters.Length; i++)
                {
                    buttons[int.Parse(parameters[i])].OnInteract();
                    yield return new WaitForSeconds(.2f);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = stage; i < 3; i++)
        {
            int end = answer.Count;
            for (int j = pressIndex; j < end; j++)
            {
                buttons[answer[j]].OnInteract();
                yield return new WaitForSeconds(.2f);
            }
        }
    }
}