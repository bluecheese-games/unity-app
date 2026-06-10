using System;
using UnityEngine;

namespace BlueCheese.App
{
	[AttributeUsage(AttributeTargets.Field)]
	public class PoolOptionsAttribute : PropertyAttribute
	{
		public int Capacity { get; set; } = 10;
		public int FillAmount { get; set; } = 0;
		public bool UseContainer { get; set; } = true;
		public bool DontDestroyOnLoad { get; set; } = false;
		public PoolOverflow Overflow { get; set; } = PoolOverflow.LogError;

		public PoolOptionsAttribute() { }

		public PoolOptions ToPoolOptions()
		{
			return new PoolOptions
			{
				Capacity = this.Capacity,
				FillAmount = this.FillAmount,
				UseContainer = this.UseContainer,
				DontDestroyOnLoad = this.DontDestroyOnLoad,
				Overflow = this.Overflow
			};
		}
	}
}