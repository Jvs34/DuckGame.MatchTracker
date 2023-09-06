# What data do you collect

Userdata wise, my MatchRecorder program loaded by the game Duck Game will only track player score, Username, SteamID, and occasionally DiscordID of players that play against me in a Duck Game match.
Most importantly, the MatchRecorder will initiate a connection to the OBS-Studio running on the same local machine as the Duck Game mod to automatically record a clip or livestream of the match in action to be able to upload on Youtube.

The MatchRecorder program is simply making use of the information given by the Duck Game client in order to track how a match progresses, who wins, who loses, what level was played and when it occured.

## What does the data look like

The data looks like this, here is a typical Duck Game round as tracked by the mod.

```json
{
  "RecordingType": 1,
  "IsCustomLevel": false,
  "LevelName": "036a2542-89b2-4d44-a5e0-a927400ccbd2", //the GUID of the Duck Game level this round was played on
  "Name": "2018-04-28 19-55-08",
  "MatchName": "2018-04-28 19-54-17",
  "Players": [
    "76561197998909316", //UserID of the user, in this case it's a SteamID because that's what the game uses for network IDS
    "76561197998646590",
    "76561197993900183",
    "76561197999418456"
  ],
  "Teams": [
    {
      "HasHat": true,
      "HatName": "DUCKS", //the name of the hat the player uses, simply cosmetic
      "IsCustomHat": false,
      "Players": [
        "76561197998909316"
      ],
      "Score": 0
    },
    {
      "HasHat": true,
      "HatName": "Player 2",
      "IsCustomHat": true,
      "Players": [
        "76561197998646590"
      ],
      "Score": 0
    },
    {
      "HasHat": true,
      "HatName": "CYCLOPS",
      "IsCustomHat": false,
      "Players": [
        "76561197993900183"
      ],
      "Score": 0
    },
    {
      "HasHat": true,
      "HatName": "Player 4",
      "IsCustomHat": true,
      "Players": [
        "76561197999418456"
      ],
      "Score": 0
    }
  ],
  "TimeEnded": "2018-04-28T19:55:20.0317968+02:00",
  "TimeStarted": "2018-04-28T19:55:08.8542885+02:00",
  "Winner": {
    "HasHat": true,
    "HatName": "DUCKS",
    "IsCustomHat": false,
    "Players": [
      "76561197998909316"
    ],
    "Score": 0
  },
  "YoutubeUrl": "SwsisBp6Qaw", //the youtube ID of this round as it's already been uploaded
  "VideoType": 2,
  "VideoMirrors": [],
  "Tags": [
    "240 159 152 161",
    "240 159 164 146",
    "240 159 152 165"
  ]
}
```

## Who can use this

As a matter of fact only me, Jvs34; the configuration requirements are pretty steep considering I do not want to share access to the Duck Game Match Tracker google api to anyone, and they would have to register their own app through the google cloud console in order to use this program, which comes with a new verification process.
