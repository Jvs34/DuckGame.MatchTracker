# Duck Game Match Tracker

A collection of modules I've been working on to track, save, upload and view my duck game matchs I play with my friends.

This entire contraption has these parts:

1. **DONE** Match Recorder *(loaded by Duck Game as a mod)* connected to OBS Studio with a websocket plugin, saves the metadata and videos of the matches in a git repository.

2. **DONE** Match Uploader, uses Youtube API to upload the match videos, then link the youtube URLs to the metadata hosted on GitHub.

3. **TO REDO** Match Bot, a discord bot that used the LUIS api for its commands, obviously overkill and I need to remove it and go back to boring old commands.

4. **NOT STARTED** MatchTrackerHandler, an out of process program that would merge 80% of the match recorder, uploader and discord bot, this would allow the client to be lighter and have less compatibility issues with the new .NET 5 apis that I wanted to make use of.

----------

## What's left to do

The setup experience is absolutely horrid considering how many config files I'm making, this hasn't changed and it's still dependant on the user ALSO configuring obs-studio and installing a websocket plugin on their own.

Unless you go through the sourcecode you won't know how to handle the whole thing.

----------

## So why go all this effort?

Before this I was using a ShadowPlay plugin to save matches, that had upsides and downsides, it was very easy to use and very fast, but it saved no metadata, had no control over the quality (since it shares settings with the current shadowplay configuration) and generally it was a pain in the butt to upload the files.

I was using Google Photos from Google Drive with the lower quality upload setting that allows the videos to take no space from my Google Drive quota, I managed to ammass at least 1500 recorded rounds this way, which took ages to upload on my current 1MB/s upload.

Even if I somehow managed to save metadata on the videos, there's no filtering whatsoever on the Google Photos side like it used to in Picasa.

Besides that, I also had to manually move all the videos uploaded to the Duck Game album, and there's no proper Google Photos apis except for using the outdated Picasa api that doesn't work properly with Photos.

Overall, I feel like I'm quite happy with this, it was a refreshing experience being able to use C# on all fronts for once and even if the usecase is pretty niche.