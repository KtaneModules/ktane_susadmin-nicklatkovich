using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public static class SusadminData {
	public const int SECURITY_PROTOCOLS_COUNT = 6;
	public const int INSTALLED_SECURITY_PROTOCOLS_COUNT = 3;

	private static readonly string[][] VirusesName = new string[SECURITY_PROTOCOLS_COUNT][] {
		new string[SECURITY_PROTOCOLS_COUNT] { "UBNL", "BIN8", "K34Q", "7R7B", "6881", "PERK" },
		new string[SECURITY_PROTOCOLS_COUNT] { "MPDZ", "3UD7", "J333", "7Q2K", "A093", "5O8K" },
		new string[SECURITY_PROTOCOLS_COUNT] { "5YP4", "23JJ", "W9Q5", "O2V5", "1K63", "32EI" },
		new string[SECURITY_PROTOCOLS_COUNT] { "D4YD", "ZI53", "8339", "48NT", "81RM", "8U63" },
		new string[SECURITY_PROTOCOLS_COUNT] { "M821", "33FT", "ONE5", "2E1E", "6V00", "X642" },
		new string[SECURITY_PROTOCOLS_COUNT] { "A125", "7Z17", "50KD", "8V00", "7D2U", "C43B" },
	};

	private static readonly int[][] VirusCompatibilityIndices = new int[SECURITY_PROTOCOLS_COUNT][] {
		new int[SECURITY_PROTOCOLS_COUNT] { 30, 85, 37, 41, 22, 80 },
		new int[SECURITY_PROTOCOLS_COUNT] { 99, 94, 82, 86, 99, 48 },
		new int[SECURITY_PROTOCOLS_COUNT] { 83, 52, 44, 57, 50, 51 },
		new int[SECURITY_PROTOCOLS_COUNT] { 67, 64, 12, 15, 48, 27 },
		new int[SECURITY_PROTOCOLS_COUNT] { 14, 33, 98, 79, 72, 46 },
		new int[SECURITY_PROTOCOLS_COUNT] { 74, 29, 94, 0, 9, 9 },
	};

	private static readonly int[][] VirusPowers = new int[SECURITY_PROTOCOLS_COUNT][] {
		new int[SECURITY_PROTOCOLS_COUNT] { 50, 16, 99, 49, 82, 79 },
		new int[SECURITY_PROTOCOLS_COUNT] { 32, 94, 20, 12, 45, 21 },
		new int[SECURITY_PROTOCOLS_COUNT] { 70, 39, 38, 79, 10, 6 },
		new int[SECURITY_PROTOCOLS_COUNT] { 15, 77, 90, 76, 71, 32 },
		new int[SECURITY_PROTOCOLS_COUNT] { 74, 73, 61, 5, 18, 92 },
		new int[SECURITY_PROTOCOLS_COUNT] { 47, 98, 24, 39, 74, 77 },
	};

	private static readonly string[] SecurityProtocolNames = new string[SECURITY_PROTOCOLS_COUNT] { "ByteDefender", "Kasperovich", "Awast", "MedicWeb", "Disco", "MOD32" };
	private static readonly HashSet<string> AllViruses = new HashSet<string>(VirusesName.SelectMany(a => a));
	private static readonly Dictionary<string, Vector2Int> VirusIds = new Dictionary<string, Vector2Int>();

	static SusadminData() {
		for (int i = 0; i < SECURITY_PROTOCOLS_COUNT; i++) for (int j = 0; j < SECURITY_PROTOCOLS_COUNT; j++) VirusIds.Add(VirusesName[i][j], new Vector2Int(i, j));
	}

	public static bool VirusNameExists(string virusName) { return AllViruses.Contains(virusName.ToUpper()); }
	public static int GetVirusCompatibilityIndex(Vector2Int id) { return VirusCompatibilityIndices[id.x][id.y]; }
	public static int GetVirusPower(Vector2Int id) { return VirusPowers[id.x][id.y]; }
	public static Vector2Int GetVirusId(string virusName) { return VirusIds[virusName.ToUpper()]; }
	public static Vector2Int GetVirusProperties(Vector2Int id) { return new Vector2Int(GetVirusCompatibilityIndex(id), GetVirusPower(id)); }
	public static string GetSecurityProtocolName(int id) { return SecurityProtocolNames[id]; }
	public static string GetVirusName(Vector2Int id) { return VirusesName[id.x][id.y]; }

	public static bool VirusIsInvisible(Vector2Int virusId, HashSet<int> securityProtocols) {
		return !securityProtocols.Contains(virusId.x) && !securityProtocols.Contains(virusId.y);
	}

	public static HashSet<Vector2Int> GetPossibleVirusesId(HashSet<int> installedSecurityProtocols) {
		IEnumerable<int> notInstalledSP = Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT).Where(i => !installedSecurityProtocols.Contains(i));
		return new HashSet<Vector2Int>(notInstalledSP.Select(x => notInstalledSP.Select(y => new Vector2Int(x, y))).SelectMany(a => a));
	}

	public static int GetSafetyLevel(Vector2Int[] sortedViruses, int vulnerability, out Vector2Int exampleRange) {
		int i = 0;
		int j = 0;
		int temp = GetVirusPower(sortedViruses[0]);
		int safetyLevel = temp;
		exampleRange = new Vector2Int(0, 0);
		while (j < sortedViruses.Length) {
			Debug.LogFormat("<SUSadmin> {0}-{1}: t:{2}; s:{3}; r:{4}-{5}", i, j, temp, safetyLevel, exampleRange.x, exampleRange.y);
			if (GetVirusCompatibilityIndex(sortedViruses[j]) - GetVirusCompatibilityIndex(sortedViruses[i]) <= vulnerability) {
				if (safetyLevel < temp) {
					safetyLevel = temp;
					exampleRange = new Vector2Int(i, j);
				}
				j++;
				if (j == sortedViruses.Length) break;
				temp += GetVirusPower(sortedViruses[j]);
				continue;
			}
			if (i == j) {
				i += 1;
				j += 1;
				if (j == sortedViruses.Length) break;
				temp = GetVirusPower(sortedViruses[j]);
				continue;
			}
			i += 1;
			temp -= GetVirusPower(sortedViruses[i]);
		}
		return safetyLevel;
	}

	public static void Generate(out HashSet<int> securityProtocols, out int vulnerability, out int safetyLevel, out HashSet<Vector2Int> answerExample) {
		securityProtocols = GetRandomSecurityProtocols();
		HashSet<Vector2Int> allowedViruses = GetPossibleVirusesId(securityProtocols);
		Vector2Int[] sortedViruses = allowedViruses.ToArray();
		Array.Sort(sortedViruses, (a, b) => GetVirusCompatibilityIndex(a) - GetVirusCompatibilityIndex(b));
		string debugString = sortedViruses.Select(i => string.Format("{0}: {1}", GetVirusCompatibilityIndex(i), GetVirusPower(i))).Join("\n\t");
		Debug.LogFormat("<SUSadmin> possible viruses:\n\t{0}", debugString);
		int leftVulnerability = GetVirusCompatibilityIndex(sortedViruses[(sortedViruses.Length - 1) / 2]) - GetVirusCompatibilityIndex(sortedViruses[0]);
		int rightVulnerability = GetVirusCompatibilityIndex(sortedViruses.Last()) - GetVirusCompatibilityIndex(sortedViruses[sortedViruses.Length / 2]);
		vulnerability = Random.Range(Mathf.Min(leftVulnerability, rightVulnerability), Mathf.Max(leftVulnerability, rightVulnerability) + 1);
		Vector2Int answerExampleRange;
		safetyLevel = GetSafetyLevel(sortedViruses, vulnerability, out answerExampleRange);
		answerExample = new HashSet<Vector2Int>(Enumerable.Range(answerExampleRange.x, answerExampleRange.y - answerExampleRange.x + 1).Select(i => sortedViruses[i]));
	}

	public static HashSet<int> GetRandomSecurityProtocols(int count = INSTALLED_SECURITY_PROTOCOLS_COUNT) {
		HashSet<int> result = new HashSet<int>();
		HashSet<int> temp = new HashSet<int>(Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT));
		for (int i = 0; i < INSTALLED_SECURITY_PROTOCOLS_COUNT; i++) {
			int securityProtocol = temp.PickRandom();
			temp.Remove(securityProtocol);
			result.Add(securityProtocol);
		}
		return result;
	}
}
