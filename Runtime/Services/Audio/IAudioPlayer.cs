//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

namespace BlueCheese.App
{
	public interface IAudioPlayer
	{
		AudioItem PlayingItem { get; }

		bool PlayMusic(AudioItem item, MusicOptions options);
		bool PlaySound(AudioItem item, SoundOptions options);
		void Stop(float fadeDuration);
	}
}