using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Random = UnityEngine.Random;

public class SusadminModule : MonoBehaviour {
	private const int LINES_COUNT = 14;
	private const int MAX_COMMAND_LENGTH = 27;
	private const float WATCH_INTERVAL = .1f;
	private const string SYSADMIN_ID = "sysadmin";

	private static int moduleIdCounter = 1;

	public TextMesh Console;
	public KMBombInfo Bomb;
	public KMBombModule Module;
	public KMSelectable Selectable;

	private bool osIsBoom;
	private bool safetyLevelIsPublic = false;
	private bool solved = false;
	private bool readyToWrite = false;
	private bool selected = false;
	private bool shouldUpdateText = false;
	private int externalStrikesCount = 0;
	private int moduleId;
	private int linePointer = 0;
	private int osVersion;
	private int safetyLevel;
	private int vulnerability;
	private string command = "";
	private Coroutine watchingCoroutine;
	private List<int> securityProtocols = new List<int>();
	private List<Vector2Int> installedViruses = new List<Vector2Int>();
	private HashSet<SusadminReflector> reflectors;
	private string[] lines = new string[LINES_COUNT];

	private void Start() {
		moduleId = moduleIdCounter++;
		Console.text = "";
		Module.OnActivate += OnActivate;
	}

	private void Update() {
		if (shouldUpdateText) UpdateConsole();
	}

	private IEnumerator Watch() {
		while (!solved) {
			HashSet<SusadminReflector> reflectorsWithStrike = new HashSet<SusadminReflector>(reflectors.Where(r => r.ShouldStrike()));
			int expectedExternalStrikesCount = reflectorsWithStrike.Count();
			foreach (SusadminReflector r in reflectorsWithStrike) reflectors.Remove(r);
			for (int i = externalStrikesCount; i < expectedExternalStrikesCount; i++) {
				Debug.LogFormat("[SUSadmin #{0}] Strike from external action", moduleId);
				Module.HandleStrike();
			}
			externalStrikesCount = expectedExternalStrikesCount;
			if (reflectors.Count == 0) StopCoroutine(watchingCoroutine);
			yield return new WaitForSeconds(WATCH_INTERVAL);
		}
	}

	private void OnActivate() {
		HashSet<int> securityProtocols;
		HashSet<Vector2Int> answerExample;
		SusadminData.Generate(out securityProtocols, out vulnerability, out safetyLevel, out answerExample);
		this.securityProtocols = securityProtocols.ToArray().Shuffle().ToList();
		string installedSecurityProtocolNames = this.securityProtocols.Select(id => SusadminData.GetSecurityProtocolName(id)).Join(", ");
		Debug.LogFormat("[SUSadmin #{0}] Installed security protocols: {1}", moduleId, installedSecurityProtocolNames);
		Debug.LogFormat("[SUSadmin #{0}] Vulnerability: {1}", moduleId, vulnerability);
		Debug.LogFormat("[SUSadmin #{0}] Safety level: {1}", moduleId, safetyLevel);
		Debug.LogFormat("[SUSadmin #{0}] Answer example: {1}", moduleId, answerExample.Select(id => SusadminData.GetVirusName(id)).Join(", "));
		osVersion = vulnerability - (-Bomb.GetBatteryCount() * 5 + Bomb.GetIndicators().Count() * 7 - Bomb.GetPorts().Count() * 3 - Mathf.FloorToInt(Bomb.GetTime() / 60f));
		if (osVersion < 0 || (osVersion == 0 && Random.Range(0, 2) == 0)) osIsBoom = true;
		Selectable.OnFocus += () => selected = true;
		Selectable.OnDefocus += () => selected = false;
		int modulesCount = transform.parent.childCount;
		IEnumerable<KMBombModule> modules = Enumerable.Range(0, modulesCount).Select(i => transform.parent.GetChild(i).GetComponent<KMBombModule>()).Where(m => m != null);
		reflectors = new HashSet<SusadminReflector>(modules.Select(m => SusadminReflector.CreateReflector(m)).Where(m => m != null));
		watchingCoroutine = StartCoroutine(Watch());
		readyToWrite = true;
		UpdateConsole();
	}

	private void OnGUI() {
		if (!selected) return;
		Event e = Event.current;
		if (e.type != EventType.KeyDown) return;
		if (ProcessKey(e.keyCode)) shouldUpdateText = true;
	}

	private void UpdateConsole() {
		if (readyToWrite) lines[linePointer] = string.Format("<color=red> > {0}{1}</color>", command, command.Length == MAX_COMMAND_LENGTH ? "" : "_");
		Console.text = Enumerable.Range(linePointer + 1, LINES_COUNT).Select(i => lines[i % LINES_COUNT]).Join("\n");
		shouldUpdateText = false;
	}

	private bool ProcessKey(KeyCode key) {
		Func<string, bool> UpdateCommand = (string command) => {
			if (this.command == command) return false;
			this.command = command;
			return true;
		};
		if (!readyToWrite) return false;
		if (key == KeyCode.Return || key == KeyCode.KeypadEnter) {
			readyToWrite = false;
			if (lines[linePointer].EndsWith("_</color>")) lines[linePointer] = lines[linePointer].Remove(lines[linePointer].Length - 9, 1);
			linePointer = (linePointer + 1) % LINES_COUNT;
			StartCoroutine(ProcessCommand());
			return true;
		}
		if (key == KeyCode.Backspace && command.Length > 0) return UpdateCommand(command.Remove(command.Length - 1));
		if (command.Length >= MAX_COMMAND_LENGTH) return false;
		if (key == KeyCode.Space) return UpdateCommand(command + " ");
		if (key >= KeyCode.Keypad0 && key <= KeyCode.Keypad9) key = KeyCode.Alpha0 + (key - KeyCode.Keypad0);
		if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9) return UpdateCommand(command + (key - KeyCode.Alpha0));
		if (key >= KeyCode.A && key <= KeyCode.Z) {
			string add = key.ToString();
			if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) add = add.ToLower();
			return UpdateCommand(command + add);
		}
		return false;
	}

	private IEnumerator ProcessCommand() {
		Action EndCommandProcessing = () => {
			this.command = "";
			shouldUpdateText = true;
			readyToWrite = true;
		};
		string command = this.command.ToLower().Trim();
		if (command == "") {
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, "^status( |$)")) {
			if (command.Split(' ').Where(s => s != "").Count() > 1) WriteLine(PrintError("ERROR") + ": invalid arguments count");
			else {
				WriteLine(PrintOSVersion());
				WriteLine("Installed security protocols:");
				foreach (int sp in securityProtocols) WriteLine("    " + PrintSecurityProtocolName(sp));
				WriteLine("Safety Level: " + (safetyLevelIsPublic ? PrintNumber(safetyLevel.ToString()) : PrintError("UNKNOWN")));
				if (installedViruses.Count > 0) {
					WriteLine("Installed viruses:");
					int i = 0;
					List<string> line = new List<string>();
					foreach (Vector2Int virusId in installedViruses) {
						if (i++ % 4 == 0 && i != 1) {
							WriteLine(PrintError("  " + line.Join("   ")));
							line.Clear();
						}
						line.Add(SusadminData.GetVirusName(virusId));
					}
					WriteLine(PrintError("  " + line.Join("   ")));
				}
			}
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, @"^(install|i|add)( |$)")) {
			string[] args = command.Split(' ').Where(s => s != "").Skip(1).ToArray();
			if (args.Length == 0) WriteLine(PrintError("ERROR") + ": invalid arguments count");
			else {
				yield return Loader("Installing", 4 * args.Length);
				InstallViruses(args);
				WriteLine("Installed");
			}
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, @"^(delete|del|remove|rm)( | $)")) {
			string[] args = command.Split(' ').Where(s => s != "").Skip(1).ToArray();
			if (args.Length == 0) WriteLine(PrintError("ERROR") + ": invalid arguments count");
			else {
				yield return Loader("Deletion", 4 * args.Length);
				DeleteViruses(args);
				WriteLine("Deleted");
			}
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, @"^clear( |$)")) {
			if (command.Split(' ').Where(s => s != "").Count() > 1) WriteLine(PrintError("ERROR") + ": invalid arguments count");
			else {
				yield return Loader("Deletion all viruses", 4 * installedViruses.Count);
				installedViruses = new List<Vector2Int>();
				Debug.LogFormat("[SUSadmin #{0}] All viruses deleted", moduleId);
				WriteLine("All viruses deleted");
			}
			EndCommandProcessing();
			yield break;
		}
		if (Regex.IsMatch(command, @"^activate( |$)")) {
			if (command.Split(' ').Where(s => s != "").Count() > 1) {
				WriteLine(PrintError("ERROR") + ": invalid arguments count");
				EndCommandProcessing();
				yield break;
			}
			yield return Loader("Activation", 4 * installedViruses.Count);
			HashSet<int> compatibilityIndices = new HashSet<int>(installedViruses.Select(id => SusadminData.GetVirusCompatibilityIndex(id)));
			HashSet<int> sp = new HashSet<int>(securityProtocols);
			int minCompatibilityIndex = compatibilityIndices.Min();
			int maxCompatibilityIndex = compatibilityIndices.Max();
			int totalPower = installedViruses.Select(id => SusadminData.GetVirusPower(id)).Sum();
			Debug.LogFormat("[SUSadmin #{0}] Submitted total power: {1}", moduleId, totalPower);
			if (maxCompatibilityIndex - minCompatibilityIndex > vulnerability) {
				Debug.LogFormat("[SUSadmin #{0}] Viruses conflict. Min: {1}. Max: {2}", moduleId, minCompatibilityIndex, maxCompatibilityIndex);
				WriteLine(PrintError("ERROR" + ": Viruses conflict"));
				yield return Loader(PrintError("STRIKE"));
				WriteLine("All viruses deleted");
				Module.HandleStrike();
			} else if (installedViruses.Any(id => !SusadminData.VirusIsInvisible(id, sp))) {
				string detectedVirusName = SusadminData.GetVirusName(installedViruses.First(id => !SusadminData.VirusIsInvisible(id, sp)));
				Debug.LogFormat("[SUSadmin #{0}] Virus detected: {1}", moduleId, detectedVirusName);
				WriteLine(string.Format("{0}: Virus {1} detected", PrintError("ERROR"), PrintError(detectedVirusName)));
				yield return Loader(PrintError("STRIKE"));
				WriteLine("All viruses deleted");
				Module.HandleStrike();
			} else if (totalPower < safetyLevel) {
				WriteLine(string.Format("{0}: OS detected viruses", PrintError("ERROR")));
				WriteLine("OS safety level: " + PrintNumber(safetyLevel.ToString()));
				safetyLevelIsPublic = true;
				yield return Loader(PrintError("STRIKE"));
				WriteLine("All viruses deleted");
				Module.HandleStrike();
			} else {
				Debug.LogFormat("[SUSadmin #{0}] Module solved", moduleId);
				WriteLine("Network damaged!");
				yield return Loader("Solving module");
				WriteLine("Module solved");
				linePointer = linePointer == 0 ? LINES_COUNT : linePointer - 1;
				solved = true;
				Module.HandlePass();
				yield return SelfDestruct();
				yield break;
			}
			EndCommandProcessing();
			yield break;
		}
		WriteLine(PrintError("ERROR") + ": Unknown command");
		EndCommandProcessing();
	}

	private IEnumerator SelfDestruct() {
		// if (!Bomb.GetSolvableModuleIDs().Contains("SouvenirModule")) yield break;
		linePointer = (linePointer + 1) % LINES_COUNT;
		int secondsToTurnOffDisplay = 10;
		for (int i = 0; i < secondsToTurnOffDisplay; i++) {
			lines[linePointer] = string.Format("Turn off display in {0}s", secondsToTurnOffDisplay - i);
			shouldUpdateText = true;
			yield return new WaitForSeconds(1f);
		}
		lines = new string[LINES_COUNT];
		shouldUpdateText = true;
	}

	private IEnumerator Loader(string str, int steps = 8, float interval = .2f) {
		for (int i = 0; i < steps; i++) {
			lines[linePointer] = str + Enumerable.Range(0, i % 4).Select(_ => ".").Join("");
			shouldUpdateText = true;
			yield return new WaitForSeconds(interval);
		}
	}

	private void InstallViruses(string[] viruses) {
		if (viruses.Any(s => !SusadminData.VirusNameExists(s))) WriteLine(PrintError("ERROR") + ": virus not found");
		else if (viruses.Any(s => installedViruses.Contains(SusadminData.GetVirusId(s)))) WriteLine(PrintError("ERROR") + ": virus already installed");
		else if (new HashSet<string>(viruses.Select(v => v.ToUpper())).Count != viruses.Length) WriteLine(PrintError("ERROR") + ": duplicates");
		else {
			installedViruses.AddRange(viruses.Select(s => SusadminData.GetVirusId(s)));
			Debug.LogFormat("[SUSadmin #{0}] Viruses installed: {1}", moduleId, viruses.Select(s => s.ToUpper()).Join(", "));
		}
	}

	private void DeleteViruses(string[] viruses) {
		if (viruses.Any(s => !SusadminData.VirusNameExists(s))) WriteLine(PrintError("ERROR") + ": virus not found");
		else if (viruses.Any(s => !installedViruses.Contains(SusadminData.GetVirusId(s)))) WriteLine(PrintError("ERROR") + ": virus not installed");
		else if (new HashSet<string>(viruses.Select(v => v.ToUpper())).Count != viruses.Length) WriteLine(PrintError("ERROR") + ": duplicates");
		else {
			foreach (Vector2Int id in viruses.Select(s => SusadminData.GetVirusId(s))) installedViruses.Remove(id);
			Debug.LogFormat("[SUSadmin #{0}] Viruses deleted: {1}", moduleId, viruses.Select(s => s.ToUpper()).Join(", "));
		}
	}

	private void SetSecurityProtocols() {
		securityProtocols = new List<int>();
		HashSet<int> temp = new HashSet<int>(Enumerable.Range(0, SusadminData.SECURITY_PROTOCOLS_COUNT));
		for (int i = 0; i < SusadminData.INSTALLED_SECURITY_PROTOCOLS_COUNT; i++) {
			int securityProtocol = temp.PickRandom();
			temp.Remove(securityProtocol);
			securityProtocols.Add(securityProtocol);
		}
		Debug.LogFormat("[SUSadmin #{0}] Installed security protocols: {1}", moduleId, securityProtocols.Select(i => SusadminData.GetSecurityProtocolName(i)).Join(", "));
	}

	private string PrintNumber(string str) {
		return string.Format("<color=#88f>{0}</color>", str);
	}

	private string PrintError(string str) {
		return string.Format("<color=red>{0}</color>", str);
	}

	private string PrintOSVersion() {
		return string.Format("<color=#4f4>{0}</color> {1}", osIsBoom ? "BoomOS" : "BombOS", PrintNumber("v" + osVersion));
	}

	private string PrintSecurityProtocolName(int securityProtocol) {
		return string.Format("<color=yellow>{0}</color>", SusadminData.GetSecurityProtocolName(securityProtocol));
	}

	private void WriteLine(string text) {
		lines[linePointer] = text;
		linePointer = (linePointer + 1) % LINES_COUNT;
		shouldUpdateText = true;
	}
}
