# Inscura’s Emby Metadata Plugin

Languages: **English** | [简体中文](docs/README-zh.md) | [日本語](docs/README-ja.md) | [한국어](docs/README-ko.md)

Inscura is a local media library application that organizes information such as movie details, cast, genres, collections, cover art, background images, photo albums, trailers, and media streams. Emby handles playback and media library management, but it is not aware of the data that Inscura has already organized.

This plugin integrates Inscura’s local API service with Emby. When Emby scans or refreshes a movie, the plugin writes the title, synopsis, release date, rating, genre, tags, collections, cast, director, screenwriter, producer, images, streaming information, and YouTube trailers to Emby’s metadata.

The plugin only reads data from Inscura’s media library and the image resources generated within it; it does not download, move, rename, delete, or modify the original media files.

## Current Capabilities

- Prioritizes matching based on the actual file paths and filenames passed by Emby to reduce mismatches caused by manually modified Emby titles.
- If no path match is found, it will continue to attempt matching based on numbers, identifiers in filenames, and Emby titles.
- Supports writing to Emby-compatible movie metadata fields, including title, original title, sort title, synopsis, tagline, release date, year, rating, content rating, production company, genre, country, tags, collections, cast, director, screenwriter, producer, and external ID.
- Supports importing media stream information, including details such as encoding, resolution, frame rate, bitrate, language, title, audio track, and default/forced subtitle tags for video, audio, and subtitle streams.
- Supports importing various types of movie images into Emby, including posters, background images, thumbnails, banners, logos, artwork, and disc covers.
- Supports automatically filling in missing image types in Emby after metadata is refreshed. By default, only missing images are filled in; existing images in Emby are not overwritten.
- For background images, Inscura’s landscape-oriented cover art/background images are used first, followed by preview images, video screenshots, and default image types from the photo album.
- Supports importing relationships between actors, directors, screenwriters, producers, and other personnel, and allows you to import profile pictures, biographies, birthdays, countries/regions, and actor tags for actors.
- For remote trailers, only YouTube URLs are currently imported. For non-YouTube local or online trailers, please export them using the Inscura app’s NFO feature and provide them to Emby for scanning.

## Enabling the Inscura Local API Service

1. Open Inscura and open the media library you want to sync to Emby.
2. Go to the API settings in Settings and enable the local API service.
3. We recommend keeping the authentication method set to token mode and saving the API token displayed on the settings page.
4. On the device hosting the Emby server, access the health check URL to confirm the service is reachable.

Example:

```bash
curl "http://[ip]:28687/api/v1/health"
```

If Emby and Inscura are not running on the same machine, do not enter `127.0.0.1` as the service address in the plugin. Instead, enter the local network address of the computer running Inscura, for example:

```text
http://[ip]:28687
```

The local API service runs for the duration of the current media library’s lifecycle: it listens on the port while the media library is open and the service is enabled, and **stops when the media library is locked, closed, or the application exits.**

## Plugin Download

From [GitHub](https://github.com/InscuraApp/inscura-emby-plugin/archive/refs/heads/main.zip) or [Releases](https://github.com/InscuraApp/inscura-emby-plugin/releases).

We recommend downloading the pre-built plugin package from the Releases section. The plugin installation files are:

| File | Purpose |
| --- | --- |
| `Emby.Plugin.Inscura.dll` | The core of the Emby plugin, responsible for searching, matching, writing metadata, and importing images and trailers |

If you download the source code package, you can also build it locally:

```bash
cd inscura-emby-plugin
dotnet build -c Release
```

The build output is located at:

```text
bin/Release/net8.0/Emby.Plugin.Inscura.dll
```

## Plugin Installation

Copy `Emby.Plugin.Inscura.dll` to the Emby Server’s plugin directory, then restart Emby Server.

The Emby plugin directory depends on your system, package source, container mapping, and installation method. The following lists common locations; if your setup differs, refer to the actual data directory shown in the Emby Server console, package page, container mapping, or your system.

| Environment | Common Plugin Directory |
| --- | --- |
| Windows | `%APPDATA%\Emby-Server\programdata\plugins` |
| Linux | `/var/lib/emby/plugins` |
| macOS | `~/.config/emby-server/plugins` |
| Docker | `/config/plugins` inside the container, or `config/plugins` as mapped from the host machine |
| Synology Package | Refer to the actual package data directory, such as `/vol1/@appdata/EmbyServer4-9/plugins` |

Installation Steps:

1. Stop or prepare to restart Emby Server.
2. Place `Emby.Plugin.Inscura.dll` in the Emby plugin directory.
3. Ensure the Emby running user has permission to read this file.
4. Restart Emby Server.
5. Open the Emby admin panel and verify that `Inscura` appears in the plugin list or on the plugin configuration page.

## Enabling the Plugin in Emby

1. Go to the Emby admin panel.
2. Open the plugin settings page and select `Inscura`.
3. Enter the Inscura API URL and API token.
4. Enable options as needed, such as movie metadata scraping, image import, automatic image type completion, actor metadata and avatar import, YouTube trailers, and media stream information.
5. Open the Movie Library settings and enable `Inscura` under Metadata Downloader and Image Downloader.
6. We recommend placing `Inscura` higher in the list so that Emby prioritizes using data from Inscura.
7. Save the settings.
8. Refresh the metadata for your movie library or individual movies. During the initial verification, it is recommended to refresh only a small number of movies first.

## Plugin Settings Guide

| Setting | Description |
| --- | --- |
| Interface Language | Language displayed on the plugin configuration page; supports English and Simplified Chinese |
| Inscura API Address | The address of the local Inscura API service. You must enter a local network address if Emby and Inscura are not on the same machine |
| API Token | Enter this when the local API service uses token-based authentication; leave blank if Inscura is set to “No Authentication” |
| Number of Search Candidates | The number of candidates requested from Inscura per match |
| Request Timeout | The timeout period for the plugin to access the Inscura API, in seconds |
| Enable Movie Metadata Scraping | When enabled, Emby will retrieve movie information from Inscura when refreshing movie metadata |
| Enable Image Import | When enabled, Emby can retrieve movie images from Inscura |
| Automatically Fill in Missing Emby Image Types | When enabled, the plugin automatically saves missing posters, background images, thumbnails, banners, logos, artwork, and disc covers in Emby after movie metadata is refreshed |
| Enable actor metadata and avatar import | When enabled, Emby can retrieve actor profiles and images from Inscura |
| Import YouTube Trailers | When enabled, the plugin imports only remote trailers in YouTube format |
| Use Inscura Previews as Thumbnail Candidates | When enabled, Inscura previews will be used as candidates for Emby thumbnails |
| Use Gallery Images and Screenshots as Backdrop/Screenshot Candidates | When enabled, Inscura background images, preview images, video screenshots, and default gallery images will be used as candidates for background images or screenshots |
| Import Inscura media stream information | When enabled, the plugin provides Emby with video, audio, and subtitle stream information recorded by Inscura |

## Image Type Correspondence

| Inscura Image Type | Emby Image Type |
| --- | --- |
| Poster | Primary |
| Landscape Cover, Background Image, Gallery Default Image / Fanart | Backdrop |
| Video Screenshot | Screenshot |
| Landscape Image | Thumb |
| Preview / preview | Thumb (also considered as a Backdrop candidate) |
| Banner / banner | Banner |
| Clear logo / clearlogo | Logo |
| Artwork / clearart, keyart | Art |
| Disc cover / discart | Disc |

## Usage Recommendations

- When using this for the first time, we recommend selecting a small number of movies to refresh their metadata first. Once you’ve confirmed that the titles, cast, images, and collections match your expectations, you can then refresh the entire library in bulk.
- If movie titles in Emby have been manually changed, the plugin will still prioritize matching based on the actual file path and filename, rather than relying solely on the title.
- If the Inscura service address or token has been changed, you must update it in the Emby plugin settings and then refresh the metadata.
- If the Inscura media library is locked or shut down, Emby will be unable to read the metadata.
- If the configuration page still displays old content after upgrading the plugin, first force-refresh the browser page, then reopen the plugin configuration page.

## Troubleshooting

### Emby Cannot Detect the Inscura Plugin

1. Verify that `Emby.Plugin.Inscura.dll` is located in the Emby plugin directory.
2. Verify that the Emby process has permission to read this file.
3. Verify that the current Emby Server version supports `.NET 8` plugins.
4. Restart Emby Server.
5. Reopen the Emby web interface and check the plugin list or configuration page.
6. If the plugin is still not visible, check the Emby Server logs for plugin loading errors.

### Emby Can See the Plugin but Has No Metadata

1. On the device hosting the Emby Server, access the Inscura health check URL.
2. Verify that the Inscura media library is open and that the local API service is running.
3. Verify that the API address in the Emby plugin is not the incorrect `127.0.0.1`.
4. If the API uses token-based authentication, verify that the correct token is entered in the Emby plugin.
5. Verify that `Inscura` is enabled in the metadata downloader for the movie library.
6. Refresh the metadata or re-identify the movie.

### Images or Cast Avatars Not Displaying

1. Verify that the Emby server can access the Inscura API address.
2. If the interface uses token-based authentication, verify that the plugin token is correct.
3. Verify that image import, actor metadata, and avatar import are enabled in the plugin settings.
4. Verify that the corresponding media or actor actually has available image resources in Inscura.
5. Refresh the metadata for the movie or person.

### Banner Images, Disc Covers, Artwork, or Multiple Background Images Not Imported

1. Verify that image import is enabled in the plugin settings.
2. Verify that the option to automatically fill in missing image types in Emby is enabled in the plugin settings.
3. Verify that the corresponding album images in Inscura are set to the correct type.
4. Refresh the metadata for the movie so that the plugin can save the missing images after the refresh is complete.
5. If Emby already contains a single image of the same type, the plugin will not overwrite the existing image by default.

### Trailers Not Imported

1. Verify that "Import YouTube Trailers" is enabled in the plugin settings.
2. Verify that the trailer URL in Inscura is a YouTube URL.
3. Non-YouTube local or online trailers will not be imported directly by this plugin; please export them using the NFO feature in the Inscura app and submit them to Emby for scanning.

## Upgrading the Plugin

1. Stop or prepare to restart the Emby Server.
2. Replace the old file in the Emby plugin directory with the new `Emby.Plugin.Inscura.dll`.
3. Verify that the Emby running user has permission to read the new file.
4. Restart the Emby Server.
5. Reopen the Emby web interface, go to the plugin list or plugin configuration page to check the version and settings.

If the Emby web interface still displays the old settings after the upgrade, first force-refresh the browser page, then reopen the plugin configuration page.
