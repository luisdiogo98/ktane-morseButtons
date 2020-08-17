using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

public class morseButtonsScript : MonoBehaviour 
{
	readonly string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
	readonly string[] morseTable = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--..", "-----", ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----."};

	public KMBombInfo bomb;
	public KMAudio Audio;

	public KMSelectable[] buttons;
	public GameObject[] lights;
	public Material[] mats;
	public KMColorblindMode colorblindMode;
	public TextMesh[] colorBlindTexts;

	int[] colors;
	int[] letters;
	int[] values;

	readonly string[] positionLists = new string[] { "Top Left", "Top Middle", "Top Right", "Bottom Left", "Bottom Middle", "Bottom Right" };

	HashSet<int> presses = new HashSet<int>();
	HashSet<int> pressed = new HashSet<int>();

	static int moduleIdCounter = 1;
    int moduleId;

    private bool moduleSolved;
	private bool colorBlindDetected;

	void Awake()
	{
		moduleId = moduleIdCounter++;
		for (int x = 0; x < buttons.Length; x++)
		{
			int y = x;
			buttons[x].OnInteract += delegate () { HandlePress(y); return false; };
		}
		try {
			colorBlindDetected = colorblindMode.ColorblindModeActive;
		}
		catch {
			colorBlindDetected = false;
		}
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
		for (int x = 0; x < colorBlindTexts.Length; x++)
		{
			colorBlindTexts[x].text = colorBlindDetected && !moduleSolved ? GetColorName(colors[x])[0].ToString() : "";
		}
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
				Debug.LogFormat("[Morse Buttons #{0}] Correctly pressed {1} button. Module solved!", moduleId, positionLists[button]);
				moduleSolved = true;
				StopAllCoroutines();
				TurnOffLights();
				GetComponent<KMBombModule>().HandlePass();
			}
			else
			{
				Debug.LogFormat("[Morse Buttons #{0}] Correctly pressed {1} button. The buttons that still need to be pressed are [ {2}]", moduleId, positionLists[button], GetButtonPresses());
			}
		}
		else
		{
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Morse Buttons #{0}] Strike! Incorrectly pressed {1} button. The buttons that still need to be pressed are [ {2}]", moduleId, positionLists[button], GetButtonPresses());
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
		for (int x = 0; x < 6; x++)
			Debug.LogFormat("[Morse Buttons #{0}] {3} button is flashing: {1} {2}", moduleId, GetColorName(colors[x]), alphabet[letters[x]], positionLists[x]);
	}

	void CalcButtonValues()
	{
		values = new int[6];
		string sn = bomb.GetSerialNumber();

		for(int i = 0; i < values.Count(); i++)
		{
			values[i] = GetCharValue(sn[i]) + GetCharValue(alphabet[letters[i]]);

			while(values[i] > 30)
				values[i] -= 30;

			while(values[i] < 1)
				values[i] += 30;	
		}
		for (int x=0;x<6;x++)
			Debug.LogFormat("[Morse Buttons #{0}] {2} button rule required: {1}", moduleId, values[x],positionLists[x]);
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

	bool hasMoreofAVersusB(string input, char compareA, char compareB)
	{
		int countA = 0;
		int countB = 0;

		foreach (char letter in input)
		{
			if (letter == compareA)
				countA++;
			else if (letter == compareB)
				countB++;
		}
		return countA > countB;
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
				return hasMoreofAVersusB(morseTable[letters[button]], '-', '.');
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
				return hasMoreofAVersusB(morseTable[letters[button]], '.', '-');
			case 27:
				return button <= 2;
			case 28:
				return bomb.GetSerialNumber().Contains(alphabet[letters[button]]);
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

	string GetButtonPresses()
	{
		string res = "";

		for(int i = 0; i < 6; i++)
		{
			if(presses.Contains(i))
			{
				res += (res.Length != 0 ? ", " : "") + positionLists[i];
			}
		}

		return res + " ";
	}

	void StartFlashes()
	{
		for (int x=0;x<6;x++)
			StartCoroutine(FlashLight(x));
	}

	IEnumerator FlashLight(int n)
	{
		string character = morseTable[letters[n]];

		while(!moduleSolved)
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

	string GetColorName(int color)
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
	
	//Handle Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "To press a button: \"!{0} press tl top-left BR\" \"press\" is optional. Valid buttons are TL, TM, TR, BL, BM, and BR and numbered 1-6 in reading order. To enable colorblind mode: \"!{0} colorblind\" or \"!{0} colourblind\"";
#pragma warning restore 414
	IEnumerator TwitchHandleForcedSolve()
	{
		while (presses.Count > 0)
		{
			buttons[presses.ElementAt(0)].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		yield return true;
	}

    IEnumerator ProcessTwitchCommand(string command)
    {
		string intereptedCommand = command.ToLower();
		if (intereptedCommand.RegexMatch(@"^colou?rblind$"))
		{
			yield return null;
			colorBlindDetected = true;
			yield break;
		}
		intereptedCommand = command.ToLower().StartsWith("press ") ? command.Substring(6).ToLower() : command.ToLower();
        string[] parameters = intereptedCommand.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        var buttonsToPress = new List<KMSelectable>();
        foreach (string param in parameters)
        {
			switch (param.Replace("center","middle").Replace("centre", "middle"))
			{
				case "tl":
				case "top-left":
				case "topleft":
				case "1":
					buttonsToPress.Add(buttons[0]);
					break;
				case "tm":
				case "top-middle":
				case "topmiddle":
				case "2":
					buttonsToPress.Add(buttons[1]);
					break;
				case "tr":
				case "top-right":
				case "topright":
				case "3":
					buttonsToPress.Add(buttons[2]);
					break;
				case "bl":
				case "bottom-left":
				case "bottomleft":
				case "4":
					buttonsToPress.Add(buttons[3]);
					break;
				case "bm":
				case "bottom-middle":
				case "bottommiddle":
				case "5":
					buttonsToPress.Add(buttons[4]);
					break;
				case "br":
				case "bottom-right":
				case "bottomright":
				case "6":
					buttonsToPress.Add(buttons[5]);
					break;
				default:
					yield return "sendtochaterror I'm sorry but what button is \"" + param + "\" supposed to be?";
					yield break;
			}
        }

        yield return null;
        yield return buttonsToPress;
    }
}