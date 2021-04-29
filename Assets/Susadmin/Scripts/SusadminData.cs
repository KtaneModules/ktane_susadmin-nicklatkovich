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

	private static readonly Dictionary<string, Vector2Int> VirusIds = new Dictionary<string, Vector2Int>();

	static SusadminData() {
		for (int i = 0; i < SECURITY_PROTOCOLS_COUNT; i++) for (int j = 0; j < SECURITY_PROTOCOLS_COUNT; j++) VirusIds.Add(VirusesName[i][j], new Vector2Int(i, j));
	}

	public static HashSet<string> AllViruses { get { return new HashSet<string>(VirusesName.SelectMany(a => a)); } }
	public static string[] SecurityProtocolNames { get { return new[] { "ByteDefender", "Kasperovich", "Awast", "MedicWeb", "Disco", "MOD32" }; } }

	public static bool VirusNameExists(string virusName) { return AllViruses.Contains(virusName.ToUpper()); }
	public static string[] GetAllSecurityProtocolsName() { return SecurityProtocolNames.Select(a => a).ToArray(); }
	public static Vector2Int GetVirusId(string virusName) { return VirusIds[virusName.ToUpper()]; }
	public static string GetSecurityProtocolName(int id) { return SecurityProtocolNames[id]; }
	public static string GetVirusName(Vector2Int id) { return VirusesName[id.x][id.y]; }

	public static bool VirusIsInvisible(Vector2Int virusId, HashSet<int> securityProtocols) {
		return !securityProtocols.Contains(virusId.x) && !securityProtocols.Contains(virusId.y);
	}

	public static HashSet<Vector2Int> GetPossibleVirusesId(HashSet<int> installedSecurityProtocols) {
		IEnumerable<int> notInstalledSP = Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT).Where(i => !installedSecurityProtocols.Contains(i));
		return new HashSet<Vector2Int>(notInstalledSP.Select(x => notInstalledSP.Select(y => new Vector2Int(x, y))).SelectMany(a => a));
	}

	public static int GetSafetyLevel(Vector2Int[] sortedViruses, int vulnerability, int[][] virusesPower, int[][] compatibilityIndices, out Vector2Int exampleRange) {
		int i = 0;
		int j = 0;
		int temp = virusesPower[sortedViruses[0].x][sortedViruses[0].y];
		int safetyLevel = temp;
		exampleRange = new Vector2Int(0, 0);
		while (j < sortedViruses.Length) {
			Debug.LogFormat("<SUSadmin> {0}-{1}: t:{2}; s:{3}; r:{4}-{5}", i, j, temp, safetyLevel, exampleRange.x, exampleRange.y);
			if (compatibilityIndices[sortedViruses[j].x][sortedViruses[j].y] - compatibilityIndices[sortedViruses[i].x][sortedViruses[i].y] <= vulnerability) {
				if (safetyLevel < temp) {
					safetyLevel = temp;
					exampleRange = new Vector2Int(i, j);
				}
				j++;
				if (j == sortedViruses.Length) break;
				temp += virusesPower[sortedViruses[j].x][sortedViruses[j].y];
				continue;
			}
			if (i == j) {
				i += 1;
				j += 1;
				if (j == sortedViruses.Length) break;
				temp = virusesPower[sortedViruses[j].x][sortedViruses[j].y];
				continue;
			}
			i += 1;
			temp -= virusesPower[sortedViruses[i].x][sortedViruses[i].y];
		}
		return safetyLevel;
	}

	public static void Generate(
		out HashSet<int> securityProtocols,
		out int vulnerability,
		out int safetyLevel,
		out int[][] compatibilityIndices,
		out int[][] virusesPower,
		out HashSet<Vector2Int> answerExample
	) {
		securityProtocols = GetRandomSecurityProtocols();
		HashSet<Vector2Int> allowedViruses = GetPossibleVirusesId(securityProtocols);
		int[][] _compatibilityIndices = Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT).Select((_) => (
			Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT).Select((__) => Random.Range(10, 100)).ToArray()
		)).ToArray();
		int[][] _virusesPower = Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT).Select((_) => (
			Enumerable.Range(0, SECURITY_PROTOCOLS_COUNT).Select((__) => Random.Range(10, 100)).ToArray()
		)).ToArray();
		Vector2Int[] sortedViruses = allowedViruses.ToArray();
		Array.Sort(sortedViruses, (a, b) => _compatibilityIndices[a.x][a.y] - _compatibilityIndices[b.x][b.y]);
		string debugString = sortedViruses.Select(i => string.Format("{0}: {1}", _compatibilityIndices[i.x][i.y], _virusesPower[i.x][i.y])).Join("\n\t");
		Debug.LogFormat("<SUSadmin> possible viruses:\n\t{0}", debugString);
		int minCompatibilityIndex = _compatibilityIndices[sortedViruses[0].x][sortedViruses[0].y];
		int maxCompatibilityIndex = _compatibilityIndices[sortedViruses[sortedViruses.Length - 1].x][sortedViruses[sortedViruses.Length - 1].y];
		vulnerability = Random.Range(0, maxCompatibilityIndex - minCompatibilityIndex);
		Vector2Int answerExampleRange;
		safetyLevel = GetSafetyLevel(sortedViruses, vulnerability, _virusesPower, _compatibilityIndices, out answerExampleRange);
		answerExample = new HashSet<Vector2Int>(Enumerable.Range(answerExampleRange.x, answerExampleRange.y - answerExampleRange.x + 1).Select(i => sortedViruses[i]));
		compatibilityIndices = _compatibilityIndices;
		virusesPower = _virusesPower;
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
