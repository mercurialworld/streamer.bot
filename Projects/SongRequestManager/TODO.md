# Request Redeem
- User requests song
    - Links: Apple Music, Soundcloud, Spotify, YouTube
    - Funny codes: Spotify URIs
- Filter input by type
    - Check for request limits (I'm pretty sure it's 5/min but it'll barely be hit with my audience LMAO)
    - URI: Spotify -> put through song.link
    - Link to non-Apple Music: put through song.link
    - Apple Music: put straight to Cider (nvm we're putting this through song.link xd)

# TODO
- Have a Streamer.bot managed queue (as non-persisted global)
    - ~~Cider~~ Apple Music doesn't distinguish between user-added songs and "part of playlist" songs, so this is kinda necessary
- See if the song's available in *Canada* so I don't play previews LUL