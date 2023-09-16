# Duck Game Match Tracker

This is a personal project that tracks, saves and uploads my duck game matches I play with my friends.

This contraption consists of these parts:

1. **WIP** Match Recorder Mod *(loaded by Duck Game as a clientside mod)* connects to an out of process program, sends data from Duck Game to the process to let it track stuff, doesn't do much else on its own by design because screw having to use .Net Framework 4.5.2 for anything (superjoebob pls upgrade).

2. **WIP** Match Recorder Out of Process *(loaded by Match Recorder Mod)* an out of process program that receives data from the mod and starts up recording with OBS, this is dependent on an obs websocket nuget that apparently updates scarcely and barely works on newer versions of OBS.

3. **WIP** Match Recorder Companion *(loaded by Duck Game as a mod)* a companion to Match Recorder, not really needed, but it provides Match Recorder with proper kill info, as Duck Game strips almost all info out of the kills in multiplayer.

4. **BROKEN** Match Uploader *(ran manually)*, used to use the Youtube API to upload the match videos, then link the youtube URLs to the metadata hosted on GitHub, but ever since the changes to the Youtube API in 2020, it can't upload anything since I'm an idiot and removed all my keys pre-2020 that were already authorized.

5. **BROKEN** Match Bot, a discord bot that allows some simple queries to the match database, uses DSharpPlus but I might switch (again) to Discord-Net as it feels like DSharpPlus changes api every month.

----------

## What's left to do

The setup experience is absolutely horrid considering how many config files I'm using, this hasn't changed and it's dependant on the user configuring obs-studio's websocket plugin on their own.

Unless you go through the sourcecode you won't know how to handle the whole thing.

Honestly there's probably easier ways to do this kind of stuff but at the moment I can't be arsed to look up more

----------

## So why go all this effort?

Before this I was using a ShadowPlay plugin to save matches, that had upsides and downsides, it was very easy to use and very fast, but it saved no metadata, it returned no video path of what it had just recorded, had no control over the quality (since it shares settings with the current shadowplay configuration) and generally it was a pain in the butt to upload the files.

I was using Google Photos from Google Drive with the lower quality upload setting that allows the videos to take no space from my Google Drive quota, I managed to ammass at least 1500 recorded rounds this way, which took ages to upload on my current 1MB/s upload.

Even if I somehow managed to save metadata on the videos, there's no filtering whatsoever on the Google Photos side like it used to in Picasa.

Besides that, I also had to manually move all the videos uploaded to the Duck Game album, and there's no proper Google Photos apis except for using the outdated Picasa api that doesn't work properly with Photos.

Overall, I feel like I'm quite happy with this, it was a refreshing experience being able to use C# on all fronts for once and even if the usecase is pretty niche.
