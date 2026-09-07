using BlueCheese.Core.DI;
using UnityEngine;

namespace BlueCheese.App
{
	public static class Translator
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetDomain()
		{
			_translationService = null;
		}

		private static ITranslationService _translationService;

		public static void Initialize(ITranslationService translationService)
		{
			_translationService = translationService;
		}

		private static bool TryGetService(out ITranslationService service)
		{
			// Outside play mode, lazily resolve the editor translation service on first use.
			if (_translationService == null && !Application.isPlaying)
			{
				Initialize(EditorServiceLocator.Resolve<ITranslationService>());
			}

			// During play mode, lazily resolve the runtime translation service on first use.
			if (_translationService == null)
			{
				_translationService = ServiceLocator.Resolve<ITranslationService>();
			}

			service = _translationService;
			return service != null;
		}

		public static bool HasTranslation(TranslationKey translationKey)
		{
			return TryGetService(out var service) && service.HasTranslation(translationKey);
		}

		public static string Translate(TranslationKey key)
		{
			return TryGetService(out var service) ? service.Translate(key) : key.Key;
		}
	}
}
