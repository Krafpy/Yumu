# Yumu Image Searcher

*Yumu* is a small background software that aims to solve the struggle of having
to find an image among thousands of images in a folder. It allows locating images
in a more timely manner through an interface that is similar to Discord's emote picker.

## Build

The software is developed on [Visual Studio Code](https://code.visualstudio.com/) with
the .NET 4.8 Framework SDK.
Use the `dotnet build` command to build the executable release :

```cmd
dotnet build -c release
```

## Usage

The software configuration is accessible from its tray icon.

### Referenced images and directories

*Yumu* will reference all images from a list of directories that
the user specified, i.e. it will list all files with a `.jpg`, `.jpeg`,
`.png`, `.gif`, `.tiff`, `.bmp` extension and allow access to these files from
*Yumu*'s search window.

![Screenshot](/screenshots/screenshot1.PNG)

If a referenced directory content gets modified (i.e adding, removing or renaming
an image file), it must be reloaded in the directory window for the modifications
to be taken in account.

### Search window

The search window is opened and focused when the configured hotkey is pressed.
The search results are refreshed on each edit in the search bar.

![Screenshot](/screenshots/screenshot2.PNG)

A search result image can be dragged and dropped from the search window
as a file, or copied in the clipboard if selected with a double click or
a press on the `Enter` key.

## Issues

When copied into the clipboard, the images are stored under the .NET
bitmap format. Pictures looses their transparency, and only the first
frame of a GIF is loaded. Converting the image into a DIB (*Device Independant Bitmap*) format that supports transparency (DIBv5 or *Format17*), as used by web browsers,
and copying it to the clipboard would fix the transperency issue.
