# GateToHell - The Hellgate London SDK.

This project aims to provide a full SDK for Hellgate London.
Focus lies on parsing and converting every single file format with every single value to modern open standards.
Additionally this repository will come with a bunch of commandline applications to convert various
game specific formats to and from open standard formats.
At last this repository will hold a gui based ide which can be used to work with the assets in a project scope.

The motivation on this project is primarily for educational purposes.
Otherwise the available tools did not satisfy my vision.
Tools were either outdated, their links dead, or simply incomplete.
All tools i found ignored some data because their creator did not fully implement them.
The project core consist of a library, which can be used by other software.
Example usages would be to write a patcher or a custom game client.
Additionally pro users or automated scripts can use the provided command line tools.
And at last typical modders will find a single gui to fire up and view, import, export or edit assets.

## Currently implemented

### LibHellgate
The library project contains the core of everything required to work with the Hellgate London files.
It contains all game file format related classes for easy implementation in various apps.

### ContainerTool
The ContainerTool is a simple command line application which is able to pack or unpack dat/idx containers via drag&drop.

**More to come...**
