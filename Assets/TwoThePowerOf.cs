using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class TwoThePowerOf : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMColorblindMode ColorblindMode;
   public KMRuleSeedable RuleSeedable;

   public GameObject ModuleBackground;
   public GameObject StatusLight;
   public Renderer Surface;

   public Material[] unlitColors;
   public Material[] litColors;
   public Renderer[] lights;
   public KMSelectable[] buttons;
   public Renderer[] buttonRenderers;
   public Material[] buttonColors;
   public TextMesh[] colorblindTexts;
   public TextMesh centerText;
   public AudioClip[] sounds;

   private int startingCorner;
   private int startIndex;
   private int[] lightColors = new int[16];
   private int[] powerLevels = new int[16];
   private List<int> buttonsPressed = new List<int> {};
   private List<int> correctButtons = new List<int> {};
   private int pressCount;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved;
   private bool ColorblindModeEnabled;

   void Awake() {
      ModuleId = ModuleIdCounter++;
      foreach (KMSelectable button in buttons)
         button.OnInteract += delegate () { buttonPress(button); return false; };
   }

   void buttonPress(KMSelectable button) {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
      button.AddInteractionPunch(.5f);
      if (ModuleSolved || pressCount == 5)
         return;
      for (int i = 0; i < 16; i++) {
         if (button == buttons[i]) {
            if (buttonsPressed.Contains(i))
               return;
            buttonsPressed.Add(i);
            pressCount++;
            buttonRenderers[i].material = buttonColors[1];
            lights[i].material = unlitColors[6];
            colorblindTexts[i].gameObject.SetActive(false);
         }
      }
      if (pressCount == 5)
         StartCoroutine(Submit());
   }

   void Start() {
      if (ColorblindMode.ColorblindModeActive)
         ColorblindModeEnabled = true;
      for (int i = 0; i < 16; i++) {
         colorblindTexts[i].gameObject.SetActive(ColorblindModeEnabled);
         powerLevels[i] = -1;
      }
      Debug.LogFormat("[Two The Power Of #{0}] Unless stated otherwise, this logging will refer to the buttons starting from the top going clockwise, with the topmost light being 1.", ModuleId);
      startingCorner = Rnd.Range(0, 4);
      ModuleBackground.transform.localRotation = Quaternion.Euler(new Vector3(0, 90 * startingCorner, 0));
      StatusLight.transform.localRotation = Quaternion.Euler(new Vector3(0, -90 * startingCorner, 0));
      switch (startingCorner) {
         case 0: startIndex = 2; break;
         case 1: startIndex = 6; break;
         case 2: startIndex = 10; break;
         case 3: startIndex = 14; break;
      }
      Debug.LogFormat("[Two The Power Of #{0}] The status light is in the {1} corner.", ModuleId, new[] {"TR", "BR", "BL", "TL"}[startingCorner]);
      for (int i = 0; i < 16; i++) {
         int random = Rnd.Range(0, 6);
         int pos = (i + startIndex) % 16;
         lightColors[pos] = random;
         lights[pos].material = unlitColors[random];
         colorblindTexts[pos].text = "RGBCMY"[random].ToString();
      }
      Debug.LogFormat("[Two The Power Of #{0}] The light colors are {1}.", ModuleId, colorblindTexts.Select(t => t.text).ToArray().Join(""));
      // RULE SEED
      var rnd = RuleSeedable.GetRNG();
      if (rnd.Seed != 1)
         Debug.LogFormat("[Two The Power Of #{0}] Additionally, this module is being run under rule seed {1}.", ModuleId, rnd.Seed);
      var edgework = new[] {Bomb.GetBatteryCount(), Bomb.GetBatteryHolderCount(), Bomb.GetIndicators().Count(), Bomb.GetOnIndicators().Count(), Bomb.GetOffIndicators().Count(), Bomb.GetPortCount(), Bomb.CountUniquePorts(), Bomb.GetPortPlateCount(), Bomb.GetSerialNumberNumbers().First(), Bomb.GetSerialNumberNumbers().Last()};
		var letters = new[] {0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3};
		var ordinals = Enumerable.Range(0, 16).ToArray();

		rnd.ShuffleFisherYates(letters);

      var readingDirection = 1;
      var diagramDirection = 1;
      var duplicateOperator = 1;
      var usedEdgework = new int [6];
      var usedOrder = new int[5];
		
		var tableValues = rnd.ShuffleFisherYates(Enumerable.Range(1, 32).ToArray());
		
		if (rnd.Seed == 1) {
			usedEdgework[0] = edgework[0];
			usedEdgework[1] = edgework[2];
			usedEdgework[2] = edgework[5];
			usedEdgework[3] = edgework[7];
			usedEdgework[4] = edgework[8];
			usedEdgework[5] = edgework[9];
         usedOrder[0] = 0;
			usedOrder[1] = 1;
			usedOrder[2] = 3;
			usedOrder[3] = 7;
			usedOrder[4] = 15;
		} else {
         rnd.ShuffleFisherYates(edgework);
		   rnd.ShuffleFisherYates(ordinals);
         readingDirection = new[] {1, -1}[rnd.Next(0, 2)];
         diagramDirection = new[] {1, -1}[rnd.Next(0, 2)];
         duplicateOperator = new[] {1, -1}[rnd.Next(0, 2)];
         for (int i = 0; i < 6; i++) {
            usedEdgework[i] = edgework[i];
         }
         for (int i = 0; i < 5; i++) {
            usedOrder[i] = ordinals[i];
         }
      }
      
      int currentLight = startIndex;
      int diagramPosition = startIndex;
      powerLevels[currentLight] = (int)Math.Pow(usedEdgework[lightColors[currentLight]], 2) % 32;
      for (int i = 0; i < 15; i++) {
         int previousPower = powerLevels[currentLight];
         currentLight += readingDirection;
         currentLight = currentLight < 0 ? (currentLight + 16) : (currentLight % 16);
         diagramPosition += diagramDirection * previousPower;
         diagramPosition = (diagramPosition % 16 + 16) % 16;
         int currentPower = (tableValues[lightColors[currentLight] * 4 + letters[diagramPosition]] + previousPower) % 30;
         currentPower = (int)Math.Pow(2, currentPower) % 31;
         while (powerLevels.Contains(currentPower)) {
            currentPower += duplicateOperator;
            currentPower = ((currentPower % 32) + 32) % 32;
         }
         powerLevels[currentLight] = currentPower;
      }
      Debug.LogFormat("[Two The Power Of #{0}] The power levels for each light are: {1}", ModuleId, powerLevels.Join(", "));
      int[] powerIndices = Enumerable.Range(0, 16).ToArray();
      Array.Sort(powerLevels, powerIndices);
      for (int i = 0; i < 5; i++)
         correctButtons.Add(powerIndices[usedOrder[i]]);
      Debug.LogFormat("[Two The Power Of #{0}] The buttons that need to be pressed are {1}.", ModuleId, correctButtons.Select(x => x + 1).Join(" "));
   }

   void Solve() {
      ModuleSolved = true;
      Debug.LogFormat("[Two The Power Of #{0}] Correct submission; module solved!", ModuleId);
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike() {
      Debug.LogFormat("[Two The Power Of #{0}] That was not correct... Strike.", ModuleId);
      GetComponent<KMBombModule>().HandleStrike();
   }

   IEnumerator Submit() {
      Debug.LogFormat("[Two The Power Of #{0}] Submitted buttons {1}.", ModuleId, buttonsPressed.Select(x => x + 1).Join(" "));
      centerText.text = "...";
      for (int i = 0; i < 16; i++) {
         buttonRenderers[i].material = buttonColors[1];
         colorblindTexts[i].gameObject.SetActive(false);
         lights[i].material = unlitColors[6];
      }
      Audio.PlaySoundAtTransform("Submission", transform);
      yield return new WaitForSeconds(1f);
      if (buttonsPressed.SequenceEqual(correctButtons)) {
         StartCoroutine(Brighten());
         Audio.PlaySoundAtTransform("Win2000BetaStartup", transform);
         yield return new WaitForSeconds(.17f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = litColors[lightColors[i]];
            yield return new WaitForSeconds(.11f);
         }
         for (int i = 0; i < 16; i++) {
            lights[i].material = litColors[1];
         }
      } else {
         Audio.PlaySoundAtTransform("Win2000Shutdown", transform);
         yield return new WaitForSeconds(.3f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[0];
         }
         yield return new WaitForSeconds(.5f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[6];
         }
         yield return new WaitForSeconds(.33f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[0];
         }
         yield return new WaitForSeconds(.33f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[6];
         }
         yield return new WaitForSeconds(.33f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[0];
         }
         centerText.text = "NO";
         Strike();
         yield return new WaitForSeconds(2f);
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[lightColors[i]];
            buttonRenderers[i].material = buttonColors[0];
            colorblindTexts[i].gameObject.SetActive(ColorblindModeEnabled);
         }
         centerText.text = "POWER";
         buttonsPressed.Clear();
         pressCount = 0;
      }
   }

   IEnumerator Brighten() {
      var duration = 2.11f;
      var elapsed = 0f;
      float value = 0f;
      while (elapsed < duration) {
         value = Easing.InExpo(elapsed, 0f, 1f, duration);
         Surface.material.color = new Color(value, value, value, 1f);
         centerText.color = new Color((value / 0.67f) + 0.33f, (value / 0.67f) + 0.33f, (value / 0.67f) + 0.33f, 1f);
         yield return null;
         elapsed += Time.deltaTime;
      }
      centerText.text = "SOLVED";
      Solve();
      duration = 1f;
      elapsed = 0f;
      while (elapsed < duration) {
         value = Easing.OutQuad(elapsed, 1f, 0f, duration);
         Surface.material.color = new Color(value, value, value, 1f);
         centerText.color = new Color((value / 0.67f) + 0.33f, (value / 0.67f) + 0.33f, (value / 0.67f) + 0.33f, 1f);
         yield return null;
         elapsed += Time.deltaTime;
      }
      Surface.material.color = new Color(0f, 0f, 0f, 1f);
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"!{0} (number) [presses a button] | !{0} colorblind [Enables colorblind mode] | Buttons are numbered from 1 to 16, starting at the top going clockwise. Multiple numbers can be entered to press multiple buttons.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToLowerInvariant();
      yield return null;
      if (pressCount == 5)
         yield break;
      if (Command == "colorblind") {
         if (!ColorblindModeEnabled) {
            ColorblindModeEnabled = true;
            for (int i = 0; i < 16; i++)
               colorblindTexts[i].gameObject.SetActive(ColorblindModeEnabled);
         }
         yield break;
      }
      if (!Regex.IsMatch(Command, @"^\s*\d+(?:\s+\d+)*\s*$")) {
         yield return "sendtochaterror Invalid command.";
         yield break;
      }
      string[] Commands = Command.Split(' ');
      for (int i = 0; i < Commands.Length; i++) {
         if (int.Parse(Commands[i]) < 1 || int.Parse(Commands[i]) > 16) {
            yield return "sendtochaterror Your command contains one or more numbers that are too high or too low.";
            yield break;
         }
      }
      for (int i = 0; i < Commands.Length; i++) {
         buttons[int.Parse(Commands[i]) - 1].OnInteract();
         yield return new WaitForSeconds(.1f);
         if (pressCount == 5)
            break;
      }
      if (pressCount == 5) {
         if (buttonsPressed.SequenceEqual(correctButtons))
            yield return "solve";
         else
            yield return "strike";
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      if (pressCount > 0) {
         buttonsPressed.Clear();
         pressCount = 0;
         for (int i = 0; i < 16; i++) {
            lights[i].material = unlitColors[lightColors[i]];
            buttonRenderers[i].material = buttonColors[0];
            colorblindTexts[i].gameObject.SetActive(ColorblindModeEnabled);
         }
      }
      for (int i = 0; i < 5; i++) {
         buttons[correctButtons[i]].OnInteract();
         yield return new WaitForSeconds(.1f);
      }
   }
}
