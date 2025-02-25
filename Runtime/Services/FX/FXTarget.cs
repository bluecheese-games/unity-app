//
// Copyright (c) 2025 BlueCheese Games All rights reserved
//

using BlueCheese.Core.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueCheese.App
{
	public class FXTarget : MonoBehaviour, IDespawnable
	{
		private struct FXData
		{
			public FXInstance Instance;
			public Vector3 OffsetPosition;
			public Vector3 OffsetRotation;
		}

		private readonly List<FXData> _fxList = new();

		public static void Register(GameObject target, FXInstance instance, Vector3 offsetPosition, Vector3 offsetRotation)
		{
			if (!target.TryGetComponent<FXTarget>(out var fxTarget))
			{
				fxTarget = target.AddComponent<FXTarget>();
				fxTarget.hideFlags = HideFlags.HideInInspector;
			}

			fxTarget.Register(instance, offsetPosition, offsetRotation);
		}

		public void Register(FXInstance instance, Vector3 offsetPosition, Vector3 offsetRotation)
		{
			if (_fxList.Any(i => i.Instance == instance))
			{
				return;
			}

			_fxList.Add(new()
			{
				Instance = instance,
				OffsetPosition = offsetPosition,
				OffsetRotation = offsetRotation,
			});
		}

		private void Update()
		{
			foreach (var fxData in _fxList)
			{
				if (!fxData.Instance.IsAlive)
				{
					continue;
				}

				var position = transform.position + fxData.OffsetPosition;
				var rotation = transform.rotation * Quaternion.Euler(fxData.OffsetRotation);
				fxData.Instance.transform.SetPositionAndRotation(position, rotation);
			}
		}

		public static void Unregister(GameObject target, FXInstance instance)
		{
			if (target.TryGetComponent<FXTarget>(out var fxTarget))
			{
				fxTarget.Unregister(instance);
			}
		}

		public void Unregister(FXInstance instance)
		{
			_fxList.RemoveAll(data => data.Instance == instance);

			if (_fxList.Count == 0)
			{
				Destroy(this);
			}
		}

		public void OnDespawn() => StopAllFx();

		private void OnDestroy() => StopAllFx();

		private void StopAllFx()
		{
			for (int i = _fxList.Count - 1; i >= 0; i--)
			{
				_fxList[i].Instance.StopEmitting();
			}

			_fxList.Clear();
		}
	}
}
