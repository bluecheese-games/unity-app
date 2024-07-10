//
// Copyright (c) 2024 BlueCheese Games All rights reserved
//

using BlueCheese.Core.ServiceLocator;

namespace BlueCheese.App
{
    public interface IAudioService : IInitializable
    {
        /// <summary>
        /// The master sound volume applied to every played sound.
        /// </summary>
        float MasterSoundVolume { get; set; }

        /// <summary>
        /// The master music volume applied to every played music.
        /// </summary>
        float MasterMusicVolume { get; set; }

        /// <summary>
        /// Is the service ready?
        /// </summary>
        bool IsReady { get; }
        
        /// <summary>
        /// Plays a sound.
        /// </summary>
        /// <param name="name">The sound name.</param>
        bool PlaySound(string name);

        /// <summary>
        /// Plays a sound with options.
        /// </summary>
        /// <param name="name">The sound name.</param>
        /// <param name="options">The sound options.</param>
        bool PlaySound(string name, SoundOptions options);

        /// <summary>
        /// Stops a playing sound.
        /// </summary>
        /// <param name="name">The sound name.</param>
        /// <param name="fadeDuration">The fade out duration in seconds.</param>
        void StopSound(string name, float fadeDuration = 0f);

        /// <summary>
        /// Stops all playing sounds.
        /// </summary>
        /// <param name="fadeDuration"></param>
        void StopAllSounds(float fadeDuration = 0f);

		/// <summary>
		/// Plays a music.
		/// </summary>
		/// <param name="name">The music name.</param>
		bool PlayMusic(string name);

		/// <summary>
		/// Plays a music with options.
		/// </summary>
		/// <param name="name">The music name.</param>
		/// <param name="options">The music options.</param>
		bool PlayMusic(string name, MusicOptions options);

        /// <summary>
        /// Stops a playing music.
        /// </summary>
        /// <param name="name">The music name.</param>
        /// <param name="fadeDuration">The fade out duration in seconds.</param>
        void StopMusic(string name, float fadeDuration = 0f);
	}
}
