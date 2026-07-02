//
// Copyright (c) 2026 BlueCheese Games All rights reserved
//

namespace BlueCheese.App.Editor
{
	/// <summary>
	/// An external translation store that a <see cref="TranslationTableAsset"/> can sync with.
	/// Local (.obd file) today; the same contract is meant to back a cloud source later.
	/// </summary>
	public interface ITranslationSource
	{
		bool CanRead { get; }
		bool CanWrite { get; }

		/// <summary>Reads the source into the normalized snapshot model.</summary>
		TranslationSnapshot Read();

		/// <summary>Writes the (merged) snapshot back to the source, preserving unrelated data.</summary>
		void Write(TranslationSnapshot snapshot);
	}
}
