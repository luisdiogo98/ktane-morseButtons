using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class morseButtonsScript : MonoBehaviour 
{
	readonly String alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
	readonly String[] morseTable = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--..", "-----", ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----."};

	public KMBombInfo bomb;
	public KMAudio Audio;

	public KMSelectable[] buttons;
	public GameObject[] lights;
	public Material[] mats;

	int[] colors;
	int[] letters;
	int[] values;
	HashSet<int> presses = new HashSet<int>();
	HashSet<int> pressed = new HashSet<int>();

	static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

	void Awake()
	{
		moduleId = moduleIdCounter++;
		buttons[0].OnInteract += delegate () { HandlePress(0); return false; };
		buttons[1].OnInteract += delegate () { HandlePress(1); return false; };
		buttons[2].OnInteract += delegate () { HandlePress(2); return false; };
		buttons[3].OnInteract += delegate () { HandlePress(3); return false; };
		buttons[4].OnInteract += delegate () { HandlePress(4); return false; };
		buttons[5].OnInteract += delegate () { HandlePress(5); return false; };
	}

	void Start () 
	{
		RandomizeButtons();
		CalcButtonValues();
		CalcButtonPresses();
		StartFlashes();
	}
	
	void Update () 
	{
		
	}

	void HandlePress(int button)
	{
		GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		buttons[button].AddInteractionPunch(.5f);
		
		if(moduleSolved)
			return;

		if(pressed.Contains(button))
			return;
			
		if(presses.Contains(button))
		{
			presses.Remove(button);
			pressed.Add(button);
			if(presses.Count() == 0)
			{
				Debug.LogFormat("[Morse Buttons #{0}] Correctly pressed button {1}. Module solved!", moduleId, button + 1);
				moduleSolved = true;
				StopAllCoroutines();
				TurnOffLights();
				GetComponent<KMBombModule>().HandlePass();
			}
			else
			{
				Debug.LogFormat("[Morse Buttons #{0}] Correctly pressed button {1}. The buttons that still need to be pressed are [ {2}]", moduleId, button + 1, GetButtonPresses());
			}
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Morse Buttons #{0}] Strike! Incorrectly pressed button {1}. The buttons that still need to be pressed are [ {2}]", moduleId, button + 1, GetButtonPresses());
		}
	}

	void TurnOffLights()
	{
		foreach(GameObject light in lights)
		{
			light.GetComponentInChildren<Renderer>().material = mats[6];
		}
	}

	void RandomizeButtons()
	{
		colors = new int[6];
		letters = new int[6];

		for(int i = 0; i < colors.Count(); i++)
		{
			colors[i] = rnd.Range(0, 6);
		}

		for(int i = 0; i < letters.Count(); i++)
		{
			letters[i] = rnd.Range(0, alphabet.Length);
		}

		Debug.LogFormat("[Morse Buttons #{0}] First button: {1} {2}", moduleId, GetColorName(colors[0]), alphabet[letters[0]]);
		Debug.LogFormat("[Morse Buttons #{0}] Second button: {1} {2}", moduleId, GetColorName(colors[1]), alphabet[letters[1]]);
		Debug.LogFormat("[Morse Buttons #{0}] Third button: {1} {2}", moduleId, GetColorName(colors[2]), alphabet[letters[2]]);
		Debug.LogFormat("[Morse Buttons #{0}] Fourth button: {1} {2}", moduleId, GetColorName(colors[3]), alphabet[letters[3]]);
		Debug.LogFormat("[Morse Buttons #{0}] Fifth button: {1} {2}", moduleId, GetColorName(colors[4]), alphabet[letters[4]]);
		Debug.LogFormat("[Morse Buttons #{0}] Sixth button: {1} {2}", moduleId, GetColorName(colors[5]), alphabet[letters[5]]);
	}

	void CalcButtonValues()
	{
		values = new int[6];
		String sn = bomb.GetSerialNumber();

		for(int i = 0; i < values.Count(); i++)
		{
			values[i] = GetCharValue(sn[i]) + GetCharValue(alphabet[letters[i]]);

			while(values[i] > 30)
				values[i] -= 30;

			while(values[i] < 1)
				values[i] += 30;	
		}

		Debug.LogFormat("[Morse Buttons #{0}] First button value: {1}", moduleId, values[0]);
		Debug.LogFormat("[Morse Buttons #{0}] Second button value: {1}", moduleId, values[1]);
		Debug.LogFormat("[Morse Buttons #{0}] Third button value: {1}", moduleId, values[2]);
		Debug.LogFormat("[Morse Buttons #{0}] Fourth button value: {1}", moduleId, values[3]);
		Debug.LogFormat("[Morse Buttons #{0}] Fifth button value: {1}", moduleId, values[4]);
		Debug.LogFormat("[Morse Buttons #{0}] Sixth button value: {1}", moduleId, values[5]);
	}

	int GetCharValue(char c)
	{
		if(Char.IsDigit(c))
			return Int32.Parse(c + "");
		else	
			return alphabet.IndexOf(Char.ToUpper(c)) + 1;
	}

	void CalcButtonPresses()
	{
		for(int i = 0; i < values.Count(); i++)
		{
			if(CheckRule(values[i], i))
				presses.Add(i);
		}

		if(presses.Count() == 0)
		{
			CalcSpecialRule();
			Debug.LogFormat("[Morse Buttons #{0}] Special rule applies. The buttons to be pressed are [ {1}]", moduleId, GetButtonPresses());
		}
		else
		{
			Debug.LogFormat("[Morse Buttons #{0}] The buttons to be pressed are [ {1}]", moduleId, GetButtonPresses());
		}
	}

	bool CheckRule(int rule, int button)
	{
		switch(rule)
		{
			case 1:
				return letters[button] == 12 || letters[button] == 14 || letters[button] == 17 || letters[button] == 18 || letters[button] == 4;
			case 2:
				return CheckRepeatedColors()[colors[button]] > 1;
			case 3:
				return letters[button] >= 26;
			case 4:
				return bomb.GetPortCount() >= 4;
			case 5:
				return colors[button] >= 3;
			case 6:
				return bomb.GetPortPlates().Any((x) => x.Length == 0);
			case 7:
			{
				int[] c = CheckRepeatedColors();
				for(int i = 0; i < c.Count(); i++)
				{
					if(c[i] >= 3)
						return true;
				}
				return false;
			}
			case 8:
				return ColorNameContains(colors[button], alphabet[letters[button]]);
			case 9:
				return bomb.GetSerialNumber().Distinct().Count() != bomb.GetSerialNumber().Length;
			case 10:
				return bomb.IsPortPresent(Port.Serial);
			case 11:
				return letters[button] == 5 || letters[button] == 11 || letters[button] == 0 || letters[button] == 18 || letters[button] == 7;
			case 12:
				return bomb.IsPortPresent(Port.PS2);
			case 13:
				return letters[button] == 6 || letters[button] == 9 || letters[button] == 10 || letters[button] == 12 || letters[button] == 14 || letters[button] == 16 || letters[button] == 23 || letters[button] == 25 || letters[button] == 26 || letters[button] == 27 || letters[button] == 28 || letters[button] == 34 || letters[button] == 35;
			case 14:
				return button >= 3;
			case 15:
				return bomb.GetPortCount() == 0;
			case 16:
				return colors[button] <= 2;
			case 17:
				return button == 0 || button == 2 || button == 4;
			case 18:
				return bomb.IsPortPresent(Port.DVI);
			case 19:
				return letters[button] == 0 || letters[button] == 4 || letters[button] == 8 || letters[button] == 14 || letters[button] == 20;
			case 20:
				return letters[button] == 1 || letters[button] == 20 || letters[button] == 19 || letters[button] == 14 || letters[button] == 13;
			case 21:
				return button == 1 || button == 3 || button == 5;
			case 22:
				return bomb.IsPortPresent(Port.StereoRCA);
			case 23:
				return letters.Distinct().Count() != letters.Length;
			case 24:
				return letters[button] == 15 || letters[button] == 17 || letters[button] == 4 || letters[button] == 18;
			case 25:
				return bomb.IsDuplicatePortPresent();
			case 26:
				return letters[button] == 1 || letters[button] == 3 || letters[button] == 5 || letters[button] == 7 || letters[button] == 8 || letters[button] == 11 || letters[button] == 17 || letters[button] == 18 || letters[button] == 20 || letters[button] == 21 || letters[button] == 28 || letters[button] == 29 || letters[button] == 30 || letters[button] == 31 || letters[button] == 32;
			case 27:
				return button <= 2;
			case 28:
				return Array.Exists(bomb.GetSerialNumber().ToArray(), x => x == alphabet[letters[button]]);
			case 29:
				return bomb.IsPortPresent(Port.RJ45);
			case 30:
				return CheckRepeatedColors()[colors[button]] == 1;
		}

		return false;
	}

	int[] CheckRepeatedColors()
	{
		int[] res = new int[6];

		for(int i = 0; i < 6; i++)
		{
			res[i] = 0;
		}

		for(int i = 0; i < colors.Count(); i++)
		{
			res[colors[i]]++;
		}

		return res;
	}

	bool ColorNameContains(int color, char c)
	{
		switch(color)
		{
			case 0:
				return "RED".Contains(c);
			case 1:
				return "BLUE".Contains(c);
			case 2:
				return "GREEN".Contains(c);
			case 3:
				return "YELLOW".Contains(c);
			case 4:
				return "ORANGE".Contains(c);
			case 5:
				return "PURPLE".Contains(c);
		}

		return false;
	}

	void CalcSpecialRule()
	{
		int value = letters[0];
		presses.Add(0);

		for(int i = 1; i < 6; i++)
		{
			if(letters[i] == value)
			{
				presses.Add(i);
			}
			else if (letters[i] < value)
			{
				presses.Clear();
				presses.Add(i);
				value = letters[i];
			}
		}
	}

	String GetButtonPresses()
	{
		String res = "";

		for(int i = 0; i < 6; i++)
		{
			if(presses.Contains(i))
			{
				res += ((i + 1) + " ");
			}
		}

		return res;
	}

	void StartFlashes()
	{
		StartCoroutine(FlashLight(0));
		StartCoroutine(FlashLight(1));
		StartCoroutine(FlashLight(2));
		StartCoroutine(FlashLight(3));
		StartCoroutine(FlashLight(4));
		StartCoroutine(FlashLight(5));
	}

	IEnumerator FlashLight(int n)
	{
		String character = morseTable[letters[n]];

		while(true)
		{
			for(int i = 0; i < character.Length; i++)
			{
				lights[n].GetComponentInChildren<Renderer>().material = mats[colors[n]];
				if(character[i] == '-')
					yield return new WaitForSeconds(0.6f);
				else
					yield return new WaitForSeconds(0.2f);
				
				lights[n].GetComponentInChildren<Renderer>().material = mats[6];
				yield return new WaitForSeconds(0.2f);
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	String GetColorName(int color)
	{
		switch(color)
		{
			case 0:
				return "Red";
			case 1:
				return "Blue";
			case 2:
				return "Green";
			case 3:
				return "Yellow";
			case 4:
				return "Orange";
			case 5:
				return "Purple";
		}

		return "";
	}
	
	//twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <button> [Presses the specified button] | !{0} <button>,<button> [Presses the specified buttons] | Valid buttons are: TL, TM, TR, BL, BM, and BR";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        string[] parameters = command.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var buttonsToPress = new List<KMSelectable>();
        foreach (string param in parameters)
        {
            if (param.EqualsIgnoreCase("press"))
            {
                // allow that but don't do anything
            }
            if (param.EqualsIgnoreCase("TL"))
            {
                buttonsToPress.Add(buttons[0]);
            }
            else if (param.EqualsIgnoreCase("TM"))
            {
                buttonsToPress.Add(buttons[1]);
            }
            else if (param.EqualsIgnoreCase("TR"))
            {
                buttonsToPress.Add(buttons[2]);
            }
            else if (param.EqualsIgnoreCase("BL"))
            {
                buttonsToPress.Add(buttons[3]);
            }
            else if (param.EqualsIgnoreCase("BM"))
            {
                buttonsToPress.Add(buttons[4]);
            }
            else if (param.EqualsIgnoreCase("BR"))
            {
                buttonsToPress.Add(buttons[5]);
            }
            else
            {
                yield break;
            }
        }

        yield return null;
        yield return buttonsToPress;
    }
}