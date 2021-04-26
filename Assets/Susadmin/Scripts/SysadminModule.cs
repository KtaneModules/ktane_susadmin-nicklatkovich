using System.Collections.Generic;
using UnityEngine;

public class SysadminModule : MonoBehaviour {
	public KMBombModule Module;
	public KMSelectable FixErrorButton;
	public KMSelectable SolveButton;

	private HashSet<string> _fixedErrorCodes = new HashSet<string>();
	public HashSet<string> fixedErrorCodes { get { return new HashSet<string>(_fixedErrorCodes); } }

	private bool solved = false;

	private void Start() {
		Module.OnActivate += OnActivate;
	}

	private void OnActivate() {
		FixErrorButton.OnInteract += () => { FixError(); return false; };
		SolveButton.OnInteract += () => { Solve(); return false; };
	}

	private void FixError() {
		if (solved) return;
		_fixedErrorCodes.Add(_fixedErrorCodes.Count.ToString());
		Debug.LogFormat("<Fake Sysadmin> ErrorFixed: {0}", _fixedErrorCodes.Count);
	}

	private void Solve() {
		solved = true;
		Module.HandlePass();
		Debug.LogFormat("<Fake Sysadmin> Solved");
	}
}
