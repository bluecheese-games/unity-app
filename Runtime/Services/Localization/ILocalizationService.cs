using BlueCheese.Core.ServiceLocator;
using UnityEngine;

namespace BlueCheese.App
{
	public interface ILocalizationService : IInitializable
	{
		SystemLanguage DeviceLanguage { get; }
		SystemLanguage DefaultLanguage { get; }
		SystemLanguage CurrentLanguage { get; }

		void SetCurrentLanguage(SystemLanguage language);
	}
}
