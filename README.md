# Duck Game Match Tracker

A collection of modules I've been working on to track, save, upload and view my duck game matchs I play with my friends.

----------

This entire contraption has 3 parts:

1. Match Recorder *(loaded by Duck Game as a mod)* connected to OBS Studio with a websocket plugin, saves the metadata and videos of the matches in a folder

2. Match Uploader, uses Youtube API to upload the match videos, then link the youtube URLs to the metadata hosted on GitHub

3. Match Viewer, a GitHub-hosted website made in Blazor *(because it's the cool thing for now on C#)* that will read the metadata off of the tracked matches repository and allow filtering and all that cool stuff

----------

# How to use

TODO

----------

# So why go all this effort?

Before this I was using a ShadowPlay plugin to save matches, that had upsides and downsides, it was very easy to use and very fast, but it saved no metadata, had no control over the quality (since it shares settings with the current shadowplay configuration) and generally it was a pain in the butt to upload the files.

I was using Google Photos from Google Drive with the lower quality upload setting that allows the videos to take no space from my Google Drive quota, I managed to ammass at least 1500 recorded rounds this way, which took ages to upload on my current 1MB/s upload.

Even if I somehow managed to save metadata on the videos, there's no filtering whatsoever on the Google Photos side like it used to in Picasa.

Besides that, I also had to manually move all the videos uploaded to the Duck Game album, and there's no proper Google Photos apis except for using the outdated Picasa api that doesn't work properly with Photos.

----------

Overall, I feel like I'm quite happy having wasted time on this, it was a refreshing experience being able to use C# on all fronts for once