//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

using Cysharp.Threading.Tasks;
using System.Collections;

namespace BlueCheese.App
{
	public static class UniTaskExtensions
	{
		public static IEnumerator ToCoroutine(this UniTask task)
		{
			var handler = task.AsTask();

			while (!handler.IsCompleted)
			{
				yield return null;
			}

			if (handler.IsFaulted)
			{
				throw handler.Exception;
			}

			yield break;
		}
	}

}
