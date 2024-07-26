using BlueCheese.Core.ServiceLocator;

namespace BlueCheese.App
{
	public interface ILocalizationService : IInitializable
	{
		Locale DeviceLocale { get; }
		Locale DefaultLocale { get; }
		Locale CurrentLocale { get; }

		void SetCurrentLocale(Locale locale);
	}
}
