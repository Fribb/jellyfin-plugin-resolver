# Jellyfin Resolver
_A proper name should still be considered._

Jellyfin resolvers provide the functionality that assigns proper types like "season" or "episode" to files and folders.
This plugin adds a new resolver to Jellyfin that uses [Anitomy](https://github.com/erengy/anitomy) to parse files. Additionally the way the folder structure is defined is changed to better suit anime watchers.

## Building
Because the package used (AnitomySharp) was not compatible with netstandard I make my own fork. The fork needs to be cloned locally next to this repository.

## Installation

> :warning: **This is merely a Prototype/proof of concept forked from nielsvanvelzen/jellyfin-plugin-resolver and is more of a stopgap solution than a finished implementation**

To use this Plugin you need to build it with Visual Studio or Visual Studio Code.
This build process will generate two dll's

1. Jellyfin.Plugin.Resolver.dll
2. AnitomySharp.dll

Create a new folder in the `jellyfin\data\plugins` folder that you can name as you wish (anime-resolver, for example).
Place the above mentioned dll's in that folder.

Now you need to restart your Jellyfin Server. 
You should now have the `Resolver Plugin 1.0.0.0` in your "My Plugins" section.

## Current state
The plugin is usable to a certain degree. Unfortunately the Jellyfin server doesn't support alternative resolvers properly so this plugin needs to do some hacks to get it working:
- It defines a high priority so it will run before most of the other build-in resolvers
- It check if the library it is resolving contains "anime" in the name because there is no way to assign it to certain libraries only (yet)
- It **always** returns something so other resolvers won't provide false information
- Only works for mp4 and mkv files at this moment

A issue for custom resolvers exists in the Jellyfin issue tracker: [jellyfin/#2187](https://github.com/jellyfin/jellyfin/issues/2187)


## Folder Structure
I modified the source to remove the "Franchise" and required Number of the "show" title.

The Plugin will now scan and organize all Anime in a more "Flat" hierarchy.

For example:
* `/test-anime/One Piece/One Piece - 1015 [1080p].mkv`