using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Muc.Extensions;

public class Model : MonoBehaviour {

	public Vector3Int GetSize() {
		return transform.localScale.RoundInt();
	}

	public IEnumerable<Block> GetBlocks() {

		foreach (Transform child in transform) {
			if (child.gameObject.activeInHierarchy && child.TryGetComponent<Block>(out var block)) {
				yield return block;
			}
		}
	}

}
