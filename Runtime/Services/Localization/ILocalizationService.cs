//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;
using System.Collections.Generic;

namespace BlueCheese.App
{
	public interface ILocalizationService : IInitializable
	{
		Language DeviceLanguage { get; }
		Language DefaultLanguage { get; }
		Language CurrentLanguage { get; }

		IReadOnlyList<Language> SupportedLanguages { get; }

		void SetCurrentLanguage(Language language);
	}
}
